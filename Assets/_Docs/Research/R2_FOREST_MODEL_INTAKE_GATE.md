# R2 — Forest Model Intake Gate

Date: 2026-07-23

Purpose: prevent visually attractive but unplayable tree assets from entering the gameplay forest before they pass the 60 FPS model gate.

## Decision

The project should use lawfully acquired, game-ready tree models where possible, especially packs with authored LODs and billboards. Raw high-resolution scans remain useful as reference, material sources, or decimation inputs, but they are not gameplay prefabs.

The Mantissa Japanese Maple trial proved why this distinction matters: the smallest raw FBX measured in Unity at about 2.42 million triangles. That is valuable source material, but it is far beyond the hard near-tree limit for a normal playable forest.

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
