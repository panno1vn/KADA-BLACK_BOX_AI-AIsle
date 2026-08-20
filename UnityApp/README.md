# AIsle Unity Legacy/Frozen Reference

Unity không còn là runtime sản phẩm. Thư mục này được giữ FROZEN để bảo toàn code/test cũ theo `docs/rule.md`; không mở rộng trong MVP.

Population foundation hiện được nạp qua hai local UPM packages:

- `com.blackbox.aisle.contracts` → `../src/AIsle.Contracts`;
- `com.blackbox.aisle.simulation` → `../src/AIsle.Simulation`.

Unity EditMode tests kiểm tra deterministic golden populations và JSON round-trip. Web prototype ở `web/`, backend legacy ở `backend/`, dữ liệu local ở `runtime/` và các test `.mjs` vẫn là baseline/reference.

Chỉ mở project này khi có task cleanup/verification riêng được chủ dự án phê duyệt.
