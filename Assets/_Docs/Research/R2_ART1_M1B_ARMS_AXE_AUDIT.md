# R2-ART1-M1B — FPS Arms and Fire Axe Technical/Visual Audit

Audit date: 2026-08-20. Unity: 6000.3.10f1 / HDRP. Repository base:
`e84e6606c25f6971181f3d7371e0957493493287` on
`codex/r2-forest1f-harvest-quality`.

This is a pre-integration audit. It does not replace the current player, axe, animation,
prefab, scene, or gameplay implementation. Static poses in the evidence are compatibility
probes, not production animation.

## Evidence status and source binding

Both required FBX files were readable, non-empty, and matched the locked SHA-256 before
Unity import. Temporary inspection roots retained the exact required names
`AUDIT_DJMaesen_FirstPersonArms` and `AUDIT_Solidbot_FireAxe`. No generated substitute,
primitive, Pine, ground, or rock asset was used.

| Asset | Locked source | Bytes | SHA-256 | Result |
|---|---|---:|---|---|
| FPS arms | `E:\SOTF_AssetIntake\R2_ART1_HERO_FOREST_01\01_Hands\DJMaesen_FirstPersonArms\source\fpsarms.fbx` | 476,960 | `03CABD1797A90993F630544D1C7794E842EA074C221CCC8EE89511FB6DD61A76` | `VERIFIED` |
| Fire axe | `E:\SOTF_AssetIntake\R2_ART1_HERO_FOREST_01\02_Axe\Solidbot_FireAxe\source\hacha-bomberos-terminada.fbx` | 67,196 | `FB880E4940BC51AF2F21C8C71D7ED8CEB1D07D52CF2A74838584EDCF6CCE0A16` | `VERIFIED` |

Associated locked textures selected for inspection:

| Asset | File / role | Bytes | SHA-256 |
|---|---|---:|---|
| Arms | `textures\armColor.png` / base color, 2048×2048 | 3,503,653 | `3BFC47A0FBF19E5F2279A944A56423E2E82B6B4A80347613120256A326A73A8C` |
| Arms | `textures\armnormal.png` / tangent-space normal, 2048×2048 | 4,741,031 | `5153C5E562ED5DDF683C7E1BD8DDB8C042FFFB444B19F066C00D6E28F6D08AA5` |
| Arms | `textures\armRoughness.png` / roughness, 2048×2048 | 2,413,198 | `C108A2F16583EC86DA9B48658DDE275E5D5FD5E33574FD3C04D4A54F70BCF037` |
| Arms | `textures\armAO.png` / ambient occlusion, 2048×2048 | 5,935,284 | `903A8A48E1D426327EC05889D6238E62DD445EDEA30340B79DA00E60EAE42C95` |
| Axe | `textures\Material_BaseColor.png` / base color, 2048×2048 | 5,371,813 | `1A756730C2F97FC5D18B9A9CB05CB52E6C3234CD86C6115FD9660C2C7438EBE7` |
| Axe | `textures\Material_Normal.png` / tangent-space normal, 2048×2048 | 3,354,620 | `490E89B776BFC0311FBD1FEA470CDEC88CF399379F499373FE7AE7958197463A` |
| Axe | `textures\Material_Roughness.png` / roughness, 2048×2048 | 3,541,766 | `7931D9826D44B7735BAC00B77CA10380D4473C59B69B9ED70FFC44A49CA0209E` |
| Axe | `textures\Material_Metallic.png` / metallic, 2048×2048 | 1,821,144 | `84FA9FDFA1054BA5A0ED51FBEB3DBA459A08E10D923C06EC438680C20BA9C6B5` |

M1A established commercial provenance separately: “First Person arms” by DJMaesen and
“Fire axe” by Solidbot are CC BY 4.0 with exact archive/model binding. Production use must
retain the attribution text and modification notice locked in
`R2_FOREST1F_ASSET_INTAKE_AND_LICENSE.md`.

## Current project dependency map

The currently connected harvest path is:

```text
Input System Player map
  Attack / Previous / Next
      ↓
PlayerAxeHarvestController
  swing/equip/unequip AxeActionState + one contact per swing
      ↓
AxeViewmodelAnimator
  legacy Animation clips on PRF_SurvivalAxe
      ↓
normalized contact marker (0.5), not an AnimationEvent
      ↓
camera-relative blade segment + Physics.OverlapCapsuleNonAlloc
      ↓
range + facing + action-state validation
      ↓
TreeDamageEvent(tool.axe.survival, swingSequence)
      ↓
ForestInteractiveTree.TryReceiveDamage → tree lifecycle reaction
```

