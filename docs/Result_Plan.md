# AIsle — Implementation Plan v2

> **Trạng thái:** kế hoạch kiến trúc tạm chốt để triển khai và benchmark.
>
> **Mục tiêu:** chuyển AIsle từ web prototype hiện tại sang desktop application bằng Unity/C#, giữ lại simulation logic đã có, nâng cấp NPC theo hướng tự nhiên hơn, hỗ trợ mật độ NPC cao, Spine 2D animation, replay/analytics và nhánh so sánh **NPC Simulator ↔ hành vi khách hàng thực tế từ video/POS**.

---

# 1. Technology Stack

Stack chính của AIsle:

```text
Unity
C#
.NET-compatible Core
DOTS / ECS
Burst / Jobs
Spine 2D
```

Phân vai:

```text
Unity
├── Desktop Application
├── Map Editor
├── Simulation Visualization
├── Replay
├── Dashboard
└── Spine 2D Rendering

C# / .NET-compatible Core
├── Domain Model
├── Simulation Rules
├── Utility AI
├── Population
├── Analytics Contracts
└── Import / Export Contracts

DOTS / ECS
├── NPC State
├── Large Population Runtime
├── Movement
├── Crowd
├── Spatial Query
└── High-frequency Simulation Systems

Burst / Jobs
├── Parallel Simulation Work
├── Crowd Calculation
├── Spatial Search
├── Batch State Update
└── Performance Optimization

Spine 2D
├── NPC Visual
├── Walk / Idle
├── Browse
├── Talk
├── Queue
└── Checkout Animation
```

---

# 2. Nguyên tắc triển khai

## 2.1 Không rewrite toàn bộ prototype

Core hiện tại được dùng làm baseline behavior.

Các phần giữ lại và port dần:

- Utility AI.
- Need dynamics.
- Affect dynamics.
- Smart Object / shelf advertisement.
- A* pathfinding.
- Reachability filter.
- Hard collision rules.
- Stuck detection.
- Replan.
- Abandon target.
- Weighted random có seed.
- Poisson spawn.
- Genetic Algorithm population.
- Decision trace.
- Trajectory replay.
- History.
- SimResult.
- Deterministic run.

Quy trình:

```text
JS Prototype
    ↓
Freeze behavior bằng test
    ↓
Port sang C#
    ↓
Cross-check JS ↔ C#
    ↓
Đưa system phù hợp sang DOTS
```

---

## 2.2 Simulation Core độc lập Presentation

Simulation không phụ thuộc renderer.

```text
Simulation
    │
    ├── World State
    ├── NPC State
    ├── Decision
    ├── Movement
    ├── Interaction
    └── Events
          │
          ▼
Presentation Bridge
          │
          ▼
Unity / Spine / UI
```

Không để:

```text
Spine
GameObject
UI
Camera
```

trở thành nơi chứa business logic.

---

## 2.3 Deterministic simulation

Cùng:

```text
Scenario
Population
Configuration
Seed
```

phải tạo cùng kết quả logic.

Rendering FPS không được quyết định simulation behavior.

Ví dụ:

```text
Simulation Tick = fixed
Rendering       = independent
```

Random chia stream:

```text
Spawn RNG
Decision RNG
Social RNG
Animation Variation RNG
```

---

# 3. Kiến trúc tổng thể

```text
                         AIsle Desktop
                             Unity
                               │
            ┌──────────────────┼──────────────────┐
            │                  │                  │
            ▼                  ▼                  ▼
       Map Editor         Live View          Analytics UI
            │                  │                  │
            └──────────────────┼──────────────────┘
                               ▼
                       Presentation Bridge
                               │
                               ▼
                     Simulation Application
                               │
        ┌──────────────────────┼──────────────────────┐
        │                      │                      │
        ▼                      ▼                      ▼
    Simulation              Population              Replay
       Core                    / GA                 / Trace
        │
        ▼
   DOTS / ECS Runtime
        │
        ├── Needs
        ├── Affect
        ├── Behavior
        ├── Navigation
        ├── Crowd
        ├── Social
        ├── Queue
        ├── Purchase
        └── Animation State
```

Nhánh thực tế:

```text
Video
  │
  ▼
Video Analytics
  │
  ├── Detection
  ├── Tracking
  ├── Calibration
  ├── Trajectory
  └── Behavioral Events
  │
  ▼
Observation Schema
  ▲
  │
POS / Transaction
```

Sau đó:

```text
Simulator Result
      │
      ▼
Observation Projection
      │
      ▼
Observation Schema
      │
      ├─────────────┐
      │             │
      ▼             ▼
 Simulation       Reality
      │             │
      └──── Compare ┘
             │
             ▼
        Calibration
```

