using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using LanMonitor.Shared;

namespace LanMonitor.Agent;

public class AgentClient
{
       private readonly Func<CancellationToken, Task<string?>> _hostProvider;
    private readonly int _port;
    private readonly SystemMonitor _mon;

    public AgentClient(Func<CancellationToken, Task<string?>> hostProvider, int port, SystemMonitor mon)
    {
        _hostProvider = hostProvider;
        _port = port;
        _mon = mon;
    }
    // Vòng lặp chính: mất kết nối thì chờ 5 giây rồi thử lại
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
                Console.WriteLine($"[!] Lỗi kết nối: {ex.Message}");
            }

            Console.WriteLine("Thử lại sau 5 giây...");
            try { await Task.Delay(5000, ct); }
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

        // 1. Giới thiệu bản thân
        await writer.WriteAsync(Protocol.Build("HELLO",
            ("name", Environment.MachineName),
            ("ip", GetLocalIp()),
            ("os", RuntimeInformation.OSDescription)));

        // 2. Đợi WELCOME để biết chu kỳ gửi (mặc định 3 giây)
        int interval = 3;
        string? line = await reader.ReadLineAsync(ct);
        if (line != null)
        {
            var (cmd, data) = Protocol.Parse(line);
            if (cmd == "WELCOME" &&
                data.TryGetValue("interval", out var s) &&
                int.TryParse(s, out var n) && n > 0)
            {
                interval = n;
            }
        }
        Console.WriteLine($"Chu kỳ gửi: {interval} giây");

        // 3. Gửi STATS định kỳ
        try
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
        finally
        {
            // Tắt agent đàng hoàng thì báo BYE (lỗi thì bỏ qua)
            try { await writer.WriteAsync(Protocol.Build("BYE")); } catch { }
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