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
