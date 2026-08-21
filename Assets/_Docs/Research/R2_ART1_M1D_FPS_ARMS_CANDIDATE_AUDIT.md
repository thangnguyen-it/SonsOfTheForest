# R2-ART1-M1D-G1 — piter207 FPS Arms Candidate Audit

## Decision

**M1D_G1_REJECT**

The pack is technically intact and rigged, but neither selected high-detail variant reaches the required commercial first-person visual quality. The normal sleeved variant is the best member of the pack, yet its simplified hand anatomy, coarse cuffs/sleeves, low-resolution clothing textures, fragile two-handed grip, and dated material response are not a clear improvement over the already human-rejected DJMaesen arms. Reaching the target would require substantial mesh, UV/texture, skinning, and material replacement rather than bounded integration work.

`HUMAN_VISUAL_APPROVAL_REQUIRED`

`M2_NOT_STARTED`

This is an asset audit, not production integration. All grip and chop views are static compatibility poses; they are not production animation clips.

## Source and rights binding

| Item | Verified value |
|---|---|
| Asset | Fps Arms Pack |
| Creator | piter207 |
| Listing | <https://www.cgtrader.com/free-3d-models/character/man/fps-arms-pack> |
| CGTrader item | 696175 |
| License | CGTrader Royalty Free License (no AI) |
| Commercial use | Permitted |
| Modification/adaptation | Permitted |
| Raw redistribution/resale | Prohibited |
| AI training/fine-tuning | Prohibited |

The locked G0 manifest SHA-256 is `EFBECB2631D414A2ACE54C287E941AC7C1202409A83337468DE2F4B493EC04F6`; the locked G0 report SHA-256 is `1F9BA67DBB9F538FCC1265B0F95D5D6D488B488183CDAA5C281CE4104FA508D7`.

| Archive | Bytes | SHA-256 | Safety result |
|---|---:|---|---|
| `Fps+Arms+Pack+[Updated]+FBX.zip` | 7,126,477 | `1BACCC2B49A6D3E7148D458E9B8216ECD31FA145BDA14D8E31D6B631AFDF0337` | PASS |
| `Fps+Arms+Pack+[Updated]+Blend.zip` | 8,367,778 | `D6F4394214D535931EE43F2A7D416FB0883E4A237C5144872BBC315D948708DF` | PASS |

Both archives passed member-path, traversal, collision, encryption, link/reparse, expansion-ratio, and executable-content checks before isolated extraction. The FBX archive contains 23 files/10 directories (8 FBX models); the Blend archive contains 23 files/10 directories (8 Blend models). Their 14 image payloads match by relative role and hash. The FBX package is the authoritative Unity source because Unity imports it directly and deterministically; the Blend package adds no variant or texture payload and would require an external DCC conversion.

The exact Solidbot Fire Axe used in visual checks is `hacha-bomberos-terminada.fbx`, SHA-256 `FB880E4940BC51AF2F21C8C71D7ED8CEB1D07D52CF2A74838584EDCF6CCE0A16`. No generated or placeholder axe was used.

## Complete FBX inventory

All eight files are binary FBX 7.3, contain one skinned mesh, UV0, 51 limb nodes, 52 skin clusters, complete bilateral shoulder/arm/forearm/roll/wrist/hand chains, and four joints for each of all ten fingers. Every control point is weighted, weights are normalized, and the maximum is two influences per vertex. No invalid indices, degenerate polygons, duplicate control-point positions, or non-manifold edges were found. Open boundary edges correspond primarily to the intentionally open shoulder ends. No animation stacks or blend shapes are included.

| Variant | Sleeve | Tier | Source verts | Tris | Material slots | SHA-256 |
|---|---|---|---:|---:|---:|---|
| Payday | Bare | High | 4,882 | 9,620 | 2 | `30CE4B59B55DC2FE2A6E34E95BC51D6B40354C96EF5FDF5CC7500EE1781F0CB2` |
| Payday | Bare | Low | 1,202 | 2,330 | 2 | `2E3F7FA7865DCB848C02C3A0F5D06FC013D2B05E4F0177F1371348210BAF2E83` |
| Payday | Sleeved | High | 5,552 | 10,936 | 3 | `A38DFBA0C2FBF983F7B8E2EC81F536589AC82539CB695326DE49789DEA1A9DF6` |
| Payday | Sleeved | Low | 1,376 | 2,664 | 3 | `C3AD8D2592A437518447C8C2975124C7A664369626E5882C2687E2132AA99CF0` |
| Normal | Bare | High | 4,662 | 9,268 | 1 | `5E365794E4544BD5C64C56C18BA9A2D69CA41D3AD9EFE4E36630BEEEC93AF2D1` |
| Normal | Bare | Low | 1,136 | 2,242 | 1 | `9D45EA0160416280E1C04C4E5F5932432267DF1ADF6B104739C1F85DF55FB0EC` |
| Normal | Sleeved | High | 5,188 | 10,296 | 2 | `043C82555C29A8BD5AAA21EBFCEB7871C8F266399A98B100BDF6BCEF521C573F` |
| Normal | Sleeved | Low | 1,274 | 2,504 | 2 | `0F218AF00C0D51C5541A8E12E8B27DD3F26277F77415D8B7E3F39AD4A9655B96` |

Source normals and tangents are not embedded; Unity successfully recalculates both. Source axes are Y-up, +Z front, X coordinate axis, with `UnitScaleFactor=1`. Embedded texture references point to obsolete absolute author-machine paths, but the required diffuse and normal images are present in both archives and were rebound only in the ignored audit wrapper.

