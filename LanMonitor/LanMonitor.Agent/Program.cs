using LanMonitor.Agent;

using var mon = new SystemMonitor();

while (true)
{
    await Task.Delay(3000);
    Console.WriteLine($"CPU={mon.GetCpu()}%  RAM={mon.GetRam()}%  DISK={mon.GetDisk()}%");
}