# AIsle — TASK 10: Shelf Interaction Slots, Reservation & Queue

> **Project root:** `D:\Big\KADA\test`  
> **Task type:** SYSTEM → UI/UX Integration  
> **WIP:** 1 — thực hiện tuần tự, không làm song song SYSTEM/UI  
> **Priority:** P1  
> **Depends On:** S4/S8 Simulation, RVO2 Local Crowd Avoidance, Task 9 Pixel NPC Renderer  
> **Target:** .NET 10 + C# Simulation Core + WPF/WebView2 + local HTML/CSS/JS  
> **Main objective:** nhiều NPC có thể tiếp cận cùng một shelf hợp lý, không cùng tranh một access point; hết vị trí thì xếp hàng; NPC đã tới shelf quay mặt vào shelf.

---

# 0. Context

Task 9 đã hoàn tất Pixel NPC Renderer.

Hiện trạng cần cải thiện:

1. Speed control hiện có:

```text
1x
5x
15x
30x
```

Cần thêm:

```text
2x
3x
```

Thành:

```text
1x | 2x | 3x | 5x | 15x | 30x
```

2. NPC đi tới shelf đúng nhưng khi dừng cần quay mặt vào shelf.

3. Shelf hiện dùng access point quá cố định.

Ví dụ một mặt shelf:

```text
┌──────────────────────────────┐
│            SHELF             │
└──────────────────────────────┘
              X
```

Nhiều NPC có thể cùng cố gắng đi tới `X`.

Điều này làm:

- NPC hội tụ về cùng goal;
- RVO2 phải giải collision avoidance cho nhiều agent có preferred velocity gần giống nhau;
- gần shelf có thể xuất hiện cảm giác đùn/đẩy;
- shelf không tận dụng được toàn bộ chiều dài mặt tiếp cận.

Target mới:

```text
         1    2    3    4    5
      ●    ●    ●    ●    ●

┌────────────────────────────────┐
│              SHELF             │
└────────────────────────────────┘
```

NPC có thể sử dụng bất kỳ interaction slot hợp lệ nào.

Nếu toàn bộ slot phù hợp đã occupied/reserved:

```text
join queue
→ wait
→ slot free
→ FIFO promotion
→ reserve
→ approach
→ interact
```

---

# 1. Mini-RFC — Shelf Capacity & Queue

## Problem

Current fixed-access shelf interaction làm nhiều NPC có thể hướng tới cùng một access point.

RVO2 giải local collision avoidance nhưng không sở hữu business semantics:

```text
ai được dùng shelf
slot nào còn trống
ai phải chờ
thứ tự ai được phục vụ tiếp
```

Những semantics này phải thuộc AIsle Simulation Interaction.

## User value

Khi quan sát simulator:

- NPC phân bố tự nhiên hơn quanh shelf;
- nhiều NPC có thể mua hàng đồng thời nếu shelf còn chỗ;
- không tranh cùng một điểm đứng;
- shelf đầy thì NPC xếp hàng;
- hàng chờ có thứ tự;
- NPC mua hàng quay mặt vào shelf;
- crowd movement vẫn dùng RVO2 hiện tại.

## Why now

Task 9 làm NPC trở thành character sprite rõ ràng nên hạn chế fixed access point hiện dễ nhìn thấy hơn.

Core hiện đã có:

```text
A*
preferred velocity
RVO2 / ORCA
shelf dwell
purchase/no-purchase
replan/abandon
```

Không cần rewrite navigation.

## Affected milestone

```text
SYSTEM
└── Simulation / Interaction
    ├── Shelf Interaction Slots
    ├── Slot Reservation
    └── Shelf Queue

UI/UX Integration
└── U4
    ├── 2x / 3x speed controls
    └── shelf-facing sprite override
```

## Affected modules

Primary:

```text
src/AIsle.Simulation/**
tests/AIsle.Simulation.Tests/**
```

Integration only:

```text
src/AIsle.DesktopApp/**
tests/AIsle.DesktopApp.Tests/**
web/**
```

Exact paths phải audit từ repository trước khi sửa.

Không tạo project mới.

## External source/repo

### Existing production dependency — KEEP

Official RVO2-CS:

```text
https://github.com/snape/RVO2-CS
License: Apache-2.0
Purpose: local reciprocal collision avoidance
```

Official RVO2 documentation:

```text
https://gamma-web.iacs.umd.edu/RVO2/documentation/2.0/using.html
https://gamma-web.iacs.umd.edu/RVO2/documentation/cs-2.0/
```

RVO2 tiếp tục nhận:

```text
preferred velocity
→ computes actual collision-avoiding velocity
```

Task 10 không đưa shelf-capacity/queue semantics vào RVO2.

### Design reference only — DO NOT integrate

Menge crowd simulation:

```text
https://gamma-web.iacs.umd.edu/Menge/
https://github.com/MengeCrowdSim/Menge
License: Apache-2.0
```