---

# 4. Repository Structure

```text
AIsle/
│
├── UnityApp/
│   ├── Assets/
│   │   └── AIsle/
│   │       ├── Bootstrap/
│   │       ├── Presentation/
│   │       ├── Editor/
│   │       ├── Rendering/
│   │       ├── Spine/
│   │       ├── Replay/
│   │       ├── Dashboard/
│   │       └── RuntimeBridge/
│   └── Packages/
│
├── src/
│   ├── AIsle.Domain/
│   ├── AIsle.Application/
│   ├── AIsle.Simulation/
│   ├── AIsle.Analytics/
│   ├── AIsle.Contracts/
│   └── AIsle.Infrastructure/
│
├── services/
│   └── VideoAnalytics/
│       ├── Detection/
│       ├── Tracking/
│       ├── Calibration/
│       ├── FeatureExtraction/
│       ├── Aggregation/
│       └── Export/
│
├── models/
│   ├── Detection/
│   ├── Tracking/
│   ├── Population/
│   └── Calibration/
│
├── data/
│   ├── Layouts/
│   ├── Catalog/
│   ├── Population/
│   ├── Observations/
│   ├── SimulationRuns/
│   └── Experiments/
│
├── tests/
│   ├── Domain/
│   ├── Simulation/
│   ├── Integration/
│   ├── Video/
│   └── Performance/
│
└── docs/
```

---

# 5. Cấu trúc Simulation

```text
AIsle.Simulation/
│
├── Core/
│   ├── SimulationClock
│   ├── SimulationContext
│   ├── SeededRandom
│   ├── Scheduler
│   └── EventLog
│
├── Population/
│   ├── NPCProfile
│   ├── Generator
│   ├── Genetic/
│   └── Spawn/
│
├── Needs/
│   ├── Components
│   └── Systems
│
├── Affect/
│   ├── Components
│   └── Systems
│
├── Behavior/
│   ├── Utility
│   ├── Intent
│   ├── Decision
│   └── Memory
│
├── Navigation/
│   ├── Grid
│   ├── AStar
│   ├── Path
│   └── NavigationSystem
│
├── Crowd/
│   ├── SpatialHash
│   ├── ORCA
│   └── CrowdSystem
│
├── Interaction/
│   ├── SmartObject
│   ├── InteractionZone
│   ├── Reservation
│   └── InteractionSystem
│
├── Queue/
│   ├── QueueZone
│   └── QueueSystem
│
├── Social/
│   ├── SocialOpportunity
│   ├── Relationship
│   └── SocialSystem
│
├── Purchase/
│   ├── PurchaseIntent
│   └── PurchaseSystem
│
└── Replay/
    ├── TrajectoryRecorder
    ├── EventRecorder
    └── ResultBuilder
```

---

# 6. NPC Data Model

NPC không dùng một class `NPCBrain` chứa toàn bộ logic.

NPC được ghép từ state/component nhỏ.

```text
NPC
├── Identity
├── TransformState
├── MovementState
├── Needs
├── Affect
├── Personality
├── Intent
├── Target
├── NavigationState
├── CrowdState
├── SocialState
├── QueueState
├── PurchaseState
├── ShortTermMemory
└── AnimationState
```

---

# 7. NPC Profile

NPC profile chứa các tham số tương đối ổn định trong một simulation run.

Ví dụ:

```text
NPCProfile
├── patience
├── sociability
├── exploration
├── impulsiveness
├── priceSensitivity
├── crowdTolerance
├── walkingSpeedPreference
├── categoryPreferences
└── shoppingMission
```

Các biến này là **model parameters**.

Không coi chúng là thông tin camera đo trực tiếp từ một người thật.

---

# 8. Need Model

Tạm chốt chỉ giữ các need phục vụ trực tiếp bài toán cửa hàng.

```text
Needs
├── shoppingNeed
├── categoryNeed[]
├── explorationNeed
├── checkoutUrgency
└── exitUrgency
```

Có thể mở rộng sau bằng dữ liệu thực nghiệm.

Need update theo fixed simulation tick.

---

# 9. Affect Model

Affect phục vụ:

- utility;
- frustration;
- satisfaction;
- queue abandonment;
- Peak-End analysis;
- retention proxy.

```text
AffectState
├── valence
├── arousal
├── frustration
└── satisfaction
```

Event có thể tác động:

```text
longQueue
failedTarget
crowding
successfulPurchase
desiredShelfVisit
checkoutDelay
```

Ví dụ:

```text
longQueue
    → frustration +

successfulPurchase
    → satisfaction +

failedTarget
    → valence -
```

