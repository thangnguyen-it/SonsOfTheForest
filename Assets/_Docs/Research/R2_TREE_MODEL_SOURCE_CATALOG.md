# R2 Tree Model Source Catalog

Date: 2026-07-23
Scope: lawful external model sources for replacing the current forest tree direction with a Sons-of-the-Forest-like temperate mixed forest art set.

## Visual direction locked by current references

The current user-supplied screenshots and notes change the forest target from a single conifer/procedural-tree problem into a mixed temperate forest problem.

Target composition:

- `Spruce`: main dark conifer canopy, tall straight trunks, sparse/dead lower limbs, heavier live crown above.
- `Birch`: bright white-gray bark, strong close-up readability, important for chopping/tree-damage views.
- `Maple`: broadleaf seasonal contrast, especially yellow/orange/red autumn foliage.
- `Oak`: broadleaf mature canopy with heavier trunk and branch mass.
- `Small woody layer`: dwarf cedar, vine maple, red maple, arbutus-like irregular forms.
- `Understory`: fern, grass, shrub, moss, litter, roots, and embedded rocks.

Current project mismatch:

- The forest must not rely on one repeated spruce/pine-like morphology.
- Near-view trees must not read as "electric poles with attached leaves".
- Thin exposed trunks are acceptable only when the canopy layer above still forms a believable forest mass.
- Cards may exist in authored foliage at mid/far distances, but near foliage must not expose obvious flat sheets, white backs, or black flips.

## Evidence classification

- `OBSERVED_USER_REFERENCE`: supplied Sons of the Forest screenshots and annotations in the July 2026 project conversation.
- `USER_SUPPLIED_SECONDARY_REFERENCE`: attached tree-family note reporting wiki.gg evidence for Birch, Maple, Oak, and Spruce plus decorative woody flora. The browser could not directly fetch wiki.gg pages due 403, so this should guide sourcing but should remain re-verifiable.
- `LICENSE_VERIFIED`: source pages were fetched directly during this pass and explicitly state CC0/Public Domain terms.
- `PROVISIONAL`: implementation choice based on current sourcing, subject to visual review in Unity.

## Candidate source ranking

## Non-negotiable 60 FPS gate

Visual quality is not enough. The playable target is a stable minimum of 60 FPS, so every tree source must be evaluated as an asset source, not accepted as a gameplay prefab by default.

Hard rule:

- A raw downloaded tree is never approved for direct forest placement until it has passed a Unity benchmark.
- Multi-million-triangle trees are source assets for baking, decimation, material extraction, silhouette study, and LOD generation only.
- The final gameplay forest must use LODs, impostors, chunking/instancing, and distance-based interaction promotion.

Initial performance budgets:

| Layer | Starting budget | Notes |
|---|---:|---|
| Near hero tree LOD0 | 30k-120k tris | Only a small number visible near the player; must look good in close camera/chopping views |
| Near exceptional tree | up to 200k tris | Requires explicit benchmark approval and limited placement |
| Mid tree LOD1 | 5k-20k tris | Must preserve silhouette and material read, not close-up branch detail |
| Far tree LOD2 | 500-3k tris | Can be solid proxy/impostor support |
| Distant forest | billboard/impostor/shell | No gameplay collider or tree logic |
| Runtime colliders/scripts | near-player only | Interactive promotion around the player; far forest is render data |

Benchmark requirement before adoption:

- scene preview: visual review only;
- cluster benchmark: at least 10 near, 50 mid, and 300 far/impostor trees;
- record FPS, frame time, batches, SetPass, triangles, vertices, and shadow impact;
- reject or rework any candidate that cannot plausibly support 60 FPS after LOD/material optimization.

This means the next acquisition pass may download large packs outside Git for inspection, but only curated, optimized Unity-ready outputs should enter the repository.

The operational Unity gate for these rules is documented in `R2_FOREST_MODEL_INTAKE_GATE.md`.

### Tier 1 - best lawful fit, but large downloads

#### Mantissa / Midge Sinnaeve free resources

Source: https://mantissa.xyz/free.html

License: `LICENSE_VERIFIED`; the page states the free resources are currently Public Domain / CC0.

Why it matters:

- It directly covers several target families instead of just pine/fir.
- Assets are distributed as FBX/Blender packs, suitable for Unity import after material/LOD processing.
- It is a better art-direction match than the current procedural conifers or the first Poly Haven sapling trial.

