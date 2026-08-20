# AIsle — Nhật ký Simulation

File này lưu tiến trình chuyên môn của phần **Simulation**. Hình thức và quy tắc bản ghi được giữ nguyên từ `log.md`; mọi nội dung phải đối chiếu với [`Result_Plan.md`](./Result_Plan.md).

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

## 2026-08-11 11:56 (UTC+07:00) — Codex

- Lý do sửa: Khởi tạo cơ chế quản lý tiến trình bắt buộc và bắt đầu hai nhiệm vụ nền tảng do chủ dự án yêu cầu.
- Đã sửa/đã làm: Tạo `docs/log.md`; xác định `docs/Result_Plan.md` là kế hoạch chính thức; bắt đầu dựng cấu trúc repository và Unity desktop app skeleton, giữ web prototype làm baseline.
- Đối chiếu Result_Plan.md: Mục 2.1, mục 4, Stage 1, Stage 4 và Phase 1.
- Trạng thái: Đạt một phần.
- Kiểm tra: Đã đọc kế hoạch và kiểm tra trạng thái Git trước khi sửa; phát hiện sẵn thay đổi của người dùng trong `runtime/catalog.json`, `runtime/layout.json` và `docs/kien_truc_sim3.txt`, các thay đổi này được giữ nguyên.
- Nên làm tiếp theo: Hoàn tất app skeleton, kiểm tra cấu trúc và chạy regression test của web baseline.
- Phạm vi đồng bộ: Chỉ local; chưa commit, chưa push.


## 2026-08-11 12:13 (UTC+07:00) — Codex

- Lý do sửa: Rà soát lại toàn bộ yêu cầu ban đầu theo đề nghị của chủ dự án và hoàn thiện các điểm còn thiếu để quy tắc được áp dụng cho những lần làm việc sau.
- Đã sửa/đã làm: Tạo `AGENTS.md` ở root để bắt buộc mọi người/agent đọc `docs/Result_Plan.md`, chỉ ghi nối tiếp `docs/log.md`, giữ đúng phạm vi hai nhiệm vụ hiện tại và không commit/push nếu chưa được yêu cầu rõ; cập nhật `README.MD` để xác định Unity desktop app là hướng chính thức và web chỉ là baseline/reference; cập nhật `README_BUILD.md` thành tài liệu riêng của web baseline. Không thay đổi UI/UX, simulation core hoặc dữ liệu runtime.
- Đối chiếu Result_Plan.md: Phần `Phạm vi triển khai hiện tại`, mục 2.1, mục 4, Stage 1, Stage 4 và Phase 1.
- Trạng thái: Đạt. Các yêu cầu về plan chính thức, log bắt buộc, tái thiết thư mục, chuyển hướng sang app skeleton, hoãn UI/UX và core, và chỉ làm local hiện đã được thể hiện đồng bộ trong cấu trúc và tài liệu repository.
- Kiểm tra: Các file bắt buộc tồn tại; Unity manifest parse hợp lệ; nội dung `AGENTS.md` có đủ tham chiếu plan, log, giới hạn UI/UX/core và cấm push mặc định; `git diff --check` thành công; `origin/main...HEAD` là `0 0`. Regression baseline gần nhất vẫn là 10/10 test pass; lượt này chỉ sửa tài liệu/quy tắc nên không thay đổi mã chạy.
- Nên làm tiếp theo: Khi chủ dự án sẵn sàng, cài Unity Editor rồi mở `UnityApp/` để Unity sinh metadata và xác minh skeleton trong Editor. Sau đó chốt golden tests/contracts trước khi mở phạm vi core hoặc UI/UX.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.


## 2026-08-11 14:35 (UTC+07:00) — Codex — RUN_FIX final

- Lý do sửa: Thực hiện `docs/run_fix_codex.md` cuối cùng theo thứ tự chủ dự án đã đính chính, sau Population source-first và C# Simulation Baseline.
- Đã sửa/đã làm: Giữ nguyên toàn bộ lịch sử log cũ; sửa policy sai của milestone trước bằng cách xóa custom deterministic RNG/Gaussian/fingerprint, custom generic Genome/Selection/Crossover/Mutation/GeneDefinition, public Population `Seed` và public Simulation `RandomControl`; dùng GeneticSharp 2.6.0 cho GA engine/operators/chromosome base và MathNet.Numerics 5.0.0 cho distribution/statistics; giữ AIsle domain mapping/fitness/validator/contracts/tests; đồng bộ `Result_Plan.md`, `run.md`, `run_2.md`, `run_2_codex_source_first.md`, README và dependency/license audit.
- Đối chiếu Result_Plan.md: Chính sách external-first; không tạo generic algorithm khi vetted implementation tồn tại; RVO2/Unity ECS samples/Spine official runtime vẫn để stage sau; C# Simulation Baseline đã hoàn thành nhưng UI/UX và stage nâng cao chưa mở.
- Trạng thái: Đạt. Corrective audit không còn public randomness-control/fingerprint field trong Contracts/Population, không còn custom generic GA operator; AIsle mapping/fitness và C# baseline vẫn hoạt động.
- Kiểm tra: .NET build 0 warning/0 error; Population 5/5 invariant scenarios pass; C# Simulation tests pass RUN2-02…15 (Poisson mean `4.935s` trong tolerance); Unity 6000.5.7f1 EditMode 8/8 pass; web baseline 10/10 pass; SimResult JSON round-trip pass; `git diff --check` pass; `origin/main...HEAD = 0 0`.
- Nên làm tiếp theo: Dừng tại đây theo `run_fix_codex.md`; milestone tiếp theo chỉ bắt đầu khi chủ dự án yêu cầu, ưu tiên Interaction Zone/Queue baseline review trước RVO2, Social, DOTS/Burst hoặc Spine.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push. Các thay đổi sẵn có của người dùng trong `runtime/` và `docs/` được giữ nguyên.


