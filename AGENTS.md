# AIsle repository rules

Các quy tắc dưới đây là bắt buộc đối với mọi người và agent thực hiện thay đổi trong repository này.

1. `docs/Result_Plan.md` là kế hoạch chính thức và là nguồn sự thật về phạm vi, kiến trúc và thứ tự triển khai.
2. Trước khi chỉnh sửa, phải đọc phần phạm vi hiện tại trong `docs/Result_Plan.md` và các quy tắc tại `docs/log.md`.
3. Sau mỗi lượt chỉnh sửa hoặc kiểm tra, phải ghi một bản ghi mới ở cuối `docs/log.md`. Chỉ được ghi nối tiếp; tuyệt đối không sửa, xóa, sắp xếp lại hoặc ghi đè bản ghi cũ.
4. Bản ghi phải có ngày/tháng/năm, giờ/phút, người thực hiện, lý do, nội dung đã làm, trạng thái đạt/chưa đạt, kiểm tra, việc tiếp theo và phạm vi đồng bộ.
5. Phạm vi hiện tại chỉ gồm tái thiết cấu trúc repository và Unity desktop app skeleton. Không triển khai UI/UX hoặc simulation core cho đến khi chủ dự án mở rộng phạm vi.
6. Giữ web prototype, backend, runtime và test JavaScript làm baseline/reference cho đến khi golden tests và contracts được chốt.
7. Mặc định mọi thay đổi chỉ ở local. Không chạy `git add`, `git commit`, `git push`, tạo pull request hoặc thao tác remote nếu chủ dự án chưa yêu cầu rõ ràng trong lượt làm việc hiện tại.
8. Không ghi đè hoặc hoàn tác thay đổi sẵn có của người dùng. Nếu có xung đột phạm vi, phải dừng và báo lại.

