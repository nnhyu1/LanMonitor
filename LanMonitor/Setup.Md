# Hướng dẫn cài đặt và sử dụng

## 1. Yêu cầu
- Windows 10/11 (Agent dùng bộ đếm hiệu năng của Windows).
- .NET SDK đúng phiên bản trong file `.csproj` của project (hiện là **.NET 10**, `net10.0-windows`).
  Kiểm tra bằng `dotnet --version`.
- Git.

## 2. Tải và build
```powershell
git clone https://github.com/nnhyu1/LAN-Machine-Monitoring.git
cd LAN-Machine-Monitoring/LanMonitor
dotnet build
```
Build thành công khi không có dòng `error`.

## 3. Chạy hệ thống

**Bước 1. Chạy Server** (máy của người quản lý):
```powershell
dotnet run --project LanMonitor.Server
```
Cửa sổ dashboard hiện lên và server tự bắt đầu nghe **TCP 5000** và **UDP 9000**.

**Bước 2. Chạy Agent** (máy cần giám sát):
```powershell
dotnet run --project LanMonitor.Agent                    # tự tìm server bằng UDP broadcast
dotnet run --project LanMonitor.Agent -- 192.168.1.20    # chỉ định IP server
```
Máy xuất hiện trên dashboard, các thanh CPU/RAM/DISK cập nhật theo chu kỳ 3 giây.

## 4. Cấu hình mạng
| Cổng | Giao thức | Mục đích |
|---|---|---|
| 5000 | TCP | Agent gửi `HELLO`, `STATS`, `BYE` |
| 9000 | UDP | Agent tìm server (`DISCOVER` / `OFFER`) |

- Khi Windows hỏi về Firewall, chọn **Allow access** (hoặc mở thủ công hai cổng trên máy chạy server).
- Một số mạng Wi-Fi chặn broadcast giữa các máy. Khi đó chạy Agent với IP của server (bước 3).

## 5. Kịch bản demo ngắn
1. Bật Server, chạy 1-2 Agent: dashboard hiện máy và số liệu thay đổi.
2. Mở nhiều tab trình duyệt hoặc video nặng trên máy Agent: CPU/RAM tăng, vượt ngưỡng thì dashboard cảnh báo.
3. Bấm **Ctrl+C** ở Agent: máy chuyển **Offline** ngay.
4. Tắt cửa sổ Agent đột ngột: sau khoảng 10 giây máy chuyển **Offline** (timeout).
5. Tắt rồi bật lại Server: Agent tự tìm và nối lại, không cần chạy lại.

## 6. Xử lý sự cố
| Hiện tượng | Cách xử lý |
|---|---|
| `address already in use` | Đã có chương trình khác (hoặc server thứ hai) dùng cổng 5000/9000. Tắt nó đi |
| Agent báo "Không thấy server nào trả lời" | Kiểm tra server đã chạy, cùng mạng, firewall; hoặc truyền IP server trực tiếp |
| Agent báo "Server không trả lời WELCOME" | Server chạy phiên bản cũ hoặc không phải LanMonitor |
| `dotnet build` lỗi phiên bản | Cài đúng .NET SDK như mục 1 |
