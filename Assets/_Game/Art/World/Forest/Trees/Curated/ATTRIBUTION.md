# Curated production tree attribution

The project-owned meshes, textures, materials and prefabs below are curated derivatives of downloadable Sketchfab packs by **LOLIPOP**, used under **CC Attribution**. Raw source packs are intentionally excluded from Git; runtime assets have no dependency on their local folder structure.

| Curated species | Source work | Published | Source URL |
|---|---|---|---|
| Pine | Pine trees pack (lowpoly, game ready, LODs) | 2025-06-02 | https://sketchfab.com/3d-models/pine-trees-pack-lowpoly-game-ready-lods-e1e9c07b8e2e445c943fec660beefba2 |
| Fir | Realistic Fir Trees Pack (LODS, gameready) | 2024-09-13 | https://sketchfab.com/3d-models/realistic-fir-trees-pack-lods-gameready-f58e8b6d733e4b0586e5b7db847b89e7 |
| Maple | Maple trees pack (lowpoly, game ready, LODs) | 2025-02-01 | https://sketchfab.com/3d-models/maple-trees-pack-lowpoly-game-ready-lods-b5d2833c258f4054a01ee2b4ef85adf0 |

License evidence and acquisition context are preserved in `Assets/_Docs/ThirdParty/FOREST_MODEL_ATTRIBUTION.md`.

## Curation performed

- Selected only 6 Pine, 2 Fir and 4 Maple variants needed by the production cell.
- Extracted mesh LODs into independent Unity mesh assets.
- Downsampled and repacked only required texture channels into project-owned textures.
- Rebuilt HDRP Lit bark and alpha-clipped, double-sided foliage materials with GPU instancing.
- Wrapped each tree in a collider-free four-level LOD prefab; the last LOD casts no realtime shadow.

No Sons of the Forest model, texture or other proprietary asset is included.
