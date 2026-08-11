# AIsle Unity Desktop App

Đây là Unity project cho hướng chuyển đổi từ web prototype sang desktop application theo `docs/Result_Plan.md`.

Population foundation hiện được nạp qua hai local UPM packages:

- `com.blackbox.aisle.contracts` → `../src/AIsle.Contracts`;
- `com.blackbox.aisle.simulation` → `../src/AIsle.Simulation`.

Unity EditMode tests kiểm tra deterministic golden populations và JSON round-trip. Chưa có UI/UX, scene sản phẩm, NPC runtime, Utility AI, DOTS/ECS, Burst/Jobs hoặc Spine. Web prototype ở thư mục gốc `web/`, `backend/`, `runtime/` và các test `.mjs` vẫn là baseline/reference.

Mở thư mục `UnityApp` bằng Unity Hub với Unity Editor 6000.5.7f1. Chạy EditMode tests trong Test Runner để xác minh project sau khi clone hoặc đổi máy.
