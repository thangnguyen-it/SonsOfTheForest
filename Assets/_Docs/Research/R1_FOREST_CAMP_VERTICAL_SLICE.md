# R1 Forest Camp Vertical Slice

Date: 2026-07-18
Evidence scope: resource collection, inventory accounting, and a first campfire in a forest clearing.

## Research conclusion

1. The released game officially presents tactile, hands-on survival interactions rather than menu-only abstraction.
2. The official Steam description explicitly says players break sticks to make fires.
3. It also frames gathering, construction, changing seasons, and survival as connected parts of one world loop.
4. Therefore the next useful milestone is a playable world slice, not another isolated foundation subsystem.
5. Current public evidence supports collecting natural resources and turning sticks into a fire.
6. It does not establish an exact current-build recipe count, input duration, pickup radius, or animation timing.
7. Secondary guides disagree or omit enough context that those values cannot be labelled verified defaults.
8. A press-to-pick-up interaction is a PROVISIONAL accessibility-friendly baseline.
9. The initial recipe of two sticks and four stones is PROVISIONAL and stored in a replaceable asset.
10. Resource capacities, interaction range, world placement, and flame presentation remain configurable.
11. The game-facing loop implemented here is: explore the clearing, collect resources, construct the campfire, ignite it.
12. Tests cover inventory limits, transactional recipe consumption, prefab wiring, scene wiring, and the complete loop.
13. Later work owns hand animation, physical stick breaking, lighter use, diegetic inventory layout, cooking, warmth, and saving.
14. World streaming, procedural distribution, weather, seasons, AI, audio, and destruction also remain later systems.

## Evidence register

| Status | Source | Date/build | Supported behavior | Limitation and implementation effect |
|---|---|---|---|---|
| VERIFIED | [Official Steam page](https://store.steampowered.com/app/1326470/Sons_Of_The_Forest/) | Current store copy, accessed 2026-07-18 | Tactile survival interactions; sticks can be broken to make fires; gathering and construction share the world | Marketing copy does not specify counts or timing; implement the loop but keep tuning provisional |
| INFERRED | [Sons of the Forest Wiki inventory](https://sonsoftheforest.wiki.gg/wiki/Inventory) | Living community documentation, accessed 2026-07-18 | Natural resources participate in an inventory-oriented survival loop | Community-maintained and not authoritative for current defaults; use only as structural support |
| INFERRED | [Pro Game Guides building/crafting guide](https://progameguides.com/sons-of-the-forest/sons-of-the-forest-building-and-crafting-guide/) | Dated public guide, accessed 2026-07-18 | Fire construction is initiated from gathered sticks in the world | Build/date and recipe detail are not controlled evidence; do not freeze its values as canonical |
| UNKNOWN | Current released-game controlled measurement | Not authorized/performed | Exact recipe, gestures, distances, timing, and cancellation rules | Keep every dependent value replaceable and track the fidelity gap |

## Lawful visual sources

All imported environment scans are from Poly Haven and are redistributed under CC0.
The [Poly Haven license](https://polyhaven.com/license) permits use, modification, and redistribution without attribution.
Poly Haven also documents that its library is available without a login or paywall.
No Sons of the Forest model, texture, source code, or extracted game asset is included.

| Asset | Source | Use | Known limitation |
|---|---|---|---|
| Fir Sapling | [Poly Haven](https://polyhaven.com/a/fir_sapling) | Three conifer variants around the clearing | The source is a high-detail scan; production LODs and denser biome variation remain required |
| Tree Stump 01 | [Poly Haven](https://polyhaven.com/a/tree_stump_01) | Landmark and forest debris | One scan cannot represent complete deadwood diversity |
| Rock Moss Set 02 | [Poly Haven](https://polyhaven.com/a/rock_moss_set_02) | Stones, resource pickups, and campfire ring | Pickup readability currently uses scale and placement rather than animation |
| Forest Floor | [Poly Haven](https://polyhaven.com/a/forest_floor) | HDRP ground material for the clearing | Tiled plane is an interim terrain surface, not final terrain authoring |

The imported source README records each asset page, author credit, resolution, and project use; the shared CC0 license text travels with the models.
HDRP Lit materials use the provided albedo, normal, roughness, and displacement data where applicable.
Downloaded DirectX normal maps have their green channel flipped because [Unity 6 uses Y+ normal maps](https://docs.unity3d.com/6000.1/Documentation/Manual/StandardShaderMaterialParameterNormalMap.html).

## Implementation decision

Use one production-oriented vertical slice in `SCN_Foundation` with reusable prefabs rather than test-only primitives.
Keep inventory and recipe rules pure C# so gameplay policy is deterministic and independently testable.
Keep scene behavior in small Application components and player-facing input/HUD in Presentation.
Use the existing interaction contract instead of creating a parallel pickup API.
Use a dedicated `Player/Interact` action bound to `E` without modifying the approved input asset.
Represent the recipe with `CampfireRecipeAsset`, allowing evidence-driven tuning without changing runtime code.
Keep the campfire dormant until the transaction succeeds; consume all requirements atomically.
Use lawful scanned assets now, then add terrain, LODs, instancing, vegetation variety, animation, VFX, and audio incrementally.

## Player-facing result

- Movement and first-person look remain provided by the approved playable-player foundation.
- Aim at loose sticks or stones and press `E` to collect them.
- The HUD reports held sticks and stones and displays the focused interaction prompt.
- Collect two sticks and four stones, aim at the campfire blueprint, and press `E` to build and ignite it.
- Repeating an exhausted pickup or a completed campfire interaction cannot duplicate resources.

## Fidelity gaps to carry forward

- Verify the current game’s exact fire recipe and construction sequence through authorized controlled observation.
- Replace the HUD/debug-oriented counters with the planned diegetic inventory and hand feedback.
- Add pickup, stick-breaking, placement, ignition, and full-body first-person animations.
- Replace the clearing plane with authored terrain and biome distribution; add LODs before expanding forest density.
- Add HDRP fire VFX, smoke, embers, light/audio response, cooking, warmth, fuel, rain interaction, and persistence.
- Validate scale, visibility, collision, performance, and player navigation in a representative built executable.
