# AIsle — TASK 9: Pixel NPC Renderer

> **Project root:** `D:\dev\kada\alsle`  
> **Task type:** UI/UX visual integration  
> **WIP:** 1  
> **Priority:** P1  
> **Target:** Desktop App — WPF + WebView2 + local HTML/CSS/JS  
> **Scope rule:** Không thay đổi Simulation behavior chỉ để phục vụ hiển thị.

---

# 0. Mini-RFC — Mở scope NPC Character Animation

## Problem

Simulation hiện hiển thị NPC dưới dạng dot/icon.

Mục tiêu của task này là thay visual NPC bằng **pixel character sprite** để simulation trực quan hơn trong demo/portfolio, nhưng không được:

- thay đổi kiến trúc app;
- đưa animation logic vào `AIsle.Simulation`;
- thay đổi A*, ORCA/RVO2, Utility Decision, Population hoặc Purchase;
- tạo animation framework lớn;
- đưa Unity/Spine trở lại;
- thêm dependency nếu renderer hiện tại đã đủ.

## User value

Người dùng khi chạy Simulation hoặc Replay có thể nhìn thấy NPC dưới dạng nhân vật pixel thực sự, có hướng di chuyển và animation đi bộ cơ bản thay vì dot/icon.

## Why now

SYSTEM đã hoàn tất nền tảng Simulation/Result/Replay và projection hiện tại đã đủ dữ liệu vị trí để renderer suy ra hướng di chuyển.

Feature này chỉ thay presentation layer.

## Affected milestone

```text
UI/UX
└── U4 — Simulation UI
    └── U4.5 — Pixel NPC Renderer
```

Replay phải reuse cùng renderer.

## Affected modules

```text
src/AIsle.DesktopApp/
└── UI/
    └── local web assets / JS renderer / sprite assets
```

Không mở rộng sang:

```text
src/AIsle.Simulation/**
src/AIsle.Contracts/**
Population
GA
A*
ORCA/RVO2
Decision
Interaction
Purchase
History persistence
```

## External source/repo

Ưu tiên:

```text
1. Native Canvas 2D API hiện có
2. requestAnimationFrame
3. CanvasRenderingContext2D.drawImage
4. Math.atan2
```

Candidate chỉ khi benchmark chứng minh Canvas hiện tại không đủ:

```text
PixiJS
https://github.com/pixijs/pixijs
License: MIT
```

Không thêm PixiJS mặc định.

## Added dependency

```text
NONE
```

Chỉ được mở dependency review nếu benchmark sau implementation chứng minh renderer hiện tại không đạt performance gate.

## Tests

- sprite asset validation;
- deterministic model selection;
- 8-direction mapping;
- animation frame progression;
- idle/stopped facing stability;
- Simulation regression;
- Replay visual regression;
- 200/500/1000 NPC render benchmark;
- memory/per-frame cost check.

## What existing scope is removed/replaced

```text
NPC dot/icon rendering
→ Pixel NPC sprite rendering
```

Simulation logic không bị thay thế.

---

# 1. Task Card

## TASK-9 / U4.5 — Pixel NPC Renderer

**State:** READY  
**Priority:** P1  
**Parent:** UIUX  
**Depends On:** S4, S5  
**Affected Module:** `src/AIsle.DesktopApp/UI/**`

---

## 2. Goal

Thay NPC dot/icon trong Simulation và Replay bằng pixel sprite character có:

```text
4 character models
×
8 movement directions
×
4 walking frames
=
128 sprite frames
```

Bộ sprite sheet đã có sẵn, mỗi model dùng layout cố định:

```text
8 hàng × 4 cột
```

8 hàng là 8 hướng theo **chiều kim đồng hồ, bắt đầu từ South**:

```text
Row 0 = S
Row 1 = SW
Row 2 = W
Row 3 = NW
Row 4 = N
Row 5 = NE
Row 6 = E
Row 7 = SE
```

4 cột là 4 walking frame theo đúng thứ tự:

```text
Column 0 = frame 0
Column 1 = frame 1
Column 2 = frame 2
Column 3 = frame 3

loop: 0 → 1 → 2 → 3 → 0
```

Mỗi NPC khi spawn được gán một trong khoảng 4 model theo cách pseudo-random nhưng ổn định trong cùng run/replay.

---

# 3. Non-Goals

Task này KHÔNG làm:

- idle animation nhiều frame;
- sleep;
- drag;
- emotion;
- facial expression;
- transition animation;
- state machine tổng quát;
- animation event system;
- Spine;
- Unity;
- DOTS/ECS/Burst/Jobs;
- pathfinding mới;
- crowd avoidance mới;
- gameplay behavior mới;
- thay đổi Population chromosome;
- thêm `ModelId` vào `NPCProfile`;
- thêm animation state vào `SimResult`;
- thêm velocity vào contract chỉ để render sprite.

---

# 4. Allowed Paths

Project root:

```text
D:\dev\kada\alsle
```

Allowed:

```text
D:\dev\kada\alsle\asset\npc_0.png
D:\dev\kada\alsle\asset\npc_1.png
D:\dev\kada\alsle\asset\npc_2.png
D:\dev\kada\alsle\asset\npc_3.png
D:\dev\kada\alsle\src\AIsle.DesktopApp\UI\**
D:\dev\kada\alsle\src\AIsle.DesktopApp\**\assets\**
D:\dev\kada\alsle\tests\AIsle.DesktopApp*\**
D:\dev\kada\alsle\docs\task_9.md
D:\dev\kada\alsle\docs\log_frontend.md
D:\dev\kada\alsle\docs\log.md
```

Bốn sprite sheet nguồn hiện có tại:

```text
D:\dev\kada\alsle\asset\npc_0.png
D:\dev\kada\alsle\asset\npc_1.png
D:\dev\kada\alsle\asset\npc_2.png
D:\dev\kada\alsle\asset\npc_3.png
```

Trong khi thực hiện task, **được phép move đúng 4 file trên** sang vị trí asset runtime hợp lý bên trong `AIsle.DesktopApp` nếu cần để WebView2/local asset packaging hoạt động đúng.

Khi move:

- giữ nguyên tên `npc_0.png`, `npc_1.png`, `npc_2.png`, `npc_3.png`;
- không tạo thêm bản copy dư thừa nếu không cần;
- cập nhật path/asset registry của renderer theo vị trí mới;
- xác minh cả 4 file được đóng gói trong release;
- không move file hoặc thư mục khác chỉ để tổ chức lại cấu trúc.

Nếu asset web hiện tại nằm ở path khác bên trong `AIsle.DesktopApp`, được phép chỉnh đúng asset/render file đang được Desktop đóng gói.

Không được tự move cấu trúc folder lớn.

---

# 5. Forbidden Paths

Không sửa nếu không phát hiện blocker thực sự:

```text
src/AIsle.Simulation/**
src/AIsle.Contracts/**
Population/**
Decision/**
Navigation/**
Interaction/**
Results/**
RVO2/**
UnityApp/**
mobile/**
backend/**
```

Cấm:

```text
rewrite SimulationHost
rewrite A*
rewrite ORCA/RVO2
change NPC behavior
change purchase logic
change GA
change result schema
change replay data contract
```

Nếu UI projection thật sự thiếu dữ liệu bắt buộc:

```text
STOP
→ ghi blocker
→ audit caller
→ không tự sửa contract
```

---

# 6. Sprite Asset Contract

## 6.1 Character count

Initial target:

```text
4 models
```

Các file sprite sheet nguồn đã có sẵn:

```text
D:\dev\kada\alsle\asset\npc_0.png
D:\dev\kada\alsle\asset\npc_1.png
D:\dev\kada\alsle\asset\npc_2.png
D:\dev\kada\alsle\asset\npc_3.png
```

Tên logic/model tương ứng:

```text
npc_0
npc_1
npc_2
npc_3
```

Không hard-code tên nhân vật thật vào Simulation.

---

## 6.2 Direction contract

Direction index phải cố định theo **layout thực tế của cả 4 sprite sheet**.

8 hàng đi theo chiều kim đồng hồ và bắt đầu từ South:

```text
Row 0 / Direction 0 = S
Row 1 / Direction 1 = SW
Row 2 / Direction 2 = W
Row 3 / Direction 3 = NW
Row 4 / Direction 4 = N
Row 5 / Direction 5 = NE
Row 6 / Direction 6 = E
Row 7 / Direction 7 = SE
```

Không được tự đổi ordering này.

Renderer chỉ có **một mapping direction duy nhất** và Live/Replay phải reuse cùng mapping.

Không tạo nhiều mapping direction rải rác.

---

## 6.3 Frame contract

