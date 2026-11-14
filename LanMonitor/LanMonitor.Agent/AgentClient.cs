using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using LanMonitor.Shared;

namespace LanMonitor.Agent;

public class AgentClient
{
    private const int MaxLineLength = 4096;     // tin dài hơn mức này bị coi là không hợp lệ
    private const int WelcomeTimeoutSeconds = 5; // chờ WELCOME tối đa bấy nhiêu giây
    private const int RetryDelaySeconds = 5;

    private readonly Func<CancellationToken, Task<string?>> _hostProvider;
    private readonly int _port;
    private readonly SystemMonitor _mon;

    public AgentClient(Func<CancellationToken, Task<string?>> hostProvider, int port, SystemMonitor mon)
    {
        _hostProvider = hostProvider;
        _port = port;
        _mon = mon;
    }

    // Vòng lặp chính: mất kết nối thì chờ rồi tìm lại server và nối lại
    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                string? host = await _hostProvider(ct);
                if (host == null)
                    Console.WriteLine("Không thấy server nào trả lời.");
                else
                    await ConnectAndSendAsync(host, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Mất kết nối: {ex.Message}");
            }

            Console.WriteLine($"Thử lại sau {RetryDelaySeconds} giây...");
            try { await Task.Delay(RetryDelaySeconds * 1000, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task ConnectAndSendAsync(string host, CancellationToken ct)
    {
        using var client = new TcpClient();
        Console.WriteLine($"Đang kết nối tới {host}:{_port}...");
        await client.ConnectAsync(host, _port, ct);
        Console.WriteLine("Đã kết nối.");

        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

        try
        {
            // 1. Giới thiệu bản thân
            await writer.WriteAsync(Protocol.Build("HELLO",
                ("name", Environment.MachineName),
                ("ip", GetLocalIp()),
                ("os", RuntimeInformation.OSDescription)));

            // 2. Chờ WELCOME (có giới hạn thời gian, không chờ mãi)
            int interval = await WaitForWelcomeAsync(reader, ct);
            Console.WriteLine($"Chu kỳ gửi: {interval} giây");

            // 3. Vừa gửi STATS định kỳ, vừa lắng nghe server.
            //    Nhờ vậy khi server đóng kết nối, Agent biết ngay chứ không phải đợi lần ghi lỗi.
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Task receiveTask = ReceiveLoopAsync(reader, linked.Token);
            Task sendTask = SendLoopAsync(writer, interval, linked.Token);

            Task finished = await Task.WhenAny(receiveTask, sendTask);
            linked.Cancel();
            try
            {
                await finished; // ném lại lỗi của tác vụ kết thúc trước (nếu có)
            }
            finally
            {
                try { await Task.WhenAll(receiveTask, sendTask); } catch { }
            }
        }
        finally
        {
            // Người dùng chủ động tắt Agent (Ctrl+C) thì báo BYE cho server
            if (ct.IsCancellationRequested)
            {
                try { await writer.WriteAsync(Protocol.Build("BYE")); } catch { }
            }
        }
    }

    private static async Task<int> WaitForWelcomeAsync(StreamReader reader, CancellationToken ct)
    {
        string? line;
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            timeout.CancelAfter(TimeSpan.FromSeconds(WelcomeTimeoutSeconds));
            try
            {
                line = await reader.ReadLineAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new TimeoutException($"Server không trả lời WELCOME trong {WelcomeTimeoutSeconds} giây.");
            }
        }

        if (line == null)
            throw new IOException("Server đóng kết nối ngay sau HELLO.");

        var (cmd, data) = Protocol.Parse(line);
        if (cmd != "WELCOME")
            throw new InvalidDataException($"Mong đợi WELCOME nhưng nhận '{cmd}'.");

        int interval = 3;
        if (data.TryGetValue("interval", out var s) && int.TryParse(s, out var n))
            interval = Math.Clamp(n, 1, 60); // tránh server báo số vô lý
        return interval;
    }

    private async Task SendLoopAsync(StreamWriter writer, int interval, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            int cpu = _mon.GetCpu(), ram = _mon.GetRam(), disk = _mon.GetDisk();
            await writer.WriteAsync(Protocol.Build("STATS",
                ("cpu", cpu.ToString()),
                ("ram", ram.ToString()),
                ("disk", disk.ToString())));
            Console.WriteLine($"Đã gửi: CPU={cpu}% RAM={ram}% DISK={disk}%");

            await Task.Delay(interval * 1000, ct);
        }
    }

    // Đọc tin server gửi xuống. Trả về (kết thúc) khi server đóng kết nối.
    private static async Task ReceiveLoopAsync(StreamReader reader, CancellationToken ct)
    {
        while (true)
        {
            string? line = await reader.ReadLineAsync(ct);
            if (line == null)
                throw new IOException("Server đã đóng kết nối.");
            HandleServerMessage(line);
        }
    }

    private static void HandleServerMessage(string line)
    {
        if (line.Length > MaxLineLength)
        {
            Console.WriteLine("[!] Bỏ qua tin quá dài.");
            return;
        }

        var (cmd, data) = Protocol.Parse(line);
        switch (cmd)
        {
            case "ERR":
                Console.WriteLine($"[Server báo lỗi] {data.GetValueOrDefault("reason", "không rõ")}");
                break;
            case "WELCOME":
                break; // đã nhận ở bước trước, bỏ qua bản lặp lại
            default:
                Console.WriteLine($"[!] Bỏ qua tin không hợp lệ: '{cmd}'");
                break;
        }
    }

    // Lấy IP LAN của máy. UDP Connect không gửi gói tin nào,
    // chỉ để hệ điều hành chọn card mạng sẽ dùng.
    private static string GetLocalIp()
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect("8.8.8.8", 65530);
            return ((IPEndPoint)s.LocalEndPoint!).Address.ToString();
        }
        catch
        {
            return "unknown";
        }
    }
}