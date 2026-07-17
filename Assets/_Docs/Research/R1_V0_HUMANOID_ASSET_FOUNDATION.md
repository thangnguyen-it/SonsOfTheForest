# R1-V0 Humanoid Player Asset Foundation

Research access date: 2026-07-17. This note records public evidence and an independent Unity asset implementation; it contains no extracted Sons of the Forest assets or code.

## Research conclusion

1. The [official Steam page](https://store.steampowered.com/app/1326470/Sons_Of_The_Forest/) identifies a realistic first-person survival game, released 2024-02-22, with solo and co-op play (`VERIFIED`).
2. Its official screenshots and trailers visibly use realistic human scale, clothing, first-person arms/hands and player shadows (`VERIFIED`), but they do not provide a controlled proof that the current build always renders torso and legs when looking down.
3. Current-build local body/leg visibility, exact camera-to-head placement, head masking and shadow topology remain `UNKNOWN` without authorized controlled observation.
4. A complete adult body is nevertheless a project requirement: it supports downward first-person visibility, coherent shadows and future remote players without replacing the visual later.
5. The pinned historical dump commit [`d20b8ab1`](https://github.com/NeuralBinary/Sons-of-The-Forest-Dump/tree/d20b8ab1b7a28ce1e4a48f5dc74c1ecbcedea2cb) exposes `PlayerAnimatorControl` concepts for head look, third-person branching, full-body layers and a head collider (`HISTORICAL`). Empty IL2CPP bodies do not reveal current logic or values.
6. Idle, walk, sprint, crouch idle, crouch movement and jump phases are required now so later gameplay animation does not begin from a mannequin-only library.
7. A head occupying the local camera can expose face interiors or occlude the view. A replaceable renderer-mode seam is safer than modifying the mesh or camera in this task.
8. The default prefab therefore renders the complete human. Disabled alternatives provide a head-hidden local body and a complete shadows-only body; a later local/remote presentation adapter owns switching.
9. Exact camera offsets, view pitch deformation, hand poses, foot placement, transition graphs, layers, IK, root-motion policy and final SOTF fidelity remain later configurable work.
10. Edge cases for later integration include extreme pitch, crouch transitions, jump apex/landing, self-shadowing, near-plane clipping, disabled-renderer bone updates, LOD changes and local/remote mode changes.

## Candidate decision

| Candidate | License and redistribution | Decision |
| --- | --- | --- |
| [Microsoft Rocketbox](https://github.com/microsoft/Microsoft-Rocketbox) | MIT notice at pinned commit; raw avatar files may be redistributed with the license. The official library documents 115 rigged avatars, Unity import and multiple LODs. | Selected `Wood_Male_01`: believable adult proportions, outdoor work clothing, 81-bone skinned rig and color/normal/specular atlases. |
| [Quaternius Universal Animation Library](https://quaternius.com/packs/universalanimationlibrary.html) | CC0; the official page states Unity-compatible Humanoid retargeting and commercial use. The Standard archive is publicly downloadable without login. | Selected in-place Unity FBX. It contains all required clips plus replaceable future coverage. |
| [MakeHuman](https://static.makehumancommunity.org/about/license.html) | Core assets are CC0; application source has GPL/AGPL terms. Export generation is lawful but needs an authored model, clothing choices and visual QA. | Not selected: no verified local export exists and generating a lower-confidence substitute would add a separate content pipeline. |
| [Adobe Mixamo](https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html) | Adobe permits use in games and requires an Adobe ID, but the reviewed FAQ/terms do not clearly authorize publishing raw FBX files in a public source repository. | Excluded from committed assets; raw redistribution is unresolved. |

The engineering pattern for a head-hidden local renderer plus a complete shadow caster is also consistent with the public [NeoFPS first-person body guidance](https://docs.neofps.com/manual/fpcharacters-firstpersonbody.html). This is an engineering reference, not evidence of Sons of the Forest internals.

## Selected source identity

- Rocketbox repository commit: `0943055db6ec570bcef9f2c8b41c9e5467c808f9` (2022-10-02).
- Rocketbox source path: `Assets/Avatars/Professions/Wood_Male_01/`.
- Quaternius archive: `Universal Animation Library[Standard].zip`, upload dated 2026-06-16; archive SHA-256 `CC73FC4E495B82958207316596317A3F40B9FA38065BDE1027937452DA537724`.
- Local source check: `E:\knee_project\CharacterSource\PlayerSurvivor\` did not exist; no unidentified local files were used.
- Repository visibility assumption: public. Only MIT-with-notice and CC0 raw files are committed.

## Committed external-file SHA-256

| File | SHA-256 |
| --- | --- |
| `LICENSE_MIT.md` | `17474E386E0B9E1A700CC3D06B2B0882A2C376D9C6B49C7F8274409B8F8D2352` |
| `Wood_Male_01.fbx` | `A484E5B447A7297CB12B71E5D2D7DEE94D789D7A8D1A43DB5AFAEF632F433E80` |
| `m110_body_color.tga` | `40DAE9F97393DA399ED58CC8B1EB08C7A5130709A349A723641F8413319D8183` |
| `m110_body_normal.tga` | `C0498208BEAF0319A3B9E57DCBBED69961A011578FE7F5F65DF9F47FB9197DF0` |
| `m110_body_specular.tga` | `0CC1B3F25976EE54FA7B8DAF290185421C7CE150D87A17441C7277D1614CBF93` |
| `m110_head_color.tga` | `76A0AC7757FF6FA6336D95F8C18493814E8CB21A6C261947F46998C1357C2B40` |
| `m110_head_normal.tga` | `14586EAA23649CA2703A4CF6619A835A7BC02CF97CA8ABE836E705B1A7EBDFDF` |
| `m110_head_specular.tga` | `1FDB5C4DC799D9AED00C7B543959BEC65791DB1C72990A3FED556E0331EAD0D1` |
| `LICENSE_CC0.txt` | `6D01F55C6E4C49A2C9963E147E561945AE2C83958C8CA667D90A6BFFDBFAC061` |
| `README_SOURCE.txt` | `CCD02718886B5A57F10F0A8911A38CFE10C36880E1D6DFA3445923FBA32D2A72` |
| `UAL1_Standard.fbx` | `21B32D912DA3CB93426D974FB945E86F5B2E86970ACD2CE89905E0FBF9F1DCC2` |

## Unity asset result

- Unity 6000.3.10f1 imports `Wood_Male_01` as Humanoid with a valid human Avatar. Its visible bounds are approximately 1.833 m tall.
- The model keeps its hierarchy and does not optimize away bones needed by later head/camera/remote-player work.
- Six 2048 textures import with color/specular in sRGB and tangent-space normal maps; materials use `HDRP/Lit` in specular workflow.
- Body/head smoothness values are provisional because the source supplies specular color rather than an HDRP mask map; final material tuning belongs to lighting/art review.
- `UAL1_Standard.fbx` imports 43 in-place Humanoid clips with `Armature/root` as motion node. `_Loop` clips are marked looping.
- Required clips validated: `Idle_Loop`, `Walk_Loop`, `Sprint_Loop`, `Crouch_Idle_Loop`, `Crouch_Fwd_Loop`, `Jump_Start`, `Jump_Loop`, and `Jump_Land`.
- `PRF_PlayerVisual_WoodMale01` is visual-only: no controller, root motion, MonoBehaviour, camera, collider, Rigidbody or CharacterController.
- Default `FullBodyRenderer` is visible and casts complete shadows. Disabled `LocalFirstPersonRenderer` hides the head material; disabled `LocalFullBodyShadowRenderer` preserves the complete shadows-only silhouette.
- No Animator state machine or renderer-switching behavior is included. Physics, input, camera, scene placement, animation policy and gameplay integration remain deferred.
