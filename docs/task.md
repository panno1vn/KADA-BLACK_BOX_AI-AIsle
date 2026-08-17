# AIsle — SOLO EXECUTION TASKS v2

> **Mô hình:** Solo Developer  
> **Cách tổ chức task:** 2 nhánh cha duy nhất:
>
> 1. **SYSTEM** — toàn bộ phần không phải UI/UX.
> 2. **UI/UX** — toàn bộ trải nghiệm và hiển thị cho người dùng.
>
> Kiến trúc/rules/source chính nằm tại [`rule.md`](./rule.md).

---

# 0. Cách đọc file này

Cấu trúc task:

```text
AIsle
│
├── A. SYSTEM
│   ├── S0 — Architecture
│   ├── S1 — Desktop Foundation
│   ├── S2 — Project / Layout
│   ├── S3 — Population
│   ├── S4 — Simulation
│   ├── S5 — Result / History / Replay Data
│   ├── S6 — KPI / Compare Logic
│   └── S7 — Release / QA
│
└── B. UI/UX
    ├── U0 — UX Foundation
    ├── U1 — Desktop Shell
    ├── U2 — Project / Layout UI
    ├── U3 — Population UI
    ├── U4 — Simulation UI
    ├── U5 — Result / History / Replay UI
    ├── U6 — KPI / Compare UI
    └── U7 — Portfolio Polish
```

Không có Backend track riêng.

Không có Core track riêng.

Không có Mobile track trong MVP.

Không có Unity track.

Không có Reality / Sim-to-Real track.

---

# 1. Quy tắc Solo bắt buộc

## 1.1 WIP = 1

Tại mọi thời điểm:

```text
1 task IN_PROGRESS
```

Không làm song song SYSTEM và UI/UX.

---

## 1.2 Thứ tự trong mỗi feature

Một feature luôn đi theo:

```text
SYSTEM
   ↓
Contract / Logic / Data
   ↓
Tests
   ↓
UI/UX
   ↓
Integration
   ↓
Manual Verify
   ↓
DONE
```

Ví dụ:

```text
S3.1 Population Contract
S3.2 Population Generate
S3.3 Population Test
        ↓
U3.1 Population Form
U3.2 Population Summary
        ↓
M3 VERIFY
```

Không làm UI trước khi contract tương ứng đủ ổn.

---

# 2. Task State

```text
BACKLOG
   ↓
READY
   ↓
IN_PROGRESS
   ↓
SELF_REVIEW
   ↓
VERIFY
   ↓
DONE
```

Hoặc:

```text
IN_PROGRESS
   ↓
BLOCKED
```

Chỉ task `READY` mới được bắt đầu.

---

# 3. Priority

```text
P0 = blocker
P1 = bắt buộc cho MVP
P2 = polish
P3 = future
```

Solo developer chỉ active:

```text
P0
→ P1
```

P2 chỉ làm sau khi feature chính chạy.

P3 không được mở trong MVP.

---

# 4. Task Card chuẩn

```markdown
## <TASK-ID> — <Tên>

State: READY
Priority: P1
Parent: SYSTEM / UIUX
Depends On:
Affected Module:

### Goal
...

### Source / Theory
...

### Allowed Paths
...

### Forbidden Paths
...

### Work
1. ...
2. ...
3. ...

### Verification
- Automated:
- Regression:
- Manual:

### Self Review
- [ ] Không mở rộng scope.
- [ ] Không duplicate business logic.
- [ ] Không thêm dependency ngoài plan.
- [ ] Không sửa future module.

### Definition of Done
- [ ] ...
- [ ] ...

### Stop
DONE → STOP.
Không tự mở task kế tiếp.
```

---

# A. SYSTEM

> SYSTEM = tất cả phần kỹ thuật không thuộc UI/UX.

SYSTEM bao gồm:

```text
Contracts
Simulation Core
Population
GA
Navigation
Application
Bridge
Persistence
History
Replay Data
KPI Logic
Build
Tests
Performance
Release
```

SYSTEM không bao gồm:

```text
visual layout
typography
color
screen composition
interaction design
chart appearance
animation
UI polish
```

---

# S0 — ARCHITECTURE & BASELINE

## S0 Goal

