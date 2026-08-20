# AIsle — TASK 11: Store Visual Fixes, Checkout Queue, App Media & Repository Cleanup

> **Project root:** `D:\Big\KADA\simulator`  
> **Task type:** SYSTEM → UI/UX → Release/Cleanup  
> **Priority:** P1  
> **State:** READY  
> **WIP:** 1  
> **Explicit owner approval:** YES — Task 11 được chủ dự án yêu cầu trực tiếp.  
> **Depends On:** S1–S7 baseline, Task 9 Pixel NPC Renderer, Task 10 Shelf Interaction Slots/Queue, native C# Simulation bridge  
> **Active runtime:** .NET 10 + WPF + WebView2 + local HTML/CSS/JS + C# Simulation Core  
> **No new runtime dependency by default.**

---

# 0. Task Summary

Task 11 xử lý đúng các vấn đề sau:

```text
1. Shelf rotate bug
   → xoay kệ không được làm kệ nhỏ bất thường.

2. Floor rendering bug
   → san.jpg phải lát ổn định, liền mạch.
   → không scale sàn theo object bị kéo ra xa.
   → không có vòng render/tile vô hạn.
   → kéo shelf ra ngoài vùng map không được làm sàn phóng cực lớn / treo UI.

3. NPC visual size
   → tăng nhẹ kích thước sprite NPC để cân với shelf.
   → chỉ đổi visual scale, không tự đổi physical collision radius.

4. Checkout / cashier
   → dùng quay_thu_ngan.png.
   → checkout capacity = 1 service position.
   → một hàng FIFO duy nhất bên trái quầy.
   → không dùng multi-side queue giống shelf.
   → NPC xếp thành một đường thẳng hợp lý.
   → NPC đầu hàng thanh toán, xong thì hàng tiến lên.

5. App icon
   → logo.png trở thành icon ứng dụng Windows.

6. Simulation music
   → music.mp3 phát khi người dùng vào màn hình Mô phỏng bằng hành động click.
   → một audio instance, loop.
   → không tạo dependency audio mới.

7. Asset organization
   → chuẩn hóa asset runtime vào src/AIsle.DesktopApp/UI/assets/.

8. Repository cleanup
   → audit toàn D:\Big\KADA\simulator.
   → chỉ xóa file/folder thật sự không cần sau khi đã chứng minh không còn caller/build/test dependency.
   → không “xóa sạch rồi rewrite”.
```

---

# 1. Source of Truth

Agent bắt buộc đọc theo thứ tự:

```text
1. AGENTS.md
2. docs/rule.md
3. docs/task.md
4. docs/task_11.md
5. docs/log.md
6. log chuyên môn liên quan nếu AGENTS yêu cầu
7. git status
```

`docs/rule.md` là source kiến trúc/phạm vi.

`docs/task.md` là source execution model.

`docs/task_11.md` là task explicit được chủ dự án phê duyệt.

`API_Frontend_Integration.md` chỉ là:

```text
LEGACY / REFERENCE
```

nếu nó còn mô tả:

```text
browser
→ Node backend
→ REST
→ runtime JSON
```

Không được dùng tài liệu legacy này để khôi phục Node.js thành runtime bắt buộc.

Architecture active phải giữ:

```text
WPF/WebView2 UI
        ↓
Application / Bridge
        ↓
C# Simulation Core
        ↓
Local Result Storage
```

---

# 2. RFC-011 — Store Presentation & Checkout Polish

## Problem

Store scene đã có pixel-art asset nhưng đang có các lỗi trực quan và interaction:

- rotate shelf làm scale/size sai;
- floor tiling có thể seam/scale sai;
- object kéo quá xa có thể làm floor/world scale bùng lên và UI treo;
- NPC sprite hơi nhỏ so với shelf;
- checkout chưa có một single-line FIFO queue đúng hình học quầy;
- app chưa dùng logo hiện có làm Windows icon;
- màn Simulation chưa phát background music đã cung cấp;
- asset hiện rải ở nhiều vị trí;
- repository có legacy/generated/duplicate file cần audit cleanup.

## User value

Sau Task 11:

```text
store scene ổn định
+
shelf edit không làm méo kích thước
+
floor không vỡ / không infinity
+
NPC nhìn cân tỉ lệ
+
checkout queue dễ hiểu
+
app có icon riêng
+
màn simulation có nhạc
+
asset tree rõ ràng
+
repository sạch hơn
```

## Why now

Simulation/queue/sprite baseline đã tồn tại.

Task 11 là polish + correction slice trước khi tiếp tục demo/CV/release.

## Affected milestone

```text
M2 — Project / Layout
M4 — Simulation
M7 — Release / Portfolio Polish
```

## Affected modules

```text
src/AIsle.Simulation/**
src/AIsle.DesktopApp/**
src/AIsle.DesktopApp/UI/**
web/**
tests/**
scripts/**
docs/**
```

Chỉ sửa file thực sự có caller.

## External source / repo

Default:

```text
reuse current code
reuse current A*
reuse current RVO2
reuse Task 10 queue concepts where appropriate
use WPF/WebView2/native browser media APIs
```

Không thêm engine/framework mới.

## Added dependency

```text
NONE
```

## Tests

Bắt buộc:

```text
shelf rotation invariants
floor bounded render
drag bounds
checkout FIFO
checkout single service capacity
queue geometry
payment exactly once
NPC visual scale
audio singleton behavior
asset packaging
application icon
full regression
release smoke
cleanup reference audit
```

## Existing scope removed / replaced

Có thể thay:

```text
broken shelf rotate implementation
broken floor tiling implementation
current checkout direct-target behavior
duplicate runtime asset copies
dead/generated/unreferenced files proven unnecessary
```

Không thay:

```text
A*
RVO2
Population/GA
ShoppingDecisionSystem
purchase formula
Task 10 shelf interaction semantics
SimResult schema
History/KPI architecture
```

---

# 3. Mandatory Pre-Audit

Trước mọi sửa đổi:

```bat
cd /d D:\Big\KADA\simulator

git status
git branch --show-current
git remote -v
git log -n 10 --oneline
```

Đọc:

```text
AGENTS.md
docs/rule.md
docs/task.md
docs/log.md
```

Audit:

```text
active Desktop entry point
active WebView2 UI source
asset copy/packaging rules
active shelf renderer
active shelf editor
active floor renderer
active Simulation canvas renderer
Task 9 NPC renderer
Task 10 shelf queue/runtime
checkout target/current checkout journey
current checkout coordinates/dimensions
current A*/RVO2 boundaries
current project layout bounds
current release build script
current tests
```

Không đoán class/file name.

Nếu path trong Task 11 khác source thực tế:

```text
audit
→ use actual active path
→ document difference
```

---

# 4. Git Rules

Current `rule.md` branch policy là authoritative.

Không tự dùng `develop` chỉ vì log cũ từng dùng `develop`.

Expected model:

```text
main
└── stable/demo

test
└── integration

task/TASK-11-store-polish
└── work branch
```

Trước khi tạo/switch branch:

```text
preserve uncommitted user changes
```

Không được mất file local.

Nếu working tree không sạch và branch switch có nguy cơ mất/conflict:

```text
STOP branch mutation
→ document blocker
→ continue only if safe
```

Không:

```text
direct push main
force-push main
orphan history
rebase user work silently
```

Task 11 không tự push.

Commit/push chỉ khi owner explicit yêu cầu.

---

# 5. Architecture Boundary

Ownership:

```text
AIsle.Simulation
├── checkout queue behavior
├── payment service order
├── queue membership
├── queue advancement
├── checkout completion
└── navigation destination semantics

Desktop Application / Bridge
├── orchestration
├── projection
└── lifecycle

WebView2 UI
├── floor rendering
├── shelf visual transform
├── NPC visual scale
├── music playback
└── visual checkout placement

WPF shell/project
├── app icon
└── asset packaging
```

UI không được sở hữu:

```text
checkout FIFO truth
who pays next
payment completion business rule
A*
RVO2
simulation tick
purchase formula
```

---

# 6. Asset Inputs

Current known source assets:

```text
D:\Big\KADA\simulator\src\AIsle.DesktopApp\Assets\san.jpg
D:\Big\KADA\simulator\src\AIsle.DesktopApp\Assets\quay_thu_ngan.png
D:\Big\KADA\simulator\src\AIsle.DesktopApp\UI\assets\asset\logo.png
D:\Big\KADA\simulator\src\AIsle.DesktopApp\UI\assets\asset\music.mp3
```

Current checkout image:

```text
quay_thu_ngan.png
112 × 271 px
portrait aspect ratio
```

Do not assume asset content beyond actual file inspection.

Before move:

```text
verify dimensions
verify format
verify transparency where expected
verify all callers
verify build copy rules
```

---

# 7. Canonical Runtime Asset Tree

Task 11 should converge toward:

```text
src/
└── AIsle.DesktopApp/
    └── UI/
        └── assets/
            ├── brand/
            │   ├── logo.png
            │   └── app.ico
            │
            ├── audio/
            │   └── music.mp3
            │
            ├── npc/
            │   ├── npc_1.png
            │   ├── npc_2.png
            │   ├── npc_3.png
            │   └── npc_4.png
            │
            └── store/
                ├── floor/
                │   └── san.jpg
                │
                ├── shelves/
                │   ├── do_uong.jpg
                │   ├── hang_tuoi_song.png
                │   ├── snack.png
                │   ├── hang_kho_cham_soc_ca_nhan.png
                │   └── hoa_pham.png
                │
                └── fixtures/
                    ├── cua_vao.png
                    ├── quay_thu_ngan.png
                    └── wall.png
```

Nếu repo hiện có thêm asset active:

```text
classify
→ place into nearest existing category
```

Không tạo nhiều cấp folder vô ích.

---

# 8. Asset Migration Rules

Dùng `git mv` cho tracked file khi phù hợp.

Migrate theo slice:

```text
move one asset group
→ update callers
→ build/test
→ continue
```

Không:

```text
move all first
→ sửa path sau
```

Sau migration:

```text
one canonical runtime copy
```

Duplicate copy chỉ được giữ nếu build system thực sự cần physical duplicate và phải document lý do.

Không duplicate chỉ vì “web cần” / “WPF cần” nếu cả hai có thể reference cùng packaged local asset.

---

# 9. Shelf Rotation Bug

## 9.1 Problem

Khi bấm:

```text
Xoay 90°
```

shelf hiện có thể bị nhỏ bất thường.

Task phải tìm root cause.

Không chữa bằng hard-code scale correction theo từng click.

---

# 10. Rotation Geometry Invariants

Đối với 90° rotation của rectangular shelf:

```text
center position unchanged
physical area unchanged
physical long/short dimensions preserved
image aspect not re-derived into smaller world footprint
```

Nếu simulation/layout sử dụng axis-aligned rectangle:

```text
oldW = W
oldH = H

rotate 90:
newW = oldH
newH = oldW
```

Invariants:

```text
newW * newH == oldW * oldH
sorted(newW,newH) == sorted(oldW,oldH)
```

within floating tolerance.

Sau 4 lần rotate:

```text
position == original
width == original
height == original
rotation == original
```

Không cumulative shrink/grow.

---

# 11. Rotation Must Not Reset Category Size

Không làm:

```text
rotate
→ reload category preset
→ recalculate size from rendered image
```

Nếu shelf có preset world dimensions:

```text
preset determines initial physical footprint
```

Rotation chỉ thay orientation.

Image natural pixel size không được thay physical shelf size.

---

# 12. Rotation Boundary Handling

Nếu rotate làm footprint vượt map bounds:

Không:

```text
shrink shelf until fit
```

Allowed:

```text
Option A:
clamp/reposition shelf center minimally to remain in bounds

Option B:
reject rotation and show clear UI warning
```

Preference:

```text
preserve physical size
```

Không giảm scale.

---

# 13. Flip Invariants

Nếu còn:

```text
Flip Horizontal
Flip Vertical
```

thì flip là visual transform.

Không được thay:

```text
world X/Y
world W/H
A* footprint
interaction geometry
```

trừ khi current layout model explicitly defines otherwise.

---

# 14. Shelf Rotation Tests

## ROT-1

One 90° rotate:

```text
W,H → H,W
```

## ROT-2

Two rotates:

```text
180°
→ W,H original pair
```

## ROT-3

Four rotates:

```text
exact original geometry within tolerance
```

## ROT-4

No shrink:

```text
area constant
```

## ROT-5

Flip after rotate:

```text
geometry unchanged
```

## ROT-6

Rotate near boundary:

```text
no shrink
no NaN
no Infinity
no object outside allowed bounds after accepted edit
```

---

# 15. Floor Rendering — Core Rule

Floor represents:

```text
layout world bounds
```

Floor must NOT represent:

```text
bounding box of every object currently dragged
```

Render floor only from:

```text
layout.width
layout.height
camera/view transform
tile world size
```

Object coordinates must not expand floor dimensions.

---

# 16. Floor Texture Contract

Asset:

```text
san.jpg
```

Use stable world-space tile size.

If existing visual contract is:

```text
1 texture tile = 1m × 1m
```

keep it.

Do not dynamically rescale each floor tile based on:

```text
canvas object extents
selected shelf
furthest object
zoom-to-fit of invalid object
```

---

# 17. Floor Tiling

Preferred minimal implementation:

```text
clip to layout rectangle
→ render repeated san.jpg
→ fixed world-space tile size
```

Allowed:

```text
Canvas createPattern
```

or bounded tile loops.

If manual loops used:

```text
tileCountX derived only from layout width
tileCountY derived only from layout height
```

Add explicit finite guards.

Example:

```text
if layout width/height non-finite or <= 0:
    render safe fallback
    report error
```

No loop with upper bound derived from arbitrary object position.

---

# 18. Floor Anti-Infinity Guard

The renderer must never do effectively:

```text
for x = worldMin to farthestDraggedShelfX
```

where farthestDraggedShelfX may be huge.

Floor draw work must be bounded by:

```text
layout dimensions
+
small viewport overscan only
```

Add sanity maximum if useful, but do not hide bad geometry silently.

---

# 19. Drag Bounds

Shelf drag/edit must obey layout geometry.

During drag:

```text
candidate position
→ validate/clamp to layout bounds
→ commit only valid finite position
```

Never allow committed:

```text
NaN
Infinity
absurd coordinates far outside layout
```

If pointer leaves canvas:

```text
keep object at nearest valid boundary
```

or cancel invalid move.

Do not change floor/camera scale to accommodate invalid shelf position.

---

# 20. Camera / Auto-Fit Rule

If current renderer has:

```text
fit-to-content
auto scale
world extents
```

do NOT include invalid/out-of-bounds object coordinates in world bounds.

Preferred:

```text
camera fits layout rectangle
```

not object extents.

Manual zoom/pan can remain if current UI supports it.

---

# 21. Floor Tests

## FLOOR-1

Normal layout:

```text
floor fills entire store
```

## FLOOR-2

Tile continuity:

```text
no visible accidental gaps caused by coordinate rounding
```

## FLOOR-3

Shelf dragged to each boundary:

```text
floor scale unchanged
```

## FLOOR-4

Pointer dragged far outside canvas:

```text
shelf remains/clamps in valid map
floor does not zoom
UI remains responsive
```

## FLOOR-5

Invalid/non-finite layout dimension:

```text
safe error/fallback
no infinite loop
```

## FLOOR-6

Resize window/sidebar:

```text
floor still maps to same world layout
```

---

# 22. NPC Visual Scale

Task 9 sprite renderer remains owner of NPC visual size.

Do not modify:

```text
CollisionRadius
RVO radius
A* geometry
interaction spacing
queue spacing
```

just to make sprite look larger.

---

# 23. NPC Scale Target

Increase NPC sprite visual size slightly.

Target:

```text
NPC_VISUAL_SCALE = 1.12x current
```

If renderer already has a scale constant:

```text
multiply existing draw size by 1.12
```

Do not resize source PNG files.

Keep:

```text
foot anchor
bottom-center world position
direction mapping
4-frame loop
pixel nearest-neighbor
Y sort
selected ring alignment
```

---

# 24. NPC Scale Tests

## NPC-V1

Same NPC world position before/after scale:

```text
foot anchor unchanged
```

## NPC-V2

Visual size:

```text
+12%
```

## NPC-V3

No physics change:

```text
CollisionRadius unchanged
RVO config unchanged
```

