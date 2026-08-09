# R2-FOREST1B — Production Forest Cell

## Result

This milestone introduces a project-owned, deterministic forest-cell boundary. The runtime consumes explicit baked placement records and never scatters trees again. One `ForestCellRuntime` owns the cell's static visual lifecycle; individual trees contain no `MonoBehaviour`, collider, or per-frame callback.

## Data and ownership

- `ForestCellDefinition` owns stable cell identity, schema/generator versions, provisional bounds/density/seed, explicit placements, and an order-independent SHA-256 placement checksum.
- `ForestSpeciesDefinition` separates stable species/variant identity from imported asset filenames and paths. Collider, stump, log, and resource references are reserved extension points only.
- Each `ForestTreePlacementRecord` has a generated stable ID independent of runtime list ordering.
- The editor-only generator is deterministic. The runtime assembly has no dependency on the generator or benchmark harness.
- `ForestCellRuntime` loads/unloads one baked static hierarchy idempotently and fails closed on missing species, variants, bindings, LODs, colliders, or per-tree behaviours.

## Visible validation composition

`PRF_ForestCell_Production_001` contains 38 trees across three tracked interim conifer variants. `SCN_Validation_ForestCell` provides a separate ground, camera, and light for visual inspection. Neither asset is referenced by `SCN_Foundation`.

The interim conifers are architecture-validation art, not final Pine/Fir/Maple promotion. Their replacement is isolated to `ForestSpeciesDefinition`; placement identity and runtime code remain stable.

## Evidence and provisional values

- Current rendering authority remains R2-PERF1: controlled full forest averaged 6.0781 ms, GPU 5.9430 ms, p95 6.6691 ms and p99 7.0506 ms at 1280x720 High Fidelity.
- Forest1B does not run a standalone benchmark and does not claim this new composition meets the product gate.
- Cell bounds `80 x 30 x 80 m`, seed `481516`, density `0.006/m2`, scale `0.88–1.15`, and the single interim species mix are **PROVISIONAL**.
- Runtime exposes baked renderer, LOD, collider, tree, and LOD0-triangle counters for the next controlled measurement.
- Target: green at no more than 10 ms render frame time, warning at 10–13 ms, hard gate 16.67 ms average / 20 ms p95 / 25 ms p99.

## Deferred by design

Tree damage/falling, stump/log spawning, persistent deltas, interactive promotion, understory/deadwood acquisition, multi-cell streaming, BRG/DOTS, multiplayer, and GC-OBS-001 remain outside this milestone.
