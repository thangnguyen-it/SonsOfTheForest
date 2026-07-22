# R2 - Forest LOD Optimization Pass

Scope: forest rendering performance for the current playable forest camp.

## Research conclusion

1. Current project evidence showed a valid visual complaint: the first forest pass improved tree/grass topology, but every placed conifer still rendered the same heavy mesh regardless of distance.
2. Asset inspection found eight mature conifers in `PRF_ForestCampPlayground`, all under `ForestFidelityVegetation/MatureConiferClusters`.
3. The corrected source conifers remain volumetric: two mesh renderers per tree, one woody mesh and one `Needles3D` mesh, with no plane/card object names.
4. The corrected source conifers target roughly 0.75M-1.25M triangles per tree, preserving near-field branch and needle mass while avoiding the earlier 3M-4M triangle per-tree load.
5. No LODGroup existed for the mature conifer placement before this optimization pass.
6. Ground cover was still casting realtime shadows, which is unnecessary for small grass and fern patches in the current camp.
7. HDRP shadow distance was treated as a provisional tuning value; 100 m is selected as an editable starting point for this small forest camp.
8. User feedback explicitly rejects returning to visible 2D cards for near trees and ground cover.
9. Therefore, LOD0 must keep the volumetric source tree instead of replacing it with alpha cards.
10. LOD1, LOD2, and LOD3 may use cheaper closed opaque proxy geometry because those levels are for distance silhouette, not close inspection.
11. Distance proxies must not use transparent surfaces, alpha clipping, double-sided foliage cards, or per-tree unique materials.
12. Far trees must not carry colliders or per-tree gameplay scripts during benchmarking.
13. Only the tree root receives a simple capsule collider in the reusable LOD prefab.
14. Small ground cover receives a cull-only LODGroup and no realtime shadow casting.
15. The implementation remains provisional until a proper player-hardware benchmark confirms the frame-time bottleneck.
16. The final Editor stress benchmark improved `baseline_orbit` from 121.30M to 39.54M triangles/frame and `walkthrough_camp` from 93.13M to 22.65M triangles/frame.
17. `trees_hidden_orbit` and `forced_lowest_lod` stayed near 4.6M-5.0M triangles/frame, so the remaining large cost is still tree LOD/shadow/draw-call behavior, not terrain alone.
18. The benchmark still reports the HDRP stack shadow distance as 500 m even after the project volume assets are configured to 100 m; this is tracked as a validation gap for the dedicated atmosphere/rendering pass.

## Implemented decision

- `ForestLodContentBuilder` builds three reusable LOD conifer prefabs.
- Each LOD prefab uses the existing volumetric conifer as LOD0.
- LOD1 and LOD2 are closed, opaque solid branch/canopy proxy meshes.
- LOD3 is a very low-cost horizon proxy with realtime shadows disabled.
- The first measured pass showed that 0.40 kept too many trees at LOD0 during a 30 m orbit. Later passes at 0.62 and 0.90 still left excessive triangle counts in stress views, so the thresholds were retuned to 0.99 / 0.35 / 0.10 / 0.005.
- Materials are shared and instancing-enabled.
- The forest camp world prefab swaps the eight mature conifers to the LOD prefabs while preserving placement.
- The world prefab now carries a small global performance Volume with HDShadowSettings maxShadowDistance = 100 m.
- Grass and fern prefabs cast no realtime shadows and cull at small screen size.
- A manual benchmark runner records frame time and render stats from Play Mode; it no longer auto-starts from a file state.

## Remaining gaps

- Current proxies are engineered silhouettes, not baked multi-angle impostors.
- GPU wind is deferred until a dedicated shader/Shader Graph pass.
- GPU Resident Drawer, BatchRendererGroup, and chunk streaming still require profiling.
- Interactive tree promotion and collider pooling remain later gameplay systems.
- Terrain/floor blending, rocks, fog, atmosphere, and campfire VFX remain separate visual milestones.