## NPC-V4

Live and Replay:

```text
same visual scale
```

---

# 25. Checkout Queue — Behavior Requirement

Checkout differs from shelf.

Shelf Task 10:

```text
multiple interaction slots
+
potential per-side queue
```

Checkout Task 11:

```text
ONE service position
+
ONE straight FIFO line
```

Do not reuse shelf behavior blindly.

---

# 26. Checkout Geometry

Current checkout visual:

```text
quay_thu_ngan.png
112 × 271 px
portrait
```

Audit current world dimensions from active layout/preset.

Do not derive physical world size every frame from 112×271.

Image aspect is visual reference.

World geometry remains layout-owned.

---

# 27. Service Side

For current portrait checkout:

```text
customer service side = LEFT side of checkout rectangle
```

The head customer stands:

```text
outside the left edge
near the service point
```

and faces toward checkout.

Do not place customer center inside checkout rectangle.

---

# 28. Single Service Slot

Checkout capacity:

```text
1
```

States can remain internal:

```text
FREE
SERVING npcId
```

No need for:

```text
multiple checkout interaction slots
multi-side service
checkout Smart Object framework
```

---

# 29. Single FIFO Queue

Logical structure:

```text
Checkout
   │
Service Head
   │
NPC A
NPC B
NPC C
NPC D
...
```

Exactly:

```text
ONE queue
FIFO
```

No:

```text
north queue
south queue
east queue
west queue
```

---

# 30. Physical Queue Line

For the current portrait cashier asset, preferred physical queue line:

```text
LEFT side
parallel to the long axis of checkout
```

Concept:

```text
NPC D  ○
NPC C  ○
NPC B  ○
NPC A  ○   ← head/service
          ┌─────┐
          │     │
          │ POS │
          │     │
          └─────┘
```

All queue positions must be collinear within tolerance.

---

# 31. Queue Direction Selection

The line remains on LEFT side.

There are two possible directions along that left-side line:

```text
upward
downward
```

Choose the direction with more valid walkable capacity.

Do not create two queues.

If one direction is blocked:

```text
use the other direction
```

If both are unusable:

```text
layout warning/error
bounded fallback
no infinite waiting
```

Do not silently move queue to right side unless a future task explicitly allows it.

---

# 32. Checkout Queue Spacing

Reuse physical values already active:

```text
effective agent radius
stop tolerance
obstacle clearance
```

Minimum center spacing:

```text
>= 2 × effectiveAgentRadius + safe tolerance
```

Do not use sprite width.

---

# 33. Queue Waiting Positions

Each waiting position must be:

```text
finite
inside world bounds
outside checkout rectangle
outside walls
walkable
reachable
unique owner
```

No two NPC share same queue point.

Queue point count can be generated on demand up to active queued NPC count, with a bounded maximum equal to practical population/layout capacity.

No infinite point generation.

---

# 34. Checkout Journey

Target:

```text
shopping complete
        ↓
go checkout
        ↓
join single FIFO queue
        ↓
receive queue position
        ↓
A* path
        ↓
RVO2 movement
        ↓
wait
        ↓
advance when ahead moves
        ↓
become queue head
        ↓
move to service point
        ↓
stop
        ↓
checkout/payment
        ↓
release service
        ↓
next head promoted
        ↓
exit
```

---

# 35. FIFO Invariants

If arrival order is:

```text
A
B
C
D
```

service order is:

```text
A
B
C
D
```

unless a specific NPC becomes invalid/unreachable and current bounded recovery removes it.

New arrival cannot bypass valid older customer.

---

# 36. Payment Exactly Once

For each NPC:

```text
checkout completion
```

must occur at most once.

No duplicate:

```text
revenue
purchase completion
checkout event
completed counter
```

caused by queue advance/replan.

---

# 37. Queue Advance

When A leaves service:

```text
B becomes head
C advances
D advances
```

Movement uses:

```text
existing A*
+
existing RVO2
```

No teleport.

No positional push force.

No custom ORCA rewrite.

---

# 38. RVO2 / A* Policy

Keep:

```text
A* = global navigation
RVO2 = local collision avoidance
```

Do not modify vendored RVO2 source for checkout queue.

Do not tune RVO2 parameters before checkout queue target semantics pass.

If agents appear overlapped:

```text
1. check duplicate queue position
2. check queue line spacing
3. check physical circle overlap
4. distinguish sprite overlap
5. only then audit RVO2
```

---

# 39. Checkout Facing

When NPC is:

```text
SERVING / CHECKOUT
```

visual facing should point toward checkout center/service point.

Reuse Task 9/10 visual facing resolver.

Do not add orientation to:

```text
NPCProfile
RVO2
SimResult
```

---

# 40. Checkout Queue Tests

## CQ-1 — Capacity

Checkout service capacity exactly 1.

## CQ-2 — FIFO

A,B,C arrival → A,B,C service.

## CQ-3 — No bypass

D arrives while A/B/C queued → D cannot service before valid B/C.

## CQ-4 — Unique positions

No two queued NPC own same physical point.

## CQ-5 — Straight line

All waiting positions collinear on checkout left side within geometric tolerance.

## CQ-6 — Blocked direction

One vertical direction blocked → single queue uses other valid direction.

## CQ-7 — Both blocked

Bounded recovery / layout error, no infinite loop.

## CQ-8 — Payment once

Each agent checkout completion event count <= 1.

## CQ-9 — Queue advance

After head finishes → next NPC becomes head.

## CQ-10 — Geometry

No customer position inside cashier bounds/wall.

---

# 41. Reuse From Task 10

