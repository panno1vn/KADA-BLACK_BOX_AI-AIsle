# AIsle — ARCHITECTURE & RULES v1

> **Vai trò:** Tài liệu kiến trúc, phạm vi và luật kỹ thuật của AIsle.
>
> **Không chứa roadmap/task execution chi tiết.**
>
> Task triển khai nằm riêng tại: [`task.md`](./task.md).
>
> **Mục tiêu:** giữ AIsle là một Windows Desktop App .NET có NPC simulator, đủ rõ để demo/CV nhưng không phình thành nhiều sản phẩm và nhiều runtime không cần thiết.

---

# 1. Product Direction

## 1.1 Sản phẩm chính

```text
AIsle Windows Desktop App
```

AIsle hiện được xem là một **desktop simulator application**, không phải game và không phải platform microservice.

## 1.2 Stack active

```text
.NET 10 LTS
C#
WPF
WebView2
HTML / CSS / JavaScript cho UI hiện có
GeneticSharp
Math.NET Numerics
```

## 1.3 Core direction

```text
Desktop App
    │
    ▼
Application / Bridge
    │
    ▼
C# Simulation Core
    │
    ├── Population / GA
    ├── Utility Decision
    ├── Navigation
    ├── Interaction
    ├── Purchase / Exit
    └── Result / Replay
```

Node.js **không phải runtime bắt buộc** của desktop product.

---

# 2. Active / Frozen / Removed

## 2.1 ACTIVE

```text
.NET 10
C# Simulation Core
WPF
WebView2
HTML/CSS/JS UI
Population
GeneticSharp
Math.NET
Utility Decision
A*
Interaction
Purchase / Exit
SimResult
History
Replay
KPI
Scenario Compare
```

## 2.2 FROZEN UNTIL AFTER MVP

```text
Mobile companion
Emotion / Affect mở rộng
Animation
Spine
Social AI
Short-Term Memory
ORCA / RVO2
DOTS / ECS
Burst / Jobs
High-density game-style rendering
```

Nếu code cũ của các phần này đã tồn tại:

- không cần xóa ngay;
- không mở rộng;
- không để task MVP mới phụ thuộc vào chúng;
- chỉ cleanup khi có task riêng.

## 2.3 REMOVED FROM PRODUCT ROADMAP

```text
Video Analytics
Reality Analytics
RT-DETR
YOLOX
ByteTrack
Homography
POS Integration
Observation Schema
Sim-to-Real
Calibration từ dữ liệu thật
```

Không tạo task mới cho các phần này.

---

# 3. Scope Control Rules

## 3.1 Rule chống scope creep

Feature mới chỉ được active nếu trả lời đủ:

1. Feature phục vụ milestone nào?
2. Người dùng nhận được giá trị gì?
3. Module nào sở hữu?
4. Có source/library/repo tin cậy nào đã có?
5. Test nào chứng minh đạt?
6. Bỏ feature này thì MVP có bị chặn không?

Nếu câu 6 là **Không**:

```text
BACKLOG / FUTURE
```

Không làm vì lý do:

```text
"sau này có thể cần"
"cho chuyên nghiệp hơn"
"đã tiện code thì thêm luôn"
```

## 3.2 Không rewrite lớn

AIsle đã có nhiều phần hoạt động.

Migration bắt buộc:

```text
Legacy đang chạy
    ↓
Tạo slice thay thế
    ↓
Test slice mới
    ↓
Chuyển caller
    ↓
Regression
    ↓
Mới archive dependency cũ
```

Cấm:

```text
xóa sạch
→ rewrite toàn bộ
→ cuối dự án mới integration test
```

---

# 4. Kiến trúc tối thiểu

Không microservice.

Không tách project chỉ để nhìn "enterprise".

Mục tiêu logic:

```text
src/
├── AIsle.Contracts/
│
├── AIsle.Simulation/
│   ├── Population/
│   ├── Decision/
│   ├── Navigation/
│   ├── Interaction/
│   ├── Runtime/
│   └── Results/
│
└── AIsle.DesktopApp/
    ├── Application/
    ├── Bridge/
    ├── Infrastructure/
    ├── UI/
    └── MainWindow.*
```

