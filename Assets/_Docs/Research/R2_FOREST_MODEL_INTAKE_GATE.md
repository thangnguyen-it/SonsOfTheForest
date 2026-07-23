# R2 — Forest Model Intake Gate

Date: 2026-07-23

Purpose: prevent visually attractive but unplayable tree assets from entering the gameplay forest before they pass the 60 FPS model gate.

## Decision

The project should use lawfully acquired, game-ready tree models where possible, especially packs with authored LODs and billboards. Raw high-resolution scans remain useful as reference, material sources, or decimation inputs, but they are not gameplay prefabs.

The Mantissa Japanese Maple trial proved why this distinction matters: the smallest raw FBX measured in Unity at about 2.42 million triangles. That is valuable source material, but it is far beyond the hard near-tree limit for a normal playable forest.

The Poly Haven `fir_tree_01` adult conifer trial confirmed the same rule for a legally downloadable CC0 conifer source. Its raw 1K FBX import measured about 6.98 million triangles across three mesh filters, with no LODGroup. It is lawful and useful as reference/source material, but it is rejected for direct gameplay placement until a curated decimated LOD/impostor version exists.

## Acceptance gate

A tree model candidate can move from external trial to playable forest only when it satisfies all hard checks:

- LODGroup exists.
- At least three LOD levels exist.
- LOD0 is at or below 200k triangles; the preferred near-tree budget remains 30k–120k.
- LOD1 is at or below 20k triangles.
- LOD2 is at or below 3k triangles.
- Final distant LOD is at or below 500 triangles.
- Final distant LOD does not cast realtime shadows.
- Materials are not Transparent.
- The asset is then benchmarked in a cluster: at least 10 near, 50 mid, and 300 far/impostor trees.

Warnings, not automatic hard failures:

- more than four materials;
- GPU instancing disabled;
- alpha clipping in foliage;
- double-sided foliage;
- renderer names suggesting visible cards or planes.

Those warnings require close visual inspection because real game-ready foliage may use alpha-cut cards internally, but the player must not see obvious flat sheets at near range.

## Unity tool

Use:

`Sons Of The Forest > Forest Models > Validate Selected Model Candidates`

or:

`Sons Of The Forest > Forest Models > Write Selected Model Candidate Report`

The report is written outside Assets:

- `Benchmarks/forest_model_intake_report.json`
- `Benchmarks/forest_model_intake_report.md`

This keeps raw/manual acquisition files out of Git while still recording the technical decision.

## Implementation impact

- Marketplace/Fab/Sketchfab models are not imported automatically unless license and acquisition are clear.
- The current playable world must not reference `ExternalTrials`, Mantissa, Fab, or Sketchfab raw trial assets.
- The next real visual upgrade should start with a manually acquired game-ready Maple or Fir pack with documented LODs, not another raw scan import.

## Current conifer validation finding

The existing generated conifer LOD prefabs initially failed this gate on 2026-07-23:

| Prefab | Total triangles | LOD0 triangles | Gate result |
|---|---:|---:|---|
| `PRF_ConiferLod_A` | 919,546 | 914,604 | REJECT for final 60 FPS forest |
| `PRF_ConiferLod_B` | 1,092,446 | 1,087,504 | REJECT for final 60 FPS forest |
| `PRF_ConiferLod_C` | 836,154 | 831,212 | REJECT for final 60 FPS forest |

The interim playable rebuild replaced that million-triangle LOD0 with an opaque 3D branch/canopy mesh:

| Prefab | Total triangles | LOD0 triangles | LOD1 triangles | LOD2 triangles | Final LOD triangles | Gate result |
|---|---:|---:|---:|---:|---:|---|
| `PRF_ConiferLod_A` | 34,666 | 29,724 | 4,756 | 168 | 18 | PASS |
| `PRF_ConiferLod_B` | 34,666 | 29,724 | 4,756 | 168 | 18 | PASS |
| `PRF_ConiferLod_C` | 34,666 | 29,724 | 4,756 | 168 | 18 | PASS |

Interpretation:

- The current gameplay conifers now satisfy the model intake performance gate.
- They remain an interim playable solution, not the final art target.
- The next tree art milestone should still replace them with lawfully acquired or properly authored game-ready tree models before expanding forest density.

## Scene duplicate and benchmark finding

The foundation scene contained three identical active `PRF_ForestCampPlayground` instances at the same transform. This multiplied forest geometry, rocks, stumps, pickups, colliders, and interaction objects without adding design value.

The scene was corrected to keep exactly one playground instance and rewire `FoundationSceneCompositionRoot` to that instance and its `ForestGround` playable surface.

Editor Play Mode stress benchmark after the correction:

| Scenario | Avg FPS | Avg ms | Tris (M) | Batches |
|---|---:|---:|---:|---:|
| `baseline_orbit` | 5.2 | 193.26 | 2.41 | 557 |
| `shadows_off_orbit` | 10.6 | 94.19 | 1.35 | 426 |
| `forced_lowest_lod` | 12.0 | 83.14 | 1.67 | 589 |
| `full_rings_450_trees` | 8.7 | 115.56 | 2.72 | 720 |
| `walkthrough_camp` | 11.9 | 83.77 | 1.12 | 243 |

Interpretation:

- The duplicate-scene bug and million-triangle conifer LOD0 were both real problems and are now corrected.
- This Editor/MCP benchmark still does not meet the 60 FPS product target, so the forest should not be declared performance-complete.
- The largest remaining render groups in the corrected scene are understorey, conifers, rocks/stumps, and shadows. The next optimization milestone should reduce understorey density/LOD cost, replace high-poly stumps/rocks where needed, and benchmark in a focused standalone build or a foreground Editor window before claiming the 60 FPS target.

## External model acquisition finding

Automated source checks on 2026-07-23 found two different acquisition paths:

- CC0/raw-source path: Poly Haven adult conifers can be downloaded without login, but the adult `fir_tree_01` raw import measured 6,982,937 triangles and no authored LODGroup. Use this as reference or decimation/bake input, not as a gameplay prefab.
- Game-ready/manual path: Sketchfab game-ready pine/fir packs with LODs are visible through public metadata and use CC Attribution, but the download endpoint requires authentication. These remain the strongest next conifer candidates if acquired manually and documented with attribution.

Policy consequence:

- Do not replace the current playable conifers with raw adult CC0 scans.
- Do not claim the project has final tree art until a lawfully acquired game-ready pack, or a curated decimated version of a raw source, passes the intake gate and 60 FPS cluster benchmark.
- If a CC Attribution model is imported, add a source/attribution manifest next to the imported asset before it is allowed into any scene or prefab.