Texture inventory relevant to the candidates:

- Normal bare arms: diffuse and normal, each 1900×1010.
- Normal sleeve camouflage: four diffuse choices and one normal, each 600×1000.
- Payday hand/glove diffuse and normal: each 669×955.
- Payday sleeve diffuse and normal: each 600×1000.

The Payday variants were excluded from visual testing because their stylized glove treatment conflicts with the target survival presentation. Low variants reduce source geometry by roughly 75% and provide no close-up quality advantage. The two selected visual candidates were therefore:

1. `Fps_Arms/With_Sleeve/FpsArmsHigh.fbx`
2. `Fps_Arms/No_Sleeve/FpsArmsHigh.fbx`

## Unity import metrics

Unity 6000.3.10f1 imported the selected FBX files as Generic skinned rigs. The imported sleeved mesh has 5,880 Unity vertices, 10,296 triangles, two submeshes/material slots, 52 bones, complete generated normals/tangents/UV0, no blend shapes, and bounds approximately 1.8073×0.3225×0.3721 m. The imported bare mesh has 5,050 Unity vertices, 9,268 triangles, one submesh/material slot, 52 bones, complete generated normals/tangents/UV0, no blend shapes, and bounds approximately 1.8073×0.3089×0.3592 m.

The exact axe imported at 930 Unity vertices and 1,568 triangles. Its source bounds are approximately 0.00043×0.00275×0.00885 m, requiring an audit-wrapper scale correction of 100×. This transform correction did not alter axe geometry.

## Visual and compatibility findings

The audit used neutral, one-hand, two-hand, wind-up, contact, follow-through, and recovery static poses; FOV 60 and 75; neutral and dark lighting; and direct Rocketbox/DJMaesen comparison. One corrective wrapper pass reduced exposure, replaced unsupported HDRP wireframe capture with an explicit topology overlay, aligned the axe to palm centers, and targeted finger bones toward the handle. No source vertices, UVs, weights, skeletons, or textures were edited.

- **Hands:** the fingers are thin and simplified, the palm lacks close-up anatomical volume, and the silhouette reads as dated at normal FPS distance.
- **Sleeves/cuffs:** the sleeves are bulky tubes with coarse shoulder openings and cuff transitions. The 600×1000 camouflage source lacks the texel/detail reserve expected of a persistent hero viewmodel.
- **Grip:** even after the single permitted wrapper correction, the handle/palm relationship is inconsistent. Finger wrap and thumb placement remain visibly implausible, with intersection/floating risk in both one- and two-hand views.
- **Deformation risk:** the rig is complete, but the two-influence weighting ceiling and coarse elbow/wrist distribution are weak foundations for a hero two-handed chop. Static poses already expose hard bends; authored animation alone cannot add missing deformation fidelity.
- **Materials:** the diffuse/normal-only source needs a full HDRP surface rebuild. Controlled captures still show over-bright skin/clothing and an inconsistent response beside the axe; correcting this would not fix the geometry and skinning deficiencies.
- **Framing:** FOV 60 and 75 do not clip the camera plane, but both reveal open shoulder ends, weak cuffs, oversized sleeves, and the unstable grip.
- **Comparison:** this candidate is not a clear visual improvement over DJMaesen, which is already rejected as final hero art. Rocketbox provides better overall character coherence but is not itself an approved FPS-arms source.

The audit did not attempt a third cosmetic rescue pass. Passing this candidate to production would require remodeled hands/palms, revised sleeve/cuff geometry, new high-resolution PBR textures, improved wrist/elbow/finger weighting, and a production material authoring pass before animation. That is substantial external art replacement, so it does not meet the bounded-repair meaning of `M1D_G1_CONDITIONAL`.

## Evidence

Evidence is ignored and remains outside tracked production assets:

- Contact sheet: `Temp/R2_ART1_M1D_G1_Audit/Evidence/M1D_G1_CONTACT_SHEET.png`
- Contact-sheet SHA-256: `9C1D2FD2FA80EDC6CCCA51267BA9AAD27CBBB1A66FB27FFC8FE0F24E7E85AF5C`
- Contact-sheet index: `Temp/R2_ART1_M1D_G1_Audit/Evidence/M1D_G1_CONTACT_SHEET_INDEX.json`
- Candidate inventory: `Temp/R2_ART1_M1D_G1_Audit/Evidence/m1d_g1_inventory.json`
- Measured/evidence metrics: `Temp/R2_ART1_M1D_G1_Audit/Evidence/m1d_g1_metrics.json`
- Individual captures: 16 unique 1600×900 lossless PNG files, all non-zero and hash-verified unchanged after contact-sheet generation.

The contact sheet was opened and visually inspected: all 16 tiles are present, non-empty, aspect-correct, and captioned outside the render. Its over-bright areas are retained as audit evidence rather than edited away.

## Production disposition

- piter207 normal sleeved high: **REJECT as production FPS viewmodel source**.
- piter207 normal bare high: **REJECT as production FPS viewmodel source**.
- Payday and low variants: **REJECT without additional production trial**.
- Solidbot Fire Axe: unchanged; this audit does not revise its earlier conditional human acceptance.
- Raw archives/models/textures: remain external/ignored and are not distributable repository content.
- Gameplay integration, AxeActionState, production controller/clips, and M2: untouched.

`M1D_G1_REJECT`

`HUMAN_VISUAL_APPROVAL_REQUIRED`

`M2_NOT_STARTED`