Affect là state của simulator.

Không dùng video để khẳng định trực tiếp cảm xúc thật.

---

# 10. Utility AI

Tạm chốt tiếp tục dùng Utility AI làm decision engine chính.

Pipeline:

```text
Perception
    ↓
Update Needs
    ↓
Update Affect
    ↓
Collect Advertisements
    ↓
Reachability Filter
    ↓
Utility Scoring
    ↓
Top-K
    ↓
Seeded Weighted Choice
    ↓
Intent
```

Utility tổng quát:

```text
Utility =
    NeedValue
  + PersonalityModifier
  + AffectModifier
  + ContextModifier
  + ProductAttraction
  - TravelCost
  - QueueCost
  - CrowdCost
```

Không hard-code behavior kiểu:

```text
if need == food:
    go FoodShelf
```

---

# 11. Smart Object

World object chứa intelligence liên quan đến chính object đó.

```text
SmartObject
├── Advertisement[]
├── InteractionZone[]
├── Capacity
├── Requirements
├── ExpectedEffect
└── Occupancy
```

Shelf:

```text
Shelf
├── category
├── products
├── promotion
├── attractiveness
├── advertisements
├── interactionZone
└── capacity
```

Checkout:

```text
Checkout
├── serviceRate
├── queueZone
├── serviceZone
└── advertisements
```

---

# 12. Interaction Zone

Tạm chốt thay interaction point cố định bằng vùng tương tác.

```text
Shelf
└── InteractionZone
    ├── polygon
    ├── capacity
    ├── preferredSlots
    ├── reservations
    └── occupancy
```

NPC:

```text
Select Smart Object
        ↓
Select Reachable Zone
        ↓
Reserve Approximate Space
        ↓
Navigate
        ↓
Local Avoidance
        ↓
Interact
```

Mục tiêu:

- nhiều NPC đứng trước cùng shelf;
- không xếp đúng một tọa độ;
- giảm cảm giác robot;
- dễ xử lý crowd.

---

# 13. Navigation

## 13.1 Global Pathfinding

Tạm chốt:

```text
A*
```

A* chịu trách nhiệm:

- đường global;
- obstacle;
- wall;
- shelf blocking;
- reachability;
- replan.

Pipeline:

```text
Target
  ↓
A*
  ↓
Path
```

---

## 13.2 Local Crowd Avoidance

Tạm chốt thử nghiệm:

```text
ORCA / RVO2
```

Pipeline:

```text
A* Path
   ↓
Preferred Velocity
   ↓
ORCA / RVO2
   ↓
Actual Velocity
   ↓
Movement
```

Mục tiêu:

- tránh agent;
- tránh crowd locking;
- di chuyển tự nhiên;
- hạn chế NPC chồng nhau.

ORCA/RVO2 được benchmark trước khi đưa vào production runtime.

---

# 14. Spatial Query

Tạm chốt:

```text
Spatial Hash / Uniform Grid
```

Dùng cho:

- neighbor lookup;
- crowd;
- social scan;
- zone density;
- nearby smart object query.

Thay vì:

```text
O(N²)
```

ưu tiên:

```text
query nearby cells
```

Spatial Hash chỉ triển khai khi benchmark cho thấy pairwise query trở thành bottleneck hoặc khi bắt đầu scale population.

---

# 15. Movement Model

NPC movement gồm:

```text
Global Route
+
Preferred Speed
+
Crowd Avoidance
+
Interaction Reservation
```

NPC không cần quay đầu tức thời.

Movement state:

```text
MovementState
├── preferredSpeed
├── currentSpeed
├── desiredDirection
├── currentDirection
├── acceleration
└── stoppingDistance
```

Có thể dùng acceleration/deceleration để tránh chuyển động máy móc.

---

# 16. Queue System

Queue được mô hình hóa bằng vùng.

```text
QueueZone
├── entryArea
├── waitingArea
├── servicePoint
├── capacity
├── serviceRate
└── members
```

NPC:

```text
QueueState
├── joinedAt
├── waitDuration
├── estimatedWait
├── patience
└── abandonThreshold
```

Flow:

```text
See Checkout
    ↓
Estimate Queue
    ↓
Utility
    ├── Join
    ├── Continue Shopping
    └── Exit / Abandon
```

---

# 17. Social System

Tạm chốt social behavior bằng Utility-based Social Interaction.

Không dùng LLM ở simulation core.

Social opportunity:

```text
SocialUtility =
    proximity
  + sociability
  + relationship
  + compatibleState
  - currentGoalUrgency
  - queueUrgency
```

State:

```text
Approach
   ↓
Greet
   ↓
Talk
   ↓
React
   ↓
Leave
```