Chỉ tham khảo separation of concerns:

```text
goal / goal capacity
behavior state
movement / collision avoidance
```

Không vendor Menge.

Không thêm BFSM framework.

Không migrate AIsle sang Menge.

## Added dependency

```text
NONE
```

---

# 2. Task Card

## TASK-10 — Shelf Interaction Slots, Reservation & Queue

**State:** READY  
**Priority:** P1  
**Parent:** SYSTEM → UIUX integration  
**Depends On:** S4, S8, RVO2 integration, Task 9  
**Affected Module:** Simulation Interaction + minimal Desktop/UI integration

### Goal

Thay semantic:

```text
Shelf
→ one/few fixed access points
```

bằng:

```text
Shelf Geometry
      ↓
Accessible Sides
      ↓
Interaction Slots
      ↓
Free / Reserved / Occupied
      ↓
Queue when full
```

Trong khi giữ nguyên:

```text
A*
RVO2
Population
GA
Shopping Decision
Purchase Formula
SimResult schema
Task 9 sprite assets
```

---

# 3. Non-Goals

Task này KHÔNG:

- rewrite A*;
- rewrite RVO2/ORCA;
- thay RVO2 bằng crowd framework khác;
- thêm Unity;
- thêm DOTS/ECS/Burst/Jobs;
- thêm Spatial Hash mới nếu RVO2/current code đã đủ;
- thêm path cache mới;
- thêm generic Smart Object framework;
- thêm generic Queue framework cho toàn app;
- làm checkout queue;
- làm entrance queue;
- làm social groups;
- thêm Emotion/Social AI;
- sửa Population chromosome;
- sửa purchase probability;
- sửa Utility Decision trừ đúng phần chọn access destination sau khi shelf đã được chọn;
- thêm database;
- thay SimResult schema chỉ để lưu slot/queue;
- thêm orientation vào RVO2;
- tạo animation state machine mới;
- thêm dependency chỉ để giảm vài dòng code.

Task 10 chỉ làm:

```text
SHELF interaction capacity
+
SHELF waiting queue
+
minimal visual integration
```

---

# 4. Mandatory Pre-Audit

Trước khi code:

```text
cd D:\Big\KADA\test

git status
git branch --show-current
```

Đọc:

```text
AGENTS.md
docs/rule.md
docs/task.md
docs/task_9.md
docs/log.md
```

Audit source thực tế:

```text
Shelf geometry contract
current access-point generation
SimulationHost interaction flow
NPCRuntimeState
A* / PathGrid boundary
RVO2 adapter boundary
current agent radius
stop tolerance
shelf dwell
targetId projection
Task 9 direction resolver
speed control implementation
```

Phải ghi lại trong implementation log:

```text
current shelf access source
current shelf size fields
current agent radius source
current fixed timestep
current speed multiplier behavior
current interaction states
current RVO2 stopped-agent behavior
```

Không đoán tên field/class.

---

# 5. Architecture Boundary

Bắt buộc giữ:

```text
UI = View + Input
Application = Orchestration
Simulation Core = Behavior
```

Ownership:

```text
Simulation
├── shelf slot generation
├── slot availability
├── reservation
├── queue membership
├── queue order
├── queue promotion
├── access destination
└── release / cleanup

RVO2
└── local collision avoidance only

A*
└── global/path reachability only

Desktop/UI
├── speed buttons
├── render positions/status
└── sprite facing override
```

UI không tự quyết:

```text
slot free?
queue position?
who is first?
who gets slot?
```

---

# 6. Existing Navigation Policy

A* hiện tại:

```text
KEEP CURRENT
```

Không đổi A* chỉ để nhiều slot hơn.

RVO2 hiện tại:

```text
KEEP CURRENT
```

RVO2 tiếp tục pipeline:

```text
A* waypoint / destination
        ↓
preferred velocity
        ↓
RVO2
        ↓
actual velocity
        ↓
walkability / geometry safety
```

Task 10 thay:

```text
destination semantics
```

không thay:

```text
collision avoidance algorithm
```

---

# 7. Shelf Interaction Model

Mỗi rectangular shelf có tối đa 4 side:

```text
NORTH
EAST
SOUTH
WEST
```

Concept:

```text
           N slots
      ● ● ● ● ● ● ●
    ┌───────────────┐
 W  ●               ●  E
    ●     SHELF     ●
    ●               ●
    └───────────────┘
      ● ● ● ● ● ● ●
           S slots
```

Một side chỉ usable nếu candidate slots của side đó:

- nằm ngoài shelf geometry;
- walkable;
- không nằm trong wall;
- không nằm trong shelf;
- có route hợp lệ;
- không vi phạm geometry invariant.

Không mặc định cả 4 mặt đều usable.

---

# 8. Interaction Slot Data

