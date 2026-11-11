using LanMonitor.Agent;
using LanMonitor.Shared;

// Dùng: dotnet run --project LanMonitor.Agent -- 192.168.1.20
// Không truyền gì thì mặc định nối tới chính máy này
string host = args.Length > 0 ? args[0] : "127.0.0.1";

using var mon = new SystemMonitor();
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var agent = new AgentClient(host, Protocol.TcpPort, mon);
await agent.RunAsync(cts.Token);

Console.WriteLine("Agent đã dừng.");