Mỗi direction là **một hàng**, gồm đúng **4 cột**:

```text
Column 0 = frame 0
Column 1 = frame 1
Column 2 = frame 2
Column 3 = frame 3
```

Walking loop bắt buộc:

```text
0 → 1 → 2 → 3 → 0
```

Không đảo thứ tự cột và không dùng ping-pong nếu không có task riêng yêu cầu.

Không tạo AnimationClip abstraction lớn.

---

## 6.4 Frame dimensions

Tất cả frame của cả 4 model phải:

- cùng logical width;
- cùng logical height;
- cùng foot anchor;
- cùng scale;
- transparent background;
- không chứa padding bất thường giữa model.

Nếu source sprite khác size:

```text
normalize asset
```

Không bù bằng nhiều special-case render code.

---

## 6.5 Pixel rendering

Bắt buộc:

```javascript
ctx.imageSmoothingEnabled = false;
```

Render phải giữ pixel-art sharpness khi scale.

---

# 7. Asset Packaging

## 7.1 Existing source assets

Task **không tạo sprite sheet mới**.

Bộ asset đầu vào đã có sẵn:

```text
D:\dev\kada\alsle\asset\npc_0.png
D:\dev\kada\alsle\asset\npc_1.png
D:\dev\kada\alsle\asset\npc_2.png
D:\dev\kada\alsle\asset\npc_3.png
```

Mỗi file là một sprite sheet độc lập cho một model.

---

## 7.2 Fixed sheet layout

Mỗi sprite sheet phải được đọc theo đúng:

```text
8 rows × 4 columns
```

Trong đó:

```text
rows    = 8 movement directions
columns = 4 walking frames
```

Thứ tự row:

```text
0 = S
1 = SW
2 = W
3 = NW
4 = N
5 = NE
6 = E
7 = SE
```

Thứ tự column:

```text
0 = frame 0
1 = frame 1
2 = frame 2
3 = frame 3
```

Animation loop:

```text
0 → 1 → 2 → 3 → 0
```

Renderer tính frame size trực tiếp từ kích thước ảnh:

```text
frameWidth  = imageWidth / 4
frameHeight = imageHeight / 8
```

Asset validation phải báo lỗi rõ ràng nếu:

```text
imageWidth % 4 != 0
hoặc
imageHeight % 8 != 0
```

Không cần JSON/atlas metadata riêng cho layout này.

---

## 7.3 Asset relocation

Được phép move đúng 4 file:

```text
npc_0.png
npc_1.png
npc_2.png
npc_3.png
```

từ:

```text
D:\dev\kada\alsle\asset
```

sang thư mục asset runtime phù hợp bên trong:

```text
D:\dev\kada\alsle\src\AIsle.DesktopApp\...
```

nếu điều đó cần thiết cho local WebView2 asset loading hoặc release packaging.

Yêu cầu:

- giữ nguyên filename;
- giữ đủ cả 4 model;
- renderer chỉ tham chiếu vị trí cuối cùng;
- không để tồn tại hai bộ asset runtime trùng nhau nếu không cần;
- release artifact phải chứa đủ 4 PNG;
- không thay đổi cấu trúc app ngoài phạm vi asset/render.

---

## 7.4 No extra asset framework

Không thêm:

```text
runtime texture packer
sprite atlas generator
asset database
asset pipeline framework
JSON metadata file
```

chỉ để đọc 4 sprite sheet đã có.

---

# 8. Model Selection

## 8.1 Requirement

NPC phải được chọn model pseudo-random khi spawn.

Nhưng cùng một NPC trong cùng run phải luôn giữ model đó.

Replay cũng phải thấy đúng model tương ứng.

---

## 8.2 Default algorithm

Renderer sử dụng deterministic hash từ:

```text
runSeed
+
npcId
```

Ví dụ logic:

```text
modelIndex = hash(runSeed, npcId) % modelCount
```

Yêu cầu:

- không gọi RNG mỗi render frame;
- không thay model khi pause/resume;
- không thay model khi NPC đổi hướng;
- cùng run + cùng npcId → cùng model;
- model count hiện tại = 4;
- modelCount phải lấy từ asset registry, không rải magic number.

---

## 8.3 Important boundary

Không thêm:

```text
ModelId
```

vào:

```text
NPCProfile
SimulationHost
GA chromosome
SimResult schema
```

trừ khi task riêng sau này explicit yêu cầu persistence semantics khác.

