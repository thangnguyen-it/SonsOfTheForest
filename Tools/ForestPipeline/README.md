# R2 Forest Harvest Asset Pipeline

This project-owned pipeline audits and derives the R2-FOREST1E felling assets from the
licensed LOLIPOP Pine/Fir/Maple source packs. It never overwrites a source pack.

Run from the repository root:

```powershell
pwsh -File Tools/ForestPipeline/Invoke-ForestPipeline.ps1 -Mode Audit
```

`forest-harvest-manifest.json` is the source of stable species/variant identity, source
FBX and mesh names, provisional cut geometry, log yield, output paths and attribution.
Outputs and reports are bounded to the repository. Missing sources, requested meshes,
materials, unexpected paths or stale checksums fail closed.

The Build, Validate and Turntable entry points are implemented by the companion Blender
scripts and produce structured JSON under `Artifacts/ForestPipeline`. Generated Unity
content is written only below `Assets/_Game/Art/World/Forest/Harvest/Generated`.
