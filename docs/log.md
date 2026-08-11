# AIsle — Nhật ký tiến trình

File này dùng để lưu tiến trình thực hiện dự án, phục vụ quản lý và kiểm soát dữ liệu được chỉnh sửa hoặc cập nhật. Mọi nội dung trong log phải dựa trên kế hoạch chính thức tại [`Result_Plan.md`](./Result_Plan.md).

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

## 2026-08-11 12:14 (UTC+07:00) — Codex

- Lý do sửa: Đính chính minh bạch thứ tự ghi log của bản audit `12:13`.
- Đã sửa/đã làm: Ghi thêm bản đính chính này ở đúng cuối file. Bản `12:13` đã bị công cụ chèn nhầm giữa bản `11:56` và `12:09` vì dùng câu neo trùng nhau; không có bản ghi cũ nào bị xóa, sửa nội dung hoặc ghi đè. Giữ nguyên các bản trước để bảo toàn dấu vết audit.
- Đối chiếu Result_Plan.md: Phần `Phạm vi triển khai hiện tại` và yêu cầu quản lý tiến trình bằng `docs/log.md`.
- Trạng thái: Đạt sau đính chính; nội dung công việc không thay đổi, nhưng thứ tự hiển thị của bản `12:13` được ghi nhận là ngoại lệ thao tác.
- Kiểm tra: Xác nhận các heading thời gian hiện có là `11:56`, `12:13`, `12:09`, `12:14`; bản này nằm ở cuối file. Từ các lượt sau phải append bằng neo duy nhất lấy từ cuối file hoặc cơ chế append trực tiếp, không dùng câu neo có thể lặp.
- Nên làm tiếp theo: Tiếp tục chỉ ghi bản mới ở cuối file; cài Unity Editor và xác minh `UnityApp/` khi chủ dự án bắt đầu bước kế tiếp.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.

## 2026-08-11 12:14 (UTC+07:00) — Codex (xác minh cuối)

- Lý do sửa: Ghi kết quả xác minh cuối sau khi hoàn thiện audit và bản đính chính.
- Đã sửa/đã làm: Xác nhận bản đính chính nằm ở cuối file tại thời điểm kiểm tra; xác nhận các tài liệu và quy tắc repository đã được bổ sung, không thay đổi UI/UX, core hoặc dữ liệu runtime của người dùng.
- Đối chiếu Result_Plan.md: Phần `Phạm vi triển khai hiện tại`, mục 4 và Phase 1.
- Trạng thái: Đạt đối với toàn bộ yêu cầu hiện tại; bước xác minh bằng Unity Editor vẫn để sau vì máy chưa cài Editor.
- Kiểm tra: `git diff --check` thành công; `origin/main...HEAD` là `0 0`; working tree chỉ có thay đổi local và không có commit/push mới. Thứ tự ngoại lệ của bản `12:13` đã được ghi minh bạch trong bản đính chính ngay trước bản này.
- Nên làm tiếp theo: Cài Unity Editor và mở `UnityApp/` khi chuyển sang mốc kế tiếp; chưa bắt đầu UI/UX hoặc simulation core nếu chưa có yêu cầu mới.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.

## 2026-08-11 12:18 (UTC+07:00) — Codex

- Lý do sửa: Chủ dự án yêu cầu thay toàn bộ nội dung nhánh GitHub `test` bằng snapshot mới từ thư mục local hiện tại.
- Đã sửa/đã làm: Xác minh remote `origin` trỏ đúng `https://github.com/panno1vn/KADA-BLACK_BOX_AI-AIsle.git`; xác minh đang ở local `main`, nhánh local/remote `test` tồn tại; chuẩn bị tạo orphan snapshot từ toàn bộ file không bị ignore mà không chuyển nhánh, không thay đổi index/working tree hiện tại.
- Đối chiếu Result_Plan.md: Phần `Phạm vi triển khai hiện tại`; snapshot gồm tái thiết cấu trúc repository, Unity app skeleton, plan chính thức và web baseline.
- Trạng thái: Chưa đạt; đang chờ tạo commit snapshot và force-push thay thế nhánh `test`.
- Kiểm tra: `origin` đúng repository được yêu cầu; `main` đang khớp `origin/main`; không có tracked file nào bị `.gitignore` loại trừ; Git identity là `PANdeVInCent <yeuemnhieu3377@gmail.com>`.
- Nên làm tiếp theo: Tạo orphan commit bằng index tạm, force-push lên `origin/test`, xác minh hash và cây file remote, sau đó ghi bản hoàn tất.
- Phạm vi đồng bộ: Local đã cập nhật log; chưa stage bằng index chính, chưa commit lên nhánh local, chưa push.