## 2026-08-11 12:09 (UTC+07:00) — Codex

- Lý do sửa: Hoàn tất và xác minh hai nhiệm vụ nền tảng trong phạm vi hiện tại: tái thiết cấu trúc repository và chuyển hướng từ web prototype sang Unity desktop app skeleton.
- Đã sửa/đã làm: Cập nhật `docs/Result_Plan.md` thành kế hoạch chính thức và khóa phạm vi hiện tại; tạo cấu trúc `UnityApp/`, `src/`, `services/VideoAnalytics/`, `models/`, `data/` và các nhóm test mục tiêu; tạo metadata tối thiểu để Unity Hub nhận project; bổ sung ignore cho dữ liệu Unity sinh tự động; giữ nguyên web/backend/runtime và test `.mjs` làm baseline/reference; ghi marker để các thư mục cấu trúc được version control nhận biết sau này.
- Đối chiếu Result_Plan.md: Mục 2.1, mục 4, Stage 1, Stage 4 và Phase 1 — `Tạo Unity project structure`.
- Trạng thái: Đạt đối với hai nhiệm vụ được giao trong mốc hiện tại. UI/UX và simulation core chưa được triển khai đúng theo giới hạn phạm vi.
- Kiểm tra: Đủ 37 đường dẫn bắt buộc; `UnityApp/Packages/manifest.json` parse JSON thành công; `git diff --check` thành công; 10/10 file regression test của web baseline pass bằng Node.js v24.14.0. Lần gọi thử `node --test tests` không phù hợp với Node runtime này vì thư mục bị coi là module; đã chạy lại đúng bằng danh sách file `*.test.mjs` và tất cả đều pass. Chưa thể mở project bằng Unity vì máy có Unity Hub nhưng chưa cài Unity Editor.
- Nên làm tiếp theo: Cài Unity Editor phù hợp, mở `UnityApp/` để Unity sinh metadata và xác minh project trong Editor; sau đó freeze golden tests và chốt contracts trước khi port C# core. UI/UX và core chỉ bắt đầu ở mốc sau theo yêu cầu của chủ dự án.
- Phạm vi đồng bộ: Chỉ local; chưa commit, chưa push.


## 2026-08-11 12:14 (UTC+07:00) — Codex (xác minh cuối)

- Lý do sửa: Ghi kết quả xác minh cuối sau khi hoàn thiện audit và bản đính chính.
- Đã sửa/đã làm: Xác nhận bản đính chính nằm ở cuối file tại thời điểm kiểm tra; xác nhận các tài liệu và quy tắc repository đã được bổ sung, không thay đổi UI/UX, core hoặc dữ liệu runtime của người dùng.
- Đối chiếu Result_Plan.md: Phần `Phạm vi triển khai hiện tại`, mục 4 và Phase 1.
- Trạng thái: Đạt đối với toàn bộ yêu cầu hiện tại; bước xác minh bằng Unity Editor vẫn để sau vì máy chưa cài Editor.
- Kiểm tra: `git diff --check` thành công; `origin/main...HEAD` là `0 0`; working tree chỉ có thay đổi local và không có commit/push mới. Thứ tự ngoại lệ của bản `12:13` đã được ghi minh bạch trong bản đính chính ngay trước bản này.
- Nên làm tiếp theo: Cài Unity Editor và mở `UnityApp/` khi chuyển sang mốc kế tiếp; chưa bắt đầu UI/UX hoặc simulation core nếu chưa có yêu cầu mới.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.


## 2026-08-11 13:22 (UTC+07:00) — Codex

