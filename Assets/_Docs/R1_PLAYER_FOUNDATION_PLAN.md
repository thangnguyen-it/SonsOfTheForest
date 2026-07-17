# R1 Player Foundation Plan

## 1. Purpose

This document prepares Player Foundation implementation without creating runtime player code, prefabs, or scene composition. It defines the smallest useful R1 boundary, maps the old prototype to that boundary, identifies contract gaps, and establishes a test-first delivery order.

R1 must create a stable base for later survival, interaction, equipment, persistence, presentation, and multiplayer adapters without owning those domains.

## 2. Foundation Philosophy

- R1 is not a quick movement prototype.
- The old PlayerController is reference material, not a copy target.
- Domain intent and movement decisions must be testable without a scene, camera, Input System device, CharacterController, UI, AI, save manager, or Netcode.
- Unity components translate between engine state and explicit Player contracts.
- Tests define movement invariants before runtime composition.
- New namespaces must begin with SonsOfTheForest; TheForest is forbidden under Assets/_Game.
- The official SCN_Foundation remains a minimal composition root. Player objects enter it only through the later approved prefab-composition task.

### Research basis

The public Sons Of The Forest product page describes a first-person, open-world survival-horror experience built around survival, interaction, exploration, combat, building, changing seasons, and solo/co-op play. This establishes long-term capability requirements, not exact movement values or an instruction to reproduce protected implementation details:

- [Sons Of The Forest public product page](https://store.steampowered.com/app/1326470/The_Forest/?l=english)

The project uses Unity 6000.3 and Input System 1.18.0. Unity documents Input Actions as a device-independent separation between input purpose and physical controls, with action maps, control schemes, and runtime binding overrides. The existing InputSystem_Actions asset already has separate Player and UI maps plus Keyboard and Mouse, Gamepad, Touch, Joystick, and XR control schemes:

- [Unity Input System 1.18 Actions](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/Actions.html)
- [Unity Input System 1.18 bindings and overrides](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/ActionBindings.html)
- [Unity Input System 1.18 Player Input](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.18/manual/PlayerInput.html)

Unity documents CharacterController as a suitable collision-constrained controller for first-person movement that does not use Rigidbody dynamics, with explicit slope limit, step offset, skin width, center, height, grounded state, and velocity. R1 should therefore begin with a CharacterController adapter while keeping the movement model independent enough to replace that adapter:

- [Unity 6.3 Character Controller reference](https://docs.unity3d.com/6000.3/Documentation/Manual/class-CharacterController.html)

No commercial game code, assets, balance values, or non-public implementation details were researched or reproduced.

## 3. Target Player Experience

The foundation should support grounded, deliberate first-person survival control:

- Walk is the reliable baseline.
- Sprint is an explicit request that becomes active only when movement and policy allow it.
- Crouch is a stable stance, not a temporary speed trick, and must be safe under low ceilings.
- Jump is deliberate, grounded, and one-shot.
- Camera yaw controls facing while pitch remains isolated to the view hierarchy.
- Terrain response is predictable on steps, slopes, edges, and uneven ground.
- Movement state is observable so later systems can derive stamina expenditure, noise, animation, audio, interaction context, and persistence data.
- Future restrictions from injury, carrying, blocking, water, cold, and stamina can modify movement without being implemented inside the motor.

Foundation feel should come from consistent acceleration, deceleration, ground handling, stance transitions, and camera response. Headbob, sway, shake, weapon motion, and HUD feedback are later presentation layers.

## 4. R1 Scope

R1 includes:

1. Pure movement and look intent data.
2. Validated movement configuration data.
3. Pure movement-mode and look-angle decisions.
4. Player movement-state reporting.
5. A raw-input adapter for Move, Look, Sprint, Crouch, and Jump only.
6. A CharacterController-based Unity movement adapter.
7. A camera/look adapter with body yaw and view pitch separation.
8. Explicit body, view, interaction-origin, hand, and carry anchors for future adapters.
9. Safe standing/crouching shape transitions with an obstruction check.
10. Foundation validation and EditMode tests.
11. A player prefab and SCN_Foundation composition only in the dedicated R1-F task.

Minimal R1 locomotion modes are Idle, Walk, Sprint, Crouch, Airborne, and GroundedBlocked. R1 does not need swimming, climbing, sliding, prone, ledge traversal, knockback, ladders, vehicles, or scripted movement.

## 5. Out of Scope

R1 excludes:

- survival simulation and stamina consumption;
- inventory and item implementations;
- equipment, hands animation, combat, blocking, ranged weapons, and damage;
- AI perception, stealth scoring, noise propagation, and stealth kills;
- interaction raycasting or prompt UI;
- crafting, cooking, water, building, log carrying, and world harvesting;
- player save/load orchestration;
- multiplayer authority, prediction, reconciliation, and Netcode components;
- full HUD, headbob, sway, camera shake, audio, VFX, and animation graphs;
- old scenes, prefabs, Resources data, and old project scripts.

R1 may expose neutral state and anchor seams needed by those systems, but it must not implement their behavior.

## 6. Sons Of The Forest-Inspired Requirements

These are rebuild design targets derived from the survival-horror genre, public product positioning, existing foundation constraints, and the old-script audit. They are not claims about the commercial game's internal implementation.

### 6.1 Walk, sprint, crouch, jump, and gravity

- Move input is clamped to unit magnitude so diagonal input cannot exceed configured speed.
- Walk is the fallback whenever sprint is not requested or permitted.
- Sprint requires nontrivial planar input, standing stance, appropriate ground state, and an allow-sprint constraint.
- Crouch takes precedence over sprint.
- Crouch state persists according to the chosen hold/toggle policy; the policy must be explicit and independently testable.
- Standing up requires a successful capsule-clearance query. A blocked stand request leaves the player crouched.
- Jump is accepted once per press and only from an allowed grounded state.
- Gravity has explicit downward semantics and is independent of frame rate.
- Grounded motion uses a small configurable downward adhesion/snap policy rather than an unexplained magic value.
- Walkable slopes expose their normal and angle. Desired planar motion may be projected onto walkable ground.
- Slopes above the configured limit are not treated as walkable. Any sliding policy must be explicit rather than an accidental CharacterController side effect.
- Step offset, slope limit, skin width, radius, standing height, crouching height, and center changes are validated as one coherent capsule configuration.

### 6.2 Input architecture

- InputSystem_Actions remains the binding source; R1 does not poll Keyboard.current, Mouse.current, or Gamepad.current in gameplay logic.
- The existing Player map supplies Move, Look, Crouch, Jump, and Sprint. Attack, Interact, Previous, and Next are ignored by Player Foundation and remain owned by later domains.
- The UI map remains separate so UI mode can disable gameplay actions without changing domain state.
- PlayerInputAdapter converts InputAction callback values into MovementIntent and LookIntent. It does not move transforms.
- Movement is continuous state; jump is a latched one-shot consumed exactly once; crouch and sprint policy are explicit.
- Mouse and gamepad look use device-appropriate scaling. Mouse delta must not be multiplied by frame time as though it were a rate; stick look is a rate and is integrated over time.
- Control schemes and binding overrides stay outside Player domain logic, preserving a future rebind UI.
- Gameplay code must not rely on callback ordering when multiple actions occur in one frame.

### 6.3 Movement modifiers and exertion readiness

- The motor resolves a neutral MovementConstraints or modifier input instead of reading SurvivalStats, equipment, or blocking components.
- R1 default constraints permit normal movement and sprint.
- Later sources may contribute speed multipliers, sprint permission, jump permission, or stance restrictions for stamina, injury, carrying, blocking, crouch, water, and cold.
- Modifier composition must be deterministic, clamped, source-identifiable, and order-independent unless priority is an explicit part of the contract.
- Movement reports requested and resolved locomotion states. A later Survival adapter consumes exertion facts and owns stamina drain.
- The motor never writes stamina, energy, health, temperature, or HUD state.

### 6.4 Stealth and noise readiness

- R1 reports locomotion mode, grounded state, planar speed, and optionally distance traveled.
- Crouch state is factual player state; it is not itself an AI stealth score.
- A later noise adapter converts motion facts, surface type, equipment, and stance into typed noise events.
- A later perception system decides visibility and audibility.
- Player movement never references AI classes, global NoiseSystem APIs, tags, or enemy registries.

### 6.5 Camera and look

- Body yaw and camera pitch are separate values and transforms.
- Pitch is clamped symmetrically by validated minimum and maximum values.
- Yaw accumulation and wrapping are deterministic pure math.
- Sensitivity, invert-Y, and mouse/stick scaling are configuration inputs.
- Cursor lock and visibility are presentation/input-mode responsibilities, not look math.
- Camera position is anchored to the player body; headbob, sway, recoil, FOV kick, and shake are layered later.
- Look can be suspended by input mode without setting domain state from a UI component.

### 6.6 Interaction readiness

- PlayerAnchorSet exposes a stable view camera and interaction origin/forward reference.
- A later Interaction adapter uses these references to build InteractionContext.
- Interaction acquisition, focus, prompts, press/hold handling, and execution remain in Gameplay.Interaction and its adapters.
- No raycast, IInteractable lookup, HUD text, chopping, or stealth-kill priority enters PlayerController or PlayerMotor.

### 6.7 Equipment and carrying readiness

- PlayerAnchorSet reserves view, hands/equipment, and carry anchors.
- Anchors are references only; R1 does not spawn, equip, animate, or validate inventory items.
- Blocking and carrying later contribute movement constraints through a neutral contract.
- Weapon logic never becomes a branch inside the motor.

### 6.8 Persistence readiness

- Player position, body yaw, view pitch, stance, and any required vertical state must be capturable later.
- Gameplay.Player should not depend on Infrastructure.Persistence.
- A later persistence adapter maps a player-state reader/writer to IPersistentObject and PersistenceSnapshot.
- No SaveGameManager, scene scan, singleton, Resources lookup, or direct scene load is designed in R1.

### 6.9 Multiplayer readiness

- R1 is local-authoritative and offline-first.
- Input intent, simulation state, and Unity transform application remain distinct so a later network adapter can transport intents or snapshots.
- R1 does not reference Unity.Netcode and does not add network identity or ownership branches to the motor.
- Fixed-step/network timing, prediction, reconciliation, remote interpolation, and reconnect policy remain R10 concerns.

### 6.10 Presentation readiness

- Animation, footsteps, breathing, camera effects, VFX, and HUD consume player state through readers/events.
- Presentation may depend on Player contracts; Player gameplay must not depend on Presentation.
- No movement class directly controls UI, Animator, AudioSource, camera shake, or FOV effects.

## 7. Old Script Mapping

The source inspected read-only was E:/knee_project/TheForest_Unity at commit 7d8739ef6d9a34f6945672e3b29e74293579a1fb.

| Old Script | Decision | What to Keep | What to Avoid | Future Target |
|---|---|---|---|---|
| Player/PlayerController.cs | REWRITE_CLEAN | walk/sprint/crouch precedence, grounded jump, gravity, capsule bottom preservation, movement facts | InputValue in motor, direct SurvivalStats/PlayerBlock/PlayerMudCamo, AI noise, Update-owned policy, magic grounded velocity | R1 pure movement model plus CharacterController adapter |
| Player/PlayerLook.cs | KEEP_IDEA | body yaw, isolated camera pitch, pitch clamp | cursor ownership, raw InputValue in look math, direct transform mutation as the only state | R1 pure look math plus camera adapter |
| Player/SurvivalStats.cs | NOT_R1 | later exertion gate, stamina capacity, causal survival rules, event/read-model intent | motor writing survival flags, Time-driven monolith, difficulty singleton, item and death coupling | R4 Survival; Player later reads a narrow exertion policy, not the component |
| Player/EquipmentController.cs | NOT_R1 | explicit hand anchor and equipped-state change | prefab spawning, inventory identity, animation discovery, GetComponent wiring inside Player movement | R3 Equipment and Presentation adapters |
| Player/EquipHotkeys.cs | NOT_R1 | logical equip-slot commands | direct ItemData array and InputValue-to-equipment call | R3 input command adapter |
| Player/WeaponSwinger.cs | NOT_R1 | later exertion request and timed action ideas | combat, stamina, AI, camera, UI, VFX, Resources, physics, and coroutine coupling | later Combat/Harvesting and Presentation |
| Player/PlayerBlock.cs | NOT_R1 | later movement multiplier/sprint-denial requirement | direct equipment, survival, NavMesh, AI, input, and time coupling | later Combat; contributes neutral MovementConstraints |
| Player/PlayerDamageReceiver.cs | NOT_R1 | ordered damage pipeline idea | concrete block/armor/stats/UI dependencies | later Combat/Health |
| Player/PlayerLogCarry.cs | NOT_R1 | carry anchor and movement-restriction need | Camera.main, keyboard polling, Rigidbody/building/equipment coupling | R8 Building/Carry adapter |
| Player/BowController.cs | NOT_R1 | view/shoot origin requirement | Resources, inventory, projectiles, input, equipment, and AI noise | later Ranged Combat |
| Player/FirearmController.cs | NOT_R1 | view/shoot origin requirement | hitscan, inventory, Resources, AI noise, animation, and concrete damage lookup | later Ranged Combat |
| Player/StealthKillController.cs | NOT_R1 | view-origin eligibility seam | AI types, physics query, equipment and input in Player foundation | later Interaction/Combat/AI |
| Player/PlayerMudCamo.cs | NOT_R1 | stance and motion facts can inform later stealth/noise | stealth score in Player, physics foliage query, difficulty singleton, global AI noise | R6 Perception/Stealth adapters |
| Player/PlayerWarpaint.cs | DISCARD | none for foundation | cosmetic material instances and survival wetness coupling | optional Presentation task only if approved |
| Interaction/IInteractable.cs | DISCARD | focus/availability concepts already represented by new contracts | GameObject interactor and raw prompt strings | existing Gameplay.Interaction contracts |
| Interaction/IHoldInteractable.cs | NOT_R1 | opt-in hold semantics | parallel old interface and frame-by-frame Player calls | R2 InteractionInputKind/InteractionPrompt semantics |
| Interaction/InteractionRaycaster.cs | NOT_R1 | camera-forward acquisition, focus transitions, press/hold intent | Player ownership, stealth/chop priority, HUD strings, Camera.main, direct InputValue | R2 Interaction runtime adapter using Player anchors |
| Player/Inventory.cs | NOT_R1 | stable identity, stacking, overflow, count, consume | ScriptableObject runtime identity and equipment callback | R3 Inventory using ItemId/ItemStack |
| Items/ItemData.cs | PORT_DATA_ONLY | stable ID, display/category/max stack concepts | combined consumable, equipment, prefab, combat, pose, and presentation fields | R3 split item definitions |

### Focused old-script conclusions

- PlayerController contributes requirements, not code. Only locomotion intent, grounded movement, jump/gravity, stance precedence, and safe capsule transitions belong to R1. Stamina moves to Survival, noise to AI/Perception, block/equipment to Combat/Equipment, and prompts/raycasting to Interaction.
- PlayerLook provides the useful yaw/pitch split. R1 simplifies it into pure angle math and a thin transform adapter; cursor and UI mode are separate.
- SurvivalStats must not be instantiated or depended on by R1. A later exertion policy may expose can-sprint, can-jump, capacity, and cost acceptance while ISurvivalStatsReader remains read-only stat presentation.
- EquipmentController, WeaponSwinger, and PlayerBlock stay out of R1. Their only foundation implication is the need for anchors and neutral movement constraints.
- PlayerMudCamo, StealthKillController, and PlayerWarpaint stay out of R1. Motion/stance facts are sufficient future hooks; Player does not calculate stealth or call AI.
- InteractionRaycaster must be a later Interaction adapter. Player supplies only actor/body/view references needed to build InteractionContext.

## 8. Proposed New Components

These names are proposals for later implementation and may be refined in R1-A.

| Proposed Component | Responsibility | Layer/Domain | Dependencies | R1 or Later |
|---|---|---|---|---|
| PlayerInputAdapter | translate Input System Player actions into intent snapshots and one-shot commands | Presentation/Input | Gameplay.Player contracts, Unity Input System | R1 |
| PlayerMovementModel | pure resolution of stance, locomotion mode, desired planar/vertical motion, and state | Gameplay/Player | Core and validated data only | R1 |
| CharacterControllerPlayerMotor | query ground/clearance, apply motion, and report collision results | Infrastructure/Player Unity adapter | Gameplay.Player, UnityEngine CharacterController | R1 |
| PlayerLookModel | pure sensitivity, yaw accumulation, and pitch clamp | Gameplay/Player | configuration only | R1 |
| PlayerLookController | apply resolved body yaw and view pitch to explicit transforms | Presentation/Player | Gameplay.Player, UnityEngine | R1 |
| PlayerMovementStateSource | retain and expose the latest immutable movement state | Gameplay/Player | Gameplay.Player | R1 |
| PlayerBodyReferences | explicit body root, capsule/motor, and facing references | Infrastructure/Player composition | UnityEngine, Gameplay.Player | R1 |
| PlayerAnchorSet | explicit view camera, interaction origin, hands/equipment, and carry anchors | Presentation/Player composition | UnityEngine | R1 |
| PlayerFoundationValidator | report missing references and invalid settings through IValidatable | Infrastructure/Player composition | Core validation contracts | R1 |
| PlayerMovementModifierAggregator | compose injury, carry, block, water, cold, and stamina constraints | Application/Player | provider contracts only | Later |
| PlayerMovementSignalAdapter | publish abstract motion/exertion/noise facts | Application adapters | Core events, Player state | Later |
| PlayerPersistenceAdapter | map player state to IPersistentObject snapshots | Infrastructure/Persistence | Player state contracts, Persistence | R5 |
| NetworkPlayerMotorAdapter | transport intent/state without changing domain rules | Infrastructure/Networking | Player contracts, networking package | R10 |
| PlayerAnimationAdapter | map state to animation parameters | Presentation/Animation | Player state reader | Later |

No global PlayerManager is proposed.

## 9. Proposed Data / Pure Types

| Type | Purpose | R1 or Later |
|---|---|---|
| MovementIntent | clamped planar input plus sprint, crouch, and one-shot jump requests | R1 |
| LookIntent | device-normalized look delta/rate plus source semantics if required | R1 |
| PlayerMovementConfig | validated speeds, acceleration, gravity, jump, slope, step, ground-snap, and capsule settings | R1 |
| PlayerLookConfig | mouse/stick sensitivity, invert-Y, pitch limits, and optional yaw wrapping | R1 |
| PlayerGroundInfo | grounded flag, ground normal, slope angle, walkable flag, and contact confidence | R1 |
| PlayerShapeState | standing/crouching height, center, requested stance, and obstruction result | R1 |
| MovementConstraints | allow-sprint/jump/stand flags and clamped speed multiplier | R1 with permissive default |
| PlayerMotionCommand | desired planar velocity, vertical velocity, stance, and ground policy output | R1 |
| PlayerMovementState | immutable resolved locomotion mode, grounded/stance flags, planar/vertical velocity, facing, and requested/resolved sprint | R1 |
| PlayerLookState | accumulated yaw and clamped pitch | R1 |
| PlayerAnchorKind | stable semantic key for view, interaction, hands, and carry anchors | R1 |
| MovementModifier | source-identified external modifier contribution | Later |
| PlayerExertionFact | neutral motion/action cost fact consumed by Survival | Later |
| PlayerMovementNoiseFact | distance/speed/stance/surface fact consumed by noise presentation/perception | Later |
| PlayerTransformSnapshot | persistence-facing position/facing/stance data | R5 |

Configuration values require design approval; old numeric values are not migration defaults.

## 10. Dependency Rules

1. Gameplay.Player uses SonsOfTheForest.* namespaces only.
2. A new Gameplay.Player assembly should depend on SonsOfTheForest.Core only. It must not reference UI, AI, Interaction implementation, Survival implementation, Persistence, Networking, or Input System.
3. Input System references belong to PlayerInputAdapter in Presentation/Input.
4. CharacterController, collision queries, Transform, Camera, and scene references remain in Unity adapters.
5. Presentation and Infrastructure may depend inward on Player contracts; Player gameplay never depends outward on them.
6. R1 must not reference Unity.Netcode.
7. R1 must not call Resources.Load, FindObjectOfType, FindFirstObjectByType, FindAnyObjectByType, GameObject.Find, or Camera.main.
8. All required references are serialized or supplied by the composition root.
9. Player does not write UI, AI, Survival, Inventory, Equipment, Interaction, or Persistence state.
10. Time and frame delta are explicit inputs to pure movement/look operations.
11. Domain decisions do not depend on MonoBehaviour Update order.
12. Player input, movement calculation, Unity collision application, state publication, and presentation are separate responsibilities.
13. SCN_Foundation receives no one-off player logic; later composition uses one validated prefab/root.
14. Every runtime type is introduced with proportional EditMode coverage and architecture guards.

## 11. Contract Gaps

R1-A should review and create only the minimum approved set. No contract is created by this plan.

### Required before motor implementation

- MovementIntent and LookIntent immutable values.
- PlayerMovementConfig and PlayerLookConfig validation rules.
- PlayerGroundInfo, PlayerMotionCommand, PlayerMovementState, and PlayerLookState.
- IPlayerIntentSource or an equivalent read boundary between Input adapter and Player application logic.
- IPlayerMovementStateReader for animation, interaction, survival, diagnostics, and tests.
- IPlayerAnchorProvider for semantic anchor lookup without Camera.main or hierarchy search.
- A ground/shape query boundary so stand clearance and walkable-ground facts are supplied by the Unity adapter.
- A permissive MovementConstraints value that avoids direct Survival/Equipment dependencies.

### Recommended later, not required for initial R1 motor

- IPlayerMovementModifierSource and deterministic modifier aggregation.
- IPlayerExertionSink for Survival-owned stamina and energy policy.
- IPlayerMovementSignalSource or typed movement-state event.
- IPlayerNoiseEmitter adapter contract after AI perception event vocabulary exists.
- ILocalPlayerContext or local-player marker before multiplayer/input pairing work.
- Player persistence reader/writer boundary used by an Infrastructure.Persistence adapter.
- Input-mode/cursor ownership contract for switching Player and UI action maps.

ISurvivalStatsReader is not a sprint authorization contract; it should not be polled by the motor to infer policy from raw stamina values.

## 12. R1 Test Plan

Tests should be added before or with each implementation slice.

### 12.1 Player movement configuration

- walk, sprint, and crouch speeds are finite and nonnegative;
- sprint speed is greater than or equal to walk speed;
- crouch speed does not exceed walk speed unless explicitly allowed;
- crouch height is lower than standing height and both remain valid for the radius;
- capsule center preserves a stable bottom when stance changes;
- gravity uses explicit negative/downward semantics;
- jump height, acceleration, deceleration, slope limit, step offset, skin width, and ground snap are valid;
- pitch limits are ordered and bounded.

### 12.2 Movement intent

- move input is clamped to unit magnitude;
- diagonal input cannot exceed full input magnitude;
- sprint request resolves false while crouching;
- sprint request without meaningful motion does not produce Sprint state;
- jump request is latched and consumed once;
- holding jump does not create repeated jump commands;
- crouch hold/toggle policy preserves state deterministically;
- blocked stand request preserves crouch.

### 12.3 Pure movement model

- idle, walk, sprint, crouch, airborne, and landing transitions are deterministic;
- grounded state and walkable slope facts are preserved;
- desired movement projects onto a walkable slope;
- an unwalkable slope does not become grounded walk motion;
- gravity integration is frame-rate independent within tolerance;
- grounded adhesion does not accumulate downward velocity;
- jump only starts from allowed grounded state;
- permissive constraints preserve base speeds;
- external speed multiplier is clamped and does not mutate configuration;
- resolved state reports planar and vertical velocity correctly;
- no result depends on UI, scene objects, or Input System types.

### 12.4 Look model

- pitch clamps at both limits;
- yaw accumulates and wraps according to the selected convention;
- invert-Y is deterministic;
- mouse delta and stick rate use their documented scaling paths;
- zero input preserves state;
- pure look math has no Camera or Transform dependency.

### 12.5 Unity adapter EditMode tests

- CharacterController settings match validated configuration;
- standing/crouching center changes preserve capsule bottom;
- obstruction prevents standing;
- missing required body/view/anchor references produce validation issues;
- movement state can be read without a HUD or Animator.

Physics behavior that cannot be trusted in EditMode should receive narrowly scoped PlayMode tests only after R1-F composition exists.

### 12.6 Architecture guards

- Player runtime contains no old TheForest namespace.
- Gameplay.Player has no UI, AI, Netcode, Input System, Persistence, or concrete Survival dependency.
- Player runtime contains no FindObjectOfType variants, GameObject.Find, Camera.main, Resources.Load, or direct device polling.
- Player gameplay does not reference HUD, Animator, AudioSource, VFX, or weapon types.
- no scene or prefab is changed before its dedicated task.

## 13. Recommended R1 Breakdown

### R1-A - Player contract gaps and tests

Approve namespaces/assemblies, minimal pure types, validation rules, dependency guards, and failing EditMode tests. Decide crouch input policy and movement-update clock.

### R1-B - Player input and intent types

Implement intent values and PlayerInputAdapter for Move, Look, Sprint, Crouch, and Jump only. Preserve separate Player/UI action maps and control schemes.

### R1-C - Player motor pure logic

Implement config validation, locomotion/stance transitions, vertical motion, constraints, movement state, and pure look math with tests.

### R1-D - Unity CharacterController adapter

Implement explicit ground/shape queries, safe crouch/stand capsule handling, slope/step configuration, motion application, and state reconciliation. Do not add camera effects or other domains.

### R1-E - Camera/look adapter

Apply body yaw and view pitch through explicit references. Add input-mode suspension seam; do not implement UI, headbob, sway, shake, recoil, or FOV kick.

### R1-F - Foundation prefab composition

Create one validated player foundation prefab, explicit anchors, and approved SCN_Foundation composition. This is the first task allowed to touch a prefab/scene and should have its own safety gate.

### R1-G - Validation and docs

Run EditMode and targeted PlayMode tests, validate architecture/scene cleanliness, document settings and ownership, and record deferred hooks.

Each task requires a separate review/authorization; this plan does not start R1-A.

## 14. Risks and Open Questions

| Question | Current Recommendation | Decision Needed Before |
|---|---|---|
| CharacterController vs Rigidbody | start with CharacterController adapter; keep pure motor and adapter boundary replaceable | R1-D |
| Crouch hold vs toggle | support one explicit policy first; do not infer from old code | R1-A/R1-B |
| Mouse/gamepad scope | Keyboard and Mouse plus Gamepad in R1; keep other existing schemes untouched but unvalidated | R1-B |
| Dynamic vs fixed update | choose one authoritative simulation clock and test input latching across it | R1-A |
| Stamina ownership | Survival owns cost/capacity; Player owns resolved motion state only | R4 contract task |
| Movement modifier composition | deterministic multiplicative speed plus explicit capability flags is the initial candidate | before first external modifier |
| Crouch collision | require an upward clearance probe and stable capsule bottom | R1-C/R1-D |
| Slope sliding | define explicitly; do not accept incidental engine behavior | R1-C |
| Camera ownership | Presentation owns Camera/cursor; Player owns look state/facing intent | R1-E |
| Animation integration | state reader only in R1; Animator adapter later | post-R1 |
| Interaction origin | explicit anchor/provider; raycaster belongs to R2 | R1-F/R2 |
| Persistence | later adapter maps player state to IPersistentObject; no gameplay-to-Infrastructure dependency | R5 |
| Multiplayer timing | offline/local authority first; transport/prediction remains R10 | R10 |
| Input asset changes | reuse existing logical actions where suitable; any edits require a dedicated reviewed task | R1-B |

Additional risks:

- CharacterController grounded/slope results can vary around edges and steps; adapter tests need representative fixtures.
- Capsule resizing can overlap ceilings if clearance is not queried before standing.
- Mixing mouse delta and stick rate semantics causes frame-rate-dependent look.
- Early movement feel tuning can hide architectural coupling; configuration values must remain replaceable data.
- Adding external modifiers before a deterministic composition contract will recreate old PlayerController branches.
- Player identity, respawn, teleport, and scene transitions are intentionally unresolved.
- Swimming, climbing, carrying, block movement, injury, cold, stealth scoring, footstep surfaces, combat, and all old numeric tuning are deferred.

## R1-A Completion Note

R1-A creates the approved Player contracts/types and tests from this plan. Runtime movement implementation remains deferred.

## R1-B Completion Note

R1-B adds the approved Player intent source and Presentation/Input adapter while movement, camera application, prefab, and scene composition remain deferred.

## R1-C0 Research Foundation

- Build reference introduced.
- Evidence grades introduced.
- Evidence matrix introduced.
- Measurement protocol introduced.
- No gameplay defaults selected.
- At the end of R1-C0, R1-C implementation remained blocked.
- The physics-driver decision was undecided and remains open.

## R1-C0 Physics-Driver Supersession Notice

1. Earlier references to `CharacterControllerPlayerMotor`, R1-D as a Unity CharacterController adapter, and “start with CharacterController adapter” are historical proposals from before the evidence-driven locomotion investigation.
2. They are not approved implementation decisions.
3. Until the R1-C8 physics-driver decision gate is complete, use neutral terminology: `IPlayerMotionDriver`, physics-driver candidate, Rigidbody candidate, and CharacterController candidate.
4. No CharacterController or Rigidbody implementation may begin solely from the earlier sections.
5. The final driver decision remains OPEN.
6. Pure movement contracts must remain independent of either candidate.

## R1-C1 Pure Movement/Look Completion Note

R1-C1 was later opened under separate authorization and completed only the pure movement/look architecture implementation: `PlayerMovementModel`, `PlayerLookModel`, their documentation, and EditMode coverage.

- Exact Sons of the Forest fidelity and final tuning remain blocked on controlled evidence and measurements.
- The physics-driver decision remains OPEN.
- Physics application and queries, camera application, player model, animation, prefab composition, and scene integration remain deferred.

### R1-C1 Test Reconciliation

- Previous full-suite baseline: 126 EditMode test cases.
- New movement-model cases: 20.
- New look-model cases: 10.
- Total new cases: 30.
- Final full suite: 156 passed, 0 failed, 0 skipped.
- The earlier focused run reported 29 because it ran only the new model tests before the final sprint-constraint movement case was added. The final full-suite count includes the 126 baseline cases and all 30 finalized model cases.