Download candidates verified by HEAD request:

| Candidate | Target role | Source page listing | HEAD size | Decision |
|---|---:|---:|---:|---|
| CG Japanese Maple Pack | Maple / autumn broadleaf | 5 Japanese maple FBX models with textures | 417.9 MB | Highest first-download candidate |
| CG Birch Tree Pack | Birch canopy + chopping close-up bark | 5 Birch FBX models with textures | 845.9 MB | Second priority; key SOTF visual contrast |
| CG Fir Trees | conifer substitute / fir-spruce benchmark | 5 fir trees, Blender + FBX, photoscanned bark | 1246.3 MB | Good conifer trial, but large |
| CG Spruce Tree Pack | direct Spruce family | 5 spruce FBX models, no textures | 2149.2 MB | Relevant but expensive and material work required |

Recommended acquisition order:

1. Download and inspect `CG Japanese Maple Pack` first. It is the smallest Mantissa tree pack and directly tests broadleaf/seasonal vibe.
2. Download `CG Birch Tree Pack` second if the file budget is acceptable. Birch is the most visually important missing family.
3. Only then evaluate conifer packs. The current project already has conifer placeholders; the missing broadleaf/birch contrast is a bigger art-direction gap.

Japanese Maple inspection, 2026-07-23:

- Downloaded outside Git to `E:/knee_project/Reference/SOTF_MODEL_SOURCING/Mantissa/JapaneseMaple/mantissa_japanese_maple_pack.zip`.
- Zip contents: 5 FBX files, 4 JPG textures, 1 displacement TIF, 2 text/license files.
- Zip size on disk: 438,241,399 bytes.
- Uncompressed FBX total: about 395.1 MB.
- Largest texture: `Textures/Bark_DISP.tif`, about 192 MB uncompressed.
- Smallest FBX trial: `Mantissa_Japanese_Maple_004.FBX`, about 75.1 MB in the zip.
- Unity raw import measurement for Maple 004: 1 renderer, 1 mesh filter, 2,417,961 triangles, 1,895,762 vertices, 8 materials, bounds about 4.12 x 6.68 x 4.00 m.
- Decision: reject raw Maple 004 as direct gameplay forest content under the 60 FPS rule.
- Implementation impact: Mantissa Maple remains a strong visual/source candidate for decimation, LOD generation, bark/leaf material extraction, and silhouette reference. It must not be placed directly into the playable forest at raw resolution.
- Repository impact: raw extracted Maple files were removed from `Assets/` after measurement; only the external zip in `Reference/` and this report remain.

Do not commit the full raw packs blindly. Preferred workflow:

- download to an external `Reference/` or temporary import workspace;
- inspect mesh count, material count, texture count, bounds, and topology;
- create curated Unity prefabs under `Assets/_Game/Prefabs/World/...`;
- commit only approved, documented, Unity-ready assets.

### Tier 2 - CC0 but either too heavy or not exact enough

#### Poly Haven

Source/license: https://polyhaven.com/license

License: `LICENSE_VERIFIED`; Poly Haven states its assets are CC0 and can be redistributed and used commercially.

Already trialed:

- `fir_sapling`
- `pine_sapling_small`
- `rock_09`
- `rock_moss_set_02`
- `stone_fire_pit`

Trial conclusion:

- Rocks/fire-pit assets are promising.
- Saplings are useful as young/understory conifers.
- Saplings still use twig alpha/card foliage and are not a final answer for mature near-field canopy.

Adult Poly Haven candidates:

| Candidate | Target role | Observed/source metadata | Decision |
|---|---|---|---|
| `pine_tree_01` | tall conifer silhouette reference | Poly Haven page describes a tall slender pine; API/source package is very large | Hold; too heavy for casual repo import |
| `fir_tree_01` | conifer comparison source | relevant conifer model, smaller than pine but still large | Inspected as raw source on 2026-07-23; source/reference only, rejected for direct gameplay |

`fir_tree_01` adult conifer inspection, 2026-07-23:

- Downloaded outside Git to `E:/knee_project/Reference/SOTF_MODEL_SOURCING/PolyHaven/fir_tree_01/`.
- License: Poly Haven CC0.
- Downloaded files: `fir_tree_01_1k.fbx` plus 1K bark/trunk/twig texture maps.
- Raw FBX size: 249,300,492 bytes.
- Temporary Unity import path: `Assets/_Game/Art/Models/World/ExternalTrials/PolyHaven/fir_tree_01_raw_import/`.
- Raw Unity import measurement: 3 mesh filters, 3 renderers, no LODGroup, bounds about 18.78 x 18.94 x 6.51 m.
- Raw triangle count: 6,982,937 triangles total:
  - `fir_tree_01_a_LOD0`: 4,176,819 triangles.
  - `fir_tree_01_b_LOD0`: 2,300,624 triangles.
  - `fir_tree_01_c_LOD0`: 505,494 triangles.
- Decision: reject raw import as direct gameplay content under the 60 FPS gate.
- Implementation impact: keep the external source for lawful visual reference, future decimation/baking experiments, bark/twig material study, and silhouette comparison. Do not place it in `SCN_Foundation` or any gameplay forest prefab until a curated LOD/impostor version is produced and benchmarked.
- Repository impact: the temporary raw import was removed from `Assets/`; only the external `Reference/` copy and this documented result remain.

### Tier 3 - potentially useful, but needs manual/license gate

#### Sketchfab, Fab, Unity Asset Store, SpeedTree

These sources may contain better game-ready assets with LODs, wind, and sensible poly budgets, but they are not first-choice for this repository until license and redistribution details are explicit.

Rules:

- Do not commit raw paid marketplace assets unless the license and repository visibility allow it.
- Do not rely on Sketchfab search snippets as final proof; inspect the asset page/license manually before acquisition.
- Treat SpeedTree/Asset Store/Fab packs as production candidates if CC0 sources cannot meet the visual target.

Search result notes, 2026-07-23:

- Sketchfab search surfaced a `Maple trees pack (lowpoly, game ready, LODs)` candidate with 12 maple models and LOD levels.
- Sketchfab search surfaced a `Pine trees pack (lowpoly, game ready, LODs)` candidate with 15 unique pine models, bark/cluster textures, and billboard textures.
- Sketchfab search surfaced a low-poly game-ready spruce candidate under CC Attribution.
- Direct automated page fetch returned 403, so these are not yet license-verified or acquisition-ready.
- Implementation impact: these are likely better aligned with the 60 FPS rule than raw CC0 photogrammetry-scale trees, but they require manual page/license inspection and probably manual download or Sketchfab API credentials.

Sketchfab API metadata check, 2026-07-23:

| Candidate | URL | API result | License | Acquisition result | Decision |
|---|---|---|---|---|---|
| `Realistic Fir Trees Pack (LODS, gameready)` | https://sketchfab.com/3d-models/realistic-fir-trees-pack-lods-gameready-f58e8b6d733e4b0586e5b7db847b89e7 | downloadable, published 2024-09-13 | CC Attribution | `/download` endpoint returned 401 Unauthorized without Sketchfab credentials | Strong manual candidate; requires attribution and authenticated/manual acquisition |
| `Pine trees pack (lowpoly, game ready, LODs)` | https://sketchfab.com/3d-models/pine-trees-pack-lowpoly-game-ready-lods-e1e9c07b8e2e445c943fec660beefba2 | downloadable, published 2025-06-02 | CC Attribution | `/download` endpoint returned 401 Unauthorized without Sketchfab credentials | Strong manual candidate; likely better first conifer production trial than raw CC0 scans |
| `Pine Tree [Game-ready]` | https://sketchfab.com/3d-models/pine-tree-game-ready-dc3fbd9205cf4027a4455d1f415e0478 | downloadable, published 2022-07-14 | CC Attribution | `/download` endpoint returned 401 Unauthorized without Sketchfab credentials | Smaller fallback candidate; requires attribution and visual/FPS intake |

These candidates are not blocked by license in principle, but they are blocked for automatic acquisition in this session because Sketchfab requires authentication for download URLs. If acquired manually, store attribution metadata with the imported asset and run the normal intake gate before scene use.

Paid/marketplace direction:

- A production forest should probably use a game-ready tree pack with authored LODs, billboards/impostors, wind setup, and shared atlas materials.
- If free lawful sources fail visual/performance gates, marketplace packs become the practical route.
- Marketplace assets must be stored according to their license; a public repository may need manifest-only documentation rather than raw asset redistribution.

### Unity Asset Store local trial - Mayo Games Pine forest set Free sample