Audit current Task 10 code.

Allowed to reuse:

```text
effective radius derivation
walkable point validation
reservation ownership pattern
FIFO data structure concepts
bounded cleanup/recovery
A* destination assignment
RVO2 stopped-agent policy
```

Do not turn Task 10 shelf queue into a huge generic queue framework only to reuse code.

Prefer a small shared helper only if it already naturally exists or duplication is clear and measurable.

---

# 42. App Icon

Source logo:

```text
src/AIsle.DesktopApp/UI/assets/asset/logo.png
```

Canonical after migration:

```text
src/AIsle.DesktopApp/UI/assets/brand/logo.png
```

Derived Windows icon:

```text
src/AIsle.DesktopApp/UI/assets/brand/app.ico
```

---

# 43. Icon Generation

If `app.ico` does not exist:

Generate once from `logo.png`.

Required sizes when tooling supports:

```text
16×16
32×32
48×48
256×256
```

Do not add runtime image-processing package.

The generated `.ico` is a committed static build asset.

Keep `logo.png` for UI branding.

---

# 44. WPF Icon Wiring

Set project application icon using current `.csproj`.

Expected concept:

```xml
<ApplicationIcon>UI\assets\brand\app.ico</ApplicationIcon>
```

Also ensure main WPF window/taskbar uses correct icon if current shell needs explicit `Icon`.

Do not hard-code absolute machine path.

Use project-relative path.

---

# 45. Icon Verification

Check:

```text
AIsleDesktop.exe icon
Windows taskbar icon
window/titlebar icon where applicable
self-contained release icon
```

No default blank .NET/WPF icon.

---

# 46. Simulation Music

Source:

```text
src/AIsle.DesktopApp/UI/assets/asset/music.mp3
```

Canonical:

```text
src/AIsle.DesktopApp/UI/assets/audio/music.mp3
```

---

# 47. Music Trigger

Music starts only after explicit user interaction:

```text
click "Mô phỏng" / Run Simulation
→ enter Simulation screen
→ play music
```

Do not auto-play at app startup.

---

# 48. Music Lifecycle

Use one audio instance.

Concept:

```javascript
const simulationMusic = new Audio(...);
simulationMusic.loop = true;
simulationMusic.volume = 0.20;
```

Exact implementation may use existing UI abstraction.

Rules:

```text
enter Simulation
→ play/resume

leave Simulation
→ pause

re-enter Simulation
→ resume or restart consistently

app close
→ no special background process
```

Do not create a new Audio object each click.

---

# 49. Music Is Screen-Level, Not Tick-Level

Music must NOT depend on:

```text
simulation fixed dt
speed 1x/30x
RVO2
simulation pause
queue logic
```

Default behavior:

```text
while Simulation screen is active
→ music plays
```

Simulation pause button does not need to pause music.

Leaving Simulation screen pauses music.

---

# 50. Music Failure Handling

If file missing/unsupported/play() fails:

```text
warn once
do not crash
do not block simulation
```

No browser alert spam.

No new audio library.

---

# 51. Music Tests

## MUSIC-1

First click Simulation → one play request.

## MUSIC-2

Repeated click/screen navigation → still one audio instance.

## MUSIC-3

Leave screen → paused.

## MUSIC-4

Return → plays again consistently.

## MUSIC-5

Missing file → UI remains functional.

## MUSIC-6

Release → music.mp3 packaged.

---

# 52. Asset Registry

Audit current:

```text
STORE_ASSETS
LocalUiAssets
csproj Content/Resource items
web-relative paths
WPF image references
```

Goal:

```text
one canonical path per asset
```

Use one mapping location per layer.

Do not scatter old absolute/relative asset folders through many render functions.

If current `STORE_ASSETS` already centralizes web paths, update it.

Do not create new asset framework.

---

# 53. Active UI vs Legacy UI

Before updating both:

```text
web/**
src/AIsle.DesktopApp/UI/**
WPF XAML views
```

determine which runtime is active.

Rule:

```text
fix active path first
```

Only update legacy/reference duplicate if required by build/tests/explicit compatibility.

Do not maintain three copies of the same renderer just because old code exists.

---

# 54. Repository Cleanup — Safety Principle

Cleanup is allowed because Task 11 explicitly contains a cleanup phase.

But cleanup means:

```text
inventory
→ classify
→ prove unused
→ delete
→ regression
```

NOT:

```text
delete first
→ see what breaks
```

---

# 55. Cleanup Classification

Every top-level candidate should be classified:

```text
ACTIVE
REFERENCE
LEGACY
FROZEN
REMOVED
GENERATED
USER DATA
UNKNOWN
```

Do not delete `UNKNOWN`.

Resolve classification first.

---

# 56. Always-Protect List

Do not delete without explicit additional proof/owner instruction:

```text
.git/**
.github/**
AGENTS.md
docs/rule.md
docs/task.md
docs/log.md
docs/task_11.md
current source projects
current tests
current scripts used by release/QA
current default-project
current history seed if release/demo uses it
current SQLite/history schema/data migration code
current UI assets
NuGet config needed by build
solution/project files
licenses
README files still referenced by product/release
```

Logs are append-only.

Never rewrite old log history for cleanup.

---

# 57. User Data Protection

Do not treat data as garbage merely because not source code.

Audit before deleting:

```text
runtime/**
%LOCALAPPDATA% references
history.db
history seed
project-v1.json
demo data
user-imported JSON
```

Task 11 cleanup is repository cleanup.

Do not delete user LocalAppData as part of normal repo cleanup.

