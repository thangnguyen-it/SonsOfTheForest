# R1 Foundation Scene Composition

Status: implementation baseline, 2026-07-18

## Targeted research conclusion

1. The current official Steam product page describes *Sons of the Forest* as a first-person, open-world survival horror simulator (`VERIFIED`).
2. It supports a playable first-person character entering an explorable world, but does not specify scene hierarchy, spawn coordinates, terrain dimensions, or camera bootstrap (`UNKNOWN`).
3. The official page describes free world traversal and environmental interaction (`VERIFIED`).
4. A static flat ground is therefore useful for engineering validation, but cannot be presented as a reconstruction of the released island (`PROVISIONAL`).
5. Current release-specific spawn placement, world lighting, terrain, weather, season state, and initial presentation remain `UNKNOWN`.
6. No original game was launched, injected, modified, extracted, or decompiled for this task.
7. No proprietary Sons of the Forest asset or source code was copied.
8. The pinned historical dump and old TheForest project were not needed to decide scene ownership; they provide no trustworthy current scene layout (`HISTORICAL`, no implementation impact).
9. A controlled local run of `SCN_PlayerFoundationValidation` on 2026-07-18 showed the approved prefab grounded, accepted keyboard input, moved, animated, and retained its local first-person renderer policy (`VERIFIED` for this project only).
10. The same inspection showed `SCN_Foundation` contained organization roots, a light, and a fallback camera, but no player or collision surface (`VERIFIED` for the pre-milestone repository).
11. The official build settings contained only `SCN_Foundation` (`VERIFIED` for this project).
12. Consequently the built project could not expose the already validated player foundation without scene composition (`VERIFIED` project gap).
13. Unity 6.3 documentation identifies prefabs as reusable GameObject configurations whose instances remain synchronized (`VERIFIED`).
14. Unity explicitly lists placing the main playable-character prefab at a scene starting point as a common prefab use (`VERIFIED`).
15. This supports reusing `PRF_PlayerFoundation` rather than duplicating its hierarchy in the scene.
16. Unity 6.3 documents that one Audio Listener per scene is required for correct operation (`VERIFIED`).
17. The player prefab already owns a camera and Audio Listener; the scene fallback camera must therefore be inactive during local play (`VERIFIED` project requirement).
18. Unity 6.3 exposes `Scene.GetRootGameObjects`, allowing readiness checks to count scene-local cameras/listeners without global object lookup (`VERIFIED`).
19. The foundation scene is the project composition root, so scene-only references belong in an outward infrastructure assembly (`INFERRED` from approved architecture).
20. Player movement, input, physics, view, animation, and lawful visual assets are already composed by `PRF_PlayerFoundation` and must not be reimplemented here (`VERIFIED` project state).
21. The scene composition component can confidently require one local player, one active player camera/listener, one disabled fallback camera, and one solid playable surface.
22. It can confidently validate ownership, scene membership, and serialized references before runtime begins.
23. It cannot confidently assert released-game spawn, ground size, surface appearance, or world layout.
24. Those choices remain replaceable prefab/scene data and are explicitly marked `PROVISIONAL`.
25. The initial 40 m square ground and origin spawn are engineering baselines only.
26. The foundation playground is a reusable fixture, not the future island terrain system.
27. Edge tests must cover missing/mismatched scene identity, duplicate cameras/listeners, prefab linkage, grounded spawn, and input-driven movement.
28. Interaction, inventory, survival, weather, terrain streaming, save/load, multiplayer spawning, UI, audio content, and fidelity tuning belong to later systems.

## Implementation decision

- Add `FoundationSceneCompositionRoot` in `Infrastructure.SceneComposition` while preserving `Infrastructure.SceneBootstrap` as engine-free contracts.
- Keep composition explicit through serialized references; do not use runtime scene lookups for dependencies.
- Place prefab instances of the existing player and a replaceable playground under the approved scene containers.
- Disable, but retain and explicitly wire, the original scene camera as a fallback.
- Fail fast in Play Mode if required composition is invalid.
- Keep the playground material HDRP-compatible and visually neutral; do not imply released-game color or terrain fidelity.
- Validate structure in EditMode and scene load plus player movement in PlayMode.

## Sources

- Official Steam product page, current page inspected 2026-07-18: https://store.steampowered.com/app/1326470/Sons_Of_The_Forest/
- Unity 6.3 prefab introduction: https://docs.unity3d.com/6000.3/Documentation/Manual/prefabs-introduction.html
- Unity 6.3 Audio Listener manual: https://docs.unity3d.com/6000.3/Documentation/Manual/class-AudioListener.html
- Unity 6.3 `Scene.GetRootGameObjects`: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/SceneManagement.Scene.GetRootGameObjects.html
- Local controlled project observation at base `93906a9db4e22a11def35eed9062477f7217495f`, 2026-07-18.
