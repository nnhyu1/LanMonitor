using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using LanMonitor.Shared;

namespace LanMonitor.Server;

public class MonitorServer
{
    private TcpListener? _tcpListener;
    private UdpClient? _udpListener;
    private CancellationTokenSource? _cts;

    // Danh sách các máy đã kết nối (Key = Tên máy + IP)
    public ConcurrentDictionary<string, AgentInfo> Agents { get; } = new();
    private readonly ConcurrentDictionary<string, TcpClient> _activeClients = new();

    // Chu kỳ gửi báo cáo (giây) gửi cho Agent trong bản tin WELCOME
    public int ReportIntervalSeconds { get; set; } = 3;

    public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

    // Sự kiện báo cho Form cập nhật giao diện
    public event Action<AgentInfo>? AgentUpdated;
    public event Action<string>? LogMessage;

    public void Start()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        _ = Task.Run(() => RunUdpDiscoveryAsync(ct), ct);
        _ = Task.Run(() => RunTcpServerAsync(ct), ct);

        Log($"Server đã khởi động (TCP: {Protocol.TcpPort}, UDP: {Protocol.UdpPort}).");
    }

    public void Stop()
    {
        if (_cts == null) return;

        _cts.Cancel();
        try { _tcpListener?.Stop(); } catch { }
        try { _udpListener?.Close(); } catch { }
        _cts = null;

        foreach (var kv in _activeClients)
        {
            try { kv.Value.Close(); } catch { }
        }
        _activeClients.Clear();

        foreach (var agent in Agents.Values)
        {
            agent.Online = false;
            AgentUpdated?.Invoke(agent);
        }

        Log("Server đã dừng.");
    }

    // Lắng nghe UDP broadcast từ Agent và trả lời OFFER
    private async Task RunUdpDiscoveryAsync(CancellationToken ct)
    {
        try
        {
            _udpListener = new UdpClient(Protocol.UdpPort);
            Log($"Đang nghe UDP broadcast trên cổng {Protocol.UdpPort}...");

            while (!ct.IsCancellationRequested)
            {
                var result = await _udpListener.ReceiveAsync(ct);
                string message = Encoding.UTF8.GetString(result.Buffer);
                var (cmd, _) = Protocol.Parse(message);

                if (cmd == "DISCOVER")
                {
                    byte[] offer = Encoding.UTF8.GetBytes(
                        Protocol.Build("OFFER", ("tcpPort", Protocol.TcpPort.ToString())));
                    await _udpListener.SendAsync(offer, offer.Length, result.RemoteEndPoint);
                    Log($"[UDP] Đã trả lời DISCOVER từ {result.RemoteEndPoint.Address}");
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (SocketException ex) when (ct.IsCancellationRequested)
        {
            _ = ex;
        }
        catch (Exception ex)
        {
            Log($"[Lỗi UDP] {ex.Message}");
        }
    }

    // Lắng nghe kết nối TCP từ các Agent
    private async Task RunTcpServerAsync(CancellationToken ct)
    {
        try
        {
            _tcpListener = new TcpListener(IPAddress.Any, Protocol.TcpPort);
            _tcpListener.Start();
            Log($"Đang nghe kết nối TCP trên cổng {Protocol.TcpPort}...");

            while (!ct.IsCancellationRequested)
            {
                var client = await _tcpListener.AcceptTcpClientAsync(ct);
                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (SocketException ex) when (ct.IsCancellationRequested)
        {
            _ = ex;
        }
        catch (Exception ex)
        {
            Log($"[Lỗi TCP] {ex.Message}");
        }
    }

    // Xử lý từng Agent kết nối tới
    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        var remoteEp = client.Client.RemoteEndPoint as IPEndPoint;
        string remoteIp = remoteEp?.Address.ToString() ?? "unknown";
        AgentInfo? agent = null;

        try
        {
            using (client)
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

                string? line;
                while (!ct.IsCancellationRequested && (line = await reader.ReadLineAsync(ct)) != null)
                {
                    var (cmd, data) = Protocol.Parse(line);

                    if (cmd == "HELLO")
                    {
                        string name = data.GetValueOrDefault("name", "Unknown");
                        string ip = data.GetValueOrDefault("ip", remoteIp);
                        if (ip == "unknown" || string.IsNullOrWhiteSpace(ip))
                            ip = remoteIp;
                        string os = data.GetValueOrDefault("os", "Unknown OS");

                        string key = $"{name}@{ip}";
                        agent = Agents.GetOrAdd(key, _ => new AgentInfo { Key = key, ConnectedAt = DateTime.Now });
                        agent.Name = name;
                        agent.Ip = ip;
                        agent.Os = os;
                        agent.Online = true;
                        agent.LastSeen = DateTime.Now;
                        _activeClients[key] = client;

                        int interval = ReportIntervalSeconds;
                        await writer.WriteAsync(Protocol.Build("WELCOME", ("interval", interval.ToString())));

                        Log($"[+] Máy '{name}' ({ip}) đã kết nối - HĐH: {os}");
                        AgentUpdated?.Invoke(agent);
                    }
                    else if (cmd == "STATS")
                    {
                        // Nếu Agent gửi STATS mà chưa qua HELLO thì tạo tạm theo IP
                        if (agent == null)
                        {
                            string key = $"Unknown@{remoteIp}";
                            agent = Agents.GetOrAdd(key, _ => new AgentInfo
                            {
                                Key = key,
                                Name = "Unknown",
                                Ip = remoteIp
                            });
                            _activeClients[key] = client;
                        }

                        int cpu = agent.Cpu, ram = agent.Ram, disk = agent.Disk;
                        if (data.TryGetValue("cpu", out var sCpu) && int.TryParse(sCpu, out int c))
                            cpu = c;
                        if (data.TryGetValue("ram", out var sRam) && int.TryParse(sRam, out int r))
                            ram = r;
                        if (data.TryGetValue("disk", out var sDisk) && int.TryParse(sDisk, out int d))
                            disk = d;

                        agent.RecordSample(cpu, ram, disk);
                        agent.Online = true;
                        agent.LastSeen = DateTime.Now;
                        AgentUpdated?.Invoke(agent);
                    }
                    else if (cmd == "BYE")
                    {
                        Log($"[-] Máy '{agent?.Name ?? remoteIp}' đã chủ động ngắt kết nối (BYE).");
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Log($"[!] Mất kết nối với '{agent?.Name ?? remoteIp}': {ex.Message}");
        }
        finally
        {
            if (agent != null)
            {
                if (_activeClients.TryGetValue(agent.Key, out var current) && ReferenceEquals(current, client))
                    _activeClients.TryRemove(agent.Key, out _);

                agent.Online = false;
                agent.WasAlerting = false;
                Log($"[-] Máy '{agent.Name}' ({agent.Ip}) chuyển sang trạng thái Offline.");
                AgentUpdated?.Invoke(agent);
            }
        }
    }

    // Kick (ngắt kết nối cưỡng bức) 1 máy trạm theo Key
    public bool KickAgent(string key)
    {
        if (!Agents.TryGetValue(key, out var agent))
            return false;

        if (_activeClients.TryRemove(key, out var client))
        {
            try
            {
                if (client.Connected)
                {
                    byte[] msg = Encoding.UTF8.GetBytes(Protocol.Build("KICK", ("reason", "Admin")));
                    client.GetStream().Write(msg, 0, msg.Length);
                }
            }
            catch { }

            try { client.Close(); } catch { }
        }

        agent.Online = false;
        agent.WasAlerting = false;
        Log($"[Kick] Đã ngắt kết nối máy '{agent.Name}' ({agent.Ip}) theo yêu cầu quản trị viên.");
        AgentUpdated?.Invoke(agent);
        return true;
    }

    // Đánh dấu Offline các máy quá thời gian không gửi dữ liệu (rút dây mạng đột ngột)
    public void CheckTimeouts(int timeoutSeconds)
    {
        var now = DateTime.Now;
        foreach (var agent in Agents.Values)
        {
            if (agent.Online && (now - agent.LastSeen).TotalSeconds > timeoutSeconds)
            {
                agent.Online = false;
                agent.WasAlerting = false;
                Log($"[Timeout] Máy '{agent.Name}' ({agent.Ip}) không phản hồi quá {timeoutSeconds}s -> Offline.");
                AgentUpdated?.Invoke(agent);
            }
        }
    }

    private void Log(string message) =>
        LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
}
