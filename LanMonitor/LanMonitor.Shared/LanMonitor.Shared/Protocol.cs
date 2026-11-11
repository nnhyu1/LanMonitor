namespace LanMonitor.Shared;

public static class Protocol
{
    public const int TcpPort = 5000;
    public const int UdpPort = 9000;

    // Tạo tin: Build("STATS", ("cpu","45"), ("ram","70"))
    public static string Build(string cmd, params (string k, string v)[] fields)
    {
        var parts = new List<string> { cmd };
        foreach (var (k, v) in fields)
            parts.Add($"{k}={Uri.EscapeDataString(v)}");
        return string.Join("|", parts) + "\n";
    }

    // Đọc tin: trả về tên lệnh + từ điển khóa/giá trị
    public static (string cmd, Dictionary<string, string> data) Parse(string line)
    {
        var parts = line.TrimEnd('\n', '\r').Split('|');
        var data = new Dictionary<string, string>();
        foreach (var p in parts.Skip(1))
        {
            int i = p.IndexOf('=');
            if (i > 0)
                data[p[..i]] = Uri.UnescapeDataString(p[(i + 1)..]);
        }
        return (parts[0], data);
    }
}