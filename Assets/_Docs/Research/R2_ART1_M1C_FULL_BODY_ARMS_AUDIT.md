# R2-ART1-M1C — Existing Full-Body Character FPS Arms Suitability Audit

## Status and scope

- Audit date: 2026-08-20.
- Repository branch/base: `codex/r2-forest1f-harvest-quality` at `608e22b031cbd35e398a52062d423419d5d143a4`.
- Result: `M1C_TECHNICAL_COMPLETE`.
- Visual decision state: `HUMAN_VISUAL_APPROVAL_REQUIRED`.
- Integration state: M2 has not started.
- No model derivative, production animation, gameplay integration, player prefab change, or `SCN_Foundation` change was made.

The audit evaluates the real Rocketbox character mesh used by the current player and compares it with the exact DJMaesen source previously classified `HUMAN_VISUAL_REJECT_AS_FINAL_HERO_ASSET`. Temporary posing and evidence rendering were technical-only and remained under ignored `_LocalTrials`/`Temp` paths.

Evidence labels follow the project research protocol: repository assets and measured Unity properties are `VERIFIED`; extraction cost and likely deformation risks are `INFERRED`; a replacement asset specification is `PROVISIONAL` until an actual candidate is inspected.

## 1. Current player dependency trace (`VERIFIED`)

### Scene and prefab ownership

```text
SCN_Foundation/_GAMEPLAY/Player
└── LocalPlayer
    └── BodyYaw
        ├── PlayerVisual
        │   └── PRF_PlayerVisual_WoodMale01
        │       ├── FullBodyRenderer
        │       ├── LocalFirstPersonRenderer
        │       └── LocalFullBodyShadowRenderer
        └── ViewPitch
            ├── ViewCamera
            └── Hands
```

| Dependency | Project path | GUID / identity |
|---|---|---|
| Player visual prefab | `Assets/_Game/Prefabs/Player/PRF_PlayerVisual_WoodMale01.prefab` | `b3ad7753f2190a0408bade4427b33bbd` |
| Full-body FBX | `Assets/_Game/Art/Models/Player/Rocketbox/Wood_Male_01/Wood_Male_01.fbx` | `0deb090373130c947bb3312acc3eb7d8` |
| Animator Controller | `Assets/_Game/Art/Animations/Player/Controllers/AC_PlayerFoundation.controller` | `ff5ade30e10b9f048a60708f02a8bebf` |
| Local visual controller | `Assets/_Game/Presentation/Player/LocalPlayerVisualController.cs` | `2800ae0e87a3a1e4497bda4a9aa9350d` |
| Body material | `Assets/_Game/Art/Materials/Player/Wood_Male_01/MAT_WoodMale01_Body.mat` | `37353d952f7135441b158f1d4bc34933` |
| Head material | `Assets/_Game/Art/Materials/Player/Wood_Male_01/MAT_WoodMale01_Head.mat` | `d3ce64ac4290f5641ae0b3609c087144` |
| Head-hidden material | `Assets/_Game/Art/Materials/Player/Wood_Male_01/MAT_WoodMale01_HeadHidden.mat` | `3c43f5037677aa540928142cd8434372` |

The FBX SHA-256 is `A484E5B447A7297CB12B71E5D2D7DEE94D789D7A8D1A43DB5AFAEF632F433E80` (501,552 bytes).

### Representation and camera boundary

`LocalPlayerVisualController` disables `FullBodyRenderer`, enables `LocalFirstPersonRenderer`, and enables `LocalFullBodyShadowRenderer` as shadows-only for the locally controlled player. All three renderers use the same `m110_hipoly_81_bones` mesh, skeleton, and body materials; this is not a separate first-person arms mesh.

- World/remote representation: `FullBodyRenderer`, normal render + shadows.
- Local first-person representation: `LocalFirstPersonRenderer`, full body with the head-hidden material, no shadow casting.
- Local shadow representation: `LocalFullBodyShadowRenderer`, `ShadowsOnly`.
- FPS camera: `.../BodyYaw/ViewPitch/ViewCamera`, FOV 60, near plane 0.3, culling mask `Everything (-1)`.
- All current renderers and the `Hands` anchor are on layer 0.

