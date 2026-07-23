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
| `fir_tree_01` | conifer comparison source | relevant conifer model, smaller than pine but still large | Hold; only inspect if Mantissa conifers fail |

### Tier 3 - potentially useful, but needs manual/license gate

#### Sketchfab, Fab, Unity Asset Store, SpeedTree

These sources may contain better game-ready assets with LODs, wind, and sensible poly budgets, but they are not first-choice for this repository until license and redistribution details are explicit.

Rules:

- Do not commit raw paid marketplace assets unless the license and repository visibility allow it.
- Do not rely on Sketchfab search snippets as final proof; inspect the asset page/license manually before acquisition.
- Treat SpeedTree/Asset Store/Fab packs as production candidates if CC0 sources cannot meet the visual target.

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

Do a controlled download/import trial of the Mantissa `CG Japanese Maple Pack` first.

Reasons:

- It is the smallest high-value tree-family pack found in this pass.
- It directly tests the missing broadleaf/autumn dimension seen in the latest SOTF references.
- If it imports cleanly, it establishes the pipeline for Birch and Spruce/Fir.

Expected blocker:

- The pack is still about 418 MB before import. Downloading/importing should be treated as an explicit asset acquisition step, not a background cleanup task.