Trong MVP không bắt buộc tạo:

```text
AIsle.Application.csproj
AIsle.Infrastructure.csproj
```

Có thể chỉ dùng folder/module trong `AIsle.DesktopApp`.

Chỉ tách project khi có lý do đo được:

- cần reuse cho Mobile/API;
- dependency bắt đầu khó kiểm soát;
- test isolation thực sự có lợi;
- compile/project boundary giúp giảm coupling rõ ràng.

---

# 5. Module Boundaries

# 5.1 AIsle.Contracts

Chỉ chứa:

- DTO;
- schema;
- enum;
- value object chung;
- request/result contract giữa các layer.

Không chứa:

```text
WPF
WebView2
filesystem
HTTP
UI state
database implementation
simulation algorithm
```

Public contract chỉ sửa khi task explicit yêu cầu.

Contract change phải kiểm tra toàn bộ caller trước khi merge.

---

# 5.2 AIsle.Simulation

Sở hữu:

```text
NPCProfile
Population
GA integration
SimulationHost
Utility decision
A*
Movement
Interaction
Purchase
Checkout
Exit
Event trace
Trajectory
SimResult generation
```

Core không được biết:

```text
Window
Button
WebView2
HTML
CSS
JavaScript
Mobile
HttpContext
File dialog
Toast
```

Core:

```text
input data
→ simulation
→ output data
```

---

# 5.3 Desktop Application / Backend Logic

Trong kiến trúc mới, từ **backend** không đồng nghĩa với server Node.js.

Backend active là:

```text
AIsle.DesktopApp/Application
+
AIsle.DesktopApp/Infrastructure
```

Sở hữu:

```text
use-case orchestration
project load/save
history persistence
import/export
bridge command handling
error mapping
```

Không chứa:

```text
GA internals
A* internals
Utility formula
NPC behavior rule
HTML/CSS layout
```

Desktop MVP không được cần:

```text
localhost Node server
```

để chạy feature chính.

---

# 5.4 Frontend

Frontend:

```text
WPF shell
+
WebView2
+
HTML/CSS/JS UI
```

Giữ UI Web hiện tại để tận dụng tài sản đã có.

Không rewrite toàn bộ sang XAML trong MVP.

Frontend được làm:

- visual layout;
- toolbar/sidebar;
- form;
- chart;
- map visualization;
- replay visualization;
- loading/error/empty state;
- nhận user input;
- gửi command sang C#;
- render result từ C#.

Frontend không được làm:

```text
A*
GA
Poisson spawn
simulation tick
purchase rule
NPC utility formula
history persistence trực tiếp
tự sửa runtime JSON
business logic riêng trong JS
```

Rule:

```text
UI = View + Input
Application = Orchestration
Core = Behavior
```

---

# 5.5 Mobile

Mobile là **companion read-only**, không phải simulator thứ hai.

Khi được mở sau MVP:

```text
Mobile
   │
   ▼
ASP.NET Core Read-only API
   │
   ▼
Application / Result Store
```

Mobile được phép:

```text
Dashboard
History
KPI
Simulation Result
Scenario Comparison
```

Không được phép trong Mobile MVP:

```text
Run Simulation
Edit Layout
Generate Population
Tune Algorithm
Write Simulator State
```

---

# 6. WebView2 Bridge

Ưu tiên UI local.

```text
Local HTML/CSS/JS
        │
        ▼
WebView2
        │ postMessage
        ▼
C# Host
        │
        ▼
Application / Core
```

UI gửi qua:

```text
window.chrome.webview.postMessage(...)
```

C# nhận qua WebView2 `WebMessageReceived`.

## 6.1 Command contract

Request:

```text
requestId
type
payload
```

Response:

```text
requestId
ok
payload
error
```

Không tạo event framework lớn nếu command/response đơn giản đủ dùng.

## 6.2 Local assets

Ưu tiên WebView2 Virtual Host Mapping hoặc local content mechanism.

Không chạy HTTP server nội bộ chỉ để serve static HTML/CSS/JS.

---

# 7. Source-first Policy

## 7.1 Thứ tự ưu tiên