Conversation data:

```text
topic
duration
affectEffect
animationVariant
```

Ví dụ:

```text
topic = product
duration = 5 sec
affectEffect = +small
```

Social system chỉ tạo đủ tín hiệu để NPC trông tự nhiên và có ảnh hưởng nhỏ tới decision.

---

# 18. Short-Term Memory

NPC lưu memory ngắn.

```text
Memory
├── visitedShelves
├── failedTargets
├── recentInteractions
├── recentSocialPartners
├── queueAbandoned
└── lastDecision
```

Mục tiêu:

- tránh lặp target vô lý;
- không retry shelf unreachable liên tục;
- tạo sự khác biệt giữa các hành trình.

---

# 19. Purchase System

Purchase logic tiếp tục dựa trên:

```text
Need
Utility
Product
Price
Promotion
Impulse
Affect
```

Tách:

```text
Browse
Select
PurchaseIntent
Checkout
ConfirmedPurchase
```

Simulator lưu:

```text
product
category
shelf
decision factors
purchase type
time
```

---

# 20. Animation Architecture

Tạm chốt dùng Spine 2D.

State từ simulation:

```text
AnimationState
├── Idle
├── Walk
├── Browse
├── PickItem
├── Talk
├── React
├── Queue
└── Checkout
```

Flow:

```text
ECS NPC State
      │
      ▼
Animation State
      │
      ▼
Unity Presentation Bridge
      │
      ▼
Spine Controller
```

Simulation không gọi trực tiếp Spine animation.

---

# 21. Spine Rendering Strategy

Mục tiêu:

```text
simulation population
≠
full animation population
```

Tạm chốt LOD:

```text
Visible / Near
    → Full Spine Update

Visible / Far
    → Reduced Update Frequency

Off-screen
    → ECS State Only
```

Việc này giúp tăng population mà không buộc tất cả NPC update skeleton đầy đủ.

---

# 22. DOTS / ECS Architecture

DOTS quản lý data-oriented simulation state.

Ví dụ entity:

```text
NPC Entity
├── NPCId
├── Position
├── Velocity
├── NeedComponent
├── AffectComponent
├── PersonalityComponent
├── IntentComponent
├── NavigationComponent
├── CrowdComponent
├── SocialComponent
├── QueueComponent
├── PurchaseComponent
└── AnimationStateComponent
```

World object có thể là entity:

```text
Shelf Entity
├── Position
├── ShelfData
├── InteractionZone
├── Advertisement
├── Capacity
└── Occupancy
```

---

# 23. DOTS System Groups

Tạm chốt update order:

```text
SimulationInitializationGroup
    ├── SpawnSystem
    └── SpatialIndexBuildSystem

SimulationDecisionGroup
    ├── NeedSystem
    ├── AffectSystem
    ├── PerceptionSystem
    ├── UtilityDecisionSystem
    └── SocialDecisionSystem

SimulationNavigationGroup
    ├── PathRequestSystem
    ├── NavigationSystem
    ├── CrowdAvoidanceSystem
    └── MovementSystem

SimulationInteractionGroup
    ├── ReservationSystem
    ├── InteractionSystem
    ├── QueueSystem
    └── PurchaseSystem

SimulationRecordGroup
    ├── EventRecordSystem
    ├── TrajectoryRecordSystem
    └── KPIAggregationSystem
```

Update order phải cố định.

---

# 24. Burst / Jobs

Burst + Jobs ưu tiên cho system tính toán nhiều entity.

Candidate:

```text
Movement
Crowd Neighbor Query
ORCA
Spatial Hash Build
Need Update
Affect Update
Utility Candidate Scoring
Zone Occupancy
Trajectory Sampling
```

Không cố parallel hóa ngay những phần:

- có dependency phức tạp;
- ít NPC;
- chưa phải bottleneck.

---

# 25. Simulation Frequencies

Không chạy mọi system bằng rendering FPS.

Tạm chốt benchmark với:

```text
Movement        10–20 Hz
Crowd           10–20 Hz
Need            2–5 Hz
Affect          2–5 Hz
Decision        1–2 Hz / event-driven
Social Scan     1–2 Hz
Analytics       1 Hz
Rendering       independent
```

Thông số cuối cùng quyết định sau benchmark.

---

# 26. Replay

Replay tiếp tục là tính năng bắt buộc.

Ghi:

```text
time
npc_id
position
status
target
shelf
affect
important events
```

Không nhất thiết ghi toàn bộ ECS state mỗi frame.

Dùng:

```text
periodic samples
+
state transition events
```

---

# 27. Result Schema

Giữ hướng:

```text
aisle.sim-result.v1
```

Về sau version theo schema migration:

```text
aisle.sim-result.v2
```

Result phải chứa:

```text
scenario snapshot
configuration
seed
population
summary
events
purchases
trajectory
KPI
decision trace
```

---

# 28. Video Reality Analytics

Nhánh này do **Trần Đăng Khôi** phụ trách chính.

Mục tiêu:

```text
Recorded Video / CCTV
        ↓
Anonymous Customer Tracks
        ↓
Behavior Features
        ↓
Observation Data
```

Video không sinh NPC trực tiếp.

Video sinh **ground-truth behavioral observations** để:

- phân tích cửa hàng;
- so với simulator;
- calibrate population;
- validate assumptions.

---

# 29. Video Pipeline

Tạm chốt pipeline:

```text
Video
  ↓
Decode
  ↓
Person Detection
  ↓
Multi-Object Tracking
  ↓
Track Cleaning
  ↓
Camera Calibration
  ↓
Store Coordinates
  ↓
Zone Event Engine
  ↓
Feature Extraction
  ↓
Aggregation
  ↓
aisle.observation.v1
```

---

# 30. Detection

Candidate tạm chốt để benchmark:

```text
RT-DETR
YOLOX
```

Không khóa hệ thống vào một model.

Interface:

```text
IPersonDetector
```

Benchmark bằng video cửa hàng thực tế:

```text
person recall
false positive
occlusion
FPS
VRAM
track quality downstream
```

Chọn detector dựa trên kết quả end-to-end.

---

# 31. Tracking

Tạm chốt baseline:

```text
ByteTrack
```

Output:

```text
frame
track_id
bbox
confidence
```

`track_id` là ID tạm thời trong một video/session.

Không dùng như customer identity.

---

# 32. Camera Calibration

Mục tiêu chuyển:

```text
camera pixel
→
store floor coordinate
```

Tạm chốt:

```text
Homography
```

Flow:

```text
Known floor points
      ↕
Image points
      ↓
Homography Matrix
      ↓
Person foot point
      ↓
Store X/Y
```

Điểm người:

```text
bottom-center bounding box
```

Sau calibration:

```text
track:
    time
    x
    y
```

Trajectory này có cùng hệ tọa độ logic với simulator.

---

# 33. Track Cleaning

Pipeline:

```text
Raw Track
  ↓
Confidence Filter
  ↓
Short Track Removal
  ↓
Gap Handling
  ↓
Smoothing
  ↓
Clean Trajectory
```

Candidate:

```text
Kalman Filter
```

Không nội suy gap quá dài.

---

# 34. Layout Zones

Layout business cần định nghĩa zone.

```text
Store
├── EntranceZone
├── ExitZone
├── ShelfInteractionZone[]
├── AisleZone[]
├── CheckoutZone
└── QueueZone
```

Video dùng cùng logical zone IDs với simulator.

Ví dụ:

```text
SHELF_DRINKS_01
SHELF_SNACKS_02
CHECKOUT_01
```

---

# 35. Video Event Extraction

Từ trajectory sinh event:

```text
STORE_ENTER
STORE_EXIT
ZONE_ENTER
ZONE_EXIT
SHELF_APPROACH
SHELF_BROWSE
QUEUE_JOIN
QUEUE_LEAVE
CHECKOUT_ENTER
QUEUE_ABANDON
```

Dùng hysteresis/debounce để tránh event rung ở boundary.

---

# 36. Feature Level 1

Bộ feature bắt buộc:

| Feature | Mục đích |
|---|---|
| entry time | traffic |
| exit time | journey |
| visit duration | dwell |
| trajectory | movement |
| walking speed | movement behavior |
| zone visit | customer flow |
| zone dwell | area interest |
| shelf approach | shelf interest |
| shelf dwell | browse behavior |
| checkout enter | conversion funnel |
| queue wait | checkout experience |
| store occupancy | crowd |
| zone occupancy | local crowd |
| exit without checkout | abandonment proxy |

---

# 37. Feature Level 2

Sau khi pipeline Level 1 ổn định:

```text
path length
stop count
turn rate
revisit count
unique zone ratio
zone transition matrix
queue abandon rate
crowd exposure
group candidate
social proximity event
```

---

# 38. Shelf Browse Detection

Camera chỉ xác định mức độ quan sát được.

Tách:

```text
SHELF_APPROACH
SHELF_BROWSE
PURCHASE
```

Tạm chốt shelf browse:

```text
inside shelf interaction zone
AND
low speed
AND
dwell > threshold
```

Optional sau này:

```text
body orientation
pose
hand motion
```

Không dùng camera làm purchase ground truth chính.

