using LanMonitor.Shared;

string msg = Protocol.Build("STATS", ("cpu", "45"), ("ram", "70"));
Console.Write(msg);                       // STATS|cpu=45|ram=70

var (cmd, data) = Protocol.Parse(msg);
Console.WriteLine($"{cmd}: cpu={data["cpu"]}, ram={data["ram"]}");