Trước khi tiếp tục code:

```text
repo có một hướng active duy nhất
+
biết phần nào legacy
+
biết dependency hiện tại
+
baseline tests rõ ràng
```

---

## S0.1 — Repository Classification

**Priority:** P0

### Goal

Phân loại mọi folder/project lớn thành:

```text
ACTIVE
REFERENCE
LEGACY
FROZEN
REMOVED
```

### Work

1. Liệt kê root folders.
2. Liệt kê `.csproj`.
3. Liệt kê entry point.
4. Unity → LEGACY/FROZEN.
5. Web prototype → REFERENCE.
6. Node backend → LEGACY đối với Desktop.
7. Mobile → FROZEN.
8. Reality/Video → REMOVED.
9. C# Core/Desktop → ACTIVE.

### DoD

- [ ] Không xóa code.
- [ ] Không move folder.
- [ ] Có bảng source map.
- [ ] Không còn module không rõ trạng thái.

---

## S0.2 — Dependency Audit

**Depends On:** S0.1

### Goal

Biết Desktop App đang phụ thuộc cái gì.

Target:

```text
Desktop
→ Application/Bridge
→ Simulation
→ Contracts
```

### Audit

Tìm:

```text
Node
localhost
HTTP
backend/server.mjs
Unity dependency
Web API dependency
```

### DoD

- [ ] Có dependency graph.
- [ ] Có danh sách Node dependency.
- [ ] Có danh sách dependency cần thay.
- [ ] Chưa refactor.

---

## S0.3 — Baseline Tests

**Depends On:** S0.2

### Run

```text
dotnet build
Population tests
Simulation tests
web regression nếu còn giữ reference
git diff --check
```

### DoD

- [ ] Baseline status rõ.
- [ ] Failure hiện hữu được ghi lại.
- [ ] Log append.

---

# S1 — DESKTOP FOUNDATION

## S1 Goal

```text
AIsleDesktop.exe
→ chạy độc lập
→ không cần Node.js
→ WebView2 ↔ C# hoạt động
```

Sources:

https://learn.microsoft.com/en-us/microsoft-edge/webview2/get-started/wpf

https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/working-with-local-content

https://github.com/MicrosoftEdge/WebView2Samples

---

## S1.1 — Local UI Hosting

**Priority:** P0

### Goal

WebView2 load HTML/CSS/JS trực tiếp từ local assets.

### Allowed

```text
src/AIsle.DesktopApp/**
UI asset packaging/config
```

### Forbidden

```text
Simulation algorithms
Population algorithms
Mobile
Unity
```

### DoD

- [ ] Desktop mở UI khi Node.js không chạy.
- [ ] CSS/JS load đúng.
- [ ] Không phụ thuộc localhost để load static UI.

---

## S1.2 — Bridge Envelope

**Depends On:** S1.1

### Contract

Request:

```json
{
  "requestId": "...",
  "type": "app.ping",
  "payload": {}
}
```

Response:

```json
{
  "requestId": "...",
  "ok": true,
  "payload": {},
  "error": null
}
```

### DoD

- [ ] JS → C#.
- [ ] C# → JS.
- [ ] requestId match.
- [ ] invalid message không crash.

---

## S1.3 — Remove Desktop Node Boot

**Depends On:** S1.2

### Goal

Desktop không spawn:

```text
node.exe
backend/server.mjs
```

### DoD

- [ ] Desktop chạy không Node.
- [ ] Close Desktop không còn child process.
- [ ] Standalone Web prototype vẫn giữ reference nếu cần.

---

# S2 — PROJECT / LAYOUT SYSTEM

## S2 Goal

Backend logic cho:

```text
Open Project
Save Project
Layout Contract
Validation
```

---

## S2.1 — Freeze Layout Contract

### MVP fields

```text
wall
shelf
entrance
checkout
```

### Rule

Không thêm future field chỉ để "sau này dùng".

### DoD

- [ ] Một schema active.
- [ ] Serialization pass.
- [ ] Contract tests pass.

---

## S2.2 — Project Load Use Case

**Depends On:** S2.1

### Flow

```text
Desktop
→ Application
→ Infrastructure
→ JSON
→ DTO
```

### Tests

