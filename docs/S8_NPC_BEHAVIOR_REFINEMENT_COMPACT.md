# S8 — NPC BEHAVIOR REFINEMENT

> Parent: SYSTEM  
> Mode: Solo Developer  
> WIP: 1 task  
> Mục tiêu: NPC **chọn/mua hàng hợp lý hơn** và **di chuyển tới target tự nhiên hơn**.
>
> Không mở rộng sang: Emotion, Social, Queue, Animation, ORCA/RVO2, DOTS/ECS.

---

# 1. Cấu trúc S8

```text
S8 — NPC BEHAVIOR REFINEMENT
│
├── S8.1 — Shopping Decision
│   ├── Target Decision
│   └── Purchase Decision
│
├── S8.2 — Movement & Arrival
│   ├── Smooth Movement
│   └── Arrive / Stop
│
└── S8.3 — Verification
    ├── Behavior Tests
    └── Regression Gate
```

Thứ tự:

```text
S8.1
 ↓
S8.2
 ↓
S8.3
 ↓
DONE
```

Không làm song song.

---

# 2. Rule chung

## Giữ nguyên

```text
C# Simulation Core
Current A*
Current Population / GA
Current SimResult
Current weighted stochastic behavior
```

## Không làm trong S8

```text
Emotion
Social
Memory
Queue
Animation
Spine
ORCA / RVO2
DOTS / ECS
Burst / Jobs
Spatial Hash
Path Cache
new AI framework
new pathfinding framework
```

## Repo tham khảo

```text
A* reference:
https://github.com/roy-t/AStar

Steering / Arrive reference:
https://github.com/meshula/OpenSteer

GA:
https://github.com/giacomelli/GeneticSharp

Statistics:
https://github.com/mathnet/mathnet-numerics
```

ORCA chỉ tham khảo sau này nếu thật sự cần:

```text
https://github.com/snape/RVO2-CS
https://github.com/snape/RVO2
```

---

# S8.1 — SHOPPING DECISION

State: READY  
Priority: P1

## Goal

Tách rõ:

```text
1. Đi tới shelf nào?
2. Tới rồi có mua không?
```

Target selection không đồng nghĩa với purchase.

---

## Flow mục tiêu

```text
Reachable Shelves
      ↓
Target Utility
      ↓
Choose Shelf
      ↓
Travel
      ↓
Arrive
      ↓
Purchase Utility
      ↓
Buy / Skip
```

---

## Dùng các factor đang có

Chỉ dùng nếu field đã tồn tại:

```text
Need
Category Preference
Shopping Mission
Reachability
Path / Distance Cost
Price Sensitivity
Impulsiveness
Price
Promotion
```

Không thêm trait mới.

---

## Task

### S8.1.1 — Audit current decision

Xác định code hiện tại cho:

```text
Need
Candidate
Reachability
Utility
Target Choice
Purchase
Impulse Purchase
```

Kết quả cần có:

```text
Factor
→ dùng ở Target Decision?
→ dùng ở Purchase Decision?
```

---

### S8.1.2 — Separate Target / Purchase

Target Decision:

```text
Need
+ Preference
+ Mission
- Travel Cost
+ Reachability hard filter
```

Purchase Decision:

```text
Need
+ Preference
- Price effect
+ Impulse
+ Promotion nếu có
```

Không confirm purchase ngay khi chọn shelf.

---

### S8.1.3 — Behavior Tests

Bắt buộc test:

```text
D1 — Unreachable shelf
→ không được chọn

D2 — Matching Need cao hơn
→ utility không được thấp hơn do Need

D3 — Distance cao hơn
→ travel factor không được làm target tốt hơn

D4 — PriceSensitive cao + price tăng
→ purchase tendency không tăng do price

D5 — Impulsiveness tăng
→ impulse tendency không giảm
```

---

## Done khi

- [ ] Target Decision tách khỏi Purchase Decision.
- [ ] Unreachable shelf không được chọn.
- [ ] Need / distance / price / impulse tests pass.
- [ ] Full journey vẫn pass.
- [ ] Không thêm trait mới.
- [ ] Không thêm dependency.
- [ ] Log append.

STOP.

---

# S8.2 — MOVEMENT & ARRIVAL

State: BACKLOG  
Ready when: S8.1 DONE  
Priority: P1

## Goal

Giữ A* hiện tại nhưng NPC:

```text
đi mượt hơn
giảm tốc gần target
dừng đúng access point
không overshoot
không rung quanh target
```

---

## Flow mục tiêu

```text
A* Path
   ↓
Next Waypoint
   ↓
Preferred Velocity
   ↓
Smooth Velocity
   ↓
Near Target?
   ├── No  → Continue
   └── Yes → Slow Down
              ↓
          Stop
              ↓
          Interact
```

---

## Task

### S8.2.1 — Audit current movement

Xác định:

```text
position update
waypoint switch
walking speed
target detection
access point
overshoot handling
walkability recheck
```

Không sửa A* trong bước này.

---

### S8.2.2 — Smooth Movement

Implement tối thiểu:

```text
current velocity
→ preferred velocity
→ smooth transition
```

Rule:

```text
actual speed <= walking speed
```

Không physics engine.

Không steering framework lớn.

---

### S8.2.3 — Arrival

Khi gần target:

```text
far
→ normal speed

near
→ slow down

inside stop tolerance
→ stop
→ interact
```

Phải tránh:

```text
overshoot
oscillation
A ↔ B waypoint loop
```

---

### S8.2.4 — Movement Tests

Bắt buộc:

```text
M1 — Speed bound
M2 — Arrive and stop
M3 — No overshoot
M4 — No oscillation
M5 — No wall penetration
M6 — Full journey still pass
```

Manual check:

```text
straight path
90-degree turn
narrow path to shelf
```

---

## Done khi

- [ ] NPC giảm tốc trước target.
- [ ] NPC dừng đúng access point.
- [ ] Không overshoot.
- [ ] Không oscillate.
- [ ] A* invariants vẫn pass.
- [ ] Full journey pass.
- [ ] Không thêm ORCA/RVO2.
- [ ] Không thêm DOTS/ECS.
- [ ] Log append.

STOP.

---

# S8.3 — VERIFICATION GATE

State: BACKLOG  
Ready when: S8.2 DONE  
Priority: P1

## Goal

Không thêm feature.

Chỉ xác minh S8 thật sự cải thiện:

```text
Shopping Decision
+
Movement / Arrival
```

---

## Scenario

### V1 — Need

```text
NPC có need cao
→ matching shelf phải có lợi thế đúng chiều
```

### V2 — Reachability

```text
Shelf tốt nhưng unreachable
→ không được selected
```

### V3 — Price

```text
PriceSensitivity cao
+
price tăng
→ purchase tendency không tăng do price
```

### V4 — Impulse

```text
Impulsiveness tăng
→ impulse tendency không giảm
```

### V5 — Arrival

```text
NPC đi tới shelf
→ giảm tốc
→ dừng
→ interact
```

### V6 — Full Journey

```text
spawn
→ decide
→ move
→ interact
→ buy/skip
→ checkout
→ exit
```

---

## Regression

Chạy:

```text
dotnet build
Population tests
Simulation tests
A* tests
Purchase / Exit tests
SimResult round-trip
git diff --check
```

---

## Done khi

- [ ] S8.1 pass.
- [ ] S8.2 pass.
- [ ] V1–V6 pass.
- [ ] Regression pass.
- [ ] Không có feature ngoài scope.
- [ ] Không có dependency mới.
- [ ] Log append.
- [ ] S8 DONE.

STOP.

---

# 3. ORCA / RVO2

Không nằm trong S8.

Chỉ mở task riêng nếu sau S8 thấy rõ:

```text
NPC overlap nhiều
NPC kẹt nhau
crowd làm sai result
demo nhìn quá giả
```

Khi đó mới xem:

```text
https://github.com/snape/RVO2-CS
https://github.com/snape/RVO2
```

Architecture lúc đó:

```text
A*
↓
Global Path
↓
Preferred Velocity
↓
RVO2 / ORCA
↓
Actual Velocity
```

---

# 4. DOTS / ECS

Không dùng trong S8.

Chỉ xem lại khi benchmark chứng minh .NET runtime hiện tại không đáp ứng.

Không dùng DOTS/ECS chỉ để:

```text
"scale sau này"
```

---

# 5. S8 Tóm tắt

```text
S8.1
Need + Shopping Decision
        ↓
S8.2
Movement + Arrival
        ↓
S8.3
Verification
```

Chỉ 3 task.

Không mở rộng thêm.