---

# 9. Direction Detection

## 9.1 Input

UI projection hiện có:

```text
npc.id
npc.x
npc.y
npc.status
npc.targetId
```

Renderer lưu:

```text
previousX
previousY
lastDirection
```

cho mỗi NPC.

---

## 9.2 Delta

```text
dx = currentX - previousX
dy = currentY - previousY
```

Nếu:

```text
dx² + dy² < movementEpsilon²
```

thì:

```text
giữ lastDirection
không đổi hướng
```

---

## 9.3 Angle

Dùng:

```javascript
Math.atan2(dy, dx)
```

Sau đó quantize thành 8 sector 45°.

Không thêm library.

Không thêm generic vector package chỉ cho task này.

---

## 9.4 Coordinate mapping

Phải audit coordinate system của canvas hiện tại trước khi mapping.

Nếu world Y tăng theo hướng ngược screen Y:

```text
mapping phải invert đúng một lần
```

Không sửa Simulation coordinates.

---

# 10. Animation Timing

## 10.1 Renderer clock

Animation visual dùng:

```text
requestAnimationFrame
```

Không dùng Simulation tick làm animation framework.

Simulation vẫn quyết định vị trí.

Renderer chỉ interpolate/render theo projection hiện có nếu app hiện tại đã làm vậy.

---

## 10.2 Walk frame rate

Initial target:

```text
6–10 sprite frames/second
```

Recommended default:

```text
8 FPS
```

Frame index:

```text
floor(animationTime / frameDuration) % 4
```

Không tạo timer riêng cho từng NPC.

---

## 10.3 Shared clock

Tất cả NPC dùng một animation clock chung.

Per-NPC chỉ lưu phase nhỏ nếu cần tạo variation.

Không tạo:

```text
setInterval per NPC
requestAnimationFrame per NPC
```

Chỉ có một render loop.

---

# 11. NPC Visual State

Một `Map<npcId, VisualState>` nhỏ là đủ.

Ví dụ conceptual state:

```text
npcId
modelIndex
lastX
lastY
direction
phaseOffset
lastSeenFrame
```

Không chứa business state.

Không duplicate:

```text
status
target
decision
purchase
navigation
```

nếu projection đã có.

---

# 12. Spawn / Despawn Handling

## Spawn

Khi NPC id lần đầu xuất hiện:

1. tạo `VisualState`;
2. chọn deterministic model;
3. gán initial facing;
4. gán optional phaseOffset;
5. render.

## Despawn

Khi NPC không còn active:

- không xóa state ngay trong cùng frame nếu replay/live pipeline có transient gap;
- cleanup state theo current active-id set hoặc bounded stale-frame policy.

Không để `Map` tăng vô hạn qua nhiều run.

## Reset

Simulation Reset:

```text
clear NPC visual state
reset animation epoch
```

---

# 13. Render Pipeline

Target pipeline:

```text
C# Simulation
      │
      ▼
Projection
id / x / y / status / targetId
      │
      ▼
WebView2 JS
      │
      ├── VisualStateMap
      ├── ModelSelector
      ├── DirectionQuantizer
      └── SpriteFrameResolver
      │
      ▼
Canvas drawImage()
```

Core không biết sprite tồn tại.

---

# 14. Canvas Rendering

Mỗi NPC render bằng một `drawImage` crop.

Concept:

```javascript
ctx.drawImage(
  spriteSheet,
  sourceX,
  sourceY,
  frameWidth,
  frameHeight,
  screenX - anchorX,
  screenY - anchorY,
  drawWidth,
  drawHeight
);
```

Không tạo DOM `<img>` cho từng NPC.

Không tạo HTML element cho từng frame.

---

# 15. Top-Down Perspective

NPC visual phải phù hợp map hiện có.

Yêu cầu:

- feet/ground anchor nằm đúng world position;
- sprite body được vẽ phía trên anchor;
- NPC không trông như đang trượt quanh tâm sprite;
- cùng scale với map;
- top-down perspective nhất quán giữa 8 hướng.

Không thay camera/map transform chỉ để sprite đẹp hơn.

---

# 16. Draw Order

Để character overlap tự nhiên hơn trong top-down view:

Ưu tiên sort theo screen/world Y trước khi draw:

```text
lower on screen
→ draw later
```

Nếu renderer hiện tại đã có world draw ordering phù hợp:

```text
reuse current ordering
```