```text
valid file
missing file
malformed JSON
invalid schema
```

### DoD

- [ ] Không Node API.
- [ ] Core không trực tiếp đọc filesystem.
- [ ] Error mapping rõ.

---

## S2.3 — Project Save Use Case

**Depends On:** S2.2

### Flow

```text
DTO
→ validation
→ persistence
→ reload
```

### DoD

- [ ] Save bằng C#.
- [ ] JS không tự ghi file.
- [ ] Round-trip pass.

---

## S2.4 — Layout Validation

**Depends On:** S2.3

### Checks

```text
entrance exists
checkout exists
geometry valid
shelf reachability
```

Navigation source:

- current A* first;
- candidate only if current implementation fails:

https://github.com/roy-t/AStar

### DoD

- [ ] Invalid layout xử lý đúng.
- [ ] Warning/error rõ.
- [ ] Không rewrite A* nếu current implementation pass.

---

# S3 — POPULATION SYSTEM

## S3 Goal

```text
PopulationConfig
→ Generate
→ Validate
→ Statistics
→ NPCProfile[]
```

Sources:

https://github.com/giacomelli/GeneticSharp

https://github.com/mathnet/mathnet-numerics

---

## S3.1 — NPCProfile Audit

### Goal

Mỗi field active phải có caller thực.

### Không thêm

```text
emotion trait mới
social trait mới
animation metadata
future-only parameter
```

### DoD

- [ ] Active fields documented.
- [ ] Future unused fields không được mở rộng.

---

## S3.2 — GeneticSharp Boundary Verify

**Depends On:** S3.1

AIsle chỉ sở hữu:

```text
domain chromosome
fitness
config mapping
output mapping
validation
tests
```

GeneticSharp sở hữu generic GA machinery.

### DoD

- [ ] Không custom generic Selection.
- [ ] Không custom generic Crossover.
- [ ] Không custom generic Mutation framework.
- [ ] Tests pass.

---

## S3.3 — Statistics / Validation

**Depends On:** S3.2

Tests:

```text
count
bounds
validation rejection
mean
std
percentile
distribution sanity
serialization
```

Không exact RNG fingerprint.

---

## S3.4 — Population Application Command

**Depends On:** S3.3

Command:

```text
population.generate
```

Input:

```text
PopulationConfig
```

Output:

```text
Profiles
Summary
Validation
```

### DoD

- [ ] Bridge-ready contract.
- [ ] No UI dependency.
- [ ] Tests pass.

---

# S4 — SIMULATION SYSTEM

## S4 Goal

```text
spawn
→ decide
→ navigate
→ interact
→ purchase/no purchase
→ checkout
→ exit
```

Không Emotion expansion.

Không Animation.

Không ORCA.

Không DOTS.

---

## S4.1 — Freeze SimulationConfig

### DoD

- [ ] Chỉ active fields.
- [ ] Default documented.
- [ ] Bounds documented.
- [ ] No future field addition.

---

## S4.2 — Navigation Invariants

**Depends On:** S4.1

Tests:

```text
no wall penetration
no corner cutting
unreachable handling
bounded replan
abandon target
```

Nếu current A* pass:

```text
KEEP CURRENT
```

---

## S4.3 — Full Journey Test

**Depends On:** S4.2

Scenarios:

```text
normal journey
unreachable shelf
blocked target
no purchase
purchase
checkout
exit
```

### DoD

- [ ] Simulation terminates.
- [ ] Result consistent.
- [ ] No geometry violation.

---

## S4.4 — Simulation Commands

**Depends On:** S4.3

Commands:

```text
simulation.start
simulation.pause
simulation.step
simulation.reset
```

### Rule

UI không sở hữu tick logic.

---

## S4.5 — Simulation State Projection

**Depends On:** S4.4

Output tối thiểu cho UI:

```text
time
NPC id
x
y
status
targetId
basic counters
```

### DoD

- [ ] UI không cần đọc internal simulation objects.
- [ ] Projection serializable.
- [ ] No animation state required.

---

# S5 — RESULT / HISTORY / REPLAY SYSTEM

## S5 Goal

```text
Run
→ SimResult
→ Save
→ History
→ Load
→ Replay Data
```