---

# 39. Queue Detection

Tạm chốt:

```text
inside QueueZone
AND
low movement
AND
progress toward service point
```

Trích:

```text
queue length
wait time
join event
leave event
abandon event
```

Các metric này dùng calibrate:

```text
patience
queueCost
checkoutUrgency
```

---

# 40. Social Observation

Video chỉ sinh tín hiệu có thể quan sát.

Tạm chốt:

```text
SOCIAL_PROXIMITY
GROUP_CANDIDATE
```

Candidate:

```text
distance < threshold
AND
duration > threshold
```

Optional:

```text
similar heading
co-movement
facing estimate
```

Không suy ra nội dung hội thoại.

---

# 41. POS Integration

POS cung cấp purchase ground truth.

POS schema tối thiểu:

```text
timestamp
transaction_id
product_id
category
quantity
price
```

Phase đầu so aggregate theo:

```text
5-minute
15-minute
session
```

Không cần ghép một track camera với một hóa đơn cụ thể.

---

# 42. Observation Schema

Cả Reality và Simulator phải quy về cùng schema.

Tạm chốt:

```text
aisle.observation.v1
```

Ví dụ:

```json
{
  "schema": "aisle.observation.v1",
  "source": "video",
  "storeId": "store-001",
  "sessionId": "session-001",
  "customers": [],
  "metrics": {}
}
```

Reality:

```text
Video / POS
    ↓
Observation
```

Simulator:

```text
SimResult
    ↓
Observation Projector
    ↓
Observation
```

Analytics chỉ so `Observation`.

---

# 43. Simulator Observation Projection

Không so trực tiếp internal state như:

```text
personality
need
affect
```

với camera.

Simulator phải project ra observable metrics:

```text
NPC Internal State
        ↓
Observation Projector
        ↓
trajectory
zone dwell
shelf visit
queue wait
checkout
purchase
occupancy
```

Nhờ vậy Reality và Model có cùng miền so sánh.

---

# 44. Sim-to-Real Comparison

Các nhóm metric chính:

## Traffic

```text
arrival curve
store occupancy
```

## Movement

```text
trajectory distribution
speed distribution
path length
heatmap
```

## Zone

```text
zone visit rate
zone dwell
transition matrix
revisit
```

## Shelf

```text
approach rate
browse dwell
```

## Queue

```text
queue length
wait time
abandon rate
```

## Conversion

```text
checkout rate
transaction count
purchase conversion
```

---

# 45. Calibration

GA hiện tại được mở rộng sang calibration.

Mục tiêu:

```text
find NPC population parameters
such that
simulation observations
≈
real observations
```

Candidate latent parameters:

```text
patience distribution
exploration distribution
walking speed distribution
category preference distribution
crowd tolerance distribution
impulsiveness distribution
queue sensitivity distribution
```

Loss tạm chốt dạng weighted distance:

```text
Loss =
    w1 * arrivalDifference
  + w2 * dwellDifference
  + w3 * transitionDifference
  + w4 * queueDifference
  + w5 * heatmapDifference
  + w6 * conversionDifference
```

Từng metric vẫn được hiển thị riêng trong dashboard.

---

# 46. Business Dashboard

Dashboard tách ba loại dữ liệu:

```text
OBSERVED
MODELED
ASSUMED
```

## Observed

Dữ liệu từ:

```text
video
POS
```

## Modeled

Dữ liệu simulator.

## Assumed

Parameter chưa được calibrate hoặc design constant.

Ví dụ comparison:

```text
Metric              Reality      Simulator      Delta
-----------------------------------------------------
Shelf Drinks Dwell   18.2 s       16.7 s        -8.2%
Queue Wait           42.0 s       47.5 s       +13.1%
Checkout Rate        71.0%        68.2%         -2.8 pp
```

---

# 47. Heatmap

Heatmap dùng chung coordinate system.

```text
Reality Trajectory
        │
        ├──► Reality Heatmap
        │
Simulation Trajectory
        │
        └──► Simulation Heatmap
```

Có thêm:

```text
Difference Heatmap
```

để biết simulator đang sai ở vùng nào.

---

# 48. Data Privacy

Video analytics ưu tiên:

```text
anonymous tracking
local processing
minimal retained data
```

Mặc định không triển khai:

```text
face recognition
customer identity recognition
```

Dữ liệu lưu lâu dài ưu tiên:

```text
trajectory
events
aggregate metrics
```

Raw video retention phải cấu hình theo chính sách doanh nghiệp và quy định pháp lý.

---

# 49. Video Analytics Deployment

Tạm chốt tách Video Analytics khỏi Unity runtime.