Tạo representation nhỏ, internal Simulation.

Conceptual:

```text
ShelfInteractionSlot
{
    ShelfId
    Side
    Index
    Position
    FacingVector
    State
    OwnerNpcId?
}
```

State tối thiểu:

```text
FREE
RESERVED
OCCUPIED
```

Không cần generic state machine framework.

Không expose class này thành public cross-layer DTO nếu không thực sự cần.

---

# 9. Dynamic Slot Generation

## 9.1 Không hard-code 5 điểm

Không làm:

```text
shelf A always has 5 points
```

Slot count phải derive từ:

```text
shelf geometry
+
agent physical footprint
```

## 9.2 Reuse existing radius

Audit current agent/RVO radius.

Ưu tiên:

```text
existing collision radius
```

Không tạo một second independent NPC radius nếu current config đã có.

Minimum center spacing:

```text
>= 2 × effectiveAgentRadius
```

Nếu current code đã có safe clearance/stop tolerance phù hợp:

```text
reuse
```

Không thêm config public mới chỉ vì tiện.

## 9.3 Corner padding

Không đặt slot ngay sát shelf corner.

Recommended derivation:

```text
cornerPadding >= effectiveAgentRadius
```

Không hard-code theo pixel UI.

Simulation dùng world units.

## 9.4 Interaction offset

Slot phải nằm phía ngoài shelf edge.

Concept:

```text
slotOffset
≈ effectiveAgentRadius
+ existing safe tolerance
```

Mục tiêu:

- agent center không nằm trong shelf;
- collision circle không cắt shelf;
- sprite foot anchor đứng ở vị trí hợp lý;
- RVO2/static geometry không liên tục cố đẩy agent ra.

Không dùng sprite width để tính physical radius.

## 9.5 Side sampling

Với mỗi usable side:

```text
usableLength =
    sideLength
    - 2 × cornerPadding
```

Từ đó sinh các slot center-to-center đều nhau.

Không yêu cầu slot phải nằm đúng các điểm chia cứng.

Nếu side chỉ đủ một slot hợp lệ:

```text
1 slot
```

Nếu không đủ:

```text
0 slot
```

---

# 10. Slot Validation

Mỗi generated candidate phải qua:

```text
V1 — finite coordinates
V2 — outside shelf bounds
V3 — inside valid simulation/world bounds
V4 — walkable
V5 — not inside wall
V6 — no corner cutting requirement preserved
V7 — reachable by current A*
```

Không gọi A* lại mỗi render frame.

Validation/generation nên cache theo static layout lifetime nếu layout không đổi trong run.

Nếu layout thay trước run:

```text
rebuild slots
```

Không tạo global path cache framework.

---

# 11. Shelf Selection vs Slot Selection

Shopping Decision vẫn chọn:

```text
SHELF / PRODUCT TARGET
```

Task 10 không thay Utility/Purchase semantics.

Sau khi shelf đã được chọn:

```text
Shelf target
      ↓
Interaction access resolver
      ↓
slot / queue destination
```

Tách rõ:

```text
"What shelf do I want?"
≠
"Where do I stand at that shelf?"
```

---

# 12. Free Slot Assignment

Khi NPC cần approach shelf:

```text
all valid FREE slots
        ↓
reachable slots
        ↓
rank / select
```

Không pure random toàn bộ shelf.

Ưu tiên:

```text
path cost / travel cost
```

Sau đó tạo variation trong nhóm tốt nhất.

Recommended:

```text
rank by path cost
→ take top K
→ deterministic/per-run random tie selection
```

Default conceptual:

```text
K = min(3, availableCount)
```

Nếu current code có weighted stochastic choice helper phù hợp:

```text
reuse
```

Không viết generic random framework mới.

---

# 13. Reservation

Khi NPC chọn slot:

```text
FREE
↓
RESERVED by NPC-X
```

Ngay sau reservation:

```text
NPC-Y cannot select same slot
```

Reservation phải xảy ra trong Simulation ownership.

Lifecycle:

```text
FREE
  ↓ reserve
RESERVED
  ↓ arrive
OCCUPIED
  ↓ interaction complete / leave
FREE
```

Release reservation nếu:

```text
agent abandons shelf
agent exits
agent becomes invalid
path permanently unreachable
simulation reset
runtime failure invokes safe cleanup
```

Không leak reservation.

Trong cùng một simulation tick:

```text
NPC A reserves slot 2
→ NPC B processed after that sees slot 2 unavailable
```

Không có double-owner.

---

# 14. Occupied Slot Behavior

Khi NPC đã tới slot và đang DWELL/interacting:

```text
preferred velocity = 0
```

Giữ current RVO2 stopped-agent policy.

Nếu current integration đang set stopped agent:

```text
velocity = 0
maxSpeed = 0
```

thì reuse behavior đó.

Mục tiêu:

```text
agent đang mua hàng không bị agent khác đẩy khỏi slot
```

Không teleport agent về slot mỗi tick.

Không positional-force push agent thủ công.

---

# 15. Queue Trigger

Queue chỉ dùng khi:

```text
selected shelf
+
no suitable FREE interaction slot
```

Không join queue nếu shelf còn một free reachable slot phù hợp.

---

# 16. Queue Scope

Task 10 dùng:

```text
per-shelf-side FIFO queue
```

Không generic queue engine toàn app.

Concept:

```text
ShelfRuntime
├── NORTH
│   ├── InteractionSlot[]
│   └── Queue
├── EAST
│   ├── InteractionSlot[]
│   └── Queue
├── SOUTH
│   ├── InteractionSlot[]
│   └── Queue
└── WEST
    ├── InteractionSlot[]
    └── Queue
```

Chỉ tạo side runtime nếu side đó có usable interaction region.

---

# 17. Queue Side Selection

Nếu shelf full:

NPC chọn queue side dựa trên:

```text
reachable side
+
approach/path cost
+
queue length
```

Không chạy vòng vô lý sang side xa chỉ vì random.

Không cần optimization solver.

Simple score/ranking đủ dùng.

---

# 18. Queue Ordering

Rule:

```text
FIFO
```

Ví dụ:

```text
queue:
NPC-17
NPC-23
NPC-41
```

Khi slot free:

```text
NPC-17 gets first eligible slot
```

Không cho NPC mới tới bypass head chỉ vì gần slot hơn, trừ khi head thực sự không thể reach slot đó.

---

# 19. Queue Waiting Positions

Queue không chỉ là logical list; NPC cần điểm đứng.

Tạo queue slots từ selected shelf side.

Concept:

```text
Shelf
┌──────────────┐
└──────────────┘
      ● interaction

      ○ queue 0
      ○ queue 1
      ○ queue 2
```

Queue extends outward theo side normal hoặc placement tương đương phù hợp geometry.

Spacing:

```text
>= 2 × effectiveAgentRadius
```

Mỗi queue point phải:

- walkable;
- không nằm trong shelf;
- không nằm trong wall;
- nằm trong valid bounds;
- reachable.

Không tạo queue point vô hạn.

---

# 20. Queue Slot Reservation

Queue position cũng phải có single owner.

Không để:

```text
2 NPC
→ same queue position
```

Khi front NPC rời queue:

```text
remaining queue indices shift forward logically
```

Agents di chuyển về new waiting position bằng current movement pipeline.

Không teleport.

RVO2 tiếp tục tránh nhau trong lúc tiến queue.

---

# 21. Queue Promotion

Khi interaction slot được release:

```text
slot FREE
+
queue non-empty
        ↓
front eligible NPC
        ↓
RESERVE interaction slot
        ↓
leave queue
        ↓
approach slot via A* / current movement
```

Reservation xảy ra trước khi NPC bắt đầu rời queue.

Nhờ đó NPC khác không tranh slot vừa free.

---

# 22. Internal Runtime State

Ưu tiên internal state nhỏ.

Conceptual phases:

```text
APPROACH_SHELF
APPROACH_QUEUE
WAITING
APPROACH_SLOT
INTERACTING
```

Nhưng:

- không bắt buộc tạo public enum mới;
- không đổi public projection status nếu existing status đủ biểu diễn;
- không đổi SimResult schema;
- không tạo generic FSM.

Có thể dùng internal enum/private fields trong runtime nếu giúp logic rõ ràng.

Public contract change chỉ mở khi audit chứng minh không thể làm đúng nếu thiếu.

---

# 23. Full Shelf Journey

Target flow:

```text
Spawn
  ↓
Decision chooses shelf
  ↓
Resolve interaction access
  ↓
Free reachable slot?
 ┌───────────────┐
YES              NO
 ↓                ↓
Reserve slot    Choose side queue
 ↓                ↓
A* path         Reserve queue position
 ↓                ↓
RVO2             A* + RVO2
 ↓                ↓
Arrive slot     Wait
 ↓                ↓
Interact       Slot released?
 ↓                ↓ YES
Purchase?      Front promoted
 ↓
Release slot
 ↓
Next target / checkout / exit
```

---

# 24. Shelf Facing

Task 9 hiện dùng movement delta để resolve 8-direction sprite.

Khi NPC đang di chuyển:

```text
KEEP current Task 9 direction logic
```

Khi NPC đã tới interaction slot và đang shelf dwell/interacting:

```text
override visual facing
```

Facing vector:

```text
shelfCenter - npcPosition
```

hoặc equivalent side inward vector nếu renderer đã có reliable shelf geometry.

Sau đó quantize bằng mapping Task 9:

```text
S
SW
W
NW
N
NE
E
SE
```

Không thêm orientation vào RVO2.

Không thêm facing field vào NPCProfile.

