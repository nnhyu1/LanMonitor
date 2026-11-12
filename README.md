# LanMonitor

Hệ thống giám sát máy tính trong mạng LAN (môn Network Programming).

Mỗi máy chạy một **Agent** gửi thông số CPU, RAM, ổ đĩa về **Server**. Server hiển thị trên dashboard theo thời gian thực, đánh dấu máy offline và cảnh báo khi vượt ngưỡng.

## Công nghệ
- C# / .NET, WinForms
- TCP (truyền dữ liệu), UDP broadcast (Agent tự tìm Server)
- Giao thức văn bản tự thiết kế: `LỆNH|khóa=giá_trị`

## Cấu trúc
- `LanMonitor.Shared`: giao thức dùng chung
- `LanMonitor.Agent`: chạy trên máy được giám sát
- `LanMonitor.Server`: server + dashboard

## Chạy thử
```powershell
cd LanMonitor
dotnet run --project LanMonitor.Server
dotnet run --project LanMonitor.Agent
```

## Thành viên
- Nguyễn Nhật Huy: Agent, giao thức
- Bùi Tuấn Phong: Server, dashboard