1. Official documentation.
2. Original repository của tác giả.
3. Maintained/vetted OSS library.
4. Paper/reference implementation.
5. Stack Overflow để debug/tìm hướng.
6. Tự viết khi các lựa chọn trên không đáp ứng.

Stack Overflow không phải source of truth cho architecture.

## 7.2 Khi thêm dependency

Phải ghi:

```text
Tên
Link
License
Mục đích
Module sử dụng
Lý do code hiện tại không đủ
Integration test
```

Không thêm package chỉ để giảm vài dòng code.

Không tự viết generic algorithm nếu vetted implementation đã đủ.

---

# 8. Approved Technical Sources

## 8.1 .NET

Official support/release:

https://learn.microsoft.com/en-us/dotnet/core/releases-and-support

## 8.2 WPF

https://learn.microsoft.com/en-us/dotnet/desktop/wpf/

## 8.3 WebView2

WPF:

https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/wpf

Local content:

https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/working-with-local-content

Official samples:

https://github.com/MicrosoftEdge/WebView2Samples

## 8.4 Genetic Algorithm

GeneticSharp:

https://github.com/giacomelli/GeneticSharp

AIsle chỉ sở hữu:

```text
NPC chromosome/domain mapping
AIsle fitness
config adapter
output mapping
validation
tests
```

Không tự viết lại:

```text
generic population engine
generic selection
generic crossover
generic mutation framework
```

## 8.5 Math / Statistics

Math.NET Numerics:

https://github.com/mathnet/mathnet-numerics

Dùng khi cần:

- mean;
- median;
- standard deviation;
- percentile;
- distribution/statistical primitive.

## 8.6 A*

Reference candidate:

https://github.com/roy-t/AStar

AIsle đã có A* và regression tests.

Do đó:

> Không thay A* chỉ vì có library.

Chỉ thay nếu:

```text
correctness bug
maintenance cost cao
benchmark fail
missing required capability
```

Nếu implementation hiện tại pass:

```text
KEEP CURRENT
```

---

# 9. Simulator MVP Theory

Không xây "human AI hoàn chỉnh".

Pipeline:

```text
Profile
   ↓
Available Candidates
   ↓
Reachability Filter
   ↓
Utility Score
   ↓
Choice
   ↓
Navigation
   ↓
Interaction
   ↓
Purchase / No Purchase
   ↓
Checkout / Exit
   ↓
Result
```

---

# 9.1 Population

Mục tiêu:

Tạo population có variation hợp lệ trong các thuộc tính hành vi cần cho simulator.

Dùng:

```text
GeneticSharp
Math.NET
```

Không yêu cầu psychology sâu.

---

# 9.2 Utility Decision

MVP:

```text
candidate
→ hard validity/reachability
→ utility score
→ choice
```

Không cần:

```text
Behaviour Tree framework
GOAP
LLM
new Emotion framework
```

---

# 9.3 Smart Object

Shelf/object expose dữ liệu để NPC đánh giá.

Không hard-code:

```text
if shelfA ...
else if shelfB ...
```

Nhưng cũng không mở rộng thành một Smart Object framework tổng quát lớn.

---

# 9.4 Navigation

Global navigation:

```text
A*
```

Invariants:

- không xuyên wall;
- không corner-cut qua blocked cell;
- unreachable được xử lý;
- replan có giới hạn;
- abandon target nếu không tiếp cận được.

Local crowd realism:

```text
NOT MVP
```

ORCA/RVO2 chỉ mở khi business result hoặc demo bị ảnh hưởng thực.

---

# 9.5 Spawn

Giữ Poisson spawn nếu current baseline đã pass.

Test:

```text
statistical tolerance
```

Không:

```text
exact random fingerprint
custom random framework
```

Nếu cần distribution primitive:

```text
Math.NET
```

---

# 10. Emotion / Affect

Trạng thái:

```text
FROZEN
```

Nếu baseline đang dùng Affect:

- giữ để tránh regression;
- không mở UI mới;
- không thêm emotional state;
- không thêm Peak-End feature mới;
- không thêm animation;
- không để Emotion trở thành DoD của MVP.

