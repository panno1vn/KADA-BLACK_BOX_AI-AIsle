# AIsle — Nhật ký Mobile

File này lưu tiến trình chuyên môn của phần **Mobile**. Hình thức và quy tắc bản ghi được giữ nguyên từ `log.md`; mọi nội dung phải đối chiếu với [`Result_Plan.md`](./Result_Plan.md).

## Quy tắc bắt buộc

1. Chỉ được ghi nối tiếp ở cuối file; không sửa, xóa, sắp xếp lại hoặc ghi đè các bản ghi đã tồn tại.
2. Mỗi lần thực hiện công việc phải tạo một bản ghi riêng.
3. Mỗi bản ghi bắt buộc có đầy đủ:
   - ngày, tháng, năm;
   - giờ và phút theo múi giờ địa phương;
   - người thực hiện;
   - lý do sửa;
   - những gì đã sửa hoặc đã làm;
   - trạng thái đã đạt hay chưa;
   - việc nên làm tiếp theo.
4. Nội dung phải nêu rõ file/thư mục/dữ liệu bị tác động và đối chiếu với phạm vi trong `Result_Plan.md`.
5. Không được ghi “đã đạt” khi chưa có kiểm tra phù hợp.
6. Nếu công việc thất bại, bị chặn hoặc chỉ hoàn thành một phần, phải ghi rõ nguyên nhân và phần còn thiếu.
7. Hoạt động local, commit và push phải được phân biệt rõ. Không được ghi hoặc suy diễn rằng thay đổi đã lên GitHub nếu chưa thực sự push.

## Mẫu bản ghi bắt buộc

```markdown
## YYYY-MM-DD HH:mm (UTC±HH:MM) — <Người thực hiện>

- Lý do sửa: ...
- Đã sửa/đã làm: ...
- Đối chiếu Result_Plan.md: ...
- Trạng thái: Đạt / Chưa đạt / Đạt một phần.
- Kiểm tra: ...
- Nên làm tiếp theo: ...
- Phạm vi đồng bộ: Chỉ local / Đã commit local / Đã push <remote/branch>.
```

---

## 2026-08-13 20:56 (UTC+07:00) — Antigravity

- Lý do sửa: Di chuyển và đồng bộ hóa thư mục ứng dụng Mobile cùng Mock API lên nhánh test trên Github theo cấu trúc mới.
- Đã sửa/đã làm: Sao lưu mã nguồn mobile app dở dang, reset local test về giống origin/test; di chuyển ứng dụng mobile ra thư mục gốc thành mobile/; tạo tài liệu mobile/README.md; tích hợp thuật toán sinh số ngẫu nhiên Mock API vào backend/routes/api-router.mjs; cập nhật cấu hình layout.json trong runtime/layout.json; khôi phục Streamlit python app trong app/; dọn dẹp các thư mục tạm backup_temp/ và build/ cũ.
- Đối chiếu Result_Plan.md: Đồng bộ cấu trúc phẳng, Mock API backend, cấu hình layout và ứng dụng Mobile.
- Trạng thái: Đạt.
- Kiểm tra: Commit 0a16a37 thành công, push thành công lên origin/test, git status sạch, không còn thư mục build/ hay backup_temp/ trong workspace.
- Nên làm tiếp theo: Tiếp tục phát triển và hoàn thiện các màn hình của ứng dụng Mobile.
- Phạm vi đồng bộ: Đã commit và push lên origin/test.