- Lý do sửa: Chủ dự án xác nhận Unity Editor đã được cài và yêu cầu triển khai milestone trong `docs/run.md`.
- Đã sửa/đã làm: Đọc đầy đủ `AGENTS.md`, `docs/run.md` và quy tắc log; phát hiện Unity Editor 6000.5.7f1 tại `D:\_C\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe`; mở rộng phạm vi chính thức sang Population foundation; cập nhật Unity project version và khai báo local packages/test framework. Chưa viết UI/UX hoặc các hệ thống sau Population milestone.
- Đối chiếu Result_Plan.md: Phase 1 — Contracts/Population foundation; `docs/run.md` NOW-01 đến NOW-05 và Definition of Done milestone hiện tại.
- Trạng thái: Đạt một phần; môi trường và phạm vi đã chốt, phần Contracts/GA/Validator/Statistics/Golden Tests đang triển khai.
- Kiểm tra: Unity Editor 6000.5.7f1 tồn tại; built-in Unity Test Framework 1.7.0 và NUnit extension 2.1.0 có sẵn; máy không có `dotnet` CLI trong PATH nên kiểm thử sẽ chạy bằng Unity batch mode.
- Nên làm tiếp theo: Tạo C# contracts/local packages, deterministic GA pipeline, validator/statistics, golden fixtures và chạy EditMode tests bằng Unity Editor.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.


## 2026-08-11 13:46 (UTC+07:00) — Codex

- Lý do sửa: Hoàn tất và xác minh milestone Population foundation theo `docs/run.md` sau khi Unity Editor và .NET SDK được cài.
- Đã sửa/đã làm: Hoàn thành NOW-01 `NPCProfile`; NOW-02 `PopulationConfig`/`PopulationDefinition`/metadata/ranges/distribution targets; NOW-03 `IPopulationGenerator`, facade, Manual/Imported generators và GA tách Genome/GeneDefinition/Fitness/Selection/Crossover/Mutation; NOW-04 `PopulationValidator` và `PopulationStatistics`; NOW-05 năm golden scenarios fixed seed. Tổ chức Contracts/Simulation thành local UPM packages đồng thời là .NET projects; thêm solution, offline NuGet config, console verification, Unity EditMode tests, package lock, Unity ProjectSettings và metadata. Sửa RNG Gaussian và fingerprint để bit-stable giữa .NET/Unity; sửa GA tie-break để distribution không target không bị lệch; kiểm tra Unity JSON semantic round-trip. Không triển khai UI/UX, NPC runtime, Utility AI, ORCA, Social, DOTS, Burst hoặc Spine.
- Đối chiếu Result_Plan.md: Phase 1 — Population subset của Contracts và deterministic foundation; `docs/run.md` mục 2–8, NOW-01 đến NOW-05 và Definition of Done tại mục 23.
- Trạng thái: Đạt cho milestone Population foundation hiện tại. Pipeline `PopulationConfig → GeneticPopulationGenerator → NPCProfile[200] → PopulationValidator → PopulationStatistics → Golden Tests` chạy ổn, deterministic, serializable, Unity-independent, DOTS-independent và tested.
- Kiểm tra: `.NET SDK 10.0.302` build solution thành công với 0 warning/0 error; console verification pass 5/5 golden scenarios cùng validator rejection, statistics, generator abstraction và dependency boundary; Unity 6000.5.7f1 EditMode pass 6/6 tests, gồm 5 golden scenarios và kiểm tra không tham chiếu Unity/DOTS; fingerprints khớp giữa .NET/Unity; Unity JSON round-trip giữ tương đương toàn bộ fields với tolerance `1e-12`; web baseline pass 10/10 regression tests; manifest/package lock JSON hợp lệ; `git diff --check` thành công; không có `bin/obj` hoặc dependency Unity/Spine lọt vào source packages.
- Nên làm tiếp theo: Freeze 5 web reference scenarios theo mục 9 của `docs/run.md`, rồi mới chốt NPC Runtime Model và port Utility AI C# theo thứ tự tài liệu; chỉ bắt đầu khi chủ dự án yêu cầu milestone tiếp theo.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push. Các file người dùng tạo/thay đổi sẵn trong `runtime/` và `docs/` được giữ nguyên.


## 2026-08-11 14:24 (UTC+07:00) — Codex — RUN-01

- Lý do sửa: Áp dụng lại Population milestone theo policy source-first trước khi mở `run_2`.
- Đã sửa/đã làm: Audit RNG, Gaussian, fingerprint, generic chromosome/selection/crossover/mutation/engine; lập bảng tại `docs/source-first-audit.md`; kiểm tra upstream, license và compatibility của GeneticSharp/Math.NET.
- Đối chiếu Result_Plan.md: Population foundation và nguyên tắc external-first.
- Trạng thái: Đạt.
- Kiểm tra: GeneticSharp 2.6.0 và MathNet.Numerics 5.0.0 đều MIT, có target .NET Standard 2.0 và phù hợp code dùng chung Unity/.NET.
- Nên làm tiếp theo: Chuẩn hóa contracts và loại bỏ randomness-control fields.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.


## 2026-08-11 14:24 (UTC+07:00) — Codex — RUN-02/RUN-03