Only remove test-generated local data that the task itself created and can identify safely.

---

# 58. Generated File Candidates

Typical safe candidates AFTER audit:

```text
**/bin/
**/obj/
.build/
publish temp folders
test temp folders
crash logs from test runs
coverage temp
generated QA raw reports
Python __pycache__
*.pyc
```

Only if not intentionally tracked / release-required.

Update `.gitignore` where appropriate.

---

# 59. Dependency / Environment Candidates

Candidates:

```text
venv/
.env
node_modules/
```

Rules:

```text
.env → never commit
venv → never commit
```

For `node_modules` delete only if not intentionally vendored.

Do not delete package manifests/lock files if current tests/tooling need them.

---

# 60. Legacy / Removed Candidates

Task 11 may remove code only if all are true:

```text
1. classified REMOVED or obsolete duplicate
2. zero active caller
3. zero build dependency
4. zero test dependency that protects active behavior
5. zero release packaging dependency
6. replacement already working
7. regression passes after deletion
```

Do not mass-delete:

```text
UnityApp
backend
web
mobile
old docs
```

based only on folder name.

Some may still be retained as reference/test input.

Audit first.

---

# 61. Duplicate Asset Cleanup

After canonical asset migration:

Find duplicates by:

```text
filename
SHA-256/hash
dimensions
actual callers
```

If same runtime asset exists in:

```text
src/AIsle.DesktopApp/Assets
src/AIsle.DesktopApp/UI/assets
web/assets
```

keep only canonical copy when build/tests permit.

Delete duplicate only after callers updated.

---

# 62. Dead Code Cleanup

Use:

```text
solution build
project references
text/reference search
compiler warnings if applicable
tests
release smoke
```

Do not delete public contract simply because text search is zero.

Contracts may be serialized/reflection-driven.

Task 11 does NOT authorize broad public schema deletion.

---

# 63. Cleanup Batch Size

Do not delete 100 unrelated files in one unverified batch.

Use groups:

```text
C1 generated
→ verify

C2 duplicate assets
→ verify

C3 dead legacy helper files
→ verify

C4 obsolete build outputs
→ verify
```

After each:

```text
git diff --check
build/test relevant slice
```

---

# 64. Cleanup Documentation

Create if useful:

```text
docs/task11-cleanup-audit.md
```

with:

```text
path
classification
reason
caller evidence
action KEEP/MOVE/DELETE
verification
```

Do not create giant enterprise inventory if a concise table is enough.

---

# 65. Allowed Paths

Primary:

```text
D:\Big\KADA\simulator\src\AIsle.Simulation\**
D:\Big\KADA\simulator\src\AIsle.DesktopApp\**
D:\Big\KADA\simulator\web\**
D:\Big\KADA\simulator\tests\**
D:\Big\KADA\simulator\scripts\**
D:\Big\KADA\simulator\docs\task_11.md
D:\Big\KADA\simulator\docs\log.md
D:\Big\KADA\simulator\docs\task11-cleanup-audit.md
D:\Big\KADA\simulator\.gitignore
```

For cleanup, other repo paths may be deleted/moved ONLY after classification/proof gates in this task.

---

# 66. Forbidden Changes

Task 11 does not authorize:

```text
rewrite A*
rewrite RVO2
replace GeneticSharp
replace Math.NET
new GA
new Utility AI
new purchase formula
new social AI
new emotion system
new animation FSM
Unity migration
Node runtime restoration
microservice
cloud backend
LLM
new database architecture
SimResult schema redesign
KPI redesign
```

Do not change app architecture.

---

# 67. Work Order

WIP = 1.

Do exactly in order.

## T11-A — Audit & Baseline

1. read sources;
2. git status/branch;
3. inventory asset paths;
4. locate active floor/shelf/NPC/checkout renderers;
5. locate checkout core behavior;
6. locate release packaging;
7. run baseline tests;
8. record current failures.

Gate A before code.

## T11-B — Checkout SYSTEM

1. audit existing checkout journey;
2. define single service point;
3. implement one FIFO queue;
4. generate left-side straight queue positions;
5. integrate A*/RVO2;
6. ensure payment once;
7. add checkout tests.

Gate B: checkout tests + simulation regression pass.

## T11-C — Shelf Rotation Fix

1. reproduce shrink;
2. identify dimension mutation;
3. enforce rotation invariants;
4. boundary policy without shrink;
5. verify flip does not mutate footprint;
6. add tests.

## T11-D — Floor Stability Fix

1. reproduce tile/seam issue;
2. reproduce far-drag zoom/freeze;
3. decouple floor bounds from object extents;
4. bound tile work to layout;
5. clamp/reject invalid drag;
6. add finite guards;
7. test resize/sidebar.

## T11-E — NPC Visual Scale

1. increase visual scale to 1.12×;
2. preserve foot anchor;
3. preserve physics;
4. Live/Replay consistency.

## T11-F — Checkout Visual Integration

1. render checkout from canonical asset;
2. align service point visually to left side;
3. queue line matches core coordinates;
4. paying NPC faces checkout;
5. no business queue logic in JS.

## T11-G — App Icon

1. move logo to brand folder;
2. generate app.ico once if needed;
3. wire csproj/WPF icon;
4. verify exe/taskbar/window;
5. verify release.

## T11-H — Simulation Music

1. move music.mp3 to audio folder;
2. one audio instance;
3. start on Simulation navigation click;
4. loop volume 0.20;
5. pause when leaving Simulation;
6. error-safe;
7. packaging tests.

## T11-I — Asset Migration

