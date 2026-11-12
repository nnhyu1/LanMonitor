using System.Net;
using System.Net.Sockets;
using System.Text;
using LanMonitor.Shared;

namespace LanMonitor.Agent;

public static class ServerDiscovery
{
    // Trả về IP của server, hoặc null nếu không ai trả lời
    public static async Task<string?> FindServerAsync(CancellationToken ct, int tries = 3)
    {
        using var udp = new UdpClient();
        udp.EnableBroadcast = true;

        byte[] request = Encoding.UTF8.GetBytes(
            Protocol.Build("DISCOVER", ("app", "LanMonitor")));
        var broadcast = new IPEndPoint(IPAddress.Broadcast, Protocol.UdpPort);

        for (int i = 0; i < tries && !ct.IsCancellationRequested; i++)
        {
            await udp.SendAsync(request, request.Length, broadcast);

            // Chờ tối đa 2 giây xem có ai trả lời không
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(2000);
            try
            {
                var result = await udp.ReceiveAsync(timeout.Token);
                var (cmd, _) = Protocol.Parse(Encoding.UTF8.GetString(result.Buffer));
                if (cmd == "OFFER")
                    return result.RemoteEndPoint.Address.ToString();
            }
            catch (OperationCanceledException)
            {
                if (ct.IsCancellationRequested) return null; // người dùng bấm Ctrl+C
                // còn lại là hết 2 giây: thử lần tiếp
            }
        }
        return null;
    }
}