Key verified facts:

- `PlayerAxeHarvestController` instantiates `PRF_SurvivalAxe` under
  `ViewRoot/ViewCamera/Hands`. `contactDispatched` prevents continuous damage; the contact
  path uses a preallocated collider array.
- The current contact segment is derived from stable camera-space offsets. Serialized
  `BladeBase`/`BladeTip` transforms exist, but are not the authority for the gameplay segment.
- The current viewmodel uses seven project-owned legacy clips: `Axe_Idle` (1.458 s),
  `Axe_Equip` (0.583 s), `Axe_Unequip` (0.542 s), `Axe_Chop_Left` and
  `Axe_Chop_Right` (1.375 s each), `Axe_Chop_Heavy` (1.750 s), and
  `Axe_Recovery` (0.625 s), all at 24 FPS. No Animator Override Controller drives the axe.
- The player world body uses `AC_PlayerFoundation.controller` through the player
  `Animator`. At runtime `LocalPlayerVisualController` disables `FullBodyRenderer`, enables
  `LocalFirstPersonRenderer` without shadows, and enables
  `LocalFullBodyShadowRenderer` as shadows-only.
- `ViewCamera` is layer 0, mask `-1`, FOV 60, near clip 0.3 m. The current generated axe
  renderers are also layer 0 with shadows off. There is no dedicated FPS-viewmodel camera
  layer contract yet.
- Consequently, adding a second arms mesh without changing the presentation boundary would
  duplicate the local full-body arms. The existing shadow-only body should remain the world
  shadow authority; the new FPS arms must not cast a second body shadow.

Exact current migration touchpoints (read-only in M1B):

- `Assets/_Game/Infrastructure/Editor/ForestCell/ForestHarvestContentBuilder.cs`
- `Assets/_Game/Art/Player/Viewmodels/Generated/VM_Axe_ProjectOwned.fbx`
- `Assets/_Game/Art/Player/Viewmodels/Generated/Animations/*.anim`
- `Assets/_Game/Prefabs/Items/Tools/PRF_SurvivalAxe.prefab`
- `Assets/_Game/Prefabs/Player/PRF_PlayerFoundation.prefab`
- `Assets/_Game/Prefabs/Player/PRF_PlayerVisual_WoodMale01.prefab`
- `Assets/_Game/Presentation/ForestCamp/AxeViewmodelAnimator.cs`
- `Assets/_Game/Presentation/ForestCamp/PlayerAxeHarvestController.cs`
- `Assets/_Game/Presentation/Player/LocalPlayerVisualController.cs`

## DJMaesen FPS arms — technical audit

| Property | Verified result |
|---|---|
| Unity importer | Generic rig; global scale `1.486089`, file scale `1`, `useFileScale=false`; normals imported; Mikk tangents calculated; mesh compression off; non-readable |
| Mesh/renderers | One skinned mesh (`armsmesh`), one `SkinnedMeshRenderer`, one submesh/material slot; 4,256 Unity vertices and 7,240 triangles |
| Geometry channels | UV0, normals, and tangents present |
| Audit physical envelope | Normalized only for inspection to 1.45 × 0.72052 × 1.34471 m (X/Y/Z); this is not a production import scale |
| Skeleton | Root bone `chest`; 49 skin bones and 49 bind poses; no missing or duplicate skin-bone names |
| Arms | Complete left and right chains: `arm → elbow → wrist` |
| Hands | `middle`, `point` (index), `ring`, `pink`, and `thumb` chains on both hands, four joints each; separate palms; L/R wrist goals and arm poles are present as helper transforms |
| Weight topology | Maximum four influences per vertex. Elbow regions influence 696 L / 706 R vertices; each wrist influences 1,034; proximal finger joints influence 221–406 vertices. Left/right values are symmetric. The `chest` root has no directly weighted vertices, which is acceptable for this disconnected-arms rig |
| Animation clips | Zero non-preview clips; no idle, equip, chop, impact, recovery, or unequip motion |

