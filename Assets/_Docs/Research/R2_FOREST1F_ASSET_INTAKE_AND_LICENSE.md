# R2-FOREST1F Asset Intake and License Decision

Audit date: 2026-08-13. Intake root: `E:\SOTF_AssetIntake\TREE_QUALITY_INTAKE_01`.
Binary source media remains outside the public repository. This document records provenance,
technical findings and the production disposition; it does not redistribute the source files.

## Current commercial provenance lock — R2-ART1-M1A-FIX2

Verified on 2026-08-20 against the separate, locked intake
`E:\SOTF_AssetIntake\R2_ART1_HERO_FOREST_01` and evidence root
`E:\SOTF_AssetIntake\R2_ART1_LICENSE_BINDING_01`. The intake manifest has 75 records and
SHA-256 `AB9A5A219216428A552B442C40F7B6AB9BFB32F1337B417F932B8EB19A9BE6CD`.

The current source acquisition evidence binds each listed archive's original FBX byte-for-byte
to its current source file. Listing captures establish title, author, canonical listing ID and
CC BY 4.0; the associated CC BY 4.0 captures establish the exact license version. CC BY 4.0
permits commercial adaptation subject to attribution, a license link and an indication of changes.
This lock is provenance only: it neither imports media nor changes runtime content.

| Candidate | Current verified source and author | Canonical URL | Archive/model SHA-256 | Commercial license | Current disposition |
|---|---|---|---|---|---|
| First Person arms | DJMaesen | https://sketchfab.com/3d-models/first-person-arms-e3c42c05b22944e5839deb8e003f0987 | `03CABD1797A90993F630544D1C7794E842EA074C221CCC8EE89511FB6DD61A76` | CC BY 4.0 | `ACCEPT_WITH_ATTRIBUTION` |
| Fire axe | Solidbot | https://www.fab.com/listings/e60d73c8-2362-4fbb-b8ae-06463de5106a | `FB880E4940BC51AF2F21C8C71D7ED8CEB1D07D52CF2A74838584EDCF6CCE0A16` | CC BY 4.0 | `ACCEPT_WITH_ATTRIBUTION` |
| Pine trees pack (lowpoly, game ready, LODs) | LOLIPOP | https://sketchfab.com/3d-models/pine-trees-pack-lowpoly-game-ready-lods-e1e9c07b8e2e445c943fec660beefba2 | `AB26F767D863FB88681EB78B5DA1CA8B6F55004F2BAF3BC778E0F88A6EB09C59` | CC BY 4.0 | `ACCEPT_WITH_ATTRIBUTION` |

### Required attribution text

- “First Person arms” by DJMaesen — https://sketchfab.com/3d-models/first-person-arms-e3c42c05b22944e5839deb8e003f0987 — CC BY 4.0. Modified/optimized for this project; no endorsement implied.
- “Fire axe” by Solidbot — https://www.fab.com/listings/e60d73c8-2362-4fbb-b8ae-06463de5106a — CC BY 4.0. Modified/optimized for this project; no endorsement implied.
- “Pine trees pack (lowpoly, game ready, LODs)” by LOLIPOP — https://sketchfab.com/3d-models/pine-trees-pack-lowpoly-game-ready-lods-e1e9c07b8e2e445c943fec660beefba2 — CC BY 4.0. Modified/optimized for this project; no endorsement implied.

Poly Haven and ambientCG records remain CC0 under their existing provenance records.

### Evidence binding

- Arms: archive `first-person-arms .zip`, SHA-256 `B27681A14DF5E3B4628029C5A4B128E669CC2159022619008A6FFC1FA7680F8E`; member `source/fpsarms.fbx` matches the locked-intake source hash above.
- Axe: archive `fire-axe .zip`, SHA-256 `5BD0DEEDF5AFF9FD97FA659643FE0ACB08E6839998DD9EE7EDE3699AA2E6D544`; member `source/hacha-bomberos-terminada.fbx` matches the locked-intake source hash above.
- Pine: archive `pine-trees-pack-lowpoly-game-ready-lods.zip`, SHA-256 `FE8DA7ED245F9B2557DD8F93DC31FECB98D3B66F4B2844F3B5E1A0AE61EF9909`; member `source/Pine_pack.fbx` matches the existing project source hash above.

The 2026-08-20 captures in the evidence root show Fire axe / Solidbot and Pine trees pack / LOLIPOP together with their canonical listing IDs. Existing captures in the same logical folders show the corresponding CC BY 4.0 listing/license chain. Evidence media and source archives remain outside Git.

## Historical R2-FOREST1F audit — retained, superseded where stated

The 2026-08-13 observations below are retained unchanged as history. Its commercial dispositions
for the DJMaesen arms and Solidbot axe are `HISTORICAL / SUPERSEDED_BY_VERIFIED_SOURCE`: they
described a prior intake without the current archive-and-listing binding. The historical LOLIPOP
record is retained and confirmed by the current exact source binding. No historical source is
deleted or rewritten.