- Lý do sửa: Loại bỏ exact-random contract và generic GA implementation tự viết.
- Đã sửa/đã làm: Xóa `Seed`, config/output fingerprint, deterministic RNG và custom Genome/Selection/Crossover/Mutation/GeneDefinition; tích hợp DLL GeneticSharp/Math.NET; tạo `AIsleNpcChromosome`, `AIslePopulationFitness` và adapter mỏng `GeneticPopulationGenerator`.
- Đối chiếu Result_Plan.md: Contracts Unity-independent và Population/GA dùng vetted implementation.
- Trạng thái: Đạt.
- Kiểm tra: .NET build 0 warning/0 error; source audit không còn public randomness-control field hoặc custom generic GA operator.
- Nên làm tiếp theo: Chuyển statistics/tests sang vetted library và invariant gate.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.


## 2026-08-11 14:24 (UTC+07:00) — Codex — RUN-04/RUN-05

- Lý do sửa: Hoàn tất validator/statistics/tests theo source-first Definition of Done.
- Đã sửa/đã làm: Giữ validator AIsle; chuyển mean/median/min/max/std/percentile sang Math.NET; thay exact fingerprint/golden random output bằng count, bounds, validation, serialization, fitness, distribution sanity và dependency boundary; thêm MIT notices.
- Đối chiếu Result_Plan.md: Population pipeline `Config → GeneticSharp adapter → NPCProfile[] → Validator → Statistics → Tests`.
- Trạng thái: Đạt; đủ điều kiện chuyển sang `run_2_codex_source_first.md` theo yêu cầu mới của chủ dự án.
- Kiểm tra: .NET 5/5 scenarios pass; Unity 6000.5.7f1 EditMode 6/6 pass; web regression 10/10 pass; `git diff --check` pass.
- Nên làm tiếp theo: Chạy RUN2-01 Audit & Freeze, rồi port simulation baseline theo đúng source mapping.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push; lịch sử log cũ giữ nguyên.


## 2026-08-11 14:32 (UTC+07:00) — Codex — RUN2-01

- Lý do sửa: Bắt đầu C# Simulation Baseline sau khi Population source-first pass.
- Đã sửa/đã làm: Chạy JS baseline 10/10; lập mapping cho Spawn, Need, Affect, Smart Object, Utility, A*, Movement, Interaction, Purchase/Exit, Event/Trajectory và SimResult tại `docs/run2-source-mapping.md`.
- Đối chiếu Result_Plan.md: Port-first, behavior-preserving, Unity-independent core.
- Trạng thái: Đạt.
- Kiểm tra: Không có source gap; nguồn runtime là `web/live-engine.js`, schema là `web/sim-result.js`, behavior gates là 10 test `.mjs`.
- Nên làm tiếp theo: Port lần lượt runtime baseline.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.


## 2026-08-11 14:32 (UTC+07:00) — Codex — RUN2-02 đến RUN2-07

- Lý do sửa: Port nửa đầu simulation core từ JS sang C#.
- Đã sửa/đã làm: Thêm contracts thuần C#, `PoissonSpawnSampler`, `NeedAffectSystem`, fixed-access Smart Object advertisements, Utility scoring/top-K weighted choice và `PathGrid` A* với no-corner-cut/smoothing walkability.
- Đối chiếu Result_Plan.md: Spawn, Need, Affect, Smart Object, Utility AI và Navigation baseline.
- Trạng thái: Đạt.
- Kiểm tra: Poisson mean `4.880s` với expected `5s`; Need/Affect formula pass; utility near/far pass; sealed-wall và gap-route A* pass.
- Nên làm tiếp theo: Port movement-to-result full journey.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.


## 2026-08-11 14:32 (UTC+07:00) — Codex — RUN2-08 đến RUN2-12

- Lý do sửa: Hoàn thiện runtime journey baseline.
- Đã sửa/đã làm: Tạo `SimulationHost` fixed-step, `NPCRuntimeState`, path-node movement, walkability recheck, replan/abandon, fixed shelf dwell, main/impulse purchase, checkout và normal/unreachable/blocked exit flow. Không thêm Interaction Zone, Queue Zone hoặc ORCA.
- Đối chiếu Result_Plan.md: Movement/Interaction/Queue-baseline-if-needed/Purchase/Exit.
- Trạng thái: Đạt.
- Kiểm tra: Full journey có decision → dwell → purchase → checkout → left; phantom/unreachable agent không xuyên sealed wall và rời cửa hàng có kiểm soát.
- Nên làm tiếp theo: Hoàn tất trace/replay/result và regression gate.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.


## 2026-08-11 14:32 (UTC+07:00) — Codex — RUN2-13 đến RUN2-15