Generic is the appropriate importer mode. This is a partial first-person rig without hips,
spine, legs, or a complete Humanoid mapping, so forcing Humanoid would create a false contract.
The bind pose and naming are stable enough for authored clips. Neutral inspection showed no
open seam or missing limb, but the model is not deformation-approved: extreme static wrist
experiments produced visible overlap and cannot substitute for elbow/wrist/finger animation QA.

Visual assessment at the audit distance: skin, fingerless gloves, sleeves, seams, and normal
detail remain readable in daylight. The 7.2k-triangle mesh is economical, but fingertip and
hand silhouettes are visibly modest for a hero close-up, and dark lighting loses sleeve detail.
This can be production-usable only after an authored two-hand animation pass, material tuning,
camera-layer isolation, and human review.

**Animation result: `ANIMATION_ASSET_REQUIRED`.** The rig exists; production action clips do not.

## Solidbot fire axe — technical audit

| Property | Verified result |
|---|---|
| Unity importer | No rig; global scale `112.9646`, file scale `1`, `useFileScale=false`; normals imported; Mikk tangents calculated; mesh compression off; non-readable |
| Raw transform signal | Imported root compensation observed at approximately position `(1647.02, 13825.57, 2505.72)`, Euler `(270.02, 0, 0)`, and scale `(100,100,100)` before audit normalization. Raw pivot/axis/scale are not production-ready |
| Mesh/renderers | One mesh (`metal-low`), one renderer, one submesh/material slot; 930 Unity vertices and 1,568 triangles |
| Geometry channels | UV0, normals, and tangents present |
| Structure | Handle and head are one mesh, not independently addressable pieces; this is acceptable for a rigid tool but requires project-owned anchors |
| Audit physical envelope | Normalized only for inspection to 0.04213 × 0.26699 × 0.86000 m before presentation rotation; 0.86 m longest-axis target is provisional |
| Texture set | Complete 2K base color, normal, roughness, and metallic maps |
| Animation | No rig and zero non-preview clips, as expected for a rigid prop |

The recognizable fire-axe silhouette and 2K surface maps survive close-up inspection. The
mesh is inexpensive and the wood/painted head remain legible, but it is not HDRP-ready as
imported: metallic, roughness, and AO need deterministic packing into the project mask-map
contract, and the one material must be tuned for distinct wood, painted metal, and exposed edge
response. No UV break or missing-normal defect was observed in the neutral render; final
specular quality remains conditional on that material build.

Recommended project-owned wrapper contract (provisional until authored grip review):

- Rotate the source so its long local Z axis becomes canonical handle +Y; normalize longest
  dimension to the approved physical axe length rather than retaining raw compensation.
- Place the rear/right-hand grip about 75% of the handle half-extent toward the butt, the
  left support about 15% toward the butt, and the blade contact plane about 82% toward the
  opposite/head end. Store authored transforms; do not recompute them from renderer bounds at
  runtime.
- Keep visual mesh/pivot, simple physical collider, and damage contact segment as separate
  project-owned children. The damage capsule/plane must never be derived from the visual mesh
  collider or remain continuously active.

## Temporary HDRP visual compatibility result

Evidence directory (outside Git):
`E:\SOTF_AssetIntake\R2_ART1_AUDIT_OUTPUT\M1B_ARMS_AXE`.

Machine-readable metrics:
`m1b_arms_axe_metrics.json` (`r2-art1-m1b-audit@1`). Required captures:

1. `01_arms_neutral_front.png`
2. `02_skeleton_bone_hierarchy.png`
3. `03_axe_neutral_material.png`
4. `04_axe_pivot_grip_points.png`
5. `05_fps_idle_grip.png`
6. `06_fps_two_hand_grip.png`
7. `07_windup_compatibility_pose.png`
8. `08_contact_strike_compatibility_pose.png`
9. `09_neutral_daylight_closeup.png`
10. `10_dark_forest_lighting_closeup.png`

The static scene checked FOV 50, 60, and 70. The models are physically compatible in the
sense that the rigid axe can be attached to `R_wrist` and both wrist chains can reach the
handle. It did **not** produce a shippable grip: hand overlap, sleeve/handle intersection, and
poor finger closure are visible in the compatibility images. This is evidence for authored
two-hand clips and constraints, not a generated-animation candidate. FOV 60 matches the current
project; FOV 50/70 exposed additional framing and near-camera risks. The axe head did not cross
the camera plane in the neutral inspection, but wind-up/strike clipping must be revalidated with
real animation.

