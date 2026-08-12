# R2-FOREST1E — Axe-to-Logs Research and Implementation Note

## Targeted research conclusion

- VERIFIED — The official Steam product page (release 2024-02-22) describes axes as
  weapons/tools and makes tactile freeform cutting/building a core experience:
  https://store.steampowered.com/app/1326470/Sons_of_The_Forest/
- VERIFIED — Official v1.0 notes preserve older saves and describe log/plank orientation,
  tree-structure placement and physics-related vegetation improvements:
  https://store.steampowered.com/news/posts/?enddate=1708624920&feed=steam_community_announcements
- VERIFIED — Official 2024-05-13 notes distinguish split-log animation and whole/log LOD
  behavior, supporting separate physical log representations rather than an inventory-only
  reward:
  https://store.steampowered.com/news/posts/?enddate=1715631634&feed=steam_community_announcements
- VERIFIED — Official 2024-05-23 notes refer to cut actions, log/plank animation state,
  stump state at distance and LOD-aware log storage. Harvest state must therefore survive
  representation changes and cannot depend on hierarchy indices:
  https://store.steampowered.com/news/posts/?enddate=1716490741&feed=steam_community_announcements
- VERIFIED — Current public v1.0 gameplay uploaded 2024-02-23 visibly records a first tree
  being chopped at 00:09:21. It supports repeated axe swings, a physical falling tree and
  usable log outputs, but is not a controlled timing measurement:
  https://www.youtube.com/watch?v=KszWnu7uiCs
- VERIFIED — The current community wiki records whole, 3/4, 1/2 and 1/4 logs plus plank
  variants, and varying yield by tree size. Only whole logs are in this milestone:
  https://sonsoftheforest.wiki.gg/wiki/Log
- INFERRED — Current 2024 player reports consistently describe the final hit/view direction
  as materially influencing fall direction. This is not an official numeric contract, so
  accumulated notch direction remains configurable rather than exact parity.
- HISTORICAL — The pinned historical dump and old TheForest project support stable tree
  state, falling damage and log pooling only as structural leads. No source or defaults are
  copied, and historical values are not treated as current SOTF behavior.
- UNKNOWN — Exact current hit count, damage values, hinge timing, fall torque, log mass,
  species-specific yield and VFX/audio timing are not established by controlled evidence.
- PROVISIONAL — Health, tool damage, cut height, trunk radius, fall/settle timing and 3–5
  log yield are replaceable data. Stable identity and lifecycle do not depend on these values.
- Confident implementation: actual blade contact during a bounded swing window; one damage
  application per tree per swing; directional progressive 3D notch; controlled hinge into
  short-lived physics; stump plus stable physical whole logs; no resurrection/duplication.
- Provisional implementation: procedural upper-body axe motion is used because no licensed
  axe animation/first-person arms package exists in the repository. It is isolated behind a
  motion/contact contract and can be replaced by authored clips without changing harvesting.
- Audio limitation: no prepared licensed chop, creak or impact clips exist. Event hooks and
  pooled emitters are implemented; missing final clips remain an explicit fidelity gap.
- Edge tests: miss, repeated collider contact in one swing, opposite-side hits, invalid kit,
  cell unload during damage/fall, settling timeout, sloped ground placement, duplicate outputs,
  stable IDs and reload after harvesting.
- Deferred: log subdivision, carrying/building consumption, stump removal/regrowth, disk save,
  tree-to-tree chain reactions, multiplayer replication and final authored audio/animation.

## Implemented architecture and lifecycle

`ForestSpeciesDefinition` owns the stable species-to-harvest-kit mapping. Each
`ForestFellingKitDefinition` selects a variant-correct split visual, species-matched whole-log
prefabs and a replaceable `ForestHarvestProfile`. The static cell remains the identity owner;
only its promoted lease owns contact, damage and short-lived physics.