## Decision table

| Candidate | Evidence | Technical audit | License conclusion | Disposition |
|---|---|---|---|---|
| Solidbot firefighter axe | Local FBX `hacha-bomberos-terminada.fbx`, SHA-256 `FB880E4940BC51AF2F21C8C71D7ED8CEB1D07D52CF2A74838584EDCF6CCE0A16`; local acquisition folder names Solidbot; no receipt, listing URL or license file was supplied | 1 mesh, 794 vertices, 1,568 triangles, one material, complete base/metal/normal/roughness set; approximately 0.885 m along its longest imported axis; no rig | `UNKNOWN`. A Solidbot seller page exists on Fab, but the exact listing and granted license cannot be connected to this binary from supplied evidence | `CONDITIONAL / LOCAL-ONLY`. May be used for local fit and animation QA. Do not commit the raw file, textures, or a recoverable derivative. A distributable proof of license is required before packaging |
| DJMaesen FP Arms | Sketchfab model `FP Arms`, author DJMaesen (`@bumstrum`), https://sketchfab.com/3d-models/fp-arms-8416c380544949bb9b224278819cbe6b; FBX SHA-256 `03CABD1797A90993F630544D1C7794E842EA074C221CCC8EE89511FB6DD61A76` | 1 skinned mesh, 3,624 vertices / 7,240 triangulated faces after Blender import, 49-bone chest/arm/hand/finger rig, one material and 4K PBR maps | Creative Commons Attribution-NonCommercial. This is incompatible with a commercial production dependency. A different 2024 model by the author is advertised under CC BY, but it is not the supplied binary and is not silently substituted | `REJECT` for distributable production; `CONDITIONAL / LOCAL-ONLY` for non-commercial visual and animation prototyping. No raw or derived rig/mesh is committed |
| LOLIPOP Pine pack | Author LOLIPOP (`@lolipop_1707`), https://sketchfab.com/3d-models/pine-trees-pack-lowpoly-game-ready-lods-e1e9c07b8e2e445c943fec660beefba2; FBX SHA-256 `AB26F767D863FB88681EB78B5DA1CA8B6F55004F2BAF3BC778E0F88A6EB09C59` | 15 variants with LOD0/1/2/billboard. Selected `Pine_large_1` is 11,844 / 6,596 / 3,176 / 16 triangles. Bark and canopy are separate material slots and the source provides wind-supporting vertex color | Creative Commons Attribution 4.0. Attribution is already tracked in the repository third-party attribution documents | `ACCEPT` as the first mature harvest species. All project-owned derivatives must retain attribution and stable species/variant identity |
| ambientCG TreeEnd003 | Author ambientCG / Lennart Demes, https://ambientcg.com/a/TreeEnd003; color-map SHA-256 `90EBFD4208E34D0E3D5E4C24486E00A6EAD148D1381B3F9CC487D83256A6F8A8` | 2K color, AO, displacement, DirectX/OpenGL normal, opacity and roughness maps; Blender/MaterialX/USD descriptors included | CC0 1.0. Provenance is still recorded even though attribution is not required | `ACCEPT` only as the Pine cut-face material input. It is not a stump or log model |

## Intake integrity

No `05_Licenses` directory was present in the supplied intake. The four gameplay videos are
research evidence, not redistributable art. Their SHA-256 values are:

- axe/arms desync: `166FCBAB109C789F8B5CF67DA9A7BB3C0861A6A1D56C8F5FD8E8ABB48DFF5058`
- felling/log spawn: `44616A64A37879C272FEAFCAD11BE70031B974CB89EDCA7D5FEEAA364E21FBE0`
- excessive log rolling: `CF723C41A511C0E0C57DD3CD5B3B6C0EFA5FED7BB59670D7A57F7A5E3A85657D`
- proximity disappearance: `C2F9331BB9BD75558C81EBA6070FC7FDBB5C36B842D8092F6DA6E9F353667FEF`

The audit used Blender 4.5.11 against source files read-only. Any subsequent Blender work uses a
working copy and deterministic scripts. Source/original files are never overwritten.

## Historical production boundary

Tracked runtime content must have no direct reference to a third-party arms or axe hierarchy. The
production contract therefore accepts an optional project-owned viewmodel override at a stable
boundary and retains a non-broken project-authored fallback. The prior restriction pending a
compatible license/acquisition record is superseded for the three locked sources above; no asset
has yet been imported or integrated by this documentation change. Pine and TreeEnd003 may produce
tracked derivatives with their provenance retained.

## Evidence classification

- `VERIFIED`: file hashes, mesh/rig statistics, Pine CC BY page, DJMaesen CC BY-NC page and
  ambientCG CC0 source.
- `UNKNOWN`: exact Solidbot listing, purchase/acquisition record and granted terms.
- `PROVISIONAL`: first-person grip and animation made with the local candidates are visual QA,
  not final distributable character art.
- `REQUIRED`: packaging validation must fail if a restricted local override is included in a
  distributable artifact.