---

## S5.1 — Freeze SimResult MVP

Fields chỉ phục vụ:

```text
summary
events
purchases
trajectory
replay
```

### DoD

- [ ] Schema version.
- [ ] JSON round-trip.
- [ ] No future-only field.

---

## S5.2 — Local History Store

**Depends On:** S5.1

MVP:

```text
JSON files
```

Không database nếu chưa có vấn đề thật.

Tests:

```text
save
list
read
corrupted file
duplicate id policy
```

---

## S5.3 — Replay Projection

**Depends On:** S5.2

Replay lấy từ stored trajectory.

Không rerun simulation để tạo replay.

### DoD

- [ ] Replay data deterministic theo stored result.
- [ ] Restart app vẫn đọc được.

---

# S6 — KPI / COMPARE SYSTEM

## S6 Goal

```text
SimResult
→ KPI
→ Compare 2 Runs
```

MVP candidates:

```text
purchase count
conversion
revenue nếu price có sẵn
shelf visits
dwell time
path length
checkout completion
```

Không dùng:

```text
emotion score
peak-end score
retention prediction
real-video comparison
```

---

## S6.1 — KPI Definitions

Mỗi KPI phải có:

```text
name
definition
unit
source fields
formula
edge cases
```

### DoD

- [ ] Không KPI mơ hồ.
- [ ] Truy vết được về SimResult.

---

## S6.2 — KPI Projection

**Depends On:** S6.1

Tests:

```text
empty run
zero purchase
normal run
multiple shelves
```

---

## S6.3 — Compare Logic

**Depends On:** S6.2

Output:

```text
Run A
Run B
absolute delta
relative delta when valid
```

### DoD

- [ ] Compare từ stored results.
- [ ] Không rerun simulator.
- [ ] UI chỉ render.

---

# S7 — RELEASE / QA SYSTEM

## S7 Goal

Build đủ sạch để:

```text
CV
đồ án
demo doanh nghiệp
máy khác
```

---

## S7.1 — Clean Machine Smoke

Test không có:

```text
Node.js
Unity
repo source
```

Flow:

```text
launch
open project
population
run
history
replay
compare
```

---

## S7.2 — Error Handling Gate

Test:

```text
invalid project
missing entrance
missing checkout
unreachable shelf
corrupted history
WebView2 startup problem
```

---

## S7.3 — Performance Benchmark

Benchmark:

```text
200 NPC
500 NPC
1000 NPC
```

Measure:

```text
runtime
memory
tick cost
correctness
```

Không optimize trước benchmark.

---

## S7.4 — Release Build

### DoD

- [ ] Build sạch.
- [ ] Full regression pass.
- [ ] No Node runtime.
- [ ] No Unity runtime.
- [ ] No Reality dependency.
- [ ] Version identifiable.
- [ ] Demo data included where appropriate.

---

# B. UI/UX

> UI/UX = toàn bộ thứ người dùng nhìn thấy, thao tác và cảm nhận.

UI/UX không được sở hữu simulation/business logic.

UI/UX có thể dùng:

```text
Stitch
Figma
HTML
CSS
JavaScript
WebView2
```

Stitch/Figma dùng để thiết kế/prototype.

UI runtime cuối cùng nằm trong Desktop App.

---

# U0 — UX FOUNDATION

## U0 Goal

Chốt trải nghiệm trước khi làm nhiều màn hình.

Đối tượng:

```text
sinh viên / giảng viên
+
portfolio reviewer
+
doanh nghiệp
```

Phong cách:

```text
modern simulator
clean
Gen-Z
professional
not childish
not enterprise-ERP boring
```

---

## U0.1 — Information Architecture

### Navigation đề xuất

```text
Overview
Project / Layout
Population
Simulation
History / Replay
Compare
```

Không tạo menu cho future feature.

Cấm menu:

```text
Emotion
Social
Video Reality
Calibration
Animation
Mobile
```

### DoD

- [ ] 5–6 navigation destination tối đa.
- [ ] Main user flow rõ.

---

## U0.2 — Design Tokens

Chốt:

```text
spacing
font scale
radius
surface hierarchy
accent usage
success/warning/error
```

Không cần design system enterprise lớn.

### DoD

