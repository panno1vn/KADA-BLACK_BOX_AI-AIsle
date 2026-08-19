# AIsle — Nhật ký review tổng quát

File này lưu review tổng quát của bốn log chuyên môn và review tổng thể mã nguồn. Tiến trình chi tiết phải ghi vào log con phù hợp; thay đổi liên phần, audit cấu trúc, kết quả kiểm thử tổng và quyết định kiến trúc được ghi nối tiếp tại đây. Mọi nội dung phải dựa trên [`Result_Plan.md`](./Result_Plan.md).

## Phạm vi bốn log chuyên môn

- [`log_backend.md`](./log_backend.md): backend, API và lưu trữ.
- [`log_frontend.md`](./log_frontend.md): desktop, web, Streamlit, Unity presentation và UI/UX.
- [`log_sim.md`](./log_sim.md): simulation core, contracts, population/GA và test mô phỏng.
- [`log_mobile.md`](./log_mobile.md): Expo/React Native mobile.

Đợt tách lịch sử ban đầu đã phân loại 8 bản ghi backend, 19 bản ghi frontend, 22 bản ghi simulation và 1 bản ghi mobile. Bản ghi liên quan nhiều phần có thể xuất hiện trong nhiều log con để bảo toàn ngữ cảnh.

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

## Lịch sử quản trị chung trước khi tách log

## 2026-08-11 12:14 (UTC+07:00) — Codex

- Lý do sửa: Đính chính minh bạch thứ tự ghi log của bản audit `12:13`.
- Đã sửa/đã làm: Ghi thêm bản đính chính này ở đúng cuối file. Bản `12:13` đã bị công cụ chèn nhầm giữa bản `11:56` và `12:09` vì dùng câu neo trùng nhau; không có bản ghi cũ nào bị xóa, sửa nội dung hoặc ghi đè. Giữ nguyên các bản trước để bảo toàn dấu vết audit.
- Đối chiếu Result_Plan.md: Phần `Phạm vi triển khai hiện tại` và yêu cầu quản lý tiến trình bằng `docs/log.md`.
- Trạng thái: Đạt sau đính chính; nội dung công việc không thay đổi, nhưng thứ tự hiển thị của bản `12:13` được ghi nhận là ngoại lệ thao tác.
- Kiểm tra: Xác nhận các heading thời gian hiện có là `11:56`, `12:13`, `12:09`, `12:14`; bản này nằm ở cuối file. Từ các lượt sau phải append bằng neo duy nhất lấy từ cuối file hoặc cơ chế append trực tiếp, không dùng câu neo có thể lặp.
- Nên làm tiếp theo: Tiếp tục chỉ ghi bản mới ở cuối file; cài Unity Editor và xác minh `UnityApp/` khi chủ dự án bắt đầu bước kế tiếp.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.


## 2026-08-11 12:18 (UTC+07:00) — Codex

- Lý do sửa: Chủ dự án yêu cầu thay toàn bộ nội dung nhánh GitHub `test` bằng snapshot mới từ thư mục local hiện tại.
- Đã sửa/đã làm: Xác minh remote `origin` trỏ đúng `https://github.com/panno1vn/KADA-BLACK_BOX_AI-AIsle.git`; xác minh đang ở local `main`, nhánh local/remote `test` tồn tại; chuẩn bị tạo orphan snapshot từ toàn bộ file không bị ignore mà không chuyển nhánh, không thay đổi index/working tree hiện tại.
- Đối chiếu Result_Plan.md: Phần `Phạm vi triển khai hiện tại`; snapshot gồm tái thiết cấu trúc repository, Unity app skeleton, plan chính thức và web baseline.
- Trạng thái: Chưa đạt; đang chờ tạo commit snapshot và force-push thay thế nhánh `test`.
- Kiểm tra: `origin` đúng repository được yêu cầu; `main` đang khớp `origin/main`; không có tracked file nào bị `.gitignore` loại trừ; Git identity là `PANdeVInCent <yeuemnhieu3377@gmail.com>`.
- Nên làm tiếp theo: Tạo orphan commit bằng index tạm, force-push lên `origin/test`, xác minh hash và cây file remote, sau đó ghi bản hoàn tất.
- Phạm vi đồng bộ: Local đã cập nhật log; chưa stage bằng index chính, chưa commit lên nhánh local, chưa push.


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


## 2026-08-11 14:36 (UTC+07:00) — Codex — Đính chính vị trí RUN_FIX

- Lý do sửa: Ghi nhận minh bạch việc công cụ đã chèn nhầm bản `RUN_FIX final` 14:35 vào vị trí dòng 58 do dùng câu neo log không duy nhất.
- Đã sửa/đã làm: Không xóa, sửa, di chuyển hoặc ghi đè bản bị chèn nhầm; append bản đính chính này và bản kết quả chính thức ngay sau nó ở đúng cuối file để bảo toàn audit trail.
- Đối chiếu Result_Plan.md: Quy tắc quản lý tiến trình append-only tại `docs/log.md`.
- Trạng thái: Đạt sau đính chính; kết quả kỹ thuật không thay đổi.
- Kiểm tra: Xác nhận heading `RUN_FIX final` 14:35 đang ở dòng 58 và bản đính chính 14:36 nằm sau `RUN2 Regression Gate` ở cuối file.
- Nên làm tiếp theo: Dùng heading cuối duy nhất làm neo cho mọi lần append sau; không dùng câu `Phạm vi đồng bộ` lặp lại.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push.


## 2026-08-11 14:47 (UTC+07:00) — Codex — Xác minh remote `test` cuối

- Lý do sửa: Xác nhận bản ghi hoàn tất và clean snapshot đã cùng tồn tại trên remote.
- Đã sửa/đã làm: Push descendant `25422b612f13ee51b94a3c3e015ec3f0cbe52404` chứa bản log hoàn tất; xác minh lại tip, ancestry và tree.
- Đối chiếu Result_Plan.md: Không đổi phạm vi sản phẩm; chỉ hoàn tất audit đồng bộ Git của milestone đã kiểm thử.
- Trạng thái: Đạt.
- Kiểm tra: `origin/test = 25422b612f13ee51b94a3c3e015ec3f0cbe52404`; 234 file; forbidden Unity root cache = 0; `origin/main` vẫn là ancestor; local branch vẫn là `main`.
- Nên làm tiếp theo: Không cần thao tác Git thêm cho yêu cầu này.
- Phạm vi đồng bộ: Đã push `origin/test`; không push hoặc thay đổi `main`.


## 2026-08-14 16:48 (UTC+07:00) - Antigravity - So sánh dự án AIsle với dự án Quản lý mượn CSVC PTIT

- Lý do sửa: Người dùng hỏi sự khác biệt giữa dự án AIsle hiện tại và dự án D:\đồ án nhập môn cnpm\he_thong_quan_ly_muon_csvc.
- Đã sửa/đã làm:
  - Đọc thông tin README.md của thư mục D:\đồ án nhập môn cnpm\he_thong_quan_ly_muon_csvc.
  - Phân tích và lập bảng so sánh chi tiết giữa 2 dự án về: Mục đích/Mô hình bài toán, Đối tượng người dùng, Kiến trúc/Công nghệ, và Kết quả đầu ra (Output).
- Đối chiếu Result_Plan.md: Phù hợp với phạm vi phân tích.
- Trạng thái: Đạt.
- Kiểm tra: Đã trình bày bảng so sánh rõ ràng bằng tiếng Việt.
- Nên làm tiếp theo: Chờ câu hỏi tiếp theo từ người dùng.
- Phạm vi đồng bộ: Chỉ local.

## 2026-08-18 00:07 (UTC+07:00) — Codex — Tách ownership, chuẩn hóa log và sửa Desktop launcher

