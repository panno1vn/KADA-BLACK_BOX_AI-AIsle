# Test layout

Các test `.mjs` ở thư mục này là baseline của web prototype và tiếp tục được giữ nguyên.

Population foundation:

- `AIsle.Population.Tests`: console verification thuần .NET, không dùng test package ngoài.
- `Golden/Population`: 5 fixed-seed scenarios, mỗi scenario có `config.json` và `expected.json`.
- Unity EditMode tests: `UnityApp/Assets/AIsle/Tests/EditMode`.

Chạy từ root:

```powershell
dotnet build AIsle.slnx -c Release
dotnet run --project tests/AIsle.Population.Tests/AIsle.Population.Tests.csproj -c Release --no-build
```

Các thư mục Domain, Simulation, Integration, Video và Performance tiếp tục dành cho các milestone sau.