```text
Video File
   ↓
Video Analytics Process
   ↓
aisle.observation.v1
   ↓
Unity AIsle
```

Lợi ích:

- sử dụng Python/CV ecosystem;
- độc lập GPU inference;
- đổi model dễ;
- không ảnh hưởng simulation tick;
- có thể xử lý offline.

Unity chỉ import kết quả.

---

# 50. Application Flow

## Simulation Workflow

```text
Open Project
    ↓
Load Layout
    ↓
Load Catalog
    ↓
Load / Generate Population
    ↓
Set Simulation Parameters
    ↓
Run
    ↓
Live View
    ↓
Replay
    ↓
KPI
    ↓
Export / Compare
```

## Reality Workflow

```text
Import Video
    ↓
Process Video
    ↓
Validate Calibration
    ↓
Generate Observation
    ↓
View Reality Heatmap / KPI
    ↓
Compare Simulator
```

## Calibration Workflow

```text
Load Reality Observation
    ↓
Select Parameters
    ↓
Run Population Search
    ↓
Compare Loss
    ↓
Select Population Model
    ↓
Validate Holdout Scenario
```

---

# 51. Migration Plan

## Stage 1 — Baseline

Giữ web prototype làm reference.

Tạo golden tests:

```text
input
seed
expected decisions
expected events
expected purchases
expected summary
```

---

## Stage 2 — Contracts

Tạo:

```text
Scenario
Layout
Catalog
NPCProfile
Population
SimulationConfig
SimResult
Observation
```

Contracts không phụ thuộc Unity.

---

## Stage 3 — C# Core

Port:

```text
Seeded RNG
Utility
Need
Affect
Smart Object
A*
Spawn
Purchase
Result
```

Chạy test tương đương JS.

---

## Stage 4 — Unity Presentation

Tạo:

```text
Desktop shell
Map editor
Live simulation view
Replay
Basic dashboard
```

Core vẫn chạy độc lập Presentation.

---

## Stage 5 — NPC Natural Behavior

Thêm:

```text
Interaction Zone
Queue Zone
ORCA/RVO2
Social
Short-Term Memory
Movement Acceleration
Spine State Bridge
```

---

## Stage 6 — DOTS Migration

Ưu tiên port system có data lớn:

```text
NPC state
movement
spatial query
crowd
need
affect
```

Decision system port sau khi data contract ổn định.

---

## Stage 7 — Video Reality

Làm:

```text
Detection
Tracking
Homography
Trajectory
Zone Events
Observation Export
```

---

## Stage 8 — Sim-to-Real

Nối:

```text
Simulation Observation
Reality Observation
Comparison
Calibration
Dashboard
```

---

# 52. Performance Plan

Benchmark các mốc:

```text
200 NPC
500 NPC
1,000 NPC
2,000 NPC
```

Đo:

```text
simulation ms/tick
render FPS
main-thread time
Burst job time
crowd time
A* time
utility time
Spine time
memory
```

Mục tiêu của benchmark là tìm bottleneck thật.

Không thêm optimization chưa cần thiết.

---

# 53. Performance Optimization Order

Nếu bottleneck ở:

### Neighbor Query

```text
Spatial Hash
```

### Crowd

```text
Burst + Jobs
ORCA optimization
lower crowd frequency
```

### A*

```text
path cache
shared routes
hierarchical pathfinding
```

### Decision

```text
lower frequency
event-driven reevaluation
candidate filtering
```

### Spine

```text
animation LOD
visibility culling
reduced update rate
```

### Analytics

```text
batch processing
offline aggregation
```

---

# 54. Testing Plan

## Simulation Unit Test

```text
Need
Affect
Utility
A*
InteractionZone
Queue
SocialUtility
Purchase
Seed RNG
```

## Determinism Test

```text
same scenario
+
same population
+
same seed
=
same logical result
```

## Integration Test

```text
spawn
→ browse
→ social / queue
→ purchase
→ checkout
→ exit
```

## Video Test

```text
video
→ detection
→ tracking
→ calibration
→ trajectory
→ event
→ observation
```

## Performance Test

```text
200
500
1000
2000 NPC
```

---

# 55. Phân công đề xuất

## Phan Trung Kiên — Simulation Core

Phụ trách:

```text
C# Core
Simulation Clock
Utility
Need
Affect
A*
Interaction
Queue
DOTS migration
Determinism
```

---

## Lê Bảo Khang — Population / GA / Calibration

Phụ trách:

```text
NPCProfile
Population Schema
Genetic Algorithm
Population Generation
Calibration Parameters
Calibration Loss
Population Validation
```

---

## Trần Đăng Khôi — Video Reality Analytics