- Lý do sửa: Chủ dự án yêu cầu lấy `docs/Result_Plan.md` làm chuẩn để rà soát cấu trúc, tách rõ mobile/backend/frontend/simulation cho các nhóm khác nhau, sửa lỗi font của lịch sử log, tạo bốn log chuyên môn và sửa `run-desktop.bat` để ứng dụng chạy được.
- Đã sửa/đã làm: Tổ chức lại giao diện thành `frontend/desktop`, `frontend/web`, `frontend/streamlit`; giữ `mobile`, `backend`, `src`, `UnityApp`, `services`, `runtime` theo ranh giới ownership; cập nhật toàn bộ đường dẫn trong backend, tests, solution, README và kiến trúc; bổ sung `frontend/README.md`; cập nhật mục 4 của `Result_Plan.md`; khôi phục `frontend/desktop/AIsle.DesktopApp.csproj`, sửa `.gitignore` không còn loại nhầm mọi file `.csproj`, bổ sung dependency WPF, sửa lỗi compile và làm `run-desktop.bat` báo lỗi rõ ràng; xóa 38 file build sinh tự động trong `mobile/dist-check` và thêm ignore; sửa mã hóa tiếng Việt của lịch sử log, tách thành `log_backend.md`, `log_frontend.md`, `log_sim.md`, `log_mobile.md`, chuyển `log.md` thành nơi review tổng quát; cập nhật `AGENTS.md` theo cơ chế log và ownership mới; sửa Population test dùng `Result_Plan.md` thay cho `docs/run.md` không tồn tại.
- Đối chiếu Result_Plan.md: Phù hợp mục 4 về repository structure, mục 2.1 về giữ web/backend/runtime làm baseline, mục 55 về phân công và mục 58 về ranh giới Simulator/Reality; không triển khai thêm stage sản phẩm mới.
- Trạng thái: Đạt. Cấu trúc và log đã tách rõ, lỗi font đã được loại khỏi năm file log, Desktop launcher khởi chạy được ứng dụng WPF.
- Kiểm tra: `dotnet build AIsle.slnx -c Release --no-restore` thành công 0 warning/0 error; `run-desktop.bat` chạy ổn định qua smoke test 10 giây và không sinh `crash_log.txt`; 10/10 Node regression tests pass; C# Simulation verification pass; Population source-first verification pass đủ 5 invariant scenarios và các gate bổ sung; `git diff --check` pass; scan không còn chuỗi mojibake hoặc dấu `?` nằm trong từ ở năm file log. Mobile typecheck chưa chạy vì `mobile/node_modules` chưa được cài; mã nguồn mobile không bị chỉnh sửa trong lượt này.
- Nên làm tiếp theo: Các nhóm tiếp tục ghi chi tiết vào log chuyên môn tương ứng và ghi review liên phần vào `log.md`; khi đội mobile làm việc tiếp, chạy `npm ci` rồi `tsc --noEmit` để thiết lập gate riêng.
- Phạm vi đồng bộ: Chỉ local; chưa commit, chưa push. File người dùng tạo sẵn `docs/AIsle_Implementation_Plan_Reorganized.docx` được giữ nguyên.

## 2026-08-18 00:09 (UTC+07:00) — Codex — Xác minh cuối sau tái cấu trúc

- Lý do sửa: Xác minh backend vẫn phục vụ đúng web baseline sau khi chuyển `web/` vào `frontend/web/`, đồng thời kiểm tra trạng thái cuối của workspace và các log.
- Đã sửa/đã làm: Khởi động `backend/server.mjs` ở cổng kiểm tra 8766, gọi health endpoint và trang gốc, sau đó dừng server kiểm tra; rà lại Git status, diff và encoding log.
- Đối chiếu Result_Plan.md: Xác nhận chính sách giữ web/backend/runtime làm baseline ở mục 2.1 vẫn hoạt động sau khi tách ownership theo mục 4.
- Trạng thái: Đạt.
- Kiểm tra: `GET /health` trả `{ok: true, engine: "javascript-live"}`; `GET /` trả HTTP 200 và có thẻ title; `git diff --check` pass; năm file log không còn mẫu mojibake hoặc dấu `?` trong từ.
- Nên làm tiếp theo: Không còn bước kỹ thuật bắt buộc cho yêu cầu hiện tại; chỉ cài dependencies mobile và chạy typecheck khi đội mobile tiếp tục phát triển.
- Phạm vi đồng bộ: Chỉ local; chưa commit, chưa push.

## 2026-08-18 00:14 (UTC+07:00) — Codex — Sửa lỗi Desktop executable bị khóa

- Lý do sửa: `run-desktop.bat` build thất bại với MSB3027/MSB3021 vì tiến trình `AIsleDesktop` cũ vẫn giữ khóa trên `.build/bin/AIsle.DesktopApp/Release/net10.0-windows/AIsleDesktop.exe`.
- Đã sửa/đã làm: Xác minh PID 23988 là tiến trình AIsle smoke-test cũ chạy đúng từ output của repository nhưng không còn cửa sổ, sau đó dừng riêng tiến trình treo này; cập nhật `run-desktop.bat` để kiểm tra tiến trình `AIsleDesktop` trước khi gọi `dotnet run`, không build lại nếu app đã chạy và trả exit code thành công.
- Đối chiếu Result_Plan.md: Chỉ sửa launcher của WPF desktop client trong ranh giới `frontend/desktop`; không thay đổi simulation core hoặc stage sản phẩm.
- Trạng thái: Đạt.
- Kiểm tra: Khởi động mới thành công; lần chạy launcher thứ hai trong khi app đang mở trả thông báo `AIsle Desktop dang chay`, exit code 0 và không phát sinh MSB3026/MSB3027/MSB3021; sau khi đóng tiến trình test, build lại thành công 0 warning/0 error; không còn tiến trình AIsleDesktop treo; `git diff --check` pass.
- Nên làm tiếp theo: Dùng `run-desktop.bat` bình thường; nếu app đã mở, launcher sẽ giữ phiên đang chạy thay vì build đè executable.
- Phạm vi đồng bộ: Chỉ local; chưa commit, chưa push.

## 2026-08-18 01:51 (UTC+07:00) — Codex — Cleanup và phân loại repository theo rule/task mới

- Lý do sửa: Chủ dự án yêu cầu đọc kỹ `docs/rule.md` và `docs/task.md`, sau đó sắp xếp repository theo hướng sản phẩm WPF/WebView2 duy nhất và xóa các thành phần không dùng hoặc thuộc roadmap đã loại bỏ.
- Đã sửa/đã làm: Đưa Desktop App về `src/AIsle.DesktopApp` đúng kiến trúc tối thiểu; đưa web prototype về `web/` với trạng thái REFERENCE; cập nhật solution, launcher, project references, backend imports và JavaScript test imports; xóa Streamlit UI; xóa skeleton `services/VideoAnalytics`, `models`, `data`; xóa các placeholder rỗng trong `src` và `tests`; xóa `Project.md`, `docs/pic.jpg`, ba ảnh cashier WPF trùng không được tham chiếu và output `.build`; viết lại README theo product direction mới; đồng bộ `AGENTS.md`, `README_BUILD.md`, `src/README.md`, `tests/README.md`, `UnityApp/README.md`; sửa các self-reference sai trong `rule.md`/`task.md`; tạo `docs/repository-map.md` phân loại ACTIVE/REFERENCE/LEGACY/FROZEN/REMOVED, liệt kê `.csproj`, entry point và source map. Các xóa tài liệu cũ đã có sẵn trong working tree trước lượt này được giữ nguyên; dữ liệu người dùng trong `runtime/` không bị xóa hoặc hoàn tác.
- Đối chiếu rule.md/task.md: Bám product direction WPF/WebView2 + C# Core, kiến trúc tối thiểu ba project, Node/web chỉ là legacy/reference, Unity/Mobile frozen và Reality/Video removed. Việc move/xóa là ngoại lệ được chủ dự án yêu cầu trực tiếp trước S0.1; không mở S0.2 hoặc milestone tiếp theo.
- Trạng thái: Đạt cho yêu cầu sắp xếp và cleanup ban đầu; repository hiện không còn module lớn chưa phân loại.
- Kiểm tra: `dotnet build AIsle.slnx -c Release` thành công 0 error (có NU1900 vì môi trường không truy cập được vulnerability feed NuGet); Population verification pass 5/5 scenarios và các gate bổ sung; C# Simulation verification pass; web regression 10/10 pass; `run-desktop.bat` khởi động đúng executable từ layout mới; Node legacy health trả OK và trang gốc HTTP 200; scan không còn tham chiếu path cũ/đã xóa; `git diff --check` pass; không còn process AIsleDesktop hoặc `.build` sau cleanup.
- Nên làm tiếp theo: Review `docs/repository-map.md`; nếu chấp nhận phân loại này thì bắt đầu đúng CURRENT TASK S0.1/S0.2 theo thứ tự trong `task.md`, không tự mở Mobile/Unity/Reality.
- Phạm vi đồng bộ: Chỉ local; chưa commit, chưa push. Các file đã xóa là tracked nên vẫn phục hồi được từ lịch sử Git cho tới khi lịch sử bị loại bỏ.

