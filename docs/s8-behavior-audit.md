# S8 NPC behavior audit

## Decision ownership before S8

| Factor / stage | Existing owner | Target decision | Purchase decision | S8 action |
|---|---|---:|---:|---|
| Need | `NPCRuntimeState.Need`, `NeedAffectSystem`, `SimulationHost.Decide/FinishDwell` | Yes | Yes | Keep active; evaluate independently in both stages. |
| Candidate shelves | `LayoutDefinition.Shelves` | Yes | No | Keep; visited shelves remain excluded. |
| Reachability | `PathGrid.ShelfAccessPaths` | Hard filter | No | Keep current A* and reject shelves with no access path. |
| Category preference | `NPCProfile.CategoryPreferences` | Previously unused | Previously unused | Activate without adding a trait. |
| Shopping mission | `NPCProfile.ShoppingMission` | Previously unused | No | Activate as target-fit context without adding a trait. |
| Travel cost | Squared reachable path length | Yes | No | Keep direction: a longer path never improves target utility. |
| Price / sensitivity | `ProductDefinition.Price`, `NPCProfile.PriceSensitivity` | No | Previously unused | Activate only after arrival. |
| Impulsiveness | `NPCProfile.Impulsiveness` | No | Previously unused | Activate only in purchase/impulse tendency. |
| Promotion | No active contract field | No | No | Omitted; S8 forbids adding a future-only field. |
| Valence / affect | Existing frozen baseline | Previously yes | Previously yes | Remove from the refined shopping decision; do not expand Emotion. |
| Weighted stochastic choice | `SimulationMath.WeightedChoice` | Yes | Random roll | Keep current mechanism. |

## Separation after S8.1

```text
reachable shelf
→ target evaluation (need + preference + mission - travel)
→ weighted target choice
→ travel / dwell
→ purchase evaluation (need + preference - price + impulse)
→ buy or skip
```

`ShoppingDecisionSystem` is a pure Simulation Core policy. `SimulationHost` remains the journey orchestrator. No UI, bridge, persistence, A*, GA framework or result contract logic is duplicated.

## Source decision

- Keep current A*: its wall, corner-cutting, unreachable and bounded-replan tests already pass. [`roy-t/AStar`](https://github.com/roy-t/AStar) remains MIT-licensed reference only.
- Keep [`GeneticSharp`](https://github.com/giacomelli/GeneticSharp) generic selection/crossover/mutation and [`Math.NET Numerics`](https://github.com/mathnet/mathnet-numerics) statistics; S8 only maps already-existing domain fields.
- Use [`OpenSteer`](https://github.com/meshula/OpenSteer)'s MIT-licensed steering shape in S8.2: preferred velocity is derived from target offset and steering is the difference between preferred and current velocity. The full OpenSteer framework is not added as a dependency.

## Movement ownership before S8.2

| Concern | Existing implementation | S8 action |
|---|---|---|
| Position update | Direct scalar step in `SimulationHost.Move` | Retain host ownership; integrate a bounded velocity. |
| Waypoint switch | Snap when the remaining segment is shorter than one tick | Keep ordered A* waypoints and explicit index progression. |
| Walking speed | `NPCProfile.WalkingSpeed × pace` | Preserve as a hard maximum. |
| Target detection | Exact/one-step distance comparison | Use a tolerance derived from active `PathCellSize`. |
| Shelf access point | `PathGrid.ShelfAccessPaths` | Keep unchanged. |
| Overshoot handling | Snap to waypoint | Keep snapping once inside tolerance; zero velocity at final arrival. |
| Walkability | `PathGrid.LineIsWalkable` before committing movement | Keep unchanged for every movement segment. |

The S8.2 update computes a preferred velocity toward the current A* waypoint, blends from current velocity, projects the result back onto the current path segment, caps it at `WalkingSpeed`, and scales preferred speed inside the final slowing radius. Projection preserves the current A* geometry instead of introducing free-form local steering or corner cutting.
