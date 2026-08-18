# RVO2-CS integration audit

Date: 2026-08-18  
Scope: `docs/run.txt` — local crowd avoidance after S8

## Sources reviewed

- ORCA research overview: https://gamma-web.iacs.umd.edu/ORCA/
- RVO2 library overview: https://gamma-web.iacs.umd.edu/RVO2/
- RVO2 C++ 2.0 documentation: https://gamma-web.iacs.umd.edu/RVO2/documentation/2.0/
- RVO2 C# 2.0 documentation: https://gamma-web.iacs.umd.edu/RVO2/documentation/cs-2.0/
- Canonical C++ repository: https://github.com/snape/RVO2
- Official C# repository: https://github.com/snape/RVO2-CS

ORCA assigns each agent half of the responsibility for pairwise collision avoidance and solves the resulting velocity constraints with a low-dimensional linear program. RVO2's intended host flow is: provide agents and preferred velocities, step the simulator, then read actual velocities.

## Dependency decision

Selected upstream: `snape/RVO2-CS` tag `v2.0.1`, revision `5b7147d36d5cc6310c8e74c6955145c0fdc5fe06`.

- License: Apache-2.0; upstream license and attribution are retained.
- Runtime compatibility: the official v2.0.1 source targets .NET Framework 4.5 and uses only APIs available to Unity's managed runtime; the same source compiles cleanly in AIsle's `net10.0` project. Keeping it inside the UPM package avoids a desktop-only external assembly reference.
- Dependencies: no external package dependency in the upstream project.
- API used: `Simulator.Clear`, `setTimeStep`, `addAgent`, `setAgentPrefVelocity`, `doStep`, and `getAgentVelocity`.
- State model: upstream exposes a process-wide singleton. `Rvo2Adapter` serializes its clear/add/step/read cycle so parallel AIsle hosts cannot share partial state.
- Distribution: no official `RVOCS` package is published on NuGet. The official source is therefore vendored under `src/AIsle.Simulation/ThirdParty/RVO2`, rather than replacing ORCA with a custom implementation.
- Maintenance boundary: the vendored revision is pinned in `src/AIsle.Simulation/ThirdParty/RVO2/SOURCE.md`; upstream source is unmodified.

## Existing movement audit and retained ownership

Before this change, `SimulationHost.Move` generated a smoothed S8 velocity toward the current A* waypoint, projected all movement back onto that segment, then applied a pairwise positional push in `Separate`. That final push was local separation but was not reciprocal velocity-obstacle planning.

The implemented ownership is now:

`A* path → current waypoint → S8 preferred velocity → RVO2 actual velocity → wall-safe position update`

- A* still owns global routing, shelf access, static-wall reachability, replan, abandon, checkout, and exit.
- S8 still owns target selection, purchasing, arrival slowdown, speed limits, and stopping.
- RVO2 owns only local agent-agent velocity adjustment.
- Stationary active NPCs are submitted with zero preferred/max velocity so moving NPCs avoid them.
- Static geometry is intentionally not duplicated into RVO2 in this increment. `PathGrid.LineIsWalkable` remains the final invariant and falls back to the A* preferred velocity before invoking the existing bounded replan path.
- The old positional `Separate` force is no longer executed. `SeparationStrength` remains in the serialized contract only for backward compatibility.

## Tunable parameters

| Parameter | Default | Supported range | Source/reason | Verification |
|---|---:|---:|---|---|
| agent radius | `CollisionRadius / 2` = 0.16 m | `CollisionRadius > 0` | RVO2 uses per-agent radius; two radii preserve the existing 0.32 m center-distance contract | R1–R3 |
| max speed | NPC `WalkingSpeed` | non-negative profile value | Existing S8 speed ownership; RVO2 cannot exceed it | M1, R1–R3 |
| neighbor distance | 2.0 m | 0.32–20 m | Official RVO2 API: larger values see agents sooner at higher cost; default covers about six collision diameters | config test, R1–R3, R8 |
| max neighbors | 10 | 1–100 | Official RVO2 API trade-off between safety neighborhood and runtime | config test, R3, R8 |
| agent time horizon | 2.0 s | 0.1–30 s | Official RVO2 API: larger values react earlier with less velocity freedom | config test, R1–R3 |
| obstacle time horizon | 2.0 s | 0.1–30 s | Required by official agent API; reserved for a future evidence-based static-obstacle adapter, with static safety currently owned by A* | config test |

The R8 stress fixture deliberately uses 3 m / 30 neighbors / 4 s to represent a dense bidirectional crowd; production defaults remain conservative.

## Failure behavior

- Zero/one submitted agent: return the S8 preferred velocity exactly.
- RVO2 exception or invalid result cardinality: use preferred velocities for the tick and emit one `avoidance-fallback` event per host.
- RVO2 candidate crosses static geometry: retry the A* preferred velocity; if still blocked, stop and enter the existing stuck/replan/abandon flow.

## Verification record

- R1: two head-on NPCs avoid severe overlap and both reach their goals.
- R2: crossing paths avoid severe overlap and terminate.
- R3: twelve NPCs in one aisle avoid severe overlap and at least 75% progress.
- R4: existing wall, corner, narrow-corridor, and full-trajectory invariants remain green.
- R5: shelf access arrival and stop remain green.
- R6: purchase → checkout → exit full journey remains green.
- R7: one-agent output equals preferred velocity; adapter failure falls back safely.
- R8: 50/100/200 NPC timings and collision/overlap counts are stored in `docs/benchmarks/rvo2-2026-08-18.json`.

R8 counts `overlapPairTicks` whenever a pair is closer than the configured 0.32 m center separation, and counts `collisionPairTicks` only for severe center intrusion below one 0.16 m agent radius. Counts are pair-ticks, not distinct NPC totals.

No DOTS/ECS/Burst/Jobs, A* rewrite, utility rewrite, population rewrite, social/memory/emotion/animation/queue system, or custom ORCA solver was introduced.
