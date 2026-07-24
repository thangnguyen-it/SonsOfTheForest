# R2 - SOTF Forest Phase 1 Visual Specification

Date: 2026-07-24

Scope: translate the user-supplied Sons of the Forest forest/ecology research pack into a practical Phase 1 forest model and benchmark specification.

## Research conclusion

Sons of the Forest presents a temperate island forest rather than a single-species pine field. Current public/user-provided evidence supports spruce as the best named conifer anchor, while `pine-like` and `fir-like` are safer morphology labels when exact in-game asset names are unknown.

Observable target:

- Tall conifer forest is the main visual mass.
- Mature conifers often have long bare boles before live crown begins.
- Dense inland forest reads as layered vertical trunks plus dark green canopy mass.
- Snow/coast areas are more open, with longer sightlines, lower understory, and stronger atmosphere/fog.
- Broadleaf trees are less dominant but important for visual variety: birch for pale bark contrast, maple for local broadleaf/seasonal accents.
- Ground layer needs fern, grass, moss, pine needles, leaf litter, small rocks, roots, fallen logs, and stumps over time.

## Phase 1 model-role mapping

| Role | Selected local pack | Use |
|---|---|---|
| Primary conifer skeleton | `pine-trees-pack-lowpoly-game-ready-lods` | Main tall tree family; good SOTF-like bare bole and authored LOD/billboard budget. |
| Dense conifer/fir fill | `realistic-fir-trees-pack-lods-gameready` | Darker denser fir/spruce-like fill; use carefully so the forest does not look like repeated Christmas trees. |
| Birch contrast | `five-birch-trees-pack-lowpoly-lods` | Pale-trunk breaks, wet/lowland/edge clusters, near-field chopping bark readability. Current pack requires a lighter final LOD/impostor before runtime promotion. |
| Broadleaf accent | `maple-trees-pack-lowpoly-game-ready-lods` | Sparse maple/sapling/seasonal accent; not a main canopy layer. |

Mayo Games `pineForset_MayoGames_free` remains a local prototype benchmark source only. It is too stylized for the desired final forest look.

## Target starting parameters

These are provisional implementation baselines derived from the research pack, not claimed exact Endnight values:

- Mature spruce-like/conifer height: 24-35 m target visual band.
- Pine-like bare bole: 45-65% of total height.
- Spruce-like bare bole: 30-45% of total height.
- Fir-like younger/dense fill bare bole: 10-25% of total height.
- Dense forest spacing: 2-4 m between many trunks in clustered areas, but only after culling/instancing supports it.
- Clearing/trail spacing: 5-10 m.
- Dense forest screen-space canopy cover target: roughly 58-72% for practical implementation; some user-supplied estimates push denser areas higher, but Phase 1 should avoid overcommitting before FPS validation.
- Trunk diameter target for mature trees: 0.45-0.90 m.
- Fallen log/stump target for later systems: 5-10 m logs, 0.5-1.0 m diameter, 2-5 build logs per harvestable large tree.

## Phase 1 acceptance rules

Phase 1 is allowed to create only local benchmark wrappers and scenes until runtime validation passes:

- Raw model folders stay ignored.
- Wrapper prefabs must have LODGroups.
- Final LOD must not cast realtime shadows.
- Materials must be opaque HDRP/Lit and GPU-instancing enabled where possible.
- Near tree wrappers may receive a simple trunk capsule collider.
- Mid/far benchmark instances must have no colliders.
- A benchmark scene must include at least 10 near, 50 mid, and 300 far trees.
- The 300-tree far benchmark cluster may use only candidates that pass the hard intake gate. Rejected candidates remain local reference/near-inspection material until they receive lighter distant LODs or impostors.
- The 300-tree far benchmark cluster should be rendered as a GPU-instanced distant/proxy cloud, not 300 independent far GameObjects.
- Runtime or standalone benchmark must be recorded before any committed gameplay scene references these trees.

## Fidelity gaps to track

- Exact in-game tree species names remain partially unknown.
- Exact tree heights and trunk diameters are inferred from screenshots/community references rather than public mesh bounds.
- Snow-load branch variants are not solved by the current packs yet.
- Ground layer assets are still incomplete; tree model work must not be mistaken for a finished forest biome.
- Chopping, stump, felled tree, and log segmentation require a separate resource-system milestone.