1. create canonical UI/assets tree;
2. migrate store assets group by group;
3. update registries/callers;
4. update csproj/package rules;
5. hash/reference audit;
6. remove proven duplicates.

## T11-J — Repository Cleanup

1. inventory top-level folders;
2. classify;
3. delete generated junk;
4. remove duplicate assets;
5. remove proven dead files only;
6. update gitignore;
7. preserve user data/logs;
8. create concise cleanup audit.

## T11-K — Full Regression / Release

1. build;
2. all C# suites;
3. JS tests;
4. Task 9 renderer tests;
5. Task 10 queue/RVO2 tests;
6. Task 11 tests;
7. QA smoke;
8. publish;
9. manual M1–M12;
10. git diff --check;
11. append log;
12. STOP.

---

# 68. Baseline Commands

Use repository current equivalents.

Expected:

```bat
dotnet build AIsle.slnx -c Release --no-restore
```

Run current test projects:

```text
AIsle.Population.Tests
AIsle.Simulation.Tests
AIsle.Results.Tests
AIsle.DesktopApp.Tests
```

Run current JS test command.

If current repository uses:

```text
node --test tests/*.test.mjs
```

use it.

Do not invent a different test runner without reason.

---

# 69. Performance / Responsiveness Gate

Task 11 floor fix must include UI responsiveness check.

Record:

```text
normal map render
window resize
sidebar collapse
shelf drag at boundary
shelf drag pointer far outside
20+ NPC checkout queue
```

No need new benchmark framework.

No optimization engine.

If floor rendering loops scale with object coordinate:

```text
FAIL
```

---

# 70. Manual Verification

## M1 — Shelf Rotate

Select each shelf type.

Rotate repeatedly:

```text
0
90
180
270
360
```

Confirm no shrink / no grow / no jump.

## M2 — Shelf Boundary

Place shelf near each store edge and rotate.

Confirm size preserved and no out-of-world corruption.

## M3 — Floor

Observe full store:

```text
continuous san.jpg
stable scale
no giant tile
no unexpected seams
```

## M4 — Far Drag

Drag shelf far outside app/canvas.

Confirm:

```text
object bounded
floor unchanged
app responsive
```

## M5 — NPC Size

Run simulation.

Confirm NPC is slightly larger, still proportional, not giant, foot anchor stable.

## M6 — Checkout 3 NPC

3 NPC finish shopping.

Confirm:

```text
single line on left
FIFO
one pays at a time
```

## M7 — Checkout Dense

10+ NPC converge checkout.

Confirm:

```text
one straight line
no multi-side shelf-like queue
no bypass
no dogpile
```

## M8 — Checkout Block

Place wall/layout constraint near one queue direction.

Confirm queue uses other valid direction on left or clear layout warning; no infinite wait.

## M9 — Checkout Facing

Paying NPC faces cashier.

Waiting NPCs remain stable.

## M10 — App Icon

Launch built Desktop app.

Check exe icon, window icon, taskbar icon.

## M11 — Music

Click Mô phỏng.

Confirm music starts, loops, no duplicate playback.

Navigate away → music pauses.

Return → music plays once.

## M12 — Clean Repository

After cleanup:

```text
app launches
project opens
layout edits
population works
simulation runs
checkout works
history/replay works
compare works
release smoke passes
```

---

# 71. Automated Task 11 Test Matrix

```text
ROT-1..ROT-6
FLOOR-1..FLOOR-6
NPC-V1..NPC-V4
CQ-1..CQ-10
MUSIC-1..MUSIC-6
ICON-1..ICON-4
ASSET-1..ASSET-6
CLEAN-1..CLEAN-8
```

---

# 72. Icon Automated Gates

## ICON-1

`app.ico` exists in canonical source.

## ICON-2

Desktop csproj references project-relative icon.

## ICON-3

Publish output executable contains configured icon.

## ICON-4

No absolute `D:\...` path baked into project config.

---

# 73. Asset Automated Gates

## ASSET-1

Canonical `UI/assets` tree exists.

## ASSET-2

All runtime assets referenced by active UI exist.

## ASSET-3

No active asset reference points to deleted old location.

## ASSET-4

Release includes:

```text
san.jpg
quay_thu_ngan.png
music.mp3
logo/app icon as required
NPC sprites
active shelves/fixtures
```

## ASSET-5

No missing asset startup error.

## ASSET-6

Duplicate copies remaining are documented with reason.

---

# 74. Cleanup Automated Gates

## CLEAN-1

`git status` contains no accidental `.env`, `venv`, `bin`, `obj` unless intentionally ignored/untracked.

## CLEAN-2

No tracked generated `bin/obj`.

## CLEAN-3

No deleted file has active source reference.

## CLEAN-4

Solution build passes after cleanup.

## CLEAN-5

All tests pass after cleanup.

## CLEAN-6

Release publish passes.

## CLEAN-7

QA smoke passes.

## CLEAN-8

`git diff --check` passes.

---

# 75. Error Handling

Shelf rotate invalid:

```text
reject/clamp
→ preserve size
```

Invalid floor geometry:

```text
safe fallback
→ clear warning
→ no loop
```

Checkout line unavailable:

```text
bounded failure
→ no infinite queue
```

Missing music:

```text
warn once
→ simulation continues
```

Missing non-critical visual asset:

```text
fallback visual
→ no app crash
```

Missing critical app icon at build:

```text
build/test should fail clearly
```

---

# 76. No Absolute Runtime Paths

Do not bake:

```text
D:\Big\KADA\simulator\...
```

into runtime code.

The absolute root is only for agent execution.

Application references use project-relative / packaged local UI-relative paths.

