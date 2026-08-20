# AIsle repository rules

Các quy tắc dưới đây là bắt buộc đối với mọi người và agent thực hiện thay đổi trong repository này.

1. `docs/rule.md` là nguồn sự thật về kiến trúc, phạm vi và luật kỹ thuật; `docs/task.md` là nguồn sự thật về task và thứ tự triển khai.
2. Trước khi chỉnh sửa phải đọc đầy đủ hai file trên, task hiện tại, `docs/log.md` và trạng thái Git.
3. Chỉ thực hiện CURRENT TASK trong `docs/task.md`, trừ khi chủ dự án đưa ra yêu cầu trực tiếp khác trong lượt hiện tại. WIP luôn bằng 1; hoàn thành task thì dừng, không tự mở task kế tiếp.
4. `docs/log.md` là log active và chỉ được append. Các file `docs/log_backend.md`, `docs/log_frontend.md`, `docs/log_sim.md`, `docs/log_mobile.md` là lịch sử chuyên môn, không tiếp tục chỉnh sửa.
5. Không tự thêm dependency, đổi framework, mở feature frozen/future hoặc tạo lại module Reality/Video đã bị removed.
6. Giữ `web/`, `backend/` và JavaScript tests làm REFERENCE/LEGACY cho đến khi Desktop local UI + bridge thay thế và regression pass.
7. `src/AIsle.DesktopApp`, `src/AIsle.Simulation` và `src/AIsle.Contracts` là ACTIVE. `UnityApp` và `mobile` là FROZEN.
8. Mặc định mọi thay đổi chỉ ở local. Không chạy `git add`, `git commit`, `git push`, tạo pull request hoặc thao tác remote nếu chủ dự án chưa yêu cầu rõ.
9. Không ghi đè hoặc hoàn tác thay đổi sẵn có của người dùng. Nếu có xung đột phạm vi, phải dừng và báo lại.