## 2026-08-11 12:19 (UTC+07:00) — Codex

- Lý do sửa: Ghi nhận kết quả thay toàn bộ nhánh GitHub `test` bằng snapshot local theo yêu cầu của chủ dự án.
- Đã sửa/đã làm: Tạo orphan tree từ toàn bộ file không bị ignore trong `D:\dev\kada\KADA-BLACK_BOX_AI-AIsle` bằng index tạm; force-push commit `bdcaa7f3cc9f84d3f83a7b70ed2d8e4afa52d307` lên `origin/test`, thay thế commit remote cũ `242c8a937121be0420d16c1895de6fa0e00a1105`; snapshot có 87 file. Không chuyển nhánh local, không thay đổi index chính và không đẩy lên `main`.
- Đối chiếu Result_Plan.md: Snapshot trên `test` chứa plan chính thức, cấu trúc repository mới, Unity desktop app skeleton và web baseline/reference đúng phạm vi hiện tại.
- Trạng thái: Đạt. Nội dung/lịch sử cũ của nhánh remote `test` đã được thay bằng orphan snapshot mới.
- Kiểm tra: `git ls-remote origin refs/heads/test` trả đúng `bdcaa7f3cc9f84d3f83a7b70ed2d8e4afa52d307`; tree là `b2a47d80e4671a5519b46af1988a716829b15884`; push báo `forced update` thành công.
- Nên làm tiếp theo: Đưa bản ghi hoàn tất này lên `test` bằng commit kế tiếp, xác minh hash cuối và tiếp tục giữ `main` không thay đổi.
- Phạm vi đồng bộ: Đã push snapshot lên `origin/test`; chưa push bản ghi hoàn tất này; không push `main`.

## 2026-08-11 12:20 (UTC+07:00) — Codex

- Lý do sửa: Ghi nhận việc bản hoàn tất `12:19` đã được đồng bộ thành công lên nhánh remote `test`.
- Đã sửa/đã làm: Tạo descendant commit `949b0cc20181d62f03f8075afe8e40ecaf1f4fd7` với parent là orphan snapshot `bdcaa7f3cc9f84d3f83a7b70ed2d8e4afa52d307`, rồi push fast-forward lên `origin/test`. Commit này bổ sung bản ghi kết quả mà không thay đổi nhánh `main` hoặc index local chính.
- Đối chiếu Result_Plan.md: Không thay đổi phạm vi sản phẩm; chỉ đồng bộ nhật ký quản lý của snapshot app foundation đã được phê duyệt.
- Trạng thái: Đạt. Yêu cầu thay nội dung nhánh `test` bằng thư mục local đã hoàn tất và bản ghi kết quả đã có trên remote.
- Kiểm tra: `git ls-remote origin refs/heads/test` trả đúng `949b0cc20181d62f03f8075afe8e40ecaf1f4fd7` sau push; parent và remote hash đều được kiểm tra trước/sau thao tác.
- Nên làm tiếp theo: Đẩy chính bản ghi đồng bộ cuối này bằng một descendant commit và dừng; không thay đổi `main`.
- Phạm vi đồng bộ: Kết quả chính đã ở `origin/test`; bản ghi này sẽ nằm trong descendant commit đồng bộ cuối; không push `main`.

## 2026-08-11 12:22 (UTC+07:00) — Codex