---

# 77. Release Packaging

After asset migration, audit:

```text
AIsle.DesktopApp.csproj
LocalUiAssets
publish script
QA smoke
```

Do not publish duplicate old asset folder unless required.

Release must run without repository source, Node runtime, or Unity per current architecture.

---

# 78. Logging

Follow `AGENTS.md`.

Default current general log:

```text
docs/log.md
```

Append only.

Do not edit old entries.

Task 11 log entry must include:

```text
date/time
actor
reason
root path
branch
files changed
shelf rotation root cause
floor root cause
checkout queue algorithm
icon setup
music behavior
asset migration
cleanup deleted paths
cleanup kept paths
dependency status
tests
release
manual verification
Git sync status
next step
```

---

# 79. Self Review

- [ ] Read rule.md completely.
- [ ] Read task.md completely.
- [ ] Read latest relevant log.
- [ ] API_Frontend_Integration treated as legacy/reference when conflicting with active architecture.
- [ ] WIP=1.
- [ ] Current rule branch policy respected.
- [ ] No direct main push.
- [ ] No architecture rewrite.
- [ ] No Node runtime restoration.
- [ ] No A* rewrite.
- [ ] No RVO2 rewrite.
- [ ] Shelf rotation preserves dimensions.
- [ ] Floor bounded to layout.
- [ ] Far drag cannot explode scale/render loops.
- [ ] NPC visual scale +12%, physics unchanged.
- [ ] Checkout is one service + one FIFO line.
- [ ] Checkout queue stays on left side.
- [ ] Queue positions collinear.
- [ ] Payment once.
- [ ] No checkout business logic duplicated in JS.
- [ ] App icon derived from logo.
- [ ] No runtime image-processing dependency.
- [ ] Music uses one instance.
- [ ] Music starts from user click.
- [ ] Asset tree canonicalized.
- [ ] Duplicate assets removed only after proof.
- [ ] User data preserved.
- [ ] Logs append-only.
- [ ] Cleanup audit exists if deletions are non-trivial.
- [ ] Full regression passes.
- [ ] Release smoke passes.
- [ ] git diff --check passes.
- [ ] No automatic push.

---

# 80. Definition of Done

Task 11 is DONE only when ALL are true.

## Shelf

- [ ] Rotate 90° no abnormal shrink.
- [ ] 4 rotations restore original geometry.
- [ ] Flip does not alter physical footprint.
- [ ] Boundary rotation does not shrink shelf.

## Floor

- [ ] `san.jpg` tiles correctly.
- [ ] Tile scale is stable.
- [ ] Floor is bounded by layout, not object extents.
- [ ] Dragging shelf far away cannot zoom floor huge.
- [ ] No infinite/unbounded tile loop.
- [ ] UI remains responsive.

## NPC

- [ ] NPC visual size increased by 12%.
- [ ] Foot anchor unchanged.
- [ ] Collision/RVO radius unchanged.
- [ ] Live/Replay consistent.

## Checkout

- [ ] Uses `quay_thu_ngan.png`.
- [ ] Service capacity exactly 1.
- [ ] Queue exactly one FIFO line.
- [ ] Queue located on left side.
- [ ] Queue waiting positions are collinear.
- [ ] Queue uses one valid vertical direction.
- [ ] No bypass.
- [ ] No duplicate queue point.
- [ ] Payment happens once.
- [ ] Queue advances without teleport.
- [ ] A*/RVO2 preserved.
- [ ] Paying NPC faces checkout.
- [ ] Dense checkout scenario passes.

## Icon

- [ ] `logo.png` preserved.
- [ ] `app.ico` generated/canonical.
- [ ] `.csproj` application icon configured.
- [ ] exe/taskbar/window icon verified.

## Music

- [ ] `music.mp3` packaged.
- [ ] Enter Simulation from click starts music.
- [ ] Music loops.
- [ ] Single audio instance.
- [ ] Leaving Simulation pauses music.
- [ ] Missing audio cannot crash app.

## Assets

- [ ] UI/assets organized by brand/audio/npc/store.
- [ ] Active paths updated.
- [ ] Build packaging updated.
- [ ] No unnecessary duplicate runtime asset copy remains.
- [ ] Release contains required assets.

## Cleanup

- [ ] Repository inventory/classification completed.
- [ ] Generated junk cleaned.
- [ ] Duplicate assets cleaned.
- [ ] Dead files deleted only with proof.
- [ ] Unknown/user data not deleted.
- [ ] `.gitignore` covers local secret/env/generated folders.
- [ ] Cleanup audit records non-trivial deletions.

## QA

- [ ] Solution Release build pass.
- [ ] Population tests pass.
- [ ] Simulation tests pass.
- [ ] Results tests pass.
- [ ] Desktop tests pass.
- [ ] JS tests pass.
- [ ] Task 9 renderer regression pass.
- [ ] Task 10 shelf queue/RVO2 regression pass.
- [ ] Task 11 tests pass.
- [ ] Self-contained publish pass.
- [ ] QA smoke pass.
- [ ] Manual M1–M12 pass.
- [ ] `git diff --check` pass.
- [ ] log appended.
- [ ] no unapproved dependency.

---

# 81. Stop

After Task 11 reaches DoD:

```text
DONE
→ STOP
```

Do not automatically open:

```text
Task 12
multiple checkout lanes
multiple cashiers
checkout optimization analytics
shopping cart system
employee AI
new music playlist
volume settings page
new animation states
dynamic lighting
procedural store generation
cloud sync
```

Any next feature requires explicit owner request / RFC.