Không thêm facing field vào SimResult.

---

# 25. Shelf Facing Priority

Direction resolver priority:

```text
1. interacting with shelf
   → face shelf

2. moving
   → movement delta direction

3. stationary non-interacting
   → keep lastDirection
```

Không flicker giữa two directions khi NPC đã đứng tại shelf.

---

# 26. Speed Control Patch

Sau SYSTEM interaction tests pass, mới làm UI integration.

Speed choices:

```text
1x
2x
3x
5x
15x
30x
```

Không thêm slider nếu UI hiện dùng preset buttons/dropdown.

Giữ layout control hiện tại.

---

# 27. Fixed-Step Invariant

Speed multiplier KHÔNG được đổi simulation physics timestep semantics.

Không làm:

```text
1x  → dt = 0.2
30x → dt = 6.0
```

Nếu current system đã dùng fixed-step:

```text
KEEP fixed dt
```

Speed chỉ thay:

```text
how many fixed simulation steps
are processed per real-time interval
```

hoặc equivalent scheduler hiện có.

RVO2 `timeStep` phải tiếp tục khớp simulation fixed-step hiện tại.

Không tune RVO2 timeStep theo UI multiplier.

---

# 28. Speed Result Invariance

Cùng:

```text
project/config
population/run randomness
```

thì speed UI:

```text
1x vs 2x vs 3x vs 5x vs 15x vs 30x
```

không được thay business result chỉ vì tốc độ hiển thị.

Cho phép:

```text
wall-clock execution duration khác
render frame count khác
```

Không cho phép:

```text
simulation time semantics khác
purchase semantics khác
queue order khác chỉ vì UI speed
```

nếu deterministic input giống nhau.

---

# 29. RVO2 Policy

RVO2 là existing production implementation.

Không:

- fork logic;
- sửa ORCA equations;
- thêm custom separation force song song;
- positional push NPC ra nhau;
- tăng radius tùy tiện để che lỗi slot allocation;
- tune `neighborDist/maxNeighbors/timeHorizon` trước khi slot/queue correctness pass.

Nếu vẫn có collision sau Task 10:

```text
1. distinguish visual sprite overlap
2. check physical circle overlap
3. check duplicate target/reservation
4. check queue destination
5. only then audit RVO2 parameters
```

---

# 30. Visual Overlap vs Physical Collision

Task 9 sprite có thể rộng hơn physical collision circle.

Không kết luận RVO2 fail chỉ vì:

```text
hair / clothes sprite pixels overlap
```

Verification phải phân biệt:

```text
Sprite overlap
vs
Physical agent-radius overlap
```

Optional debug overlay development-only:

```text
agent collision circle
interaction slot
reservation owner
queue position
queue order
```

Debug overlay:

```text
OFF by default
```

---

# 31. Smart Object Scope

Task 10 chỉ tạo component/helper tối thiểu cần cho shelf.

Không tạo:

```text
ISmartObject<T>
GenericCapacityManager
GenericQueueEngine
GenericReservationFramework
GenericInteractionGraph
```

trừ khi codebase đã có abstraction tương đương và reuse rõ ràng giảm duplication.

---

# 32. No New Public Configuration by Default

Ưu tiên derive từ existing:

```text
shelf dimensions
agent radius
stop tolerance
world/grid geometry
```

Không thêm ngay:

```text
SlotSpacing
CornerPadding
ShelfQueueSpacing
ShelfCapacity
```

vào user configuration.

Nếu constant nội bộ cần thiết:

- đặt một nơi;
- tên rõ;
- world-unit based;
- documented;
- không magic number rải rác.

---

# 33. Work Order

WIP = 1.

Bắt buộc làm đúng thứ tự.

## T10-A — Audit & Freeze

1. audit current shelf/access flow;
2. audit physical radius;
3. audit fixed timestep/speed;
4. audit RVO2 stopped-agent behavior;
5. freeze regression baseline;
6. document affected files.

## T10-B — Interaction Slot Generation

1. derive four shelf sides;
2. generate slots by side length;
3. apply radius/corner padding;
4. validate geometry;
5. reject blocked/unreachable slots;
6. add deterministic ordering.

## T10-C — Reservation & Assignment

1. FREE/RESERVED/OCCUPIED;
2. unique owner;
3. top-K/nearby free-slot selection;
4. atomic same-tick reservation;
5. release on all exit paths.

## T10-D — Shelf Queue

1. per-side FIFO queue;
2. queue waiting positions;
3. unique queue position;
4. queue movement via A* + RVO2;
5. head promotion;
6. queue compaction;
7. unreachable cleanup.

## T10-E — Crowd/RVO2 Integration

1. interacting NPC stays stationary;
2. waiting NPC stops at queue slot;
3. advancing queue uses normal movement;
4. rerun RVO2 tests;
5. verify no positional separation hack.