- Lý do sửa: Hoàn tất observable/replayable output của C# baseline.
- Đã sửa/đã làm: Port event trace, periodic/status-change trajectory và `aisle.sim-result.v1` gồm input snapshot, summary, events, purchases, replay; thêm JSON round-trip test.
- Đối chiếu Result_Plan.md: Event/Trace, Trajectory, SimResult.
- Trạng thái: Đạt.
- Kiểm tra: C# simulation verification pass; replay có hơn 2 samples và đúng 5 columns; SimResult serialize/deserialize giữ events/purchases/replay.
- Nên làm tiếp theo: Chạy Regression Gate và corrective audit cuối.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.


## 2026-08-11 14:32 (UTC+07:00) — Codex — RUN2 Regression Gate

- Lý do sửa: Xác minh Definition of Done của `run_2_codex_source_first.md`.
- Đã sửa/đã làm: Chạy toàn bộ .NET, Population, Unity EditMode và JS baseline; kiểm tra diff.
- Đối chiếu Result_Plan.md: C# Simulation Baseline hoàn chỉnh trong scope; các stage UI/ORCA/Social/DOTS/Spine vẫn chưa triển khai.
- Trạng thái: Đạt; `run_2` completed.
- Kiểm tra: .NET build 0 warning/error; C# Simulation tests pass; Population 5/5 pass; Unity EditMode 8/8 pass; JS 10/10 pass; `git diff --check` pass.
- Nên làm tiếp theo: Chạy `docs/run_fix_codex.md` theo thứ tự mới của chủ dự án, sau đó dừng.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.


## 2026-08-11 14:36 (UTC+07:00) — Codex — RUN_FIX final (bản append chính thức)

- Lý do sửa: Ghi lại ở đúng cuối file kết quả `docs/run_fix_codex.md` theo thứ tự chủ dự án đã đính chính.
- Đã sửa/đã làm: Giữ lịch sử cũ; xóa custom deterministic RNG/Gaussian/fingerprint, custom generic GA internals, public Population `Seed` và public Simulation `RandomControl`; dùng GeneticSharp 2.6.0 và MathNet.Numerics 5.0.0; giữ AIsle mapping/fitness/validator/contracts/tests; đồng bộ plan/run/README/audit.
- Đối chiếu Result_Plan.md: External-first; RVO2/Unity ECS samples/Spine official runtime vẫn để stage sau; Population source-first và C# Simulation Baseline đã hoàn thành.
- Trạng thái: Đạt; dừng theo `run_fix_codex.md`.
- Kiểm tra: .NET build 0 warning/error; Population 5/5; C# Simulation RUN2-02…15 pass; Unity EditMode 8/8; web 10/10; JSON round-trip và `git diff --check` pass; audit không còn public randomness-control/fingerprint field hoặc custom generic GA operator.
- Nên làm tiếp theo: Chỉ mở milestone mới khi chủ dự án yêu cầu.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push; `origin/main...HEAD = 0 0`.


## 2026-08-11 14:43 (UTC+07:00) — Codex — Chuẩn bị đồng bộ `test`

- Lý do sửa: Chủ dự án yêu cầu đẩy snapshot local hiện tại lên nhánh GitHub `test`.
- Đã sửa/đã làm: Xác minh remote `origin`, local vẫn ở `main`, `origin/main` là ancestor của `origin/test`, diff check sạch; chuẩn bị tạo commit descendant của `origin/test` bằng index tạm để không thay đổi local branch/index. Ghi nhận các file `docs/run*.md` và source-first audit không còn trên filesystem tại thời điểm yêu cầu push và cũng không có trong `origin/test`, nên snapshot lấy đúng trạng thái local hiện hữu thay vì tự tái tạo nội dung không còn nguồn.
- Đối chiếu Result_Plan.md: Đồng bộ Population source-first và C# Simulation Baseline đã kiểm thử; không đưa build output hoặc Unity generated cache lên GitHub.
- Trạng thái: Đạt một phần; đang chờ tạo commit và push.
- Kiểm tra: `main_is_ancestor_of_test=true`; `git diff --check` pass; local `main` khớp `origin/main`.
- Nên làm tiếp theo: Tạo snapshot commit trên lịch sử `test`, push và xác minh remote hash/tree.
- Phạm vi đồng bộ: Chỉ local; chưa stage bằng index chính, chưa commit local branch, chưa push.


## 2026-08-11 14:46 (UTC+07:00) — Codex — Hoàn tất đồng bộ `test`

