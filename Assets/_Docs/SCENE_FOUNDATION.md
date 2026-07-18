# Scene Foundation

## 1. Official Scene

- Path: `Assets/_Game/Scenes/SCN_Foundation.unity`
- Purpose: long-term composition root for the rebuild.

The scene is the official playable composition root. It provides organization containers, lighting, a disabled fallback camera, and prefab instances for the approved local-player foundation and its provisional playground. Gameplay implementation remains owned by reusable prefabs and runtime assemblies rather than one-off scene scripts.

## 2. Template Scene Status

- `Assets/OutdoorsScene.unity` is transitional template content.
- It is no longer the official build scene after R0-A3.
- It should not receive gameplay work.
- It can be removed in a later cleanup task once no settings depend on it.

## 3. Scene Rules

- Scene is composition only.
- Systems must be prefab- or bootstrap-owned.
- No one-off gameplay logic directly in scene objects.
- No prototype clutter at scene root.
- No copied objects from the old project.
- Every runtime system must have clear domain ownership before entering this scene.

## 4. Future Scene Plan

- R0-B: foundation contracts
- R0-C: bootstrap/service contracts
- R1: player foundation scene population (completed baseline)
- Later: world test scene, AI test scene, and building test scene if needed

## 5. R1 Foundation Composition Baseline

- `_BOOTSTRAP` owns `FoundationSceneCompositionRoot` and explicit readiness references.
- `_GAMEPLAY/Player` owns one instance of `PRF_PlayerFoundation`.
- `_WORLD/Terrain` owns one instance of `PRF_FoundationPlayground`.
- The player camera and Audio Listener are active; the original Main Camera is retained but inactive as a fallback.
- The origin spawn, flat ground dimensions, and ground appearance are provisional engineering fixtures, not Sons of the Forest fidelity claims.
- Future world composition may replace the playground prefab without changing player runtime contracts.
