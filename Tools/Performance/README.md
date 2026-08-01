# R2-PERF1 offline benchmark runner

`Invoke-R2Perf1Offline.ps1` runs one quality/scenario/AA/render-scale tuple in
three independent standalone processes. It refuses to start a benchmark while
`Unity.exe` or `Code.exe` is running and never closes user applications.
Before every child launch it samples the expected NVIDIA adapter and
`nvidia-smi pmon` repeatedly for at least five seconds. A busy adapter, active
thermal/hardware slowdown, or attributable external NVIDIA workload stops the
run without launching the player. P8/300 MHz is accepted when utilization is
idle.

Product-gate runs must use the non-Development executable:

`Builds/Benchmarks/R2_PERF1_Release/SOTF_R2_PERF1.exe`

Build it with Unity menu **Sons Of The Forest > Performance > R2-PERF1 > Build
Release Player**. Building does not run the player.

## Measurement authority contract v2

Every schema-v2 run declares exactly one member role:

| Role | Required build | Authoritative budget | Non-authoritative budget | Member aggregate gate |
|---|---|---|---|---|
| `release_performance` | non-Development `release` | FPS and CPU/GPU frame time | global GC | `INCOMPLETE` |
| `development_gc` | Development `diagnostic` | global `GC Allocated In Frame` | FPS and CPU/GPU frame time | `INCOMPLETE` |

The runtime writes `r2-perf1/2` reports and `r2-perf1-manifest/2` manifests.
It records `evidenceValidity=PENDING_OFFLINE_VALIDATION`; only this offline
validator may finalize evidence as valid or invalid. Budget failure does not
make otherwise complete evidence invalid. Conversely, missing authoritative
global-GC data never becomes zero or `PASS`.

B5A intentionally leaves `measurementSetId`, source/build/content/configuration/
hardware provenance empty and sets `pairingEligible=false`. Pairing and the
aggregate product gate are deferred. Schema-v1 evidence remains readable, is
never rewritten solely for migration, and is never pairing-eligible.

After closing Unity and VS Code, open Windows Terminal in the repository root:

```powershell
.\Tools\Performance\Invoke-R2Perf1Offline.cmd `
  -Quality Balanced `
  -Scenario full_forest `
  -Antialiasing TAA `
  -RenderScalePercent 100 `
  -BuildKind release `
  -MeasurementRole release_performance `
  -Runs 3
```

Inspect the command without launching the executable:

```powershell
.\Tools\Performance\Invoke-R2Perf1Offline.cmd `
  -Quality Balanced `
  -Scenario full_forest `
  -BuildKind release `
  -MeasurementRole release_performance `
  -Runs 3 `
  -DryRun
```

Dry-run performs no child-process launch and does not create or modify benchmark
output. The CMD wrapper forwards quoted values such as `-Quality "High
Fidelity"` unchanged; repository and executable paths containing spaces are
quoted with Windows command-line escaping.

Visual capture is a separate, non-measurement run:

```powershell
.\Tools\Performance\Invoke-R2Perf1Offline.cmd `
  -Quality Balanced `
  -Scenario full_forest `
  -BuildKind release `
  -MeasurementRole release_performance `
  -Runs 1 `
  -CaptureScreenshot
```

A GC-authority member uses the separately built Diagnostic Development player:

```powershell
.\Tools\Performance\Invoke-R2Perf1Offline.cmd `
  -ExecutablePath "Builds/Benchmarks/R2_PERF1_Diagnostic/SOTF_R2_PERF1.exe" `
  -Quality Balanced `
  -Scenario full_forest `
  -Antialiasing TAA `
  -RenderScalePercent 100 `
  -BuildKind diagnostic `
  -MeasurementRole development_gc `
  -Runs 3
```

Timing from this Development member is diagnostic only. The validator requires
the Unity `ProfilerRecorder` global `GC Allocated In Frame` counter; an
unavailable counter makes the GC member incomplete/invalid under the current
lifecycle rather than passing with a synthetic zero.

Valid and invalid runs are written to separate CSV tables. A runtime manifest
starts as `incomplete`, becomes `awaiting_offline_validation` only after normal
runtime completion, and becomes `valid` only after the offline runner verifies
the child exit code, report, hardware/build metadata, resolution, and NVIDIA
telemetry. The validator accepts report and screenshot paths only inside the
current run directory, rejects null/NaN/Infinity metrics, and never rewrites a
manifest whose run ID does not match. A timed-out process is invalid and its
exact process tree is terminated and verified; no process-name-wide cleanup is
used.