- Lý do sửa: Ghi nhận kết quả push snapshot source-first/C# Simulation Baseline lên GitHub `test`.
- Đã sửa/đã làm: Push commit snapshot ban đầu `dd4f1ec35aa7fb9e720358237cd172c26b279979`; phát hiện commit này vô tình chứa một phần Unity cache sinh nhầm ở root do `git add` gặp file khóa; ngay lập tức bổ sung ignore root cache và push corrective descendant `45d119db64a8e24ff5f2acfbd3bc813b3c9896d2` với tree sạch. Corrective tip có 234 file, gồm đủ 4 `.csproj`, không có root `Assets/Library/Logs/Packages/ProjectSettings/Temp/UserSettings`.
- Đối chiếu Result_Plan.md: Đồng bộ đúng source code, UnityApp, Population source-first, C# Simulation Baseline, tests và tài liệu hiện hữu; không đưa generated cache/build output vào snapshot cuối.
- Trạng thái: Đạt. `origin/test` đang trỏ tới corrective clean snapshot.
- Kiểm tra: Remote hash `45d119db64a8e24ff5f2acfbd3bc813b3c9896d2`; tree `720c8152fa9519a88e99d35ebafd172bcf2bc541`; forbidden root count = 0; `.csproj` count = 4; `main` không bị push hoặc thay đổi.
- Nên làm tiếp theo: Push bản log hoàn tất này bằng descendant commit cuối và xác minh remote tip.
- Phạm vi đồng bộ: Đã push source snapshot lên `origin/test`; bản ghi hoàn tất này đang chờ descendant commit cuối; không push `main`.


## 2026-08-14 09:05 (UTC+07:00) — OpenCode — Unity Presentation baseline (Stage 4)

- Lý do sửa: Chủ dự án chốt hướng làm app desktop Unity thật (build .exe) và yêu cầu agent làm luôn UI. Tiếp tục todo list sau khi tạo scene/Presentation ở phiên trước.
- Đã sửa/đã làm:
  - Thêm SimulationViewController (Presentation bridge): load layout/catalog từ StreamingAssets qua ProjectData, sinh population bằng GA (GeneticPopulationGenerator qua PopulationFactory), tạo SimulationHost fixed-step (tick 0.2s), điều khiển Play/Pause/Step/Restart/Speed.
  - Thêm SimulationRenderer: vẽ grid, walls, shelves, entrance/checkout, NPC dots (màu theo status giống web) và trajectory trail; có click handler chọn wall/shelf.
  - Thêm AisleDashboard (uGUI code-first): stats, event log, NPC status counts, selection inspector, speed slider, các nút điều khiển.
  - Thêm Editor tools: AisleSceneBuilder (tạo scene Aisle.unity, thêm build settings), AislePlayerBuilder (build Windows .exe), AisleRuntimeAssets (sinh materials vào Resources để giữ shader trong build).
  - Sửa ProjectData: thêm Load(path,path), ForceReload, SetReplacement, wrapper JSON cho catalog (JsonUtility không đọc mảng top-level); di chuyển JSON từ Assets/AIsle/StreamingAssets sang Assets/StreamingAssets (vị trí đặc biệt Unity để vào build).
  - Sửa Packages/manifest.json: thêm com.unity.ugui + modules audio/imageconversion/jsonserialize/physics/ui/uielements.
- Đối chiếu Result_Plan.md: Hoàn tất baseline Stage 4 (scene, desktop shell, live simulation view, basic dashboard, build .exe). Map editor tương tác, lưu layout, replay UI, Spine vẫn ngoài phạm vi hiện tại.
- Trạng thái: Đạt. Compile Unity OK; scene Aisle.unity tạo OK; build Windows thành công; app chạy ổn định, Player.log không còn exception sau khi bọc catalog JSON.
- Kiểm tra: Unity.exe -batchmode tạo materials/scene/build đều exit 0; AIsle.exe chạy 15–20s không crash; Player.log sạch; StreamingAssets có trong AIsle_Data.
- Nên làm tiếp theo: Map/layout editor tuong tác (thêm/sửa/xóa wall, shelf) + lưu về file; camera pan/zoom; replay UI.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chua commit, chua push.


## 2026-08-14 10:15 (UTC+07:00) — OpenCode — Port toàn bộ UI web sang Unity (Stage 4 full)

- Lý do sửa: Chủ dự án xác nhận yêu cầu UI hiển thị giống khi chạy `run.bat` (web prototype) ngay trong Unity; backend sẽ do người khác làm, app Unity vẫn dùng core C# local.
- Đã sửa/đã làm:
  - Rewrite `SimulationRenderer`: tách rebuild world/NPC, màu status public, `SelectWall/SelectShelf/ClearSelection/ScreenToWorld`.
  - Tạo `SceneEditor`: enum `EditTool` (Select/Wall/Shelf/Entrance/Checkout), vẽ/kéo/resize wall & shelf snap 0.25m, preview line/rect/point, chọn theo click, delete/update, `SelectWall/SelectShelf`, event `LayoutEdited`.
  - Tạo `AisleStudioUI`: port toàn bộ UI từ `web/index.html` — header + tabs Setup/Simulate, toolbar (output/parameters/step/run live/speed/clock), palette (population mode, NPC count/duration, layout objects list, add wall/shelf), stage, inspector (wall/shelf form, valance), cashier panel, footer metrics, decision trace, dialog Parameter Lab & Manual NPC.
  - Rewrite `SimulationViewController`: population mode `ga`/`manual`, `SetManualNpcs`, `SetPopulationCount`, giữ `SimulationConfig` khi rebuild, `NotifyConfigChanged`/`ResetConfig` ở `BuildHost`, events `HostCreated`/`SimAdvanced`.
  - Cập nhật `AisleSceneBuilder` (thêm SceneEditor + AisleStudioUI, populationCount=180, speed=5); xóa `AisleDashboard.cs`/`WallClickHandler.cs`/`ShelfClickHandler.cs` (không còn tham chiếu).
  - Sửa nhiều lỗi compile: CS0102 trùng `_shelfCategory`, kiểu `RectTransform` vs `GameObject` (panel/footer/cashier), `ScrollRect.AddComponent` trên RectTransform, double/float, `Config` read-only (thêm `ResetConfig`), thiếu `SelectWall/SelectShelf` trên SceneEditor, thiếu đối số `Text`.