User-acquired local package, 2026-07-23:

- Package: `Pine forest set [Free sample]`.
- Publisher: Mayo Games.
- Source: Unity Asset Store / Package Manager.
- Local import path: `Assets/pineForset_MayoGames_free/`.
- Package notes visible in Package Manager: URP compatibility, LODs and collision meshes, prefab library, low-poly models.
- Repository policy: raw package files are ignored and must not be committed to the public repository unless license/redistribution permission is explicitly resolved. Commit only this evaluation, attribution/source manifests, or curated project-authored wrapper outputs when safe.
- Local tool: `Sons Of The Forest > Forest Models > Mayo Local Trial`, including `Build Comparison Scene`, `Build 60 FPS Benchmark Scene`, and `Write Intake Report`. This builds ignored comparison and 60 FPS benchmark scenes from the user-acquired local package without making the public repository depend on raw Asset Store GUIDs.
- Local wrapper path: `Assets/pineForset_MayoGames_free/SOTF_LocalWrappers/`. Wrappers convert materials to HDRP/Lit, keep tree colliders only for near-tree trials, remove ground-cover colliders, and disable realtime shadows on final LODs.

Unity intake measurement after local wrapper generation:

| Prefab | LODGroup | LOD0 tris | LOD1 tris | LOD2 tris | Colliders | Shaders after conversion | Technical gate |
|---|---|---:|---:|---:|---:|---|---|
| `PRF_TRIAL_furTree_SOTFWrapper` | yes | 1,334 | 224 | 224 | 2 | HDRP/Lit | PASS |
| `PRF_TRIAL_furTreeSmall_SOTFWrapper` | yes | 620 | 286 | 286 | 2 | HDRP/Lit | PASS |
| `PRF_TRIAL_furTree_dead_SOTFWrapper` | yes | 516 | 152 | 152 | 2 | HDRP/Lit | PASS |
| `PRF_TRIAL_fern_SOTFWrapper` | yes | 176 | 176 | 176 | 0 | HDRP/Lit | PASS |
| `PRF_TRIAL_brunch_1_SOTFWrapper` | yes | 32 | 32 | 0 | 0 | HDRP/Lit | PASS |
| `PRF_TRIAL_brunch_2_SOTFWrapper` | yes | 64 | 64 | 0 | 0 | HDRP/Lit | PASS |
| `PRF_TRIAL_brunch_3_SOTFWrapper` | yes | 128 | 128 | 0 | 0 | HDRP/Lit | PASS |
| `PRF_TRIAL_rock_1_SOTFWrapper` | yes | 56 | 56 | 56 | 0 | HDRP/Lit | PASS |

Local 60 FPS benchmark-scene static geometry check:

| Group | Renderers | Colliders | LODGroups | Total mesh tris | LOD0 tris |
|---|---:|---:|---:|---:|---:|
| `MayoBenchmark_Near_10` | 20 | 20 | 10 | 10,954 | 8,744 |
| `MayoBenchmark_Mid_50` | 100 | 0 | 50 | 52,576 | 41,474 |
| `MayoBenchmark_Far_300` | 600 | 0 | 300 | 313,200 | 247,000 |
| `MayoBenchmark_GroundCover` | 150 | 0 | 150 | 13,680 | 13,680 |

Assessment:

- This is the first externally acquired tree pack that passes the model intake performance budget by a very large margin.
- It solves the "raw model is too heavy" problem and is appropriate for 60 FPS prototyping.
- It does not yet solve final visual realism. The package is visibly low-poly/stylized and should be treated as a gameplay-performance prototype candidate, not the final Sons-of-the-Forest-like forest art target.
- The URP material/shader setup does not work directly in this HDRP project. The local wrapper workflow converts materials to HDRP/Lit for trial scenes, but a committed production import would still need deliberate project-owned HDRP materials and license/redistribution approval.

Implementation impact:

- Use this pack for local benchmark lineups and prototype replacement experiments only if the user accepts the stylized look as temporary.
- Do not replace the current forest art target with this pack as "final".
- If promoted into committed gameplay content, create project-owned wrapper prefabs and an attribution/license manifest under project paths; keep raw package files local/ignored unless redistribution permission is confirmed.

Fab game-ready candidate pass, 2026-07-23:

| Candidate | Source | Evidence | License/acquisition state | Decision |
|---|---|---|---|---|
| Maple trees pack (lowpoly, game ready, LODs) | https://www.fab.com/listings/20abd5c9-8e44-430e-91da-a2872bd5d5ad | 12 maple models; large, medium, small, sapling variants; three LOD levels plus billboard; shared 2048 bark/branch textures; large LOD0 about 12,987-22,490 triangles; billboard about 36 triangles | Fab page requires selecting/buying a license; included format is Blender; not downloaded | Best broadleaf candidate found so far for a 60 FPS Unity trial after manual acquisition/export |
| Fir trees pack (9 models+LODS and billboards) | https://www.fab.com/listings/0015d682-64e8-4d70-9ea6-d5f6faaa22be | 9 fir models; 4 LOD levels where LOD3 is billboard; shared bark and branch textures; large/medium LOD0 about 6,813-12,969 triangles, small about 501-599 triangles; billboard about 20 triangles | Fab page requires selecting/buying a license; included format is Blender; not downloaded | Best conifer candidate found so far for a 60 FPS Unity trial after manual acquisition/export |
| White Pine Forest Pack | https://www.fab.com/listings/4f595cfc-819b-40c3-a665-fbbb205f7864 | 13 pine tree meshes with collision, LODs and billboards; includes ferns/plants/ground textures; about 720 MB | UE Marketplace License; Unreal Engine format; not downloaded | Good visual/ecosystem candidate but lower priority for Unity until conversion/legal storage path is clear |
| Norway Spruce Tree Pack | https://www.fab.com/listings/cd119d0e-ec1c-4071-be37-90d5371fa3c6 | 20 spruce trees across large/medium/small/x-small; includes collision, billboards, wind shader, fallen spruce, rocks, stumps, fungi and scatter | Fab page requires selecting/buying a license; Unreal Engine format; not downloaded | Very close to the SOTF conifer target, but conversion cost makes it a later candidate unless the user wants a paid/manual Unreal-to-Unity pipeline |

Assessment:

- The Fab Maple and Fir packs are the strongest next trial targets because their documented triangle counts already fall inside or near the 60 FPS starting budgets.
- The Unreal-only forest packs may look closer to a finished survival-game biome, but they add material conversion, wind shader conversion, and license-storage complexity.
- No marketplace model is approved for repository import until its license is selected, acquisition is lawful, and the imported asset passes the same cluster benchmark as free assets.

## Implementation implications

The next forest art pass should not be "find one better tree." It should build a small family set:

- Spruce/Fir: 3 mature, 2 sparse/dead, 2 young.
- Birch: 3 mature, 2 young, close-up bark/chop material study.
- Maple: 3 summer/autumn broadleaf variants.
- Oak: 2 mature broadleaf variants if an adequate lawful source is found.
- Understory: dwarf cedar/vine-maple-like shrub, fern, grass, moss, leaf litter.

Near-field rule:

- Trunks, primary branches, secondary branches, bark, roots, and chop surfaces need real geometry or strong normal/displacement material support.
- Authored leaf cards are acceptable only if they do not visually reveal large flat sheets near the player.
- Any imported tree must get Unity validation for material count, renderer count, bounds, LOD availability, shadow policy, and close-camera artifacts.

## Proposed next action

Do not continue with raw Mantissa imports as gameplay assets. Use Mantissa as source/reference only unless a decimation/bake pipeline is created.

Next practical sourcing order:

1. Manually acquire the Fab Maple and Fir Blender packs if their license/price is acceptable, then export/import a small subset into Unity for visual and 60 FPS cluster validation.
2. Prefer packs with per-tree LODs, billboard/impostor textures, shared materials, and documented polycounts.
3. Use Mantissa Japanese Maple/Birch/Spruce only as visual source material for decimation, bark/leaf studies, or future bake pipelines.
4. Consider Unreal-only packs such as White Pine Forest or Norway Spruce only after deciding how to handle paid assets, format conversion, and private/non-public asset storage.
5. Reject any candidate that cannot be made to fit the 60 FPS cluster benchmark.

Expected blocker:

- The best 60-FPS-suitable realistic tree packs may require manual download, account login, purchase, or private asset storage.
- Current automated acquisition result: CC0 raw adult scans are legally downloadable but too heavy for direct gameplay, while better game-ready LOD packs are available under attribution/marketplace terms but require authenticated or manual acquisition.
