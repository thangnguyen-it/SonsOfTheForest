# R2-FOREST1F Asset Intake and License Decision

Audit date: 2026-08-13. Intake root: `E:\SOTF_AssetIntake\TREE_QUALITY_INTAKE_01`.
Binary source media remains outside the public repository. This document records provenance,
technical findings and the production disposition; it does not redistribute the source files.

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

## Production boundary

Tracked runtime content must have no direct reference to the local arms or axe hierarchy. The
production contract therefore accepts an optional project-owned viewmodel override at a stable
boundary and retains a non-broken project-authored fallback. A local visual-review build may
contain the restricted candidates, but a distributable build must exclude them until a compatible
license and acquisition record are supplied. Pine and TreeEnd003 may produce tracked derivatives
with their provenance retained.

## Evidence classification

- `VERIFIED`: file hashes, mesh/rig statistics, Pine CC BY page, DJMaesen CC BY-NC page and
  ambientCG CC0 source.
- `UNKNOWN`: exact Solidbot listing, purchase/acquisition record and granted terms.
- `PROVISIONAL`: first-person grip and animation made with the local candidates are visual QA,
  not final distributable character art.
- `REQUIRED`: packaging validation must fail if a restricted local override is included in a
  distributable artifact.