## T10-F — UI Integration

Only after SYSTEM gates pass.

1. add 2x;
2. add 3x;
3. preserve 1x/5x/15x/30x;
4. preserve fixed-step;
5. shelf-facing override;
6. keep Task 9 renderer behavior.

## T10-G — Release / Final Verification

1. build Release;
2. run automated suites;
3. run JS tests;
4. run benchmarks;
5. run release/QA smoke if currently available;
6. manual visual review;
7. append active log;
8. STOP.

---

# 34. Automated Verification — Interaction Slots

## I1 — Horizontal shelf

A shelf with enough accessible length:

```text
→ multiple slots on North/South
```

not one center point only.

## I2 — Vertical side

Enough side length:

```text
→ multiple East/West slots
```

## I3 — Short shelf

Short usable side:

```text
→ 0 or 1 valid slot
```

## I4 — Corner padding

No slot center too close to shelf corner.

## I5 — Outside geometry

No slot inside shelf bounds.

## I6 — Blocked side

Wall blocks one face:

```text
blocked face candidates rejected
other accessible faces preserved
```

## I7 — Reachability

Unreachable candidate:

```text
not assignable
```

---

# 35. Automated Verification — Reservation

## R1 — Unique reservation

Two NPC choose same shelf simultaneously:

```text
NPC A reserves slot X
NPC B cannot reserve slot X
```

## R2 — Multiple capacity

Shelf has 5 valid free slots.

Five NPC can occupy:

```text
5 distinct slots
```

## R3 — Sixth NPC

When all 5 reserved/occupied:

```text
NPC 6
→ queue
```

## R4 — Release

NPC completes dwell:

```text
slot becomes FREE
```

## R5 — Abandon

NPC abandons/unreachable:

```text
reservation released
```

## R6 — Reset

Simulation reset:

```text
all reservation/queue runtime state cleared
```

---

# 36. Automated Verification — Queue

## Q1 — FIFO

Arrival order:

```text
A
B
C
```

Promotion order:

```text
A
B
C
```

unless earlier NPC cannot reach any relevant free slot.

## Q2 — Unique waiting positions

No two waiting agents own same queue slot.

## Q3 — Promotion

Interaction slot free:

```text
queue head reserves before moving
```

## Q4 — Queue compact

Head leaves:

```text
remaining members advance logically
```

## Q5 — No bypass

New NPC cannot take released slot while valid older queue head is waiting.

## Q6 — Blocked queue

Invalid queue geometry:

```text
try another usable side
or bounded recovery/abandon
```

No infinite loop.

---

# 37. Automated Verification — RVO2 / Movement

## C1 — Two agents same shelf

No severe physical overlap.

Both eventually:

```text
interact or queue/progress
```

## C2 — Crossing near shelf

Existing RVO2 crossing regression remains pass.

## C3 — Occupied interaction NPC

Agent at shelf:

```text
not pushed away from reserved slot
```

## C4 — Queue line

Waiting agents:

```text
remain near owned queue positions
```

without positional shove hack.

## C5 — Full journey

```text
spawn
→ shelf selection
→ slot/queue
→ dwell
→ purchase/no purchase
→ checkout
→ exit
```

terminates.

---

# 38. Automated Verification — Speed

## S1 — Presets

Exact UI options:

```text
1
2
3
5
15
30
```

## S2 — Fixed dt

Changing multiplier:

```text
does not change Simulation fixed dt
does not change RVO2 timeStep semantics
```

## S3 — Pause/resume

2x/3x:

```text
pause works
resume works
no extra startup tick bug
```

## S4 — Reset

Reset at any multiplier:

```text
returns simulation to T=0 policy
clears shelf runtime state
```

## S5 — Result invariance

For deterministic test input:

```text
1x result
==
30x business result
```

within existing deterministic semantics.

---

# 39. Automated Verification — Facing

## F1 — NPC North of shelf

Interacting NPC north of shelf:

```text
faces South
```

## F2 — NPC South of shelf

```text
faces North
```

## F3 — NPC West of shelf

```text
faces East
```

## F4 — NPC East of shelf

```text
faces West
```

## F5 — No flicker

Stationary interacting NPC:

```text
direction stable across frames
```

---

# 40. Regression Suite

Bắt buộc chạy current repository equivalents của:

```text
dotnet build AIsle.slnx -c Release --no-restore

Population verification
Simulation S4/S8 verification
RVO2 R1–R8 verification
Results S5/S6 verification
Desktop verification
Task 9 renderer tests
JavaScript regression
git diff --check
```

Nếu script/path thực tế khác:

```text
audit
→ run repository's current equivalent
```

Không bỏ test chỉ vì tên cũ không còn.

---

# 41. Manual Verification

Chạy app từ project root bằng launcher chính thức hiện tại.

## M1 — Speed

Confirm:

```text
1x 2x 3x 5x 15x 30x
```