Therefore a new layer-0 FPS viewmodel would be visible at the same time as `LocalFirstPersonRenderer`. Duplicate arms/body clipping is expected unless future integration assigns explicit FPS visibility ownership: the full body remains world/shadow authority, while a project-owned arms derivative or replacement uses an FPS-only layer and the local full-body color renderer is excluded from that camera.

## 2. Provenance and derivative rights (`VERIFIED`)

| Field | Result |
|---|---|
| Source | [Microsoft Rocketbox](https://github.com/microsoft/Microsoft-Rocketbox) |
| Pinned source commit | `0943055db6ec570bcef9f2c8b41c9e5467c808f9` |
| Source path | `Assets/Avatars/Professions/Wood_Male_01/` |
| Author/publisher | Microsoft, copyright 2020 |
| License | MIT; repository notice retained at `Assets/_Game/Art/Models/Player/Rocketbox/Wood_Male_01/LICENSE_MIT.md` |
| Commercial use | Allowed |
| Modification/derivative | Allowed |
| Redistribution in game/build | Allowed with copyright and permission notice retained |
| Attribution requirement | Preserve the MIT copyright and permission notice |

This result is not inferred from a filename. It is bound to the pinned source record in `R1_V0_HUMANOID_ASSET_FOUNDATION.md`, the local license text, and the exact current model hash. `FULL_BODY_PROVENANCE_BLOCKED` does not apply.

## 3. Technical arms audit (`VERIFIED` unless noted)

### Mesh and importer

| Metric | Current full-body character |
|---|---:|
| Mesh | `m110_hipoly_81_bones` |
| Whole-character vertices | 4,290 |
| Whole-character triangles | 7,108 |
| Submeshes | 2 (`4,018` body triangles; `3,090` head triangles) |
| Bind poses / skin bones | 80 |
| Blend shapes | 0 |
| Normals / tangents / UV0 | Present / present / present |
| UV1 / vertex colors | Absent / absent |
| Import rig | Humanoid, avatar created from this model |
| Imported animation clips | None in the FBX |
| Maximum observed skin influences | 4 per vertex |

Arms/hands are not a separate renderer or submesh. The following counts use a reproducible skin-weight classification: “major” means more than 50% combined weight from arm/hand/finger bones; “any” means any non-zero influence.

| Arms/hands measurement | Value |
|---|---:|
| Arm-major vertices | 1,310 |
| Arm-any vertices | 1,416 |
| Hand-major vertices | 1,024 |
| Hand-any vertices | 1,076 |
| Strict arm triangles | 2,148 |
| Mixed boundary triangles | 54 |
| Any arm-influenced triangles | 2,202 |
| Shoulder-influenced vertices | 252 |
| Elbow-influenced vertices | 176 |
| Wrist/hand-influenced vertices | 522 |
| Finger-influenced vertices | 886 |
| Vertices with 1 / 2 / 3 / 4 influences | 2,130 / 1,350 / 704 / 106 |

The 54 mixed triangles prove that a clean arms derivative needs an authored seam; it cannot be obtained by merely toggling an existing submesh. A non-destructive extraction is legally and technically possible, but the likely seam is around the upper arm/clavicle or an intentionally authored sleeve boundary. Risks are shoulder holes, exposed backfaces, wrist/cuff gaps, and altered deformation where torso and clavicle weights overlap (`INFERRED`). Existing materials and UVs can be retained by preserving original vertex/UV data.

### Skeleton and animation compatibility

- Required upper-limb chain exists on both sides: clavicle/shoulder, upper arm, forearm, hand/wrist.
- Thirty finger bones are present: thumb plus four fingers with three joints on each hand.
- Shoulder, elbow, wrist, and finger regions all have non-zero skin-weight coverage.
- Humanoid avatar compatibility makes the asset suitable for existing third-person locomotion/controller use.
- Existing project locomotion clips do not constitute production FPS axe animation. A future FPS derivative would still need authored first-person clips, two-hand constraints/IK strategy, contact markers, and clipping validation.

### Materials, textures, and close-up density

`MAT_WoodMale01_Body` and the head materials use opaque HDRP/Lit. The body color, normal, and specular textures are 2,048×2,048 BC7 with mipmaps. The hands and sleeves share the full-body atlas; there is no dedicated hero-hands texture set. Strict arm triangles cover an aggregate UV triangle area of about `0.1831`, but overlaps prevent this from being treated as exact unique pixel coverage.

Visual evidence shows adequate clothing identity at world distance but low finger silhouette density, angular knuckles/fingertips, and insufficient hand texel density at FPS distance. Neutral HDRP light also exposes strong clipping/highlight instability on the low-density posed surface. These are source-art limits; extracting the same triangles cannot recover missing shape or texel detail.

## 4. Direct comparison

The exact DJMaesen source was verified before temporary staging:

- Source: `E:\SOTF_AssetIntake\R2_ART1_HERO_FOREST_01\01_Hands\DJMaesen_FirstPersonArms\source\fpsarms.fbx`
- SHA-256: `03CABD1797A90993F630544D1C7794E842EA074C221CCC8EE89511FB6DD61A76`
- Mesh: `armsmesh`, 4,256 vertices, 7,240 triangles, 49 bones, one submesh.

DJMaesen has a cleaner dedicated arms silhouette and higher effective close-up allocation than the extracted portion of the Rocketbox body. It remains human-rejected as final hero art and requires animation authoring. The current full-body arms provide better world/FPS identity consistency, but they are visibly lower fidelity than an already rejected candidate.

## 5. Evidence inventory

All images are 1,600×900 PNGs and remain in ignored `Temp/R2_ART1_M1C_FullBodyArmsAudit`. No source image changed while the contact sheet was generated.

| # | Evidence role | File | Bytes | SHA-256 |
|---:|---|---|---:|---|
| 1 | Current full body, front | `01_current_full_body_front.png` | 328,408 | `A95762AA41E25752353B296B54BAF977EAAA4E6C359B82D8B16793D0ED344B40` |
| 2 | Current full body, back | `02_current_full_body_back.png` | 410,967 | `A5BD7578E00C676A36E7A9D0F34CBA7B94C8E9D87683F5F918C1F0312740BB4A` |
| 3 | Hands/sleeves, neutral | `03_current_hands_sleeves_neutral.png` | 1,032,772 | `14ECFD42F2B558BBB9523F095625262ACC97C088461254474B120C632102AD2A` |
| 4 | Hands/sleeves, dark | `04_current_hands_sleeves_dark.png` | 917,721 | `D57DFB49189DA8F2CCECDE841CECA6E742B1B27578DD741CFD84298677C72A65` |
| 5 | Arm/finger skeleton | `05_current_arm_skeleton_fingers.png` | 785,317 | `A06D2E78EB03F5E37150679E46E17722CB36F6985559B0C6B96CC87BA6049657` |
| 6 | Arm topology | `06_current_arm_mesh_wireframe.png` | 881,594 | `FE4BDFD32AABD4A757881943C6A7A7F86EAEECF92AF688E95E513266A02A8B15` |
| 7 | Texture/material evidence | `07_current_texture_material_evidence.png` | 829,861 | `9F1864F8CDBF724FE3D9B48B6DBAF5FF422A3B12829DD1C45F65FE90413546A3` |
| 8 | Actual FPS camera | `08_current_actual_fps_camera.png` | 245,601 | `4937C41A5E725C50B7D4645FF47691623395C73BC40A2D47293EF9B9B65441FF` |
| 9 | Static FPS compatibility crop, FOV 60 | `09_current_arms_fps_crop_fov60.png` | 610,143 | `6A0A487DAD93AD84D5FC82C6ECBF021B78882560CFA31ED4257E8F88450E8F55` |
| 10 | Static FPS compatibility crop, FOV 75 | `10_current_arms_fps_crop_fov75.png` | 597,978 | `808BFA20B4402B80A71D69361EE051CD466A300E0C9A47A6E8ECD432BA29D662` |
| 11 | Current vs DJMaesen, neutral | `11_current_vs_djmaesen_neutral.png` | 791,223 | `DD5AAE5C3D82F70B951D2DAC0CAC506ADF4F1F50507E038DAD11991D2AE22877` |
| 12 | Current vs DJMaesen, dark | `12_current_vs_djmaesen_dark.png` | 642,527 | `6E2C97D14C017414BA31E4606C58028A726903487A39CC83CC286D62280FE92F` |

Derived evidence files (ignored, not committed):

- Contact sheet: `Temp/R2_ART1_M1C_FullBodyArmsAudit/M1C_FULL_BODY_ARMS_CONTACT_SHEET.png`
- Contact sheet SHA-256: `3DD5C9EB20B459C9E7129EDC85885571265BB23524702BF656AA1DBC33C3272E`
- Index: `Temp/R2_ART1_M1C_FullBodyArmsAudit/M1C_FULL_BODY_ARMS_CONTACT_SHEET_INDEX.json`
- Metrics: `Temp/R2_ART1_M1C_FullBodyArmsAudit/m1c_full_body_arms_metrics.json`
- Source images modified: `false`.

## 6. Candidate decision matrix

| Candidate | Provenance | Close-up geometry | Skin material | Sleeve/clothing | Finger rig | Skin weights | Texture resolution | FPS suitability | World/FPS consistency | Animation compatibility | Derivative feasibility | Decision |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Current Rocketbox full-body arms | MIT verified | Too low for hero FPS; 1,310 arm-major vertices, angular hands | Shared body skin response; not hero-tuned | Strong identity match, usable world clothing | Complete, 30 finger bones | Complete coverage; extreme FPS deformation unvalidated | 2K shared full-body atlas; low effective hand density | Poor without extraction, new camera layer, material work and authored animation | Excellent | Humanoid/world compatible; no FPS axe clips | Legally possible, technically costly, cannot restore missing detail | `FULL_BODY_ARMS_REJECT` |
| DJMaesen First Person arms | CC BY 4.0 verified in M1A/M1B | Cleaner dedicated arms but already human-rejected as final hero art | Dedicated PBR source, audit still found hero-quality gaps | Generic sleeves; does not match Rocketbox clothing | Rig present; M1B requires animation asset/authoring | Technically usable, final motion unapproved | Dedicated arm maps; better effective density | Reference/prototype only | Weak without deliberate art matching | No production axe animation pack | Technically possible but not worth promoting after human rejection | `DJMAESEN_REFERENCE_ONLY` |

Combined result: `NEW_PRODUCTION_FPS_ARMS_REQUIRED`.

This is a technical suitability decision, not final visual approval. `HUMAN_VISUAL_APPROVAL_REQUIRED` remains mandatory for any future candidate.

## 7. Future paths

### Path A — if the full-body arms were later accepted by human review

1. Create a project-owned, non-destructive derivative from the pinned Rocketbox source while retaining MIT notice and the same skeleton/material identity.
2. Author stable seams at the sleeve/upper-arm boundary, preserve bind pose, weights, UVs, normals, and material IDs.
3. Put the derivative on an FPS-only visibility layer; retain the full body for world/shadow representation and exclude its color renderer from the FPS camera.
4. Validate no duplicate arms, cuff/shoulder holes, near-plane clipping, or world-shadow mismatch before any final animation is authored.

### Path B — conditionally repairable full-body arms

Required repairs would be: remodel hands/fingertips/knuckles, improve palm and wrist topology, author extraction seams and backface closure, rebalance shoulder/elbow/wrist/finger weights, create dedicated hero arm/hand textures or re-UVs, and tune HDRP skin/cloth response. This approaches replacement-asset cost. It cannot recover hero detail by extraction or texture upscaling alone; new geometry and art are unavoidable.

### Path C — recommended: replacement production FPS arms

Do not select/download in this milestone. The next candidate must provide:

- free/commercial rights explicitly allowing modification, derivatives, and redistribution inside a game build, with provenance and attribution evidence;
- dedicated first-person arms/hands geometry with clean close-up finger/knuckle/nail/palm silhouettes and no mandatory full torso;
- complete clavicle/upper-arm/forearm/wrist/hand and articulated finger hierarchy, stable bind pose, and production skin weights for a two-handed axe grip;
- sleeves/clothing that can be art-matched to the world character or a documented plan for the world body to adopt the same identity;
- dedicated, inspectable PBR base color/normal/roughness-or-mask maps at sufficient true texel density; no AI-upscaled or baked-advertisement-only evidence;
- non-destructive project-owned wrapper, FPS-only layer, explicit shadow/world-body ownership, and compatibility with authored `Idle/Equip/Unequip/Chop/Recovery` clips and one-shot contact markers.

The Solidbot axe remains `HUMAN_VISUAL_CONDITIONAL_ACCEPT`; this audit does not change its status. M2 integration and animation authoring remain not started.