- [ ] Tokens đủ dùng toàn app.
- [ ] Không mỗi screen một style.

---

## U0.3 — Main User Flow

Flow MVP:

```text
Open Project
→ Layout
→ Population
→ Run
→ Result
→ Replay
→ Compare
```

### DoD

- [ ] User hiểu flow không cần đọc manual dài.
- [ ] Mỗi bước có CTA rõ.

---

# U1 — DESKTOP SHELL

**Requires:** S1 complete

## U1 Goal

Tạo shell thống nhất.

Layout đề xuất:

```text
┌─────────────────────────────────────────────┐
│ Top Bar                                     │
├────────────┬────────────────────────────────┤
│ Sidebar    │                                │
│            │         Main Workspace         │
│            │                                │
├────────────┴────────────────────────────────┤
│ Context / Status / Timeline                 │
└─────────────────────────────────────────────┘
```

---

## U1.1 — Sidebar

Items:

```text
Overview
Layout
Population
Simulation
History
Compare
```

### DoD

- [ ] Active state rõ.
- [ ] Không nested menu sâu.
- [ ] Không future pages.

---

## U1.2 — Top Bar

Chỉ các global action thật sự cần:

```text
Project Name
Save state
App status
```

Không nhét simulator controls global nếu chỉ dùng trong Simulation page.

---

## U1.3 — Global States

Thiết kế:

```text
loading
empty
error
success
disabled
```

### DoD

- [ ] Không dùng alert browser thô cho flow chính.
- [ ] Error có context và action.

---

# U2 — PROJECT / LAYOUT UI

**Requires:** S2 complete

## U2 Goal

User tạo/chỉnh layout dễ hiểu.

---

## U2.1 — Layout Workspace

Bố cục:

```text
Left: tools/object list
Center: map canvas
Right: selected-object inspector
```

Tools MVP:

```text
Select
Wall
Shelf
Entrance
Checkout
```

Không thêm decoration/game asset editor.

---

## U2.2 — Inspector

Chỉ hiện field của object đang chọn.

Rule:

```text
progressive disclosure
```

Không hiển thị mọi parameter cùng lúc.

---

## U2.3 — Validation UX

Phân biệt:

```text
ERROR
→ không thể Run/Save nếu policy yêu cầu

WARNING
→ vẫn có thể tiếp tục
```

Ví dụ:

```text
Missing entrance = error
Unreachable shelf = warning/error theo contract
```

---

## U2.4 — Project Save/Open UX

Cần:

```text
Open
Save
Save status
Unsaved changes indication
```

Không cần project manager phức tạp.

---

# U3 — POPULATION UI

**Requires:** S3 complete

## U3 Goal

Population configuration dễ hiểu nhưng không biến thành parameter laboratory khổng lồ.

---

## U3.1 — Population Form

MVP:

```text
NPC Count
Generation Mode nếu thực sự còn cần
các parameter active
```

Các advanced parameter:

```text
collapsed
advanced section
```

Không đổ tất cả lên màn hình chính.

---

## U3.2 — Population Summary

Hiển thị:

```text
count
key distributions
validation state
small summary chart
```

Không cần dashboard thống kê lớn.

---

## U3.3 — Generate Feedback

States:

```text
ready
generating
success
validation issue
error
```

---

# U4 — SIMULATION UI

**Requires:** S4 complete

## U4 Goal

Simulator là hero visual của app.

---

## U4.1 — Simulation Workspace

Main area chiếm phần lớn màn hình.

Map hiển thị:

```text
wall
shelf
entrance
checkout
NPC dots/icons
```

NPC chưa cần character animation.

---

## U4.2 — Runtime Controls

Controls:

```text
Run
Pause
Step
Reset
Speed
Current Time
```

Nhóm cùng nhau.

Không rải control ở nhiều nơi.

---

## U4.3 — Selected NPC Inspector

MVP chỉ hiện thông tin giải thích được:

```text
NPC id
current status
current target
position
selected decision summary nếu đã có
```

Không thêm Emotion panel.

Không thêm animation state.

---

## U4.4 — Live Metrics

Chỉ vài metric cần quan sát:

```text
active NPC
completed
purchases
elapsed time
```

