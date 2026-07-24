# Forest Model Attribution Manifest

Date recorded: 2026-07-24

Purpose: record lawful source metadata for manually acquired local forest model packs before any raw or curated asset can be promoted into committed gameplay content.

Repository policy:

- Raw downloaded model folders remain local/ignored until the project explicitly decides how to store third-party assets.
- Any committed production asset derived from these packs must retain this attribution or a more specific attribution file beside the curated asset.
- These packs are evaluated as visual/reference and performance candidates; passing source attribution does not by itself pass the 60 FPS gameplay gate.

## Current local model packs

| Local folder | Source title | Author | License | Published | Source URL | Current repository state |
|---|---|---|---|---|---|---|
| `Assets/five-birch-trees-pack-lowpoly-lods/` | Five Birch trees pack (lowpoly, LODs) | LOLIPOP | CC Attribution | 2024-09-02 | https://sketchfab.com/3d-models/five-birch-trees-pack-lowpoly-lods-08fe5117138e4fdaa7ca440ef1201e07 | Local/ignored |
| `Assets/maple-trees-pack-lowpoly-game-ready-lods/` | Maple trees pack (lowpoly, game ready, LODs) | LOLIPOP | CC Attribution | 2025-02-01 | https://sketchfab.com/3d-models/maple-trees-pack-lowpoly-game-ready-lods-b5d2833c258f4054a01ee2b4ef85adf0 | Local/ignored |
| `Assets/pine-trees-pack-lowpoly-game-ready-lods/` | Pine trees pack (lowpoly, game ready, LODs) | LOLIPOP | CC Attribution | 2025-06-02 | https://sketchfab.com/3d-models/pine-trees-pack-lowpoly-game-ready-lods-e1e9c07b8e2e445c943fec660beefba2 | Local/ignored |
| `Assets/realistic-fir-trees-pack-lods-gameready/` | Realistic Fir Trees Pack (LODS, gameready) | LOLIPOP | CC Attribution | 2024-09-13 | https://sketchfab.com/3d-models/realistic-fir-trees-pack-lods-gameready-f58e8b6d733e4b0586e5b7db847b89e7 | Local/ignored |

## License interpretation

The Sketchfab API metadata available on 2026-07-24 labels each pack as `CC Attribution` and `downloadable`.

Implementation consequence:

- The project may evaluate these packs locally.
- If any derived production asset is committed, attribution must remain visible and complete.
- If the source page, license, or author changes, this manifest must be updated before promotion.