## M2 — One shelf, two NPC

Hai NPC cùng mua shelf:

- không cố đứng cùng một điểm;
- mỗi NPC có slot riêng nếu còn chỗ;
- không bị đẩy khỏi shelf sau khi tới.

## M3 — One long shelf

Nhiều NPC:

```text
phân bố dọc theo mặt shelf
```

không tụ đúng tâm.

## M4 — Four-side shelf

Shelf giữa màn hình, không vật cản:

- NPC có thể tiếp cận nhiều mặt;
- không bắt buộc chỉ một side;
- slot chọn hợp lý theo vị trí/path.

## M5 — Blocked side

Đặt wall sát một side:

- side đó không được dùng;
- các side khác vẫn hoạt động.

## M6 — Full shelf

Nhiều NPC hơn số interaction slots:

- phần dư xếp hàng;
- không cùng lao vào shelf;
- queue head được ưu tiên khi slot free.

## M7 — Facing

NPC đang mua:

```text
quay vào shelf
```

không giữ hướng đi cuối sai.

## M8 — Queue progression

Quan sát:

```text
slot release
→ queue head move
→ next agent advances
```

## M9 — Dense crowd

Review ít nhất:

```text
20+ agents converging on same shelf
```

và một overall dense scenario.

---

# 42. Performance Gate

Run before/after benchmark nếu baseline script còn tồn tại.

Record:

```text
scenario
population
tick average
P95 if available
total runtime
severe overlap
completion/progress
queue max length
```

Overall correctness:

```text
200 NPC
500 NPC
1000 NPC
```

Hotspot:

```text
20 NPC → one shelf
50 NPC → one shelf
100 NPC → one shelf
```

Nếu Task 10 làm tick cost tăng rõ rệt:

1. profile;
2. xác định slot resolution hay A* hay queue update;
3. tránh recompute static slots;
4. optimize measured location only.

Không tự thêm new engine/framework.

---

# 43. Failure / Fallback Rules

## No valid slot on selected shelf

Use current behavior policy:

```text
bounded retry/replan
→ abandon target if required
```

Không đứng vĩnh viễn.

## Slot becomes invalid before arrival

```text
release
→ resolve another slot
→ or queue/replan
```

## Queue side becomes invalid

```text
release queue ownership
→ choose another valid side
→ bounded recovery
```

## RVO2 adapter failure

Reuse existing safe fallback behavior.

Không implement second avoidance system.

---

# 44. SimResult / Replay

Default:

```text
NO SimResult schema change
```

Queue/slot runtime state không cần persist như public result.

Existing trajectory vẫn ghi positions/status theo semantics hiện tại.

Replay:

- không re-simulate queue logic;
- render stored trajectory;
- continue using Task 9 character renderer.

Không mở result schema chỉ để lưu sprite direction.

---

# 45. Desktop Bridge

Không thêm command framework mới.

Speed control:

```text
reuse current speed control/message
```

Interaction slot/queue không cần direct UI command.

UI chỉ quan sát projection.

Không gửi:

```text
reserveSlot
joinQueue
leaveQueue
```

từ JavaScript.

---

# 46. Allowed Paths

Project root:

```text
D:\Big\KADA\test
```

Allowed after audit:

```text
D:\Big\KADA\test\src\AIsle.Simulation\**
D:\Big\KADA\test\src\AIsle.DesktopApp\**
D:\Big\KADA\test\tests\AIsle.Simulation.Tests\**
D:\Big\KADA\test\tests\AIsle.DesktopApp.Tests\**
D:\Big\KADA\test\web\**
D:\Big\KADA\test\docs\task_10.md
D:\Big\KADA\test\docs\log.md
D:\Big\KADA\test\docs\benchmarks\**
```

Only touch actual existing paths needed by current implementation.

---

# 47. Forbidden Paths / Modules

Unless a blocker is documented:

```text
mobile/**
UnityApp/**
backend/**
removed Reality/Video modules
```

Do not change:

```text
Population chromosome
GeneticSharp integration
Math.NET integration
ShoppingDecisionSystem formulas
purchase probability
history persistence schema
KPI definitions
```

Do not modify vendored RVO2 source to implement queue.

Adapter-level integration is allowed only if needed to preserve current stopped/moving state handling.

---

# 48. Dependency Gate

Default:

```text
NO NEW DEPENDENCY
```

Menge:

```text
REFERENCE ONLY
```

RVO2-CS:

```text
REUSE EXISTING
```

No new NuGet/NPM package expected.

---

# 49. Self Review

