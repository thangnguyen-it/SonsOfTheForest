# R1 Forest Visual Fidelity

Date: 2026-07-20
Scope: near-field conifer, grass, and fern geometry in the playable forest camp.

## Research conclusion

1. `VERIFIED`: the released Sons of the Forest is an open-world survival-horror simulator with exploration, gathering, building, and changing seasons in one world.
2. Official evidence establishes the importance of a convincing traversable natural environment, but does not specify model topology or polygon budgets.
3. `OBSERVED_USER_REFERENCE`: the supplied current-game screenshots show mature conifers as continuous woody/foliage masses, not exposed horizontal cards.
4. `OBSERVED_USER_REFERENCE`: tree silhouettes vary in height, lean, crown width, lower-branch retention, and spacing.
5. `OBSERVED_USER_REFERENCE`: the forest uses clustered canopy, irregular openings, and layered foreground, midground, and background vegetation.
6. `OBSERVED_USER_REFERENCE`: ferns, shrubs, grasses, litter, rocks, and moss form patches influenced by light and cover rather than an even grid.
7. `NATURAL_REFERENCE`: supplied pine silhouette and bark photographs support trunk-to-crown proportion, asymmetry, bark breakup, and root flare.
8. `NATURAL_REFERENCE`: supplied boulder photographs support ground embedding, rounded weathered edges, fracture hierarchy, and moss accumulation.
9. `NATURAL_REFERENCE`: the supplied stable and dying campfire references support later flame, ember, smoke, and light-state work.
10. These natural references constrain plausibility; they are not evidence of exact Sons of the Forest assets or defaults.
11. The previous Fir Sapling source exposed large open foliage surfaces and fails the near-field no-card requirement.
12. The evaluated Poly Haven Fir Tree 01 twig mesh also fails that requirement despite its high polygon count.
13. Its foliage submesh measured 548,736 boundary edges, a boundary fraction of 0.758775, with 446,074 triangles and 589,044 vertices.
14. Polygon count alone therefore cannot establish volumetric foliage.
15. `IMPLEMENTED`: the replacement conifers use closed tapered tubes for trunks and branch generations.
16. `IMPLEMENTED`: every needle is a closed triangular prism with measurable volume.
17. `IMPLEMENTED`: every grass blade is a closed, curved, tapered mesh rather than an alpha plane.
18. `IMPLEMENTED`: every fern pinna has thickness and closed side geometry rather than a foliage card.
19. `IMPLEMENTED`: three deterministic conifer variants change height, lean, crown radius, branch count, and crown start.
20. `IMPLEMENTED`: grass and fern variants change count, height, curvature, direction, and dry/green material allocation.
21. `IMPLEMENTED`: eight mature conifers use irregular positions, rotations, scales, and slight lean around the clearing.
22. `IMPLEMENTED`: understorey is placed in deterministic patches and shade-associated fern groups, not an independent uniform grid.
23. `IMPLEMENTED`: all foliage materials are opaque HDRP/Lit; alpha clipping, transparency, and double-sided card rendering are disabled.
24. `IMPLEMENTED`: the forest-camp rebuild command reapplies this fidelity pass, preventing the old card trees from returning.
25. `PROVISIONAL`: species, crown profile, needle dimensions, material colors, and exact biome density are independent engineering baselines.
26. `PROVISIONAL`: the current clearing keeps eight multi-million-triangle trees to prioritize near-field fidelity without claiming a final performance budget.
27. `UNKNOWN`: exact current-game species mix, per-species dimensions, seasonal variation, wind response, and density distribution remain unmeasured.
28. `UNKNOWN`: exact HDRP-equivalent exposure, fog, sky scattering, wetness, and subsurface response remain unmeasured.
29. Close views must test for visible planes, black/white card flipping, scale consistency, silhouette repetition, and ground intersection.
30. Asset tests must also reject alpha cutout, double-sided foliage, mesh compression, CPU-readable model copies, and accidental old-tree restoration.
31. Later terrain work owns elevation, forest-floor blending, litter, roots, wet areas, decals, and soft transitions around trees and rocks.
32. Later rock work owns fractured macro-shapes, embedded placement, moss masks, and construction-stone variants.
33. Later atmosphere work owns sky, exposure, ambient response, volumetric fog, and distance-layer continuity.
34. Later VFX work owns campfire flame volumes, embers, smoke, light flicker, dying-fire state, and audio.
35. Later profiling may add geometry LODs or streaming only when measured changes preserve the approved near-field appearance.
36. No Sons of the Forest source code, model, texture, or extracted game asset is copied into this implementation.

## Sources

- `VERIFIED`: [Official Sons of the Forest Steam page](https://store.steampowered.com/app/1326470/The_Forest/?l=english), released 2024-02-22, accessed 2026-07-20.
- `OBSERVED_USER_REFERENCE`: screenshots and annotations supplied in this project conversation, July 2026; build identity is not independently verified.
- `NATURAL_REFERENCE`: `NAT_TREE_PINE_SILHOUETTE_001`, [Pexels](https://www.pexels.com/photo/solitary-pine-tree-in-overcast-countryside-35796937/), accessed 2026-07-20.
- `NATURAL_REFERENCE`: `NAT_TREE_BARK_CLOSE_001`, Wikimedia Commons Pine Tree Bark, user-supplied CC0 reference record.
- `NATURAL_REFERENCE`: `NAT_ROCK_BOULDER_WHOLE_001`, `NAT_ROCK_SURFACE_CLOSE_001`, and `NAT_ROCK_MOSS_001`, user-supplied Pixabay/Pexels reference records.
- `NATURAL_REFERENCE`: `NAT_FIRE_STABLE_001`, [Pixabay Campfire At Night](https://pixabay.com/videos/campfire-flames-night-fire-camp-257593/), published 2025-02-12, 3840x2160 at 30 FPS.
- `NATURAL_REFERENCE`: `NAT_FIRE_DYING_001`, user-supplied Pexels reference record; duration and resolution remain unknown until file inspection.
- `LICENSE`: [Poly Haven CC0 license](https://polyhaven.com/license), accessed 2026-07-20; applies to the reused fir-bark texture, not the independent geometry.