Phụ trách:

```text
Detection benchmark
ByteTrack
Track cleaning
Homography
Trajectory
Zone events
Feature extraction
Observation export
```

Output chính:

```text
aisle.observation.v1
```

---

## Phạm Tài Nguyên / Đặng Hải Đăng — UI/UX

Phụ trách:

```text
Unity Desktop UI
Layout Editor
Live Simulation View
Replay UI
Heatmap
Reality vs Simulation
Comparison Dashboard
```

---

# 56. Hướng kỹ thuật tạm chốt

| Problem | Tạm chốt |
|---|---|
| Desktop App | Unity |
| Language | C# |
| Core | .NET-compatible |
| Large NPC runtime | DOTS / ECS |
| Parallel CPU | Burst / Jobs |
| 2D Character | Spine 2D |
| Decision | Utility AI |
| Environment Intelligence | Smart Object |
| Global Navigation | A* |
| Local Avoidance | ORCA / RVO2 |
| Spatial Query | Spatial Hash |
| Shelf Access | Interaction Zone |
| Queue | Queue Zone + Patience |
| Social | Utility-based Social |
| NPC Variation | Seeded Weighted Random |
| Population | GA |
| Replay | Trajectory + Event |
| Video Detection | RT-DETR / YOLOX benchmark |
| Tracking | ByteTrack |
| Coordinate Mapping | Homography |
| Track Smoothing | Kalman candidate |
| Reality Data | Observation Schema |
| Purchase Truth | POS |
| Sim-to-Real | Observation Comparison |
| Calibration | GA-based parameter fitting |

Các mục trên là **baseline để triển khai thử**.

Thay đổi thuật toán chỉ thực hiện khi:

```text
test
benchmark
real-data validation
```

cho thấy baseline không đáp ứng.

---

# 57. TASKS

## Phase 1 — Foundation

- [ ] Chốt Contracts.
- [ ] Freeze JS golden tests.
- [ ] Tạo Unity project structure.
- [ ] Port C# deterministic core.

## Phase 2 — NPC

- [ ] Interaction Zone.
- [ ] Queue system.
- [ ] ORCA/RVO2 prototype.
- [ ] Social + memory.
- [ ] Spine bridge.

## Phase 3 — DOTS

- [ ] ECS component schema.
- [ ] Port movement/crowd.
- [ ] Burst + Jobs.
- [ ] Benchmark 200–2000 NPC.

## Phase 4 — Reality

- [ ] Detector + ByteTrack.
- [ ] Homography.
- [ ] Trajectory + zone events.
- [ ] `aisle.observation.v1`.

## Phase 5 — Sim-to-Real

- [ ] Simulator Observation Projector.
- [ ] Reality vs Sim comparison.
- [ ] POS integration.
- [ ] GA calibration.
- [ ] Comparison dashboard.

---

# 58. Kiến trúc mục tiêu

```text
                              AISLE
                                │
              ┌─────────────────┴─────────────────┐
              │                                   │
              ▼                                   ▼
        NPC SIMULATOR                          REALITY
              │                                   │
      Unity + C# Core                         Video + POS
              │                                   │
         DOTS / ECS                              │
       Burst / Jobs                              │
              │                                   │
   Utility + A* + ORCA                Detection + Tracking
   Smart Object + Queue                    Homography
   Social + Affect                       Zone Events
              │                                   │
              ▼                                   ▼
        Sim Observation                  Real Observation
              │                                   │
              └────────────────┬──────────────────┘
                               ▼
                          Compare
                               │
                               ▼
                         Calibration
                               │
                               ▼
                      Business Scenario Test
```

---

# 59. Kết luận triển khai

AIsle được phát triển theo bốn lớp chính:

```text
1. Simulation Core
2. High-density NPC Runtime
3. Unity Presentation
4. Reality Analytics
```

Stack:

```text
Unity
C#
.NET-compatible Core
DOTS / ECS
Burst / Jobs
Spine 2D
```

NPC baseline:

```text
Utility AI
Smart Objects
A*
ORCA / RVO2
Interaction Zone
Queue System
Social Utility
Affect
Short-Term Memory
Seeded Random
```

Reality baseline:

```text
Person Detection
ByteTrack
Homography
Trajectory
Zone Events
POS
Observation Schema
```

Vòng lặp sản phẩm:

```text
Observe Reality
      ↓
Create Behavioral Observation
      ↓
Run NPC Simulator
      ↓
Compare
      ↓
Calibrate
      ↓
Test New Layout / Policy
      ↓
Support Business Decision
```

Đây là kiến trúc tạm chốt cho giai đoạn triển khai tiếp theo.