---

# 11. Animation

Trạng thái:

```text
FUTURE
```

Không Spine trong MVP.

Không sprite state machine.

Không animation bridge.

Visual MVP có thể chỉ là:

```text
dot
circle
simple icon
status color
trajectory
```

---

# 12. Performance Rule

Không tối ưu theo cảm giác.

Không thêm ngay:

```text
DOTS
ECS
Burst
Jobs
Spatial Hash
Path Cache
ORCA
parallel runtime
```

chỉ để "scale sau này".

## 12.1 Performance Gate

Benchmark trước:

```text
200 NPC
500 NPC
1000 NPC
```

Ghi:

```text
total runtime
memory
tick cost
result correctness
```

Optimization task chỉ mở khi benchmark chứng minh vấn đề thật.

Mọi optimization phải giữ:

```text
correctness tests
behavior invariants
result contracts
```

---

# 13. Scaling Path

Kiến trúc vẫn scale mà không cần over-engineer.

## Stage A — MVP

```text
WPF + WebView2
→ in-process Application
→ C# Simulation
→ local JSON
```

## Stage B — History lớn

Nếu JSON thực sự không đủ:

```text
JSON
→ SQLite / database adapter
```

Core không đổi.

## Stage C — Mobile

```text
ASP.NET Core API
→ Application
→ Result Store
```

Simulation Core không đổi.

## Stage D — Compute lớn

Chỉ khi benchmark chứng minh cần:

```text
extract Simulation worker/process
```

Không dựng distributed architecture trước khi có nhu cầu.

---

# 14. Definition of MVP

MVP hoàn thành khi user làm được:

```text
1. Open/Create project
2. Edit/load layout
3. Configure population
4. Run simulation
5. Observe simple NPC movement/status
6. Finish run
7. Save result
8. Replay
9. View KPI
10. Compare two scenarios
```

Không cần:

```text
Emotion expansion
Animation
Spine
Unity
Node runtime
Video
Reality
Sim-to-Real
POS
Social AI
ORCA
DOTS
Mobile control
Cloud
Microservice
LLM
```

---

# 15. Scope Change Process

Feature mới phải có mini-RFC:

```markdown
# RFC-XXX

Problem:
User value:
Why now:
Affected milestone:
Affected modules:
External source/repo:
Added dependency:
Tests:
What existing scope is removed/replaced:
```

Rule:

> Feature mới phải được phê duyệt explicit trước khi trở thành task.

Không có:

```text
"thêm luôn cũng được"
```

---

# 16. AI Coding Agent Rules

Mọi agent phải đọc:

```text
docs/rule.md
docs/task.md
task hiện tại
log tương ứng
git status
```

Agent không được:

- tự mở task tiếp;
- tự mở milestone tiếp;
- tự thêm dependency;
- tự đổi framework;
- tự refactor architecture;
- tự thêm future feature;
- sửa log cũ;
- sửa file ngoài allowed paths nếu chưa báo blocker;
- tự commit/push nếu task không cho phép.

Prompt chuẩn:

```text
Đọc `docs/rule.md` và `docs/task.md`.
Chỉ thực hiện task <TASK-ID>.
Không mở rộng scope.
Không tự bắt đầu task kế tiếp.
Không thêm dependency ngoài danh sách task.
Giữ module boundaries.
Chạy toàn bộ verification của task.
Append log.
```

---

# 17. Git Rules

Mô hình:

```text
main
└── stable/demo

test
└── integration

task/<TASK-ID>-<short-name>
└── work branch
```

Rules:

- không direct push `main`;
- không force-push `main`;
- không orphan history;
- task branch tạo từ `test` mới nhất;
- một branch = một task;
- không tiện tay refactor ngoài task;
- trước merge chạy `git diff --check`;
- log append-only.

Flow:

```text
task/*
  ↓
test
  ↓
milestone verification
  ↓
main
```

Không cần `develop`.

---

# 18. One-line Architecture

```text
WPF/WebView2 UI → C# Application/Bridge → C# Simulation Core → Local Result Storage
```

Đây là architecture active cho tới khi một RFC được chấp nhận để thay đổi nó.
