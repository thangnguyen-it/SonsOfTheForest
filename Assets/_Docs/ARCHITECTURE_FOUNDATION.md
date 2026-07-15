# Architecture Foundation

## Project purpose

This project is a clean rebuild foundation for a long-term Unity survival game inspired by Sons Of The Forest.

## Main architectural principle

The rebuild is domain-first, contract-first, data-driven, and scene-composed.

## Architectural layers

- **Domain** owns gameplay rules, concepts, state, and invariants without depending on Unity presentation or infrastructure details.
- **Application** coordinates use cases through explicit contracts and defines how domain capabilities are invoked.
- **Presentation** translates player input, UI, audio, animation, and visual feedback to and from application-facing contracts.
- **Infrastructure** supplies technical adapters such as persistence, networking, registries, and scene composition.

Dependencies should point toward stable domain and application contracts. Scenes act as composition roots that connect concrete presentation and infrastructure adapters.

## Core rebuild order

1. R0 Project Foundation
2. R1 Clean Scene / Composition Root
3. R2 Player Foundation
4. R3 Interaction Foundation
5. R4 Item / Inventory / Equipment
6. R5 Survival Simulation
7. R6 Save Profile
8. R7 Basic AI Pressure
9. R8 Campfire / Cooking
10. R9 Building
11. R10 Companion
12. R11 Multiplayer Adapters

## Implementation rule

No gameplay system should be implemented before its contracts and data ownership are clear.