## 2026-08-18 02:01 (UTC+07:00) — Codex — Chuẩn bị nhánh `develop`

- Lý do sửa: Chủ dự án yêu cầu tạo nhánh mới `develop` và đẩy toàn bộ trạng thái code hiện tại trong `D:\Big\KADA\test` lên nhánh này.
- Đã sửa/đã làm: Đọc `AGENTS.md`, `docs/rule.md`, `docs/task.md`, `docs/log.md`; xác minh remote chưa có nhánh `develop`; giữ nguyên toàn bộ thay đổi local có sẵn; restore các NuGet dependency đã khai báo để chạy verification trên clone mới.
- Đối chiếu rule.md/task.md: Việc tạo `develop` là ngoại lệ do chủ dự án yêu cầu trực tiếp; không mở task sản phẩm mới, không thay đổi kiến trúc hay dependency khai báo.
- Trạng thái: Đạt ở gate trước commit; đang chờ tạo commit và push `origin/develop`.
- Kiểm tra: `dotnet restore AIsle.slnx` thành công; `dotnet build AIsle.slnx -c Release --no-restore` thành công 0 warning/0 error; C# Simulation verification pass; Population verification pass; 10/10 JavaScript regression tests pass; staged/unstaged `git diff --check` pass.
- Nên làm tiếp theo: Tạo local branch `develop`, stage toàn bộ snapshot, commit, fetch/kiểm tra remote và push không force.
- Phạm vi đồng bộ: Hiện chỉ local; chưa commit, chưa push; không thay đổi `main` hoặc remote `test`.

## 2026-08-18 02:03 (UTC+07:00) — Codex — Hoàn tất tạo nhánh `develop`

- Lý do sửa: Ghi nhận kết quả commit/push snapshot local lên nhánh `develop` và đính chính minh bạch kết quả whitespace gate sau khi stage các file mới.
- Đã sửa/đã làm: Tạo local branch `develop` từ `test` commit `00d2dd8`; stage toàn bộ trạng thái working tree; tạo commit `4f0bdfa` (`chore: establish develop workspace snapshot`); push nhánh mới lên `origin/develop` và thiết lập upstream.
- Đối chiếu rule.md/task.md: Đây là thao tác Git ngoại lệ theo yêu cầu trực tiếp của chủ dự án; không mở task sản phẩm kế tiếp và không push `main`/`test`.
- Trạng thái: Đạt. `origin/develop` đã được tạo mà không force-push.
- Kiểm tra: Build .NET 0 warning/0 error; C# Simulation và Population verification pass; 10/10 JavaScript regression pass. Đính chính bản `02:01`: unstaged diff check ban đầu pass, nhưng sau khi stage các file log lịch sử mới, `git diff --cached --check` báo trailing whitespace có sẵn trong `docs/log_backend.md`, `docs/log_frontend.md`, `docs/log_mobile.md`, `docs/log_sim.md`, `docs/repository-map.md` và `docs/task.md`; không tự sửa vì các log chuyên môn là lịch sử không được chỉnh lại.
- Nên làm tiếp theo: Nếu cần merge `develop`, review riêng các whitespace của file lịch sử theo quyết định của chủ dự án; không tự mở task kế tiếp.
- Phạm vi đồng bộ: Đã commit và push `origin/develop`; `main` và `origin/test` không thay đổi.

## 2026-08-18 02:28 (UTC+07:00) — Antigravity — Hoàn thành S0 (S0.1, S0.2, S0.3)

- Lý do sửa: Thực hiện toàn bộ phần S0 (Architecture & Baseline) theo yêu cầu trong `docs/task.md`.
- Đã sửa/đã làm:
  - S0.1 (Repository Classification): Rà soát, lập bảng phân loại toàn bộ root folders, `.csproj` và entry points theo các trạng thái ACTIVE / REFERENCE / LEGACY / FROZEN / REMOVED.
  - S0.2 (Dependency Audit): Phân tích dependency graph và scan toàn bộ C# codebase trong `src/`; xác nhận mã nguồn DesktopApp/Simulation/Contracts không phụ thuộc vào Node.js, localhost, HTTP hay Unity Engine.
  - S0.3 (Baseline Tests): Chạy kiểm thử toàn diện gồm `dotnet build AIsle.slnx -c Release` (0 error, 0 warning), Population verification (11/11 PASS), Simulation verification (6/6 PASS), JS regression test suite (10/10 PASS) và kiểm tra `git diff --check` sạch sẽ.
  - Tạo báo cáo tổng hợp `s0_report.md`.
- Đối chiếu Result_Plan.md: Bám sát mục tiêu S0 trong `task.md` và tuân thủ các quy tắc trong `rule.md` (giữ nguyên code, không thêm bớt dependency ngoài kế hoạch, không rewrite).
- Trạng thái: Đạt.
- Kiểm tra: `dotnet build` 0 warning/0 error, Population verification PASS, Simulation verification PASS, 10/10 JS tests PASS, `git diff --check` pass.
- Nên làm tiếp theo: Bắt đầu S1.1 (Local UI Hosting cho WebView2) khi có chỉ thị tiếp theo từ chủ dự án; WIP luôn giữ = 1.
- Phạm vi đồng bộ: Đã commit và push origin/develop; main và origin/test không thay đổi.

## 2026-08-18 07:48 (UTC+07:00) — Codex — Hoàn thành S1 Desktop Foundation