Không biến simulation view thành dashboard BI.

---

# U5 — HISTORY / REPLAY UI

**Requires:** S5 complete

## U5 Goal

User hiểu một run đã xảy ra như thế nào.

---

## U5.1 — History List

Mỗi run item:

```text
time
scenario/project
population
key result summary
```

Không show toàn bộ raw metadata.

---

## U5.2 — Result Detail

Sections:

```text
Summary
Purchases
Events
Replay
```

Không cần quá nhiều tab.

---

## U5.3 — Replay Timeline

Controls:

```text
Play
Pause
Seek
Speed
Time
```

Map reuse visual style của Simulation.

---

# U6 — KPI / COMPARE UI

**Requires:** S6 complete

## U6 Goal

User chọn 2 run và thấy khác biệt ngay.

---

## U6.1 — Run Selector

```text
Scenario A
Scenario B
```

Không tạo multi-dimensional compare engine trong MVP.

---

## U6.2 — KPI Cards

Mỗi KPI:

```text
A value
B value
delta
unit
```

Không dùng màu xanh/đỏ để ám chỉ tốt/xấu nếu KPI chưa có business interpretation chắc chắn.

---

## U6.3 — Charts

Chỉ thêm chart nếu giúp đọc nhanh hơn card/table.

Ưu tiên tối đa:

```text
bar
line
simple distribution
```

Không chart 3D.

Không visualization decoration-only.

---

# U7 — PORTFOLIO POLISH

**Requires:** S7 core release gate gần hoàn tất

## U7 Goal

App nhìn như một sản phẩm có chủ ý, không giống prototype ghép nhiều màn hình.

---

## U7.1 — Visual Consistency Audit

Check:

```text
spacing
font
button hierarchy
card hierarchy
icon consistency
empty states
loading
error
```

---

## U7.2 — Demo-first Polish

Chỉ polish flow:

```text
Open
→ Layout
→ Population
→ Simulation
→ Replay
→ Compare
```

Không polish screen không xuất hiện trong demo.

---

## U7.3 — Portfolio Screens

Chuẩn bị vài screen đẹp:

```text
Layout Editor
Live Simulation
Replay
Compare
```

Mục tiêu:

```text
5–10 giây nhìn vào
→ hiểu đây là NPC retail simulator
```

---

# 5. MILESTONE CHA-CON

Để dễ theo dõi, toàn bộ roadmap nhìn theo cây:

```text
M0 — Foundation
├── SYSTEM
│   ├── S0.1 Repository Classification
│   ├── S0.2 Dependency Audit
│   └── S0.3 Baseline Tests
└── UI/UX
    ├── U0.1 Information Architecture
    ├── U0.2 Design Tokens
    └── U0.3 Main User Flow

M1 — Desktop Shell
├── SYSTEM
│   ├── S1.1 Local UI Hosting
│   ├── S1.2 Bridge Envelope
│   └── S1.3 Remove Node Boot
└── UI/UX
    ├── U1.1 Sidebar
    ├── U1.2 Top Bar
    └── U1.3 Global States

M2 — Project / Layout
├── SYSTEM
│   ├── S2.1 Layout Contract
│   ├── S2.2 Load
│   ├── S2.3 Save
│   └── S2.4 Validation
└── UI/UX
    ├── U2.1 Layout Workspace
    ├── U2.2 Inspector
    ├── U2.3 Validation UX
    └── U2.4 Save/Open UX

M3 — Population
├── SYSTEM
│   ├── S3.1 NPCProfile Audit
│   ├── S3.2 GeneticSharp Boundary
│   ├── S3.3 Statistics/Validation
│   └── S3.4 Application Command
└── UI/UX
    ├── U3.1 Population Form
    ├── U3.2 Population Summary
    └── U3.3 Generate Feedback

M4 — Simulation
├── SYSTEM
│   ├── S4.1 SimulationConfig
│   ├── S4.2 Navigation
│   ├── S4.3 Full Journey
│   ├── S4.4 Runtime Commands
│   └── S4.5 State Projection
└── UI/UX
    ├── U4.1 Simulation Workspace
    ├── U4.2 Runtime Controls
    ├── U4.3 NPC Inspector
    └── U4.4 Live Metrics

M5 — Result / Replay
├── SYSTEM
│   ├── S5.1 SimResult
│   ├── S5.2 History Store
│   └── S5.3 Replay Projection
└── UI/UX
    ├── U5.1 History List
    ├── U5.2 Result Detail
    └── U5.3 Replay Timeline

M6 — KPI / Compare
├── SYSTEM
│   ├── S6.1 KPI Definitions
│   ├── S6.2 KPI Projection
│   └── S6.3 Compare Logic
└── UI/UX
    ├── U6.1 Run Selector
    ├── U6.2 KPI Cards
    └── U6.3 Charts

M7 — Release
├── SYSTEM
│   ├── S7.1 Clean Machine Smoke
│   ├── S7.2 Error Handling
│   ├── S7.3 Benchmark
│   └── S7.4 Release Build
└── UI/UX
    ├── U7.1 Consistency Audit
    ├── U7.2 Demo Polish
    └── U7.3 Portfolio Screens
```

