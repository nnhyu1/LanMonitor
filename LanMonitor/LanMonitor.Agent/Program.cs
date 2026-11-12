using LanMonitor.Agent;
using LanMonitor.Shared;

// Có truyền IP thì dùng luôn, không thì tự tìm bằng UDP broadcast
string? fixedHost = args.Length > 0 ? args[0] : null;

Func<CancellationToken, Task<string?>> hostProvider = async ct =>
{
    if (fixedHost != null) return fixedHost;
    Console.WriteLine("Đang tìm server bằng UDP broadcast...");
    return await ServerDiscovery.FindServerAsync(ct);
};

using var mon = new SystemMonitor();
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var agent = new AgentClient(hostProvider, Protocol.TcpPort, mon);
await agent.RunAsync(cts.Token);

Console.WriteLine("Agent đã dừng.");