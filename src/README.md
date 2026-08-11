# .NET-compatible source modules

Ranh giới module C# theo `docs/Result_Plan.md` và milestone trong `docs/run.md`.

Đã triển khai:

- `AIsle.Contracts`: local UPM/.NET project chứa `NPCProfile`, `PopulationConfig`, `PopulationDefinition` và `IPopulationGenerator`, không tham chiếu Unity/DOTS.
- `AIsle.Simulation`: local UPM/.NET project chứa GeneticSharp-backed Population, Math.NET statistics, validator và C# Simulation Baseline; không có exact random fingerprint contract.

Các module còn lại mới là ranh giới dành cho milestone sau.

- `AIsle.Domain`
- `AIsle.Application`
- `AIsle.Simulation`
- `AIsle.Analytics`
- `AIsle.Contracts`
- `AIsle.Infrastructure`
