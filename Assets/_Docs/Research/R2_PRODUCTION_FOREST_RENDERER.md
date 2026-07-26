# R2 - Production Forest Renderer and Benchmark Policy

Date: 2026-07-24

Scope: optimize the forest rendering path after the first lawfully acquired model-source trial proved that visually better tree models still fail the 60 FPS gate when they are scattered as many normal GameObjects.

## Decision

The project will not render the whole forest as normal tree prefabs.

Production forest rendering is split into tiers:

- Near playable bubble: limited GameObjects with colliders, interaction state, chopping support, and realtime shadows.
- Mid visual forest: GameObjects may be used for inspection and short-range silhouette, but colliders are removed and realtime shadows are disabled unless a later benchmark proves they are affordable.
- Far forest mass: rendered through `ProductionForestRenderer` using `Graphics.RenderMeshInstanced`, accepted distant/proxy LOD meshes, no colliders, no scripts per tree, no realtime shadows.

This mirrors the production principle behind large survival forests: the player can see many trees, but only nearby trees are expensive objects.

## Current policy constants

These are provisional engineering baselines, not claimed Sons of the Forest defaults:

- Minimum playable target: 60 FPS.
- Near playable tree benchmark count: 10.
- Mid static tree benchmark count: 50.
- Far instanced tree benchmark count: 300.
- Benchmark realtime shadow distance: 65 m.
- Far shadow casting: off.
- Far receive shadows: false.

## Material policy

Production forest materials must be:

- opaque, not Transparent;
- GPU-instancing ready;
- validated through the model intake gate before promotion;
- treated as local/ignored trial material until source license and runtime benchmark both pass.

The renderer enforces `enableInstancing` before drawing instance batches to prevent runtime `RenderMeshInstanced` failures from imported materials.

## Benchmark policy

`SotfForestPhase1TrialBuilder` now supports:

- local wrapper generation from the user-acquired model packs;
- a visual benchmark scene using 10 near, 50 mid, and 300 far trees;
- far trees rendered by `ProductionForestRenderer`, not by 300 far GameObjects;
- near-only realtime tree shadows in the benchmark scene;
- a local standalone benchmark build menu that builds a Windows player from the benchmark scene without editing Build Settings.

The benchmark player scene includes `ForestBenchmarkRunner`; when the player runs it writes its local FPS report beside the player data folder and exits automatically.
The runner now enforces a minimum warmup frame count and records ignored startup-stall frames so first-run HDRP/driver stalls do not get confused with steady-state forest cost.

## Current result

Editor Play Mode benchmark after the far cluster was changed to GPU instancing
and realtime tree shadows were limited to the near playable bubble:

| Scenario | Avg FPS | Avg ms | Tris (M) | Batches | Gate |
|---|---:|---:|---:|---:|---|
| `baseline_orbit` | 5.6 | 179.5 | 0.66 | 359 | FAIL |
| `shadows_off_orbit` | 14.3 | 70.1 | 0.51 | 267 | FAIL |
| `shadow_distance_60` | 12.9 | 77.3 | 0.56 | 302 | FAIL |
| `half_trees_orbit` | 12.6 | 79.1 | 0.30 | 171 | FAIL |
| `trees_hidden_orbit` | 13.0 | 76.9 | 0.23 | 97 | FAIL |
| `forced_lowest_lod` | 16.5 | 60.7 | 0.01 | 127 | FAIL |
| `walkthrough_camp` | 13.0 | 77.1 | 0.37 | 204 | FAIL |

Standalone Windows player benchmark, built from
`Assets/_LocalTrials/SOTF_ForestPhase1/Scenes/SCN_TRIAL_SOTF_ForestPhase1Benchmark.unity`,
also fails the 60 FPS gate on the current machine:

| Scenario | Avg FPS | Avg ms | p95 ms | Gate |
|---|---:|---:|---:|---|
| `baseline_orbit` | 9.3 | 107.7 | 181.5 | FAIL |
| `shadows_off_orbit` | 15.0 | 66.8 | 93.0 | FAIL |
| `shadow_distance_60` | 15.0 | 66.7 | 72.4 | FAIL |
| `half_trees_orbit` | 16.4 | 60.9 | 65.9 | FAIL |
| `trees_hidden_orbit` | 17.0 | 58.8 | 63.4 | FAIL |
| `forced_lowest_lod` | 21.8 | 45.9 | 47.6 | FAIL |
| `walkthrough_camp` | 15.5 | 64.7 | 69.5 | FAIL |

An earlier first standalone `baseline_orbit` sample hit a first-run 14.8 s startup/render stall and was not considered a steady-state forest number. The benchmark harness was updated to require warmup frames and report ignored startup stalls; the rerun above recorded `0` ignored startup stalls.

Interpretation:

- The far 300-tree cluster is no longer the main geometry problem; the instanced far proxy cluster is only about 4,960 triangles per frame in the generated scene metrics.
- The scene still fails the 60 FPS gate in both Editor Play Mode and the standalone player, so these local model-source trees are not approved for gameplay-scene promotion yet.
- Near-only realtime shadows improved baseline Editor FPS, but the benchmark remains far below the product target.
- Even `forced_lowest_lod` reaches only about 21.8 FPS in the standalone player, which means the next pass must target base HDRP scene cost, imported shader/material cost, foreground/mid tree rendering, and understorey before adding more art density.

## Fidelity gaps

- The selected local packs are lawful CC Attribution candidates, but final art promotion still requires attribution beside any committed curated asset.
- Birch remains rejected for runtime/far use until it receives a lighter final LOD or impostor.
- Snow-load tree variants, fallen logs, stumps, and chopping segmentation remain later systems.