- [ ] WIP = 1.
- [ ] SYSTEM completed before UI integration.
- [ ] No architecture rewrite.
- [ ] No A* rewrite.
- [ ] No ORCA rewrite.
- [ ] No custom separation force added.
- [ ] No positional shove workaround.
- [ ] No duplicate slot owner.
- [ ] No duplicate queue-slot owner.
- [ ] Reservation always released.
- [ ] Queue is FIFO.
- [ ] No starvation in normal valid scenario.
- [ ] Shelf selection semantics unchanged.
- [ ] Purchase semantics unchanged.
- [ ] Public contract unchanged unless blocker proves necessary.
- [ ] SimResult schema unchanged.
- [ ] Task 9 renderer preserved.
- [ ] No dependency added by default.
- [ ] No generic queue framework.
- [ ] No generic Smart Object framework.
- [ ] No per-frame A* for static slot validation.
- [ ] Fixed simulation dt preserved.
- [ ] 2x and 3x added.
- [ ] Interacting NPC faces shelf.
- [ ] RVO2 regression pass.
- [ ] Performance measured.
- [ ] Existing user changes preserved.
- [ ] Active log appended only.
- [ ] No automatic commit/push.

---

# 50. Definition of Done

Task 10 DONE only when all are true:

## Speed

- [ ] Speed presets are exactly `1x, 2x, 3x, 5x, 15x, 30x`.
- [ ] 2x works.
- [ ] 3x works.
- [ ] Fixed simulation timestep semantics are unchanged.
- [ ] RVO2 timestep semantics are unchanged by UI multiplier.

## Shelf slots

- [ ] Rectangular shelf derives multiple interaction slots where geometry permits.
- [ ] Slot count depends on shelf size/agent footprint, not hard-coded fixed count.
- [ ] Usable sides are detected.
- [ ] Blocked/unreachable slots are rejected.
- [ ] Slots never lie inside shelf/wall geometry.
- [ ] Different NPC can interact simultaneously at distinct slots.

## Reservation

- [ ] Every interaction slot has at most one owner.
- [ ] Reservation happens before approach.
- [ ] Reservation becomes occupied on arrival.
- [ ] Slot releases after interaction.
- [ ] Abandon/error/reset cannot leak reservation.

## Queue

- [ ] Full shelf causes waiting, not target dogpile.
- [ ] Queue is FIFO.
- [ ] Queue positions are physical reachable positions.
- [ ] Queue positions have unique owners.
- [ ] Released slot promotes queue head.
- [ ] Promotion reserves slot before movement.
- [ ] Waiting agents progress without teleport.
- [ ] Invalid queue geometry has bounded fallback.

## Movement

- [ ] Existing A* remains current global navigation.
- [ ] Existing RVO2 remains current local avoidance.
- [ ] No severe physical overlap introduced.
- [ ] Occupied shelf NPC is not pushed away by other agents.
- [ ] No positional separation hack added.
- [ ] Full journey still terminates.

## Visual

- [ ] NPC walking keeps Task 9 direction logic.
- [ ] NPC interacting with shelf faces shelf.
- [ ] Shelf-facing is stable/no flicker.
- [ ] Task 9 Live renderer still works.
- [ ] Replay still works.
- [ ] Pixel sprite assets remain intact.

## QA

- [ ] Automated slot tests pass.
- [ ] Reservation tests pass.
- [ ] Queue tests pass.
- [ ] Speed tests pass.
- [ ] Facing tests pass.
- [ ] Existing Simulation tests pass.
- [ ] Existing RVO2 tests pass.
- [ ] Desktop tests pass.
- [ ] Task 9 renderer tests pass.
- [ ] JavaScript regression pass.
- [ ] Release build pass.
- [ ] Benchmarks recorded.
- [ ] Manual M1–M9 reviewed.
- [ ] `git diff --check` pass.
- [ ] `docs/log.md` appended according to current AGENTS/rule.
- [ ] No unapproved dependency.

---

# 51. Logging

Follow current `AGENTS.md`.

Default active log:

```text
docs/log.md
```

Do not edit old entries.

Append a new entry with:

- date/time;
- actor;
- reason;
- files/modules changed;
- shelf-slot algorithm;
- queue behavior;
- external sources consulted;
- dependency status;
- verification;
- benchmarks;
- manual result;
- status;
- next step;
- local/commit/push scope.

If current AGENTS says another log is authoritative:

```text
follow AGENTS.md
```

---

# 52. Git Rules

Before work:

```text
git status
git branch --show-current
```

Preserve user changes.

Task 10 does NOT automatically:

```text
commit
push
merge
rebase
force-push
```

Only do Git write/sync when the project owner explicitly requests it.

---

# 53. Stop

When Task 10 passes DoD:

```text
DONE
→ STOP
```

Do not automatically open:

- checkout queue;
- entrance queue;
- social group behavior;
- crowd lanes;
- shopping-cart collision;
- dynamic shelf capacity UI;
- queue analytics;
- queue KPI;
- emotion while waiting;
- new RVO2 tuning;
- new navigation algorithm;
- more animation states;
- Task 11.

Any follow-up requires explicit new task.
