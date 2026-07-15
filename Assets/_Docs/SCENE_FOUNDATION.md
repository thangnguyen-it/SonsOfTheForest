# Scene Foundation

## 1. Official Scene

- Path: `Assets/_Game/Scenes/SCN_Foundation.unity`
- Purpose: long-term composition root for the rebuild.

The scene is intentionally minimal. It provides organization containers plus a standard Main Camera and Directional Light for safe HDRP presentation; it does not contain gameplay implementation.

## 2. Template Scene Status

- `Assets/OutdoorsScene.unity` is transitional template content.
- It is no longer the official build scene after R0-A3.
- It should not receive gameplay work.
- It can be removed in a later cleanup task once no settings depend on it.

## 3. Scene Rules

- Scene is composition only.
- Systems must be prefabs or bootstrap-owned later.
- No one-off gameplay logic directly in scene objects.
- No prototype clutter at scene root.
- No copied objects from the old project.
- Every runtime system must have clear domain ownership before entering this scene.

## 4. Future Scene Plan

- R0-B: foundation contracts
- R0-C: bootstrap/service contracts
- R1: player foundation scene population
- Later: world test scene, AI test scene, and building test scene if needed