The lighter and darker captures confirm the texture sets render, while also showing that dark
forest lighting needs viewmodel-specific exposure/material tuning. The audit intentionally did
not change project HDRP, Quality, or lighting settings.

## Commercial-quality decision

| Asset | Legal/provenance | Visual | Geometry | Material | Rig | Animation | Integration | Decision |
|---|---|---|---|---|---|---|---|---|
| DJMaesen arms | Verified CC BY 4.0; attribution required | Economical and readable, but hero hand/finger silhouette and dark response require human review | Complete two-arm skinned mesh; suitable budget | Complete 2K base/normal/roughness/AO; project HDRP mask material required | Stable Generic 49-bone arms/fingers rig; deformation approval deferred | No usable clips | Needs dedicated FPS layer, duplicate-body prevention, authored anchors/controller, clip/contact regression | `ANIMATION_ASSET_REQUIRED` |
| Solidbot fire axe | Verified CC BY 4.0; attribution required | Recognizable survival fire axe; close-up quality conditional | 1,568 tris, single rigid mesh; sufficient for viewmodel if approved | Complete 2K PBR set; HDRP packing/tuning required | Rigid prop, no rig required | Driven by arms; no prop clips required | Requires project wrapper, normalized scale/axis/pivot, two grip anchors and separate blade contact geometry | `CONDITIONAL_REPAIR_REQUIRED` |

Both assets remain **`HUMAN_VISUAL_APPROVAL_REQUIRED`**. Technical acceptance does not claim
parity with *Sons of the Forest* or approval of final visual quality.

## Required repairs before integration

1. Create project-owned derived imports/wrappers with reproducible scale, +Y handle axis,
   pivot, HDRP materials, and attribution metadata; never bind gameplay to vendor hierarchy.
2. Author `Idle`, `Equip`, `Unequip`, alternating left/right chops, heavy chop, contact recoil,
   miss recovery, and recovery/return clips on the DJMaesen Generic rig with the Solidbot axe.
3. Lock the axe to the right wrist/rear grip; author the left support hand and fingers, then use
   IK/constraints only as controlled correction. Validate shoulder, elbow, wrist, thumb, and
   finger deformation through each full clip.
4. Establish a dedicated FPS visibility contract so local viewmodel arms do not duplicate
   `LocalFirstPersonRenderer`; retain `LocalFullBodyShadowRenderer` as the body-shadow source.
5. Preserve gameplay ownership: input selects action state; animation exposes a single contact
   marker/window; controller validates range/facing/action and dispatches at most one damage
   event per intended swing. Animation never chooses the target.
6. Validate FOV 50/60/70, near-plane and sleeve/hand/head/handle clipping, both lighting cases,
   shadows, equip pop, and steady-state allocation before replacing the fallback.

## Future migration plan (not implemented in M1B)

1. Produce attributed project-owned arms/axe derivatives and HDRP materials in an isolated
   validation scene. Keep `VM_Axe_ProjectOwned.fbx` and current clips as a recoverable fallback.
2. Add a viewmodel presentation boundary under `ViewCamera/Hands`: DJMaesen arms are local FPS
   only; Solidbot axe attaches to authored right grip; left grip is clip-authored with optional
   bounded IK correction.
3. Replace the current viewmodel animation asset/controller behind the existing
   `PlayerAxeHarvestController` action/damage boundary. Migrate contact timing to one explicit,
   tested contact marker per chop without enabling continuous collider damage.
4. Update `LocalPlayerVisualController`/player visual prefab so the first-person full-body arm
   region is not rendered together with the new arms, while world representation and the
   shadows-only full body remain intact.
5. Validate equip/idle/wind-up/strike/contact/recover, single-contact damage, missed swings,
   tree reaction, camera clipping, shadows, PlayMode regressions, and visual screenshots.
6. Only after validation and human approval, switch `PRF_PlayerFoundation` to the new
   project-owned `PRF_SurvivalAxe` derivative. Remove old references first; clean legacy assets
   in a separate reviewed change, never as broad deletion.

## Audit closeout

- Exact source binding: `VERIFIED`.
- Technical inspection and required external evidence: complete.
- Production integration: not started.
- Production animation: missing.
- Final visual decision: `HUMAN_VISUAL_APPROVAL_REQUIRED`.
- Milestone state: `M1B_TECHNICAL_COMPLETE`.