- Lý do sửa: Chủ dự án báo `main` và `test` bị conflict sau khi nhánh `test` được thay bằng orphan snapshot.
- Đã sửa/đã làm: Xác minh `origin/main` là `225985ec48393f18a4f55a14dc99c557c469b38c`, `origin/test` là `0122a19d9554beef2f13a3dceb03cb24b5d219f5`; `git merge-base` trả exit code 1, chứng minh hai nhánh không có tổ tiên chung. Đã fetch lại hai ref và chuẩn bị tạo merge commit giữ nguyên tree của `test`, với parent thứ nhất là `test` và parent thứ hai là `main`.
- Đối chiếu Result_Plan.md: Không thay đổi nội dung hoặc phạm vi app; chỉ sửa quan hệ lịch sử Git để nhánh `test` có thể so sánh/merge với `main`.
- Trạng thái: Chưa đạt; nguyên nhân đã xác định, đang chờ tạo và push merge commit kết nối lịch sử.
- Kiểm tra: Remote refs sau fetch không thay đổi; working tree/index chính vẫn ở local `main` và chưa bị stage.
- Nên làm tiếp theo: Tạo merge commit bằng index tạm, push lên `origin/test`, kiểm tra `origin/main` là ancestor của `origin/test` và xác minh không còn merge conflict.
- Phạm vi đồng bộ: Chỉ cập nhật log local; chưa push bản sửa conflict; không push `main`.

## 2026-08-11 12:23 (UTC+07:00) — Codex

- Lý do sửa: Ghi nhận kết quả khắc phục conflict giữa `main` và `test` do nhánh `test` trước đó dùng orphan history.
- Đã sửa/đã làm: Tạo và push merge commit `132f7af9a27282d59eb8f1d6a58eae5c5af6f8fa` lên `origin/test`; parent thứ nhất là `test` cũ `0122a19d9554beef2f13a3dceb03cb24b5d219f5`, parent thứ hai là `main` `225985ec48393f18a4f55a14dc99c557c469b38c`. Tree vẫn là snapshot app trên `test`, không lấy lại nội dung cũ của `main` và không thay đổi nhánh `main`.
- Đối chiếu Result_Plan.md: Không đổi sản phẩm hoặc phạm vi triển khai; chỉ sửa ancestry Git để quy trình branch/PR hoạt động bình thường.
- Trạng thái: Đạt. `main` hiện là ancestor của `test`, nên Git có merge base hợp lệ và việc merge `test` vào `main` không còn lỗi unrelated histories/conflict do thiếu tổ tiên chung.
- Kiểm tra: Remote `test` trả đúng hash `132f7af9a27282d59eb8f1d6a58eae5c5af6f8fa`; `git merge-base origin/main origin/test` bằng chính `225985ec48393f18a4f55a14dc99c557c469b38c`; kiểm tra ancestor thành công.
- Nên làm tiếp theo: Đẩy bản ghi này lên `origin/test`; sau đó có thể mở lại compare/PR từ `test` sang `main` để GitHub làm mới trạng thái.
- Phạm vi đồng bộ: Bản sửa ancestry đã push lên `origin/test`; bản ghi hoàn tất này chưa push; không push `main`.

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

## 2026-08-11 14:36 (UTC+07:00) — Codex — Đính chính vị trí RUN_FIX

- Lý do sửa: Ghi nhận minh bạch việc công cụ đã chèn nhầm bản `RUN_FIX final` 14:35 vào vị trí dòng 58 do dùng câu neo log không duy nhất.
- Đã sửa/đã làm: Không xóa, sửa, di chuyển hoặc ghi đè bản bị chèn nhầm; append bản đính chính này và bản kết quả chính thức ngay sau nó ở đúng cuối file để bảo toàn audit trail.
- Đối chiếu Result_Plan.md: Quy tắc quản lý tiến trình append-only tại `docs/log.md`.
- Trạng thái: Đạt sau đính chính; kết quả kỹ thuật không thay đổi.
- Kiểm tra: Xác nhận heading `RUN_FIX final` 14:35 đang ở dòng 58 và bản đính chính 14:36 nằm sau `RUN2 Regression Gate` ở cuối file.
- Nên làm tiếp theo: Dùng heading cuối duy nhất làm neo cho mọi lần append sau; không dùng câu `Phạm vi đồng bộ` lặp lại.
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