Không xây scene graph mới.

Nếu sorting làm benchmark fail:

```text
benchmark
→ mới tối ưu
```

---

# 17. Replay Reuse

Replay KHÔNG có renderer riêng.

Phải reuse:

```text
same sprite registry
same model selection
same direction quantizer
same frame resolver
same canvas draw function
```

Khác biệt chỉ là nguồn position/time:

```text
Live Simulation projection
vs
Replay projection
```

Không duplicate sprite logic giữa Live và Replay.

---

# 18. Selected NPC

Existing selected-NPC inspector vẫn dùng dữ liệu runtime hiện tại.

Không thêm panel:

```text
animation state
sprite frame
direction enum
model debug
```

vào UI production.

Debug overlay chỉ được dùng development-time nếu cần và phải tắt mặc định.

---

# 19. Dependency Policy

## Default

```text
NO NEW DEPENDENCY
```

Native Canvas phải được thử trước.

## Candidate escalation

Chỉ audit PixiJS nếu tất cả điều sau đúng:

```text
1. native Canvas implementation đã hoàn thành;
2. correctness pass;
3. 200/500/1000 benchmark đã chạy;
4. benchmark cho thấy render bottleneck thực;
5. bottleneck nằm ở renderer chứ không phải Simulation;
6. performance ảnh hưởng demo/use-case thực.
```

Nếu mở dependency review phải ghi:

- package/repo;
- upstream;
- version;
- license;
- bundle size;
- integration reason;
- benchmark trước;
- benchmark sau;
- regression result.

Không thêm package chỉ vì API tiện hơn.

---

# 20. Performance

## 20.1 Test populations

Benchmark:

```text
200 NPC
500 NPC
1000 NPC
```

## 20.2 Metrics

Ghi ít nhất:

```text
average render frame time
P95 render frame time
FPS
memory delta
draw calls/frame
active visual-state count
Simulation tick correctness
```

Có thể ghi thêm CPU nếu tool hiện có hỗ trợ.

---

## 20.3 Rule

Không optimize trước benchmark.

Không mở:

```text
WebGL custom renderer
worker renderer
OffscreenCanvas architecture
spatial partition
object pooling framework
PixiJS
custom engine
```

chỉ vì "có thể nhanh hơn".

---

# 21. Automated Verification

Bắt buộc tạo test/verification cho logic thuần nếu structure hiện tại cho phép.

## R1 — Model deterministic

```text
same seed + same npcId
→ same model
```

## R2 — Model spread

Với nhiều NPC id:

```text
modelIndex luôn trong [0, modelCount)
```

Không yêu cầu distribution hoàn hảo.

## R3 — Direction E

```text
dx > 0
dy ≈ 0
→ E
```

## R4 — Direction W

```text
dx < 0
dy ≈ 0
→ W
```

## R5 — Direction N/S

Mapping phải đúng với coordinate convention hiện tại.

## R6 — Diagonal

Test:

```text
NE
NW
SE
SW
```

## R7 — Stationary

Movement dưới epsilon:

```text
lastDirection không đổi
```

## R8 — Frame bounds

Mọi animation frame:

```text
0 <= frame < 4
```

## R9 — Asset bounds

Mọi crop rectangle phải nằm trong sprite dimensions.

## R10 — Reset

Reset phải clear visual state.

---

# 22. Regression Verification

Bắt buộc chạy lại:

```text
dotnet build AIsle.slnx -c Release --no-restore
Desktop verification
Simulation verification
Population verification
Results / Replay verification
JavaScript regression tests
git diff --check
```

Không sửa expected result của simulation chỉ để test pass.

---

# 23. Manual Verification

Chạy:

```text
D:\dev\kada\alsle\run-desktop.bat
```

hoặc launcher chính thức hiện tại của repository nếu tên script đã thay đổi.

Kiểm tra:

### M1

RUN LIVE:

- NPC xuất hiện bằng sprite;
- không còn dot/icon cũ chồng lên.

### M2

NPC đi 8 hướng:

- facing hợp lý;
- không bị đảo East/West;
- không bị đảo North/South.

### M3

Animation:

- 4 frame loop đúng;
- không nháy;
- không đổi model;
- không tạo cảm giác teleport do anchor.

### M4

Pause:

- NPC không đổi position;
- animation policy nhất quán.

Recommended:

```text
pause simulation
→ freeze walking frame
```

### M5

