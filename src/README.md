# Active .NET source

Ranh giới module lấy `docs/rule.md` làm nguồn chính thức:

- `AIsle.Contracts`: DTO, schema, enum và contract dùng chung.
- `AIsle.Simulation`: Population/GA và C# Simulation Core; không phụ thuộc UI.
- `AIsle.DesktopApp`: WPF Desktop host; Application/Bridge/Infrastructure/UI được tổ chức bên trong project này khi các task tương ứng được mở.

Không tạo thêm project layer chỉ để mô phỏng kiến trúc enterprise. Chỉ tách project khi có lợi ích reuse, dependency hoặc test isolation đo được.
