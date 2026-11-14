# Quy tắc làm việc nhóm

Dự án: **LanMonitor** - hệ thống giám sát máy tính trong mạng LAN (môn Network Programming).

## 1. Thành viên và phân công

| Thành viên | Phụ trách chính |
|---|---|
| Nguyễn Nhật Huy | Agent (đo CPU/RAM/DISK, gửi qua TCP, UDP discovery, tự kết nối lại), giao thức dùng chung |
| Bùi Tuấn Phong | Server (nhận nhiều Agent async, timeout), dashboard WinForms, cảnh báo, đồ thị |
| Cả hai | Kiểm thử tích hợp, tài liệu, báo cáo, slide, demo |

## 2. Bảng theo dõi công việc

| # | Công việc | Người làm | Trạng thái |
|---|---|---|---|
| 1 | Chốt giao thức tin nhắn, tạo repo và solution | Huy | Hoàn thành |
| 2 | Agent: đo CPU, RAM, DISK | Huy | Hoàn thành |
| 3 | Agent: kết nối TCP, `HELLO`/`STATS`, tự nối lại | Huy | Hoàn thành |
| 4 | Agent: tự tìm server bằng UDP broadcast | Huy | Hoàn thành |
| 5 | Server: nhận nhiều Agent, timeout, `WELCOME`/`OFFER` | Phong | Hoàn thành |
| 6 | Dashboard: bảng máy, cảnh báo, đồ thị, nhật ký | Phong | Hoàn thành |
| 7 | Agent: xử lý lỗi, kiểm tra tin nhắn | Huy | Đang làm |
| 8 | Kiểm thử tích hợp Agent + Server | Cả hai | Chưa làm |
| 9 | Tài liệu, báo cáo, slide, demo | Cả hai | Chưa làm |

Cập nhật cột "Trạng thái" mỗi tuần.

## 3. Quy tắc phối hợp
1. **Mỗi tính năng một nhánh** (`agent-...`, `server-...`), gộp vào `main` bằng Pull Request.
2. **Người kia xem qua Pull Request** trước khi merge (xem nhanh, chạy thử nếu thay đổi lớn).
3. **Không commit thẳng** những thay đổi lớn vào `main`.
4. **`LanMonitor.Shared` (giao thức) là của chung**: muốn đổi phải báo người còn lại trước.
5. Không commit `bin/`, `obj/`, file `*.user` (đã có trong `.gitignore`).
6. Tên commit ngắn, nói rõ việc đã làm (ví dụ: `Agent: reconnect when server closes`).
7. `main` luôn phải **build được**.

## 4. Giao tiếp
- Trao đổi hằng ngày qua tin nhắn nhóm.
- Họp ngắn **mỗi tuần một lần** để chốt việc tuần tới và cập nhật bảng ở mục 2.
- Gặp vướng mắc quá **1 ngày** thì báo ngay để người kia hỗ trợ.

## 5. Xử lý bất đồng và chậm tiến độ
- Bất đồng về thiết kế: mỗi người nêu lý do, ưu tiên phương án đơn giản hơn và đúng với kiến thức môn học. Không thống nhất được thì hỏi giảng viên.
- Việc chậm quá hạn đã hẹn: người phụ trách báo sớm, hai bên dời hoặc chia lại việc.

## 6. Tiêu chí "hoàn thành" một công việc
- Code chạy được, đã thử với đối tác (Agent với Server).
- Đã merge vào `main` qua Pull Request.
- Đã cập nhật tài liệu liên quan (README hoặc SETUP).