- Lý do sửa: Chủ dự án phụ trách SYSTEM và yêu cầu triển khai trọn S1 theo `docs/rule.md`/`docs/task.md`: Desktop phải host UI local, có bridge WebView2 ↔ C# theo envelope chuẩn và không khởi động Node.js.
- Đã sửa/đã làm: Thay shell tại `src/AIsle.DesktopApp/MainWindow.xaml(.cs)` bằng WebView2; đóng gói nguyên trạng tài nguyên REFERENCE từ `web/` vào thư mục `UI` cạnh executable; dùng virtual host `https://aisle.local` với `CoreWebView2HostResourceAccessKind.DenyCors`, không dùng localhost/server cho static UI; thêm `Bridge/BridgeMessageProcessor.cs` và `UI/desktop-bridge.js` để hỗ trợ request `{requestId,type,payload}`/response `{requestId,ok,payload,error}`, `app.ping`, lỗi có cấu trúc và requestId round-trip; thêm kiểm tra asset trong `Infrastructure/LocalUiAssets.cs`; thêm console verification `tests/AIsle.DesktopApp.Tests` vào solution. Không sửa web prototype, Simulation, Population, Mobile hoặc Unity; không mở S2.
- Dependency audit: Thêm duy nhất `Microsoft.Web.WebView2` phiên bản `1.0.4078.44` cho module `src/AIsle.DesktopApp`; nguồn chính thức [Microsoft Learn — WebView2 WPF](https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/wpf), [Microsoft Learn — local content](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/working-with-local-content) và [NuGet chính thức](https://www.nuget.org/packages/Microsoft.Web.WebView2/1.0.4078.44); giấy phép BSD-3-Clause theo `LICENSE.txt` trong package; mục đích là control WPF, virtual host mapping và native web messaging được S1 chỉ định; integration test gồm build, packaged-assets audit, bridge verification và process smoke.
- Đối chiếu rule.md/task.md: Hoàn thành S1.1 Local UI Hosting, S1.2 Bridge Envelope và S1.3 Remove Desktop Node Boot đúng thứ tự WIP=1; Desktop vẫn theo chuỗi `WPF shell → WebView2 local UI → C# Bridge`, còn `web/` và backend giữ REFERENCE/LEGACY. Các API project/layout/history chưa được chuyển sang C# vì thuộc S2/S5.
- Trạng thái: Đạt. `AIsleDesktop.exe` chạy local UI bằng WebView2 mà không tạo tiến trình Node; bridge hai chiều hoạt động theo contract và invalid message không làm processor crash.
- Kiểm tra: `dotnet build AIsle.slnx -c Release` thành công 0 warning/0 error; Desktop bridge verification pass; output có đủ `UI/index.html`, CSS, JavaScript, ảnh và `desktop-bridge.js`; smoke process cho thấy Desktop responding, tạo 3 WebView2 process, tạo 0 Node process mới và sau khi đóng còn 0 process WebView2 do phiên test tạo; `run-desktop.bat` build/khởi động thành công và trả exit code 0 sau khi đóng app; Population verification pass, Simulation verification pass, 10/10 JavaScript regression pass; scan `src/AIsle.DesktopApp`/launcher không có `node.exe`, `server.mjs`, localhost, `127.0.0.1` hoặc `Process.Start`; không có `crash_log.txt`; `git diff --check` không có lỗi whitespace (chỉ cảnh báo chuyển LF/CRLF).
- Nên làm tiếp theo: Dừng tại S1 theo task card; chỉ mở S2 Project/Layout System khi chủ dự án yêu cầu. Khi kiểm thử thủ công, chạy `run-desktop.bat` và xác nhận badge `Desktop bridge: ready` ở góc dưới phải.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push. Giữ nguyên thay đổi có sẵn của chủ dự án tại `runtime/layout.json`.

## 2026-08-18 08:06 (UTC+07:00) — Codex — Hoàn thành S2 Project / Layout System

- Lý do sửa: Chủ dự án yêu cầu tiếp tục toàn bộ S2 theo `docs/rule.md`/`docs/task.md`, gồm khóa Layout contract, Project Load, Project Save và Layout Validation trong backend C# in-process.
- Đã sửa/đã làm: Tạo contract duy nhất `aisle.project.v1` tại `src/AIsle.Contracts/Project/ProjectContracts.cs` cho project/layout/catalog đang dùng; thêm `Application/ProjectOperations.cs` với repository boundary, use case Load/Save và error mapping; thêm `Infrastructure/JsonProjectRepository.cs` dùng `System.Text.Json`, từ chối member ngoài schema, lưu qua file tạm rồi replace và reload; thêm `Application/LayoutValidator.cs` kiểm tra entrance, checkout, bounds, wall/shelf geometry, id trùng, valence và shelf reachability bằng `PathGrid` A* hiện có; thêm bridge `project.load`/`project.save`; chuyển riêng request `/api/project` của UI local sang bridge C# trong `UI/desktop-bridge.js`, nên JavaScript không trực tiếp ghi file và không cần Node API; đóng gói `UI/default-project.json`, seed lần đầu tại `%LOCALAPPDATA%\AIsle\project-v1.json`; mở rộng `tests/AIsle.DesktopApp.Tests` cho serialization, valid/missing/malformed/invalid-schema load, save/reload, invalid-save rejection, reachability warning và bridge round-trip. Không sửa `web/`, thuật toán Simulation/Population, Mobile hoặc Unity; không mở S3 hay UI/UX U2.
- Nguồn kỹ thuật: Dùng BCL có sẵn, không thêm dependency; schema strict dựa trên [Microsoft Learn — System.Text.Json unmapped members](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members) và camel-case contract theo [Microsoft Learn — customize JSON properties](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties). Giữ nguyên A* hiện tại vì regression pass; không thêm `roy-t/AStar`.
- Đối chiếu rule.md/task.md: Hoàn thành đúng thứ tự S2.1 → S2.2 → S2.3 → S2.4 với WIP=1; flow active là `WebView2 → Bridge → Application → IProjectRepository → Infrastructure/System.Text.Json → Project DTO`; `AIsle.Contracts` và `AIsle.Simulation` không truy cập filesystem; missing entrance/checkout và geometry sai là error chặn Save, shelf không tới được là warning có `unreachableShelfIds`.
- Trạng thái: Đạt. Một schema project active, Load/Save thuần C#, round-trip pass, error mapping rõ và UI local đã dùng bridge C# cho project persistence.
- Kiểm tra: `dotnet build AIsle.slnx -c Release --no-restore` thành công 0 warning/0 error; Desktop S1/S2 verification pass toàn bộ contract/load/save/validation/bridge scenarios; invalid layout không tạo file; atomic-save không rò file `.tmp`; Population verification pass; Simulation verification pass; 10/10 JavaScript regression pass; Core filesystem audit không có kết quả; `src/AIsle.Simulation` không có diff nên A* không bị rewrite; `node --check` pass cho bridge script; smoke ngoài sandbox cho thấy Desktop responding, tạo 6 WebView2 process, 0 Node process mới, default project đúng schema và còn 0 WebView2 process của phiên test sau khi đóng; `git diff --check` không có lỗi whitespace.
- Nên làm tiếp theo: Dừng tại S2 theo task card; chỉ mở S3 Population System hoặc U2 Project/Layout UI khi chủ dự án yêu cầu đúng thứ tự. File picker và UX Open/Save thuộc U2, backend bridge hiện đã nhận path để tích hợp sau.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push. Giữ nguyên thay đổi có sẵn của chủ dự án tại `runtime/layout.json`; smoke test chỉ tạo dữ liệu ứng dụng `%LOCALAPPDATA%\AIsle\project-v1.json` từ default project đã đóng gói.

## 2026-08-18 08:13 (UTC+07:00) — Codex — Chuẩn bị đồng bộ S1/S2 lên `develop`

- Lý do sửa: Chủ dự án yêu cầu đẩy toàn bộ code hiện tại trong `D:\Big\KADA\test` lên nhánh `develop` và tránh conflict.
- Đã sửa/đã làm: Kiểm tra working tree; fetch `origin/develop`; xác minh local/remote cùng base `b632be1` và remote không có commit mới; giữ nguyên thay đổi layout của chủ dự án; chạy lại toàn bộ verification cho S1/S2 trước commit.
- Đối chiếu rule.md/task.md: Chỉ đồng bộ các task S1/S2 đã hoàn thành và thay đổi local có sẵn; không mở S3/U2, không thay đổi `main` hoặc `test` remote.
- Trạng thái: Đạt ở gate trước commit; chưa push.
- Kiểm tra: .NET build 0 warning/0 error; Desktop S1/S2 verification pass; C# Simulation pass; Population pass; 10/10 JavaScript regression pass; staged/unstaged `git diff --check` pass; divergence `HEAD...origin/develop = 0/0`.
- Nên làm tiếp theo: Stage toàn bộ snapshot, commit, fetch lần cuối và push fast-forward lên `origin/develop` không force.
- Phạm vi đồng bộ: Hiện chỉ local; chưa commit/push; `main` và remote `test` không thay đổi.

## 2026-08-18 08:14 (UTC+07:00) — Codex — Hoàn tất push S1/S2 lên `develop`

- Lý do sửa: Ghi nhận kết quả đồng bộ code Desktop Foundation và Project/Layout System lên nhánh `develop` theo yêu cầu của chủ dự án.
- Đã sửa/đã làm: Tạo commit `cd3e23a` (`feat: add desktop bridge and project persistence`) gồm 17 file S1/S2, test Desktop và thay đổi layout có sẵn; fetch lần cuối cho thấy remote không có commit mới; push fast-forward `b632be1..cd3e23a` lên `origin/develop` không force.
- Đối chiếu rule.md/task.md: Chỉ đồng bộ các task S1/S2 đã hoàn thành; không mở task kế tiếp, không thay đổi `main` hoặc remote `test`.
- Trạng thái: Đạt. Code đã có trên `origin/develop` và không phát sinh conflict.
- Kiểm tra: Toàn bộ gate trước commit pass; fetch ngay trước push cho divergence `1/0`; push fast-forward thành công.
- Nên làm tiếp theo: Dừng theo task card; chỉ mở S3 hoặc U2 khi chủ dự án yêu cầu.
- Phạm vi đồng bộ: Đã commit và push `origin/develop`; `main` và remote `test` không thay đổi.

## 2026-08-18 08:39 (UTC+07:00) — Codex — Hoàn thành S3 Population System và S4 Simulation System

- Lý do sửa: Chủ dự án yêu cầu triển khai liên tiếp toàn bộ S3 rồi S4 theo `docs/task.md`/`docs/rule.md`, ưu tiên áp dụng thư viện và hướng dẫn nguồn chính thức thay vì tự tạo generic GA, thống kê hoặc thuật toán điều hướng mới.
- Đã sửa/đã làm: S3 audit và đóng băng 12 trường `NPCProfile` có caller runtime tại `docs/npc-profile-active-fields.md`; giữ 8 trường legacy chỉ để tương thích nhưng loại khỏi chromosome/fitness/validation/statistics active; chuyển domain chromosome sang các gene thực sự được `SimulationHost`, `NPCRuntimeState` và `NeedAffectSystem` sử dụng; tiếp tục dùng generic population/EliteSelection/UniformCrossover/UniformMutation/GenerationNumberTermination của GeneticSharp 2.6 và thống kê mean/population std/median/percentile của Math.NET 5.0; bổ sung `PopulationApplicationService` và bridge `population.generate` trả `profiles`, `summary`, `validation`. S4 đóng băng đủ 24 trường config active, ghi default/bounds/caller tại `docs/simulation-config.md`, thêm validator tập trung; giữ nguyên A* hiện có; bổ sung projection chỉ gồm time, id/x/y/status/targetId và counters; thêm `SimulationApplicationService` sở hữu background tick và các bridge command `simulation.start`, `simulation.pause`, `simulation.step`, `simulation.reset`; giải phóng simulation timer khi Desktop đóng.
- Nguồn kỹ thuật: Không thêm dependency. Giữ [GeneticSharp chính thức](https://github.com/giacomelli/GeneticSharp) cho generic GA và [Math.NET Numerics chính thức](https://github.com/mathnet/mathnet-numerics) cho thống kê. A* hiện tại được KEEP vì regression mới xác nhận no wall penetration, no corner cutting, unreachable handling và bounded recovery/abandon; không thêm `roy-t/AStar`, ORCA, DOTS, emotion expansion hoặc animation state.
- Đối chiếu rule.md/task.md: Hoàn thành đúng thứ tự S3.1 → S3.2 → S3.3 → S3.4, qua quality gate rồi mới mở S4.1 → S4.2 → S4.3 → S4.4 → S4.5; Application/bridge chỉ orchestration, C# Simulation sở hữu tick/formula/navigation/projection, UI không đọc internal runtime object và không sở hữu tick logic; không mở S5, Mobile, Unity hoặc Reality.
- Trạng thái: Đạt. Pipeline `PopulationConfig → Generate → Validate → Statistics → NPCProfile[]` và journey `spawn → decide → navigate → interact → purchase/no purchase → checkout/exit` đều hoạt động qua bridge-ready C# boundary.
- Kiểm tra: `dotnet build AIsle.slnx -c Release --no-restore` thành công 0 error (chỉ NU1900 do vulnerability feed NuGet không truy cập được); Population verification pass 5 golden scenarios cùng count/bounds/rejection/mean/std/percentile/distribution/serialization/dependency-boundary; Simulation verification pass config bounds, wall/corner/unreachable, blocked target, bounded replan/abandon, no-purchase, purchase, checkout, exit, termination, counter/revenue consistency, geometry và projection serialization; Desktop bridge/application verification pass cho `population.generate` và bốn simulation commands; 10/10 JavaScript regression pass; `git diff --check` không có lỗi whitespace.
- Nên làm tiếp theo: Dừng tại S4 theo task card; chỉ mở S5 khi chủ dự án yêu cầu. Có thể chạy `run-desktop.bat` để review thủ công, nhưng bridge/projection đã được kiểm tra tự động mà không cần giữ process Desktop mở.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push. Không thay đổi dữ liệu trong `runtime/`.

## 2026-08-18 09:24 (UTC+07:00) — Codex — Hoàn thành S5 Result/History/Replay, S6 KPI/Compare và S7 Release/QA

- Lý do sửa: Chủ dự án yêu cầu triển khai tuần tự toàn bộ S5 → S6 → S7 theo `docs/task.md`/`docs/rule.md`, ưu tiên nguồn chính thức và component hiện có, không tự thêm database, framework, thuật toán mô phỏng hoặc optimization chưa có benchmark.
- Đã sửa/đã làm: S5 khóa `aisle.sim-result.v1` còn đúng 8 field top-level phục vụ identity/timestamp/summary/events/purchases/replay; thêm strict `System.Text.Json` round-trip, `IHistoryStore` và `JsonHistoryStore` tại `%LOCALAPPDATA%\AIsle\history-v1` với atomic same-directory move, duplicate-ID rejection, sorted list, corrupted-file isolation và structured error; chuyển `HistoryService` legacy thành adapter của store duy nhất; thêm history save/list/read và replay projection bridge, replay deep-copy trực tiếp stored trajectory và pass restart-store determinism. S6 định nghĩa 7 KPI truy vết được tại `docs/kpi-definitions.md`, thêm pure KPI projection và two-run comparison từ stored result cùng bridge `kpi.project`/`compare.results`; không rerun simulator và relative delta là null khi Run A bằng 0. S7 thêm project→simulation mapper, actionable WebView2 startup error, executable `--qa-smoke`, benchmark project 200/500/1000 NPC, release profile `win-x64`, script publish/audit, version `1.0.0-mvp`, demo data và tài liệu release/QA.
- Nguồn kỹ thuật: Không thêm dependency. JSON strict theo [Microsoft Learn — unmapped members](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members); release dùng cách duy nhất được .NET hỗ trợ chính thức là [`dotnet publish`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) self-contained; WebView2 Evergreen prerequisite và error handling theo [Microsoft WebView2 distribution](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution). Không dùng database, single-file/trimming rủi ro cho WPF/WebView2, Unity, Node runtime, Reality, ORCA, DOTS hoặc optimization mới.
- Đối chiếu rule.md/task.md: Hoàn thành đúng thứ tự S5.1 → S5.2 → S5.3, quality gate, S6.1 → S6.2 → S6.3, quality gate, S7.1 → S7.2 → S7.3 → S7.4; persistence/orchestration ở Desktop Application/Infrastructure, replay/KPI/compare pure logic ở Simulation, Contracts chỉ chứa DTO; UI chỉ nhận bridge projection. Không mở UI/UX milestone, Mobile, Unity, Reality hoặc task sau S7.
- Trạng thái: Đạt. Flow system `Run → SimResult → Save → History → Load → Replay → KPI → Compare` hoạt động, có artifact release self-contained nhận diện phiên bản và smoke được từ publish directory không cần source repository hay .NET shared runtime.
- Kiểm tra: Restore online và `dotnet build AIsle.slnx -c Release --no-restore` thành công 0 warning/0 error; Population, Simulation, S5/S6 Results và Desktop S1–S7 verification đều pass; JavaScript regression 10/10 pass; error gate pass invalid/malformed project, missing entrance/checkout, unreachable shelf, corrupted history và WebView2/local-asset startup; benchmark Release 15 ticks đạt correctness cho 200/500/1000 NPC với tổng runtime 120.75/242.44/286.66 ms, tick trung bình 8.05/16.16/19.11 ms; self-contained publish khoảng 163.47 MB/434 file; QA artifact report pass đủ `launch/open-project/population/run/history/replay/compare`, 2 history item, 8 replay agent, 7 KPI; GUI smoke responding, tạo WebView2 nhưng 0 Node mới; artifact có 0 source file, 0 Node/Unity/Reality dependency; executable FileVersion `1.0.0.0`, ProductVersion `1.0.0-mvp+0eaf7cf...`; `git diff --check` không có lỗi whitespace.
- Nên làm tiếp theo: Dừng tại S7 theo yêu cầu. Artifact local nằm tại `.build/release/win-x64`; khi cần phân phối lại, chạy `scripts/build-release.ps1`. UI/UX U5/U6/U7 chỉ mở khi chủ dự án yêu cầu riêng theo execution order.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push. `.build` và báo cáo QA raw được ignore; không thay đổi dữ liệu tracked trong `runtime/`.

## 2026-08-18 09:41 (UTC+07:00) — Codex — RUN LIVE khởi động NPC ngay

- Lý do sửa: Khi nhấn `RUN LIVE` tại màn hình Mô Phỏng & Run Thử, giao diện phải có NPC chạy và tiến trình hiển thị ngay, không chờ khoảng trống trước lần đến Poisson đầu tiên hoặc tick render đầu tiên.
- Đã sửa/đã làm: Đặt NPC đầu tiên của mỗi phiên live tại T=0 ở cả JavaScript reference engine và C# SimulationHost; giữ nguyên các thời điểm Poisson đã lấy mẫu từ NPC thứ hai trở đi; cho nút RUN LIVE thực thi ngay một simulation tick và cập nhật giao diện trước `requestAnimationFrame`; cho `simulation.start` C# thực thi tick đầu ngay trước khi trả projection. Không thay đổi bộ lấy mẫu `PoissonSpawnSampler` hay công thức phân bố.
- Đối chiếu rule.md/task.md: Thay đổi nằm trong S4 Runtime/Simulation và Desktop application orchestration hiện có; engine vẫn sở hữu lịch spawn/tick, UI chỉ phát lệnh và render projection; không mở task mới, không thêm dependency hay thuật toán mới.
- Trạng thái: Đạt. NPC đầu tiên spawn và bắt đầu trạng thái di chuyển/quyết định ngay khi RUN LIVE, pause/resume không tạo thêm tick khởi động, reset vẫn trả về T=0.
- Kiểm tra: JavaScript regression 10/10 pass; .NET Release build 0 warning/0 error; C# Simulation verification pass và xác nhận first NPC immediate trong khi Poisson mean vẫn trong tolerance; Desktop S1–S7 verification pass và xác nhận `simulation.start` trả time > 0, spawned = 1; `git diff --check` được chạy ở gate cuối.
- Nên làm tiếp theo: Chạy `run-desktop.bat`, vào Mô Phỏng & Run Thử và bấm RUN LIVE để review trực quan tốc độ khởi động NPC theo cấu hình hiện tại.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push. Không đụng tới thay đổi/xóa file ngoài phạm vi hành vi RUN LIVE.

## 2026-08-18 09:45 (UTC+07:00) — Codex — Random hóa từng phiên RUN LIVE

- Lý do sửa: Chủ dự án yêu cầu kiểm tra và loại bỏ seed cố định để mỗi lần chạy live mới tạo một kết quả ngẫu nhiên khác.
- Đã sửa/đã làm: Phát hiện UI thực tế vẫn dùng input ẩn seed `42` cho cả population và runtime; xóa hoàn toàn input/listener seed khỏi sản phẩm; thêm `createRunSeed` lấy seed 32-bit bằng Web Crypto, có fallback và bảo đảm không trùng seed của phiên liền trước; dùng cùng seed ngẫu nhiên trong nội bộ một phiên cho population/spawn/decision và vẫn lưu seed vào SimResult để truy vết; tự tạo phiên ngẫu nhiên mới khi input đổi, Reset hoặc chạy lại sau Complete. Pause/Resume tiếp tục phiên hiện tại; deterministic seek dựng lại đúng seed của chính phiên đó.
- Đối chiếu rule.md/task.md: Chỉ thay đổi session orchestration và RNG initialization hiện có; không đổi công thức, Poisson sampler, thuật toán GA, contract lịch sử hoặc thêm dependency. Seed vẫn được phép trong test/replay để kiểm chứng deterministic, nhưng không còn seed cố định trong UI RUN LIVE.
- Trạng thái: Đạt. Mỗi phiên live mới có seed khác phiên trước; chạy lại sau khi hoàn tất không còn mắc ở trạng thái COMPLETE và tự dựng phiên random mới.
- Kiểm tra: Scan cả `web/` và UI đã build không còn `id="seed"`, `value="42"` hoặc truy cập `#seed`; JavaScript regression 10/10 pass, gồm test Web Crypto và chống trùng seed liên tiếp; .NET Release build 0 warning/0 error; Desktop S1–S7 verification pass; `git diff --check` pass.
- Nên làm tiếp theo: Chạy thủ công hai phiên RUN LIVE hoàn chỉnh với cùng input và đối chiếu NPC/spawn/purchase khác nhau; Pause rồi Resume phải tiếp tục đúng phiên thay vì tạo lại.
- Phạm vi đồng bộ: Chỉ local; chưa stage, chưa commit, chưa push. Không sửa seed cố định trong test/benchmark vì chúng cần tái lập kết quả regression.

## 2026-08-18 10:05 (UTC+07:00) — Codex — Chuẩn bị đồng bộ nhánh develop

- Lý do: Chủ dự án yêu cầu đẩy toàn bộ code hiện tại tại `D:\Big\KADA\test` lên `origin/develop` và tránh xung đột.
- Đã làm: Xác nhận working tree ở nhánh `develop`; fetch `origin/develop`; đối chiếu lịch sử local/remote không lệch (`0/0`); giữ nguyên toàn bộ thay đổi hiện tại và không chạm `D:\Big\KADA\store\main`.
- Kiểm tra: `dotnet build AIsle.slnx -c Release --no-restore` đạt 0 warning/0 error; Desktop, Results, Simulation và Population verification đều pass; toàn bộ 10 JavaScript regression test pass; `git diff --check` pass.
- Trạng thái: Sẵn sàng stage và commit; sẽ fetch/đối chiếu remote thêm một lần ngay trước khi push; tuyệt đối không force-push.

## 2026-08-18 10:08 (UTC+07:00) — Codex — Đã đồng bộ code lên develop

- Đã làm: Commit toàn bộ thay đổi hiện tại bằng commit `99fc74e` (`feat: complete simulation results and release pipeline`) và push fast-forward từ `0eaf7cf` lên `origin/develop`.
- Kiểm soát conflict: Fetch lại ngay trước push cho kết quả local/remote `1/0`; remote không có commit mới, không cần merge hoặc rebase, không dùng force-push.
- Phạm vi: Chỉ thao tác repository `D:\Big\KADA\test` và nhánh `develop`; không chạm nhánh `main` hay thư mục `D:\Big\KADA\store\main`.
- Trạng thái: Thành công; tiếp tục commit riêng mục nhật ký này và xác nhận local/remote đồng bộ `0/0`.

## 2026-08-18 10:09 (UTC+07:00) — Codex — Hoàn thành S8 NPC Behavior Refinement

- Lý do sửa: Chủ dự án yêu cầu triển khai tuần tự toàn bộ `S8_NPC_BEHAVIOR_REFINEMENT_COMPACT.md` để NPC chọn/mua hàng hợp lý hơn và tới target tự nhiên hơn, tuân thủ `rule.md`/`task.md` và ưu tiên source gốc.
- Đã sửa/đã làm: S8.1 audit và ghi bảng ownership/factor tại `docs/s8-behavior-audit.md`; phát hiện CategoryPreferences, ShoppingMission, PriceSensitivity và Impulsiveness đã có contract nhưng chưa có caller active; thêm pure `ShoppingDecisionSystem` tách target utility `Need + Preference + Mission - Travel` khỏi purchase probability `Need + Preference - Price + Impulse`; giữ reachability làm hard filter trước utility, weighted stochastic choice/roll hiện có và bỏ Affect khỏi shopping decision mà không xóa compatibility config; không thêm Promotion vì contract không có. Kích hoạt đúng hai trait có sẵn Impulsiveness/PriceSensitivity trong domain chromosome GeneticSharp, mapping category preference/shopping mission, validation và Math.NET statistics. S8.2 thêm velocity runtime, preferred velocity, bounded smoothing, final slowing radius, stop tolerance, snap/no-overshoot và zero velocity khi arrive; chiếu movement theo segment A* và giữ `LineIsWalkable`; agent đã dừng/DWELL không bị separation đẩy khỏi access point. S8.3 chỉ thêm verification, không mở feature mới.
- Nguồn kỹ thuật: Giữ nguyên A* hiện tại sau regression; [`roy-t/AStar`](https://github.com/roy-t/AStar) chỉ dùng đối chiếu grid/corner-cutting. Preferred/current velocity dựa trên source MIT [`meshula/OpenSteer`](https://github.com/meshula/OpenSteer), nhưng chỉ áp dụng primitive seek/steering tối thiểu, không mang framework vào dự án. Tiếp tục dùng generic GA machinery của [`GeneticSharp`](https://github.com/giacomelli/GeneticSharp) và statistics của [`Math.NET Numerics`](https://github.com/mathnet/mathnet-numerics). Không thêm package/dependency.
- Đối chiếu rule.md/task.md: Thực hiện đúng WIP=1 và thứ tự S8.1 → gate D1–D5 → S8.2 → gate M1–M6 → S8.3; Simulation Core sở hữu decision/movement, không sửa A*, UI, bridge, persistence, SimResult schema, Emotion/Social/Queue/Animation, ORCA/RVO2, DOTS/ECS, Spatial Hash hoặc Path Cache; không thêm trait hay config mới.
- Trạng thái: Đạt. Unreachable shelf không được chọn; need/preference/mission/travel và price/impulse có tác động đúng chiều; target choice không còn xác nhận mua; NPC tăng/giảm velocity có bound, giảm tốc gần target, dừng đúng access point, không overshoot/oscillate và không xuyên wall.
- Kiểm tra: `dotnet build AIsle.slnx -c Release --no-restore` 0 warning/0 error; Population verification pass 5 golden scenarios cùng active shopping-field bounds/validation/statistics/GeneticSharp boundary; Simulation pass S8 D1–D5, M1–M5 trên straight path, 90-degree turn và narrow corridor, M6 full journey, A* wall/corner/unreachable/replan, purchase/no-purchase/checkout/exit và SimResult round-trip; Results S5/S6 pass; Desktop S1–S7 pass; JavaScript regression 10/10 pass; csproj/package diff trống; `git diff --check` pass.
- Nên làm tiếp theo: Dừng tại S8 theo task card. Review UI trực quan của C# projection thuộc integration/UI task riêng; không tự mở từ S8.
- Phạm vi đồng bộ: Chỉ local trên `develop`; chưa stage, chưa commit, chưa push. Giữ nguyên file task S8 do chủ dự án cung cấp.

## 2026-08-18 10:18 (UTC+07:00) — Codex — Đã đồng bộ S8 lên develop

- Đã làm: Commit toàn bộ batch S8 bằng commit `bf374d7` (`feat: refine NPC shopping decisions and movement`) và push fast-forward từ `7786fc4` lên `origin/develop`.
- Kiểm soát conflict: Fetch trước khi kiểm thử và fetch lại ngay trước push đều xác nhận remote không có commit mới; local/remote trước push `1/0`; không merge, không rebase và không force-push.
- Kiểm tra: Release build 0 warning/0 error; Desktop, Results, Simulation và Population verification pass; 10/10 JavaScript regression test pass. Các khoảng trắng cuối dòng được giữ nguyên trong file task Markdown do là cú pháp hard line break của tài liệu do chủ dự án cung cấp.
- Phạm vi: Chỉ repository `D:\Big\KADA\test`, nhánh `develop`; không chạm `main` hoặc `D:\Big\KADA\store\main`.

## 2026-08-18 10:36 (UTC+07:00) — Codex — Hoàn thành Local Crowd Avoidance bằng ORCA/RVO2-CS

- Lý do sửa: Chủ dự án yêu cầu triển khai toàn bộ `docs/run.txt` sau S8 theo `rule.md`/`task.md`, dùng nghiên cứu và implementation chính chủ thay vì tự viết công thức tránh va chạm.
- Đã sửa/đã làm: Audit luồng movement hiện hữu và nguồn ORCA/RVO2 tại `docs/RVO2_INTEGRATION_AUDIT.md`; vendor nguyên trạng bảy source C# của `snape/RVO2-CS` tag `v2.0.1`, revision `5b7147d...`, kèm Apache-2.0/source record/Unity meta trong package `AIsle.Simulation`; thêm biên `IRvoAvoidance` và `Rvo2Adapter` không rò kiểu RVO; thay positional `Separate` bằng pipeline batch `A* waypoint → preferred velocity S8 → RVO2 actual velocity → LineIsWalkable position`; đưa NPC active đã dừng vào RVO với velocity/maxSpeed bằng 0; giữ A* sở hữu wall/replan/abandon; thêm fallback preferred velocity an toàn và event một lần nếu adapter lỗi; thêm bốn config có bounds cho neighbor distance/max neighbors/two time horizons; giữ `SeparationStrength` chỉ để tương thích dữ liệu cũ. Không thay decision/purchase S8.1 hoặc SimResult schema.
- Nguồn kỹ thuật: Dùng [ORCA research](https://gamma-web.iacs.umd.edu/ORCA/), [RVO2 library](https://gamma-web.iacs.umd.edu/RVO2/), [RVO2 C# documentation](https://gamma-web.iacs.umd.edu/RVO2/documentation/cs-2.0/), [canonical RVO2](https://github.com/snape/RVO2) và [official RVO2-CS](https://github.com/snape/RVO2-CS). NuGet audit xác nhận không có package `RVOCS` công khai; chọn official tag v2.0.1 source-compatible với Unity và .NET 10, không tự viết lại ORCA.
- Đối chiếu rule.md/task.md: Thực hiện WIP=1 theo Task A audit → B adapter → C parameter/bounds → D preferred velocity → E actual velocity/fallback → R1–R8; Contracts chỉ chứa config serializable, Simulation sở hữu movement/avoidance, Desktop/UI không sở hữu thuật toán; không mở DOTS/ECS/Burst/Jobs, Social/Memory/Emotion/Animation/Queue, A*/Utility/Population rewrite hoặc feature ngoài task.
- Trạng thái: Đạt. R1 head-on và R2 crossing đều tránh severe overlap và tới đích; R3 đám đông vẫn tiến triển; R4 static geometry, R5 shelf arrival, R6 full journey, R7 no-neighbor/failure fallback đều pass. R8 được lưu tại `docs/benchmarks/rvo2-2026-08-18.json`: 50/100/200 NPC lần lượt 0.268/0.610/3.186 ms mỗi tick, 0 severe collision pair-tick, 50/968/3783 overlap pair-tick, 50/96/189 agent tiến triển và geometry safe.
- Kiểm tra: `dotnet build AIsle.slnx -c Release --no-restore` thành công 0 warning/0 error; Population, Simulation S4/S8 + R1–R7, Results S5/S6 và Desktop S1–S7 verification đều pass; benchmark correctness pass cả legacy 200/500/1000 và R8 50/100/200; JavaScript regression 10/10 pass; bảy file RVO2 vendored có SHA-256 trùng upstream tag; `git diff --check` pass. Máy không cài Unity Editor nên không chạy Unity batchmode; source v2.0.1 được đặt trực tiếp trong UPM package và .NET compile xác nhận không cần assembly ngoài.
- Nên làm tiếp theo: Review trực quan một phiên RUN LIVE đông NPC trong lối đi; nếu thay parameter chỉ dùng ranges đã audit và chạy lại R1–R8. Dừng tại phạm vi `run.txt`, không tự mở task mới.
- Phạm vi đồng bộ: Chỉ local trên nhánh `develop`; chưa commit, chưa push. Giữ nguyên file task `docs/run.txt` do chủ dự án cung cấp và không sửa `docs/log_sim.md`.

## 2026-08-18 10:53 (UTC+07:00) — Codex — Đã đồng bộ RVO2 lên develop

- Đã làm: Commit toàn bộ batch Local Crowd Avoidance bằng commit `5a18580` (`feat: integrate RVO2 local crowd avoidance`) và push fast-forward từ `c6b0442` lên `origin/develop`.
- Kiểm soát conflict: Fetch trước khi kiểm thử và ngay trước push xác nhận remote không có commit mới; local/remote trước push `1/0`; không merge, không rebase và không force-push. Lần push đầu bị hủy tại hộp thoại xác thực, sau khi xác thực lại thì push thành công và không làm thay đổi lịch sử.
- Kiểm tra: Release build 0 warning/0 error; Desktop, Results, Simulation, Population và benchmark correctness đều pass; 10/10 JavaScript regression test pass; RVO2 collision/geometry gates đạt.
- Phạm vi: Chỉ repository `D:\Big\KADA\test`, nhánh `develop`; không chạm `main` hoặc `D:\Big\KADA\store\main`.

## 2026-08-19 11:16 (UTC+07:00) — Codex — Task 9 Pixel NPC Renderer

- Lý do sửa: Chủ dự án yêu cầu thực hiện `docs/task_9.md`, thay dot NPC trong Canvas Live/Replay bằng sprite pixel 8 hướng × 4 walking frame, giữ nguyên Simulation/Contracts/result semantics và không thêm dependency.
- File bị tác động: Move/normalize đúng bốn nguồn `asset/npc_0.png` → `asset/npc_3.png` vào `src/AIsle.DesktopApp/UI/assets/npc/`; thêm `src/AIsle.DesktopApp/UI/npc-renderer.mjs` và `tests/AIsle.DesktopApp.Tests/npc-renderer.test.mjs`; tích hợp renderer tại `web/app.js`; cập nhật asset packaging/verification tại `src/AIsle.DesktopApp/AIsle.DesktopApp.csproj`, `src/AIsle.DesktopApp/Infrastructure/LocalUiAssets.cs`, `tests/AIsle.DesktopApp.Tests/Program.cs`; chỉ chuẩn hóa path project và whitespace trong task do chủ dự án đã sửa. Không sửa Simulation, Contracts, Population, Decision, Navigation, RVO2, Results, Mobile, Unity hoặc backend.
- Đã làm: Chuẩn hóa mỗi sheet từ cell nguồn có padding 192×128 thành sheet runtime 128×384 với frame 32×48, transparent và foot anchor đồng nhất; registry bốn asset; model selection FNV-1a theo `runSeed+npcId`; direction duy nhất `S,SW,W,NW,N,NE,E,SE` từ delta position; stationary giữ hướng; shared RAF clock 8 FPS theo loop `0→1→2→3→0`; freeze khi pause; clear state khi reset; sort world Y; một `drawImage`/NPC; `imageSmoothingEnabled=false`; selected ring; fallback dot an toàn và warning một lần khi asset lỗi. Live và deterministic seek/replay dùng cùng một renderer/state resolver.
- Source/dependency: Áp dụng trực tiếp Canvas `drawImage`/RAF theo contract trong `task_9.md`; không thêm package, DOM-per-NPC, timer-per-NPC, scene graph, animation framework hoặc thuật toán Simulation mới.
- Verification: R1–R10 pass; `dotnet build AIsle.slnx -c Release --no-restore` 0 warning/0 error; Desktop, Population, Simulation, Results và 10/10 JavaScript regression pass; self-contained `scripts/build-release.ps1` thành công; release có `UI/npc-renderer.mjs` cùng đủ `npc_0.png` → `npc_3.png`; release `--qa-smoke` exit 0; `run-desktop.bat` mở `AIsleDesktop` responding và WebView2 khởi tạo, không còn lỗi khóa executable. `git diff --check` pass sau khi bỏ whitespace ở dòng project root của task.
- Benchmark Canvas 120 frame: 200 NPC avg 0.353 ms, p95 0.667 ms, 2835.3 FPS, memory delta +1.34 MB; 500 NPC avg 0.290 ms, p95 0.590 ms, 3445.3 FPS, memory delta -0.82 MB; 1000 NPC avg 0.515 ms, p95 1.211 ms, 1941.2 FPS, memory delta -0.15 MB; drawCalls/state đúng bằng số NPC. Không có lý do mở dependency renderer mới.
- Trạng thái: Implementation và automated/release gate đạt; manual visual M1–M7 cần chủ dự án xác nhận trên cửa sổ Desktop đang mở. Asset gate còn một blocker dữ liệu: bốn file nguồn `npc_0..3` ban đầu có cùng SHA-256 `2F0826...`, nên registry/model assignment có đủ bốn model logic nhưng bốn hình hiện giống hệt nhau. Không tự tạo/recolor sheet vì mục 7.1 cấm tạo sprite sheet mới.
- Việc tiếp theo: Cung cấp ba sprite sheet nguồn thực sự khác nhau cho `npc_1.png` → `npc_3.png` nếu yêu cầu bốn nhân vật nhìn khác nhau; sau đó chỉ normalize/copy lại và chạy R1–R10 + manual M1–M7. Trên app đang mở, bấm RUN LIVE rồi kiểm tra facing, pause/reset, seek/replay và crowd 200 NPC.
- Phạm vi đồng bộ: Chỉ local trên `develop`; chưa stage, chưa commit, chưa push. Tuân theo `AGENTS.md` nên chỉ append log active `docs/log.md`, không sửa `docs/log_frontend.md` lịch sử. Giữ nguyên các asset untracked khác của chủ dự án.

## 2026-08-19 12:19 (UTC+07:00) — Codex — Chuẩn bị đồng bộ Task 9 không kèm thư mục asset

- Lý do: Chủ dự án yêu cầu đẩy code hoàn chỉnh tại `D:\Big\KADA\test` lên `origin/develop`, không được đẩy thư mục gốc `asset/` và phải loại thư mục này khỏi GitHub nếu đang tồn tại.
- Đã làm: Fetch xác nhận local/remote `0/0`; bỏ toàn bộ năm file `asset/` đang được track khỏi Git index; thêm `/asset/` vào `.gitignore`; giữ nguyên 14 file ảnh local trong thư mục này. Asset runtime đã chuẩn hóa tại `src/AIsle.DesktopApp/UI/assets/` vẫn thuộc code sản phẩm và được giữ lại.
- Kiểm tra: Release build đạt 0 warning/0 error; Desktop, Results, Simulation và Population verification pass; toàn bộ 11 JavaScript test pass, gồm renderer R1–R10 và benchmark 200/500/1000 NPC.
- Trạng thái: Sẵn sàng commit và push theo fast-forward; không force-push, không chạm nhánh `main`.

## 2026-08-19 12:20 (UTC+07:00) — Codex — Đã đồng bộ Task 9 và loại asset khỏi develop

- Đã làm: Commit `4d34279` (`feat: add pixel NPC renderer and exclude source assets`) và push fast-forward từ `f56d35b` lên `origin/develop`. Năm file từng được Git track trong thư mục gốc `asset/` đã bị xóa khỏi nhánh; `/asset/` được ignore để không tái xuất hiện trong các lần push sau.
- Bảo toàn local: 14 file ảnh nguồn trong `D:\Big\KADA\test\asset` vẫn còn trên ổ đĩa và không được Git track. Bốn sprite runtime tại `src/AIsle.DesktopApp/UI/assets/npc/` là thành phần ứng dụng, không thuộc thư mục gốc bị loại.
- Kiểm soát conflict: Fetch ngay trước commit và ngay trước push xác nhận remote không có commit mới; push fast-forward, không merge, không rebase, không force-push.
- Trạng thái: Thành công; tiếp tục commit riêng mục nhật ký này rồi xác nhận local/remote đồng bộ `0/0`.
