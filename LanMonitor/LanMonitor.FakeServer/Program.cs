using System.Net;
using System.Net.Sockets;
using System.Text;
using LanMonitor.Shared;

var listener = new TcpListener(IPAddress.Any, Protocol.TcpPort);
listener.Start();
Console.WriteLine($"Fake server đang nghe cổng {Protocol.TcpPort}... (Ctrl+C để dừng)");

while (true)
{
    var client = await listener.AcceptTcpClientAsync();
    _ = HandleAsync(client); // mỗi agent một tác vụ riêng, không chặn nhau
}

static async Task HandleAsync(TcpClient client)
{
    var endpoint = client.Client.RemoteEndPoint;
    Console.WriteLine($"[+] Kết nối từ {endpoint}");
    try
    {
        using (client)
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                Console.WriteLine($"  {line}");
                var (cmd, _) = Protocol.Parse(line);

                if (cmd == "HELLO")
                    await writer.WriteAsync(Protocol.Build("WELCOME", ("interval", "3")));
                else if (cmd == "BYE")
                    break;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[!] {endpoint}: {ex.Message}");
    }
    Console.WriteLine($"[-] Ngắt kết nối {endpoint}");
}