---

# 6. EXECUTION ORDER THỰC TẾ

Dù file chia 2 nhánh cha, solo developer **không làm hết SYSTEM rồi mới làm hết UI**.

Thứ tự đúng:

```text
M0
S0.1
S0.2
S0.3
U0.1
U0.2
U0.3
VERIFY M0
↓

M1
S1.1
S1.2
S1.3
U1.1
U1.2
U1.3
VERIFY M1
↓

M2
S2.*
→ U2.*
→ VERIFY M2
↓

M3
S3.*
→ U3.*
→ VERIFY M3
↓

...
```

Ý nghĩa:

```text
SYSTEM tạo khả năng
UI/UX expose khả năng
milestone verify toàn flow
```

---

# 7. Milestone Gate

Mỗi milestone chỉ DONE khi:

```text
SYSTEM tasks DONE
+
UI/UX tasks DONE
+
automated tests pass
+
manual end-to-end pass
+
git diff --check
+
log append
```

Không:

```text
"SYSTEM xong 80% nên nhảy milestone"
```

---

# 8. Mobile

Mobile nằm ngoài 2 nhánh active.

Status:

```text
FROZEN / OPTIONAL AFTER M7
```

Nếu mở sau MVP:

```text
M8 — Mobile Companion
├── SYSTEM
│   └── Read-only ASP.NET Core API
└── UI/UX
    └── Dashboard / History / Result / Compare
```

Mobile không được:

```text
run simulator
edit layout
edit population
tune algorithm
```

---

# 9. Future Features

Không xuất hiện trong active tree:

```text
Emotion expansion
Animation
Spine
Social AI
Memory
ORCA/RVO2
DOTS/ECS
Video/Reality
Sim-to-Real
```

Nếu cần sau M7:

```text
RFC
→ approval
→ new milestone
```

---

# 10. CURRENT TASK

Bắt đầu:

```text
S0.1 — Repository Classification
```

Sau đó:

```text
S0.2
S0.3
U0.1
U0.2
U0.3
VERIFY M0
```

Không chạy `run_3` cũ.

Không mở Mobile.

Không làm Emotion/Animation.

Không quay lại Unity.

---

# 11. Prompt chuẩn cho AI Agent

```text
Đọc:
1. docs/rule.md
2. docs/task.md
3. log liên quan
4. git status

CURRENT TASK: <TASK-ID>

Chỉ thực hiện task này.
Không tự mở task kế tiếp.
Không mở milestone kế tiếp.
Không thêm dependency nếu task không cho phép.
Không thay đổi architecture.
Không sửa Future/Frozen/Legacy module nếu task không yêu cầu.
Chạy Verification.
Append log.
Sau khi task đạt Definition of Done thì STOP.
```

---

# 12. Đường chính của MVP

```text
Project
  ↓
Layout
  ↓
Population
  ↓
Simulation
  ↓
Result
  ↓
Replay
  ↓
KPI
  ↓
Compare
```

Hai nhánh cha chỉ tồn tại để hỗ trợ đường chính này:

```text
SYSTEM = làm nó chạy đúng
UI/UX   = làm nó dùng được và trình bày tốt
```

Không tạo nhánh thứ ba nếu chưa có lý do thật sự.
