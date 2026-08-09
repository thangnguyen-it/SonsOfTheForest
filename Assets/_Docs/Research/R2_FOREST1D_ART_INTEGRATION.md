# R2-FOREST1D-ART — Curated Production Tree Art Integration

Date: 2026-08-09
Branch: `codex/r2-forest1d-art-integration`

## Source and license decision

- `VERIFIED`: Pine, Fir and Maple source packs are downloadable works by LOLIPOP distributed as CC Attribution. Source URLs and publication dates are recorded in `Assets/_Docs/ThirdParty/FOREST_MODEL_ATTRIBUTION.md`.
- `VERIFIED`: the curated tracked closure contains only the mesh, texture, material and prefab derivatives selected for this project. Production assets have no dependency on the ignored raw packs or `_LocalTrials`.
- `VERIFIED`: attribution is retained beside the curated assets in `Assets/_Game/Art/World/Forest/Trees/Curated/ATTRIBUTION.md`.
- `DEFERRED`: Birch remains at 0% because its distant LOD did not pass the earlier intake gate.

## Curated production set

| Species | Selected variants | Count in cell | Share |
|---|---|---:|---:|
| Pine | Large 1–3, Big 1–3 | 25 | 65.8% |
| Fir | Tall, Compact | 5 | 13.2% |
| Maple | Large 1–3, Medium 1 | 8 | 21.1% |

- Tracked curated closure: 12 prefabs, 48 mesh LOD assets, 24 PNG textures and 18 HDRP materials; 92,423,511 bytes excluding `.meta` files.
- Every wrapper has four LOD levels, an origin at the trunk base, project-normalized scale/orientation, no static collider and no per-tree behaviour.
- Final LOD does not cast realtime shadows. All materials use `HDRP/Lit` with instancing; foliage and billboards use alpha clipping and double-sided rendering.
- `PROVISIONAL`: normalized mature heights, material smoothness, alpha cutoff and LOD transition thresholds are independent engineering baselines pending production-camera visual tuning.

## Cell migration and identity

- ForestCellId remains `cell.production.forest.001`; all 38 TreeInstanceIds are preserved.
- Species assignment is derived from a stable hash of TreeInstanceId, not placement-list order. Variant selection is also identity-derived.
- The pre-migration identity-only digest and the post-migration identity-only digest both equal `5d6d68f59f7b6a1ffdc8929f1576629b8fe5419bd5127c53eab5ab10f3486e71`.
- 37 transforms remain unchanged. `tree.ba23c0bf6f61c9a1747f` moved from `(-2.488, 0, 7.538)` to `(-12, 0, 10)` because the former point was only 3.55 m from the campfire and failed the playable-clearing requirement.
- Minimum inter-tree spacing is 7.79 m; minimum player clearance is 6.99 m; minimum campfire clearance is 7.59 m.
- Schema version is 2 and the controlled placement checksum is `db48d6771cbb2b7d5746fe2e74ac59686bf47d149f10ce94d79d4a4c1ec0e22d`.

## Runtime and scene integration

- `SCN_Foundation` owns exactly one `PRF_ForestCell_Production_001` instance and binds its interaction observer to `LocalPlayer`.
- The old `MatureConiferClusters` scene instance is inactive; ground, campfire, pickups, rocks, stumps, fern/grass, lighting and volumes remain present.
- The retained `ForestGround` scene instance is expanded to 90 x 90 m so the complete 80 x 80 m production cell remains grounded without changing the cell bounds or placement identities.
- Static and promoted trees resolve the same SpeciesId, VariantId, transform and mesh set. Only promoted trees own a Rigidbody and trunk capsule.
- Damaged, Falling and Felled state remains keyed by stable cell/tree identity in the session delta store; art migration does not redefine identity.

## Controlled validation evidence

- `VERIFIED`: Unity visual inspection from the Foundation player view and overview camera shows Pine, Fir and Maple together with intact HDRP foliage, grounded trunks and retained camp composition. Local evidence is stored outside tracked production content under `Benchmarks/R2_FOREST1D_ART_Visual/`.
- `VERIFIED`: the selected Pine instance `tree.a2fa6a7695a07c479600` promoted with the same `species.pine` / `variant.pine.big.3`, transform and mesh set, then entered `Falling` with a non-kinematic Rigidbody after lethal damage.
- Focused Forest Cell EditMode and PlayMode suites validate art closure, identity preservation, species shares, spacing, scene ownership, promotion equivalence and falling-state continuity.
- Final validation: focused Forest Cell EditMode `26/26`, complete EditMode `370/370`, focused Forest Cell PlayMode `4/4`, and complete PlayMode `15 passed / 0 failed / 2 upstream ignored input-system tests`.
- Promotion no longer writes velocity to an already-kinematic Rigidbody; the focused PlayMode regression explicitly rejects unexpected promotion logs.

## Performance sanity and remaining gaps

- Static cell counters: 38 trees, 152 renderers, 38 LODGroups, 0 colliders, 18 shared material assets.
- Geometry/draw upper counts by forced LOD: LOD0 `385,248 / 38`, LOD1 `195,397 / 38`, LOD2 `101,185 / 38`, LOD3 `788 / 38` triangles/renderers.
- No static tree has an `Update`; proximity evaluation remains cell-owned. No new standalone benchmark framework was added.
- `UNKNOWN`: final standalone frame-time impact in the complete gameplay scene. Existing R2-PERF1 rendering evidence predates this curated art and is not reused as proof for this cell.
- `DEFERRED`: species-matched stumps, logs, harvest resources, save backend and multi-cell streaming.
