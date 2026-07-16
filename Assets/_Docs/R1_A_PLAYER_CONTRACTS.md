# R1-A Player Contracts

## 1. Purpose

R1-A establishes the minimum Player Foundation assembly, immutable intent/state/configuration types, read boundaries, and EditMode guardrails required before movement implementation.

The work is contract-first and test-first. It does not move a player, read an input device, control a camera, create a prefab, or modify a scene.

## 2. Created Contracts and Types

The SonsOfTheForest.Gameplay.Player assembly references SonsOfTheForest.Core only.

Created locomotion vocabulary:

- PlayerLocomotionMode
- PlayerStance
- CrouchInputPolicy
- LookInputKind
- PlayerAnchorKind

Created immutable intent and state values:

- MovementIntent
- LookIntent
- PlayerGroundInfo
- MovementConstraints
- PlayerMovementState
- PlayerLookState

Created validated configuration values:

- PlayerMovementConfig
- PlayerLookConfig

Created read/reference boundaries:

- IPlayerMovementStateReader
- IPlayerAnchorProvider

IPlayerAnchorProvider is the sole R1-A scene-reference seam. It may expose Transform references but has no implementation and performs no lookup.

## 3. Why No PlayerController Yet

The old PlayerController mixed Input System callbacks, CharacterController movement, crouch capsule mutation, SurvivalStats authorization, block modifiers, stealth, and direct AI noise emission. Creating a new MonoBehaviour before its data and dependency boundaries were tested would recreate that coupling.

R1-A therefore defines only the facts and policies that later adapters consume. Player input, pure motor logic, CharacterController collision application, camera transforms, and composition remain separate future tasks.

## 4. Dependency Rules

- All Player code uses SonsOfTheForest.Gameplay.Player.
- Gameplay.Player references SonsOfTheForest.Core only.
- No Player source references Input System, Netcode, UI, AI, Persistence, Interaction, or concrete Survival.
- No Player source uses scene lookup, Camera.main, or Resources.Load.
- No Player type derives from MonoBehaviour.
- UnityEngine value/reference types are limited to vectors and the unimplemented anchor interface.
- Runtime assemblies do not reference tests.

These constraints are enforced by PlayerArchitectureGuardTests.

## 5. Test Coverage

R1-A EditMode tests cover:

- MovementIntent clamping, movement threshold, and request preservation;
- LookIntent zero, delta, and rate semantics;
- valid defaults and invalid PlayerMovementConfig invariants;
- valid defaults and invalid PlayerLookConfig invariants;
- permissive and restricted MovementConstraints;
- Player movement/look state preservation and planar speed;
- grounded and airborne PlayerGroundInfo;
- forbidden namespaces/dependencies;
- scene lookup, Resources, and MonoBehaviour prohibition;
- exact Player asmdef reference allow-list.

## 6. What R1-A Intentionally Excludes

- PlayerController or any other MonoBehaviour implementation
- PlayerInput/Input System adapter
- CharacterController motor or physics queries
- camera/look transform adapter
- stamina, survival, equipment, inventory, combat, stealth, and interaction runtime
- persistence or networking adapter
- animation, audio, VFX, and HUD
- player prefab and scene composition

No old project code or asset was copied.

## 7. Next Step Recommendation

R1-B should plan and implement only the Player Input and Intent Adapter against these contracts. It must preserve the Player/UI action-map boundary, distinguish mouse delta from stick rate, latch jump as a one-shot request, and remain separate from movement/camera implementation.

R1-B requires its own preflight and authorization.