`PlayerAxeHarvestController` drives a camera-relative equip/swing presentation and opens one
bounded blade-contact window. A capsule cast along the actual blade path produces the contact
position, normal and direction. `ForestInteractiveTree` accepts at most one hit from a swing,
updates the 3D notch and accumulates a fall direction. Lethal damage enters controlled hinging,
then enables Rigidbody physics, detects first impact, damps settling and replaces the hidden
upper visual with 3–5 stable whole logs. The detached stump remains at the original cut plane.

The cell-owned `ForestCellInteractionCoordinator` records sparse session deltas by stable
`ForestCellId` + `TreeInstanceId`; it restores an existing lease only when that lease is newly
created or reactivated, so it cannot reset an in-progress fall every proximity tick. Felled trees
therefore do not reappear and output identities (`{TreeInstanceId}.log.NN`) do not depend on a
prefab hierarchy or list index.

Presentation is cell-pooled through `ForestHarvestPresentationPool`: wood/bark chips, sawdust,
dust/leaf impact bursts, reusable audio sources, camera impulse and a controller-rumble hook.
No particle or audio object is instantiated for each hit. Falling trees and awake logs use
physics; static trees retain no per-tree `Update`.

## Reproducible asset pipeline

`Tools/ForestPipeline/Invoke-ForestPipeline.ps1` exposes `Audit`, `Build`, `Validate` and
`Turntable` modes using Blender 4.5 background Python. The manifest pins all 12 Pine/Fir/Maple
variant IDs, ignored source FBX/mesh names, cut geometry, radii, yields, output paths and LOLIPOP
CC BY 4.0 attribution. Sources are never overwritten. The checked-in derivatives close over
project-owned Unity meshes/materials and have no runtime dependency on the ignored raw packs.

Repeat builds produce the same structured semantic geometry checksums and the validator reports
12 felling kits, three log species and two chip meshes. Binary FBX files are not compared by raw
byte hash because the FBX exporter can change embedded export metadata while preserving identical
geometry; the geometry checksum is the stale-output authority. Turntable output is geometry QA;
Unity HDRP screenshots and tests are the material/shader authority.

## Controls and validation

- Primary action: left mouse / configured primary-action input equips and swings the axe.
- Alternate left/right chops are selected by swing parity; locomotion remains camera/player owned.
- Damage is emitted only from physical blade overlap during the active window. Miss recovery
  closes without damage, and one swing cannot damage the same tree twice.
- Pine, Fir and Maple resolve their matching felling kits and species-matched log outputs.
- Automated validation covers contact timing, physical blade overlap, stable identity,
  promotion/demotion, damage/fall/settle/log output and cell reload without resurrection.

## Provisional values and known limitations

- `PROVISIONAL`: health (Fir 88, Pine 100, Maple 112), 19 damage, three notch stages, hinge/fall
  timing, falling mass/drag, impact thresholds, cut dimensions and 3–5 log yield. All live in
  definitions/profiles or project-owned prefabs and can be retuned without changing identity.
- The current axe is a project-authored model with procedural first-person motion. No lawful
  authored arm rig/clip set was present, so equip/chop/recoil/miss/final-chop states are functional
  but not final animation fidelity.
- Chop, crack/creak and heavy-impact clip slots are intentionally empty. Hooks and pooling are
  production-connected; licensed source clips remain required.
- Player damage uses `ForestImpactDamageReceiver` as the current health bridge. The later survival
  health system must consume the same impact contract rather than duplicating falling-tree logic.
- Persistence is session-scoped. The sparse delta schema is ready for a save backend, but no disk
  storage coordinator exists yet.
- Whole construction logs are implemented; carrying, subdivision, building consumption and global
  multi-cell cleanup/streaming remain later systems.

## Next milestone

`R2-FOREST1F — Harvest Presentation and Resource Integration`: acquire/author licensed
first-person arms, axe animations and species-appropriate audio; connect whole logs to carrying,
inventory/building resource ownership and the real save backend while preserving the stable
harvest identities established here.