Reset:

- visual state cũ bị clear;
- run mới có model assignment phù hợp seed/run mới.

### M6

Replay:

- hiển thị cùng visual system;
- không fallback về dot;
- không duplicate renderer.

### M7

Dense crowd:

- 200 NPC chạy ổn;
- 500 NPC review;
- 1000 NPC benchmark.

---

# 24. Fallback

Nếu sprite asset lỗi hoặc chưa load xong:

Renderer được phép fallback tạm:

```text
simple dot/icon
```

nhưng:

- không crash Simulation;
- không phá render loop;
- log warning development-safe;
- production không spam log mỗi frame.

---

# 25. Error Handling

Các lỗi cần handle:

```text
sprite file missing
sprite decode fail
invalid dimensions
frame outside atlas
model registry empty
unknown npc id
NaN position
```

Không được để một sprite lỗi làm crash toàn bộ live simulation.

---

# 26. Asset Validation Gate

Trước khi mark DONE:

- [ ] đủ đúng 4 file `npc_0.png` → `npc_3.png`;
- [ ] mỗi sprite sheet đúng layout 8 hàng × 4 cột;
- [ ] 8 hàng map đúng `S, SW, W, NW, N, NE, E, SE`;
- [ ] mỗi hướng đủ 4 frame theo cột `0, 1, 2, 3`;
- [ ] frame dimensions đồng nhất;
- [ ] transparent background;
- [ ] anchor consistent;
- [ ] no smoothing;
- [ ] no broken crop;
- [ ] all assets packaged in release.

---

# 27. Self Review

- [ ] Không mở rộng scope.
- [ ] Không sửa Simulation behavior.
- [ ] Không duplicate business logic.
- [ ] Không thêm field vào NPCProfile.
- [ ] Không sửa GA.
- [ ] Không sửa A*.
- [ ] Không sửa ORCA/RVO2.
- [ ] Không thêm animation state vào SimResult.
- [ ] Không thêm dependency mặc định.
- [ ] Không tạo state machine framework.
- [ ] Live và Replay dùng chung renderer.
- [ ] Không timer per NPC.
- [ ] Không DOM node per NPC.
- [ ] Benchmark trước optimization.
- [ ] Không sửa future module ngoài task.

---

# 28. Definition of Done

Task chỉ DONE khi:

- [ ] NPC dot/icon được thay bằng pixel sprite trong Live Simulation.
- [ ] Có 4 model character.
- [ ] Spawn gán model pseudo-random/deterministic.
- [ ] NPC giữ cùng model trong suốt run.
- [ ] 8 hướng hoạt động.
- [ ] Mỗi hướng sử dụng đúng 4 walking frame.
- [ ] Stationary NPC không flicker direction.
- [ ] Pixel art không bị blur.
- [ ] Top-down anchor đúng.
- [ ] Replay reuse cùng renderer.
- [ ] Không thay Simulation core behavior.
- [ ] Không thay result semantics.
- [ ] Automated verification pass.
- [ ] Regression pass.
- [ ] Manual verification pass.
- [ ] Benchmark 200/500/1000 được ghi.
- [ ] Không có dependency mới nếu benchmark chưa chứng minh cần.
- [ ] Release packaging chứa đầy đủ sprite assets.
- [ ] Log được append đúng quy tắc.

---

# 29. Logging

Sau khi thực hiện task, append log.

Primary:

```text
docs/log_frontend.md
```

Nếu thay đổi liên phần hoặc có quyết định kiến trúc:

```text
docs/log.md
```

Không sửa/xóa log cũ.

Log phải có:

- thời gian;
- người thực hiện;
- lý do;
- file bị tác động;
- những gì đã làm;
- source/repo nếu có;
- verification;
- benchmark;
- trạng thái;
- việc tiếp theo;
- local/commit/push status.

---

# 30. Git Rule

Task này không tự:

```text
commit
push
merge
rebase
force-push
```

trừ khi chủ dự án explicit yêu cầu.

Trước khi sửa:

```text
git status
git branch --show-current
```

Giữ nguyên thay đổi có sẵn của người dùng.

---

# 31. Stop

```text
DONE
→ STOP
```

Không tự mở:

```text
idle animation
emotion
sleep
transition
character customization
more models
equipment
sprite editor
PixiJS migration
WebGL renderer
```

Task kế tiếp chỉ mở khi chủ dự án yêu cầu explicit.