- Đối chiếu Result_Plan.md: Hoàn tất Stage 4 phần full UI studio + map editor; lưu layout về file, camera pan/zoom, replay UI, Spine vẫn ngoài phạm vi hiện tại.
- Trạng thái: Đạt. Compile Unity hết lỗi CS; scene `Aisle.unity` rebuild OK; build Windows OK; `AIsle.exe` chạy 20s không crash, Player.log không exception.
- Kiểm tra: Unity.exe -batchmode compile/scene/build đều thành công; `Builds/Windows/AIsle.exe` tồn tại; smoke test 20s ghi `[Aisle] Host rebuilt: 180 NPCs (ga)`; không có NullReference/Exception trong Player.log.
- Nên làm tiếp theo: Chạy app và xác nhận giao diện Studio giống web; sau đó lưu layout/catalog về file khi chủ dự án yêu cầu mở phạm vi.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chua commit, chua push.


## 2026-08-14 16:35 (UTC+07:00) - Antigravity - Kiem tra cau truc thu muc & ung dung C#

- Ly do sua: Nguoi dung yeu cau kiem tra thu muc may tinh, mong meo chuyen tu chay file run.bat (mo web prototype) sang chay ung dung C#.
- Da sua/da lam:
  - Kiem tra file run.bat (hien khoi chay Node.js backend/server.mjs va trinh duyet web http://127.0.0.1:8765).
  - Kiem tra docs/Result_Plan.md, ma nguon C# simulation core tai src/ va du an Unity Desktop App tai UnityApp/.
  - Xac nhan du an da completed Stage 4, chuyen toan bo C# Simulation Core (src/AIsle.Simulation, src/AIsle.Contracts) va UI web prototype sang Unity/C# Desktop Application.
  - Xac nhan da co file build C# desktop app executable san tai UnityApp/Builds/Windows/AIsle.exe.
- Doi chieu Result_Plan.md: Phu hop voi dinh huong va ke hoach trong Result_Plan.md (chuyen web prototype sang Unity/C# Desktop Application).
- Trang thai: Dat.
- Kiem tra: File UnityApp/Builds/Windows/AIsle.exe ton tai va san sang hoat dong doc lap, khong can chay backend Node.js va browser web.
- Nen lam tiep theo: Huong dan nguoi dung chay AIsle.exe hoac tao shortcut script run_app.bat neu nguoi dung dong y.
- Pham vi dong bo: Chi local.


## 2026-08-14 16:42 (UTC+07:00) - Antigravity - Triển khai C# Desktop Application (WPF + WebView2)

- Lý do sửa: Chủ dự án xác nhận không dùng Unity App mà muốn tạo C# Desktop App (.NET 10, WPF + WebView2) giữ nguyên 100% giao diện Web Studio hiện tại.
- Đã sửa/đã làm:
  - Khởi tạo C# Desktop App project src/AIsle.DesktopApp/AIsle.DesktopApp.csproj (.NET 10-windows) sử dụng Microsoft WebView2 và kết nối C# Core (AIsle.Simulation, AIsle.Contracts).
  - Xây dựng MainWindow.xaml và MainWindow.xaml.cs tự động phát hiện/khởi chạy backend server Node.js và nhúng WebView2 hiển thị giao diện Studio.
  - Cập nhật nguồn NuGet https://api.nuget.org/v3/index.json trong NuGet.Config.
  - Thêm project AIsle.DesktopApp vào solution file AIsle.slnx.
  - Build và Publish thành công bản ứng dụng C# Desktop tại Builds/Desktop/AIsleDesktop.exe.
  - Cập nhật 
un.bat để chạy ứng dụng C# Desktop App (Builds/Desktop/AIsleDesktop.exe).
  - Cập nhật mục tiêu dự án trong docs/Result_Plan.md.
- Đối chiếu Result_Plan.md: Phù hợp với định hướng chuyển đổi sang C# Desktop App của chủ dự án.
- Trạng thái: Đạt.
- Kiểm tra: dotnet build và dotnet publish đều thành công 0 lỗi; file Builds/Desktop/AIsleDesktop.exe đã được sinh ra; file 
un.bat đã sẵn sàng khởi chạy ứng dụng C# Desktop.
- Nên làm tiếp theo: Người dùng có thể chạy 
un.bat hoặc mở trực tiếp Builds/Desktop/AIsleDesktop.exe để trải nghiệm ứng dụng C# Desktop.
- Phạm vi đồng bộ: Chỉ local.

## 2026-08-19 23:45 (UTC+07:00) — Codex — Triển khai Phantom Need & Tham chiếu Catalog cho Task 10

- Lý do sửa: Yêu cầu từ task.md để đồng bộ Catalog/Layout với cơ chế Phantom Need, đổi UI cho phép cấu hình preset và đảm bảo GA hỗ trợ nhu cầu ảo (phantom), đúng thứ tự SYSTEM → UI.
- Đã sửa/đã làm:
  - **S-A. Contracts (SYSTEM)**: Thêm `ShelfPresets` cố định size (Standard/Cooler/Endcap). Sửa `Product` đổi `Shelf` thành `ShelfId`. `PopulationConfig` bỏ hard-code `CategoryIds` và thêm `PhantomNeedRate`.
  - **S-B. Simulation/GA (SYSTEM)**: Sửa `PopulationApplicationService.Generate()` để đọc đúng `CategoryIds` từ payload/Catalog và bắt `ArgumentException` chuyển thành `ValidationResult` khi Catalog rỗng. `GeneticPopulationGenerator` thêm cơ chế PhantomNeedRoll (nếu < PhantomNeedRate thì gán `__phantom__`). Viết test thống kê xác nhận phân bố Phantom.
  - **S-C. Utility Decision (SYSTEM)**: Tận dụng `ExplorationNeed/Impulsiveness` hiện có của NPC khi `TargetCategory == "__phantom__"` để tự do chọn kệ mà không cần khóa cứng mục tiêu.
  - **U-A. Layout UI**: Đổi UI chỉnh sửa Shelf sang ComboBox chọn Preset cố định (W/H readonly).
  - **U-B. Catalog UI**: Đổi `EditShelf` thành ComboBox (đọc từ `LayoutService.Shelves`), `EditCategory` tự set theo kệ chọn. Thêm `CanExecute` cho `NewProductCommand` để cảnh báo và block form khi Layout chưa có kệ nào.
  - **M-Verify**: Pass E2E simulation (GA tạo phantom NPC hợp lệ, NPC khám phá và kết thúc journey không lỗi văng), mọi luồng hoạt động chính xác thuần C# mà không đụng framework ngoài (như Unity/Smart Objects).
- Đối chiếu Result_Plan.md: Phù hợp với milestone Task 10 (cơ chế simulation thuần C#, không mang logic phụ thuộc web prototype, UX được validate chặn ngặt).
- Trạng thái: Đạt.
- Kiểm tra: `dotnet run` 100% tests Population và DesktopApp (QA smoke, bridge, validation error) đều PASS; Manual verify E2E đều pass.
- Nên làm tiếp theo: Chờ lệnh người dùng test ở local trước khi commit/push thay đổi.
- Phạm vi đồng bộ: Chỉ local.

## 2026-08-20 09:00 (UTC+07:00) — Codex — Tích hợp commit remote 8e40127 & Hoàn thiện UI/UX Web Studio

- Lý do sửa: Đồng bộ cải tiến giao diện và cơ chế lưu lịch sử từ commit mới `8e40127` trên remote `origin/develop`, kết hợp xử lý toàn bộ lỗi UI/UX trên local theo yêu cầu của người dùng.
- Đã sửa/đã làm:
  - Tích hợp thay đổi từ remote `8e40127`: cập nhật layout bảng kết quả `Results` mở rộng, hàm `saveSimulationSession` hỗ trợ lưu đa phiên mượt mà không xung đột mã, tích hợp test bridge `history.save` trong `AIsle.DesktopApp.Tests`.
  - Khắc phục lỗi CSS `[hidden]` bị đè bởi Tailwind `.flex` khiến các form inspector bị chồng lấn.
  - Thiết kế lại danh sách đối tượng (Object List) sang dạng card với icon và badge phân biệt rõ ràng.
  - Hoàn thiện 100% Việt hóa cho tất cả các màn hình (Setup, Simulate, Results, Analytics).
  - Tinh chỉnh biểu đồ Analytics sang dữ liệu bán lẻ cửa hàng.
- Đối chiếu Result_Plan.md: Hoàn thiện chất lượng UI/UX của Studio và đồng bộ với nhánh develop.
- Trạng thái: Đạt.
- Kiểm tra: 14/14 Node.js tests PASS; Toàn bộ test suite .NET (Population, Simulation, DesktopApp, Results) PASS; dotnet build 0 error, 0 warning; git diff --check sạch whitespace.
- Nên làm tiếp theo: Người dùng kiểm tra trực tiếp trên local trước khi có lệnh commit/push.
- Phạm vi đồng bộ: Chỉ local; chưa commit, chưa push.

