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

## Deterministic build provenance (B5B1)

Each successful benchmark build owns a separate
`r2-perf1.build-provenance.json` sidecar in its artifact directory. The Release
sidecar identifies the non-Development `release_performance` artifact that is
authoritative for FPS and CPU/GPU timing. The Diagnostic sidecar identifies the
Development-only `development_gc` artifact that is authoritative for global GC
allocation. Actual `BuildReport` flags must match those roles exactly; Auto Run,
Auto Connect Profiler, and Deep Profiling fail closed.

Official builds also require the committed
`PlayerSettings.enableFrameTimingStats=true` contract. The builder never mutates
that setting: it fails closed when disabled, records the actual enabled value in
the sidecar, and includes it in the build-configuration fingerprint.

The sidecar records the clean Git commit, Unity/build configuration, an
ordinally canonical SHA-256 fingerprint of the benchmark scene's complete
`AssetDatabase` dependency closure, and an artifact ID over the complete player
tree rather than the executable stub alone. Absolute workspace paths and file
timestamps do not participate in either fingerprint.

Artifact traversal rejects filesystem reparse points before reading content, so
the artifact ID cannot follow a link outside its build directory. Sidecar writes
use an exclusive same-directory temporary file; an existing temporary file causes
a fail-closed result and is never deleted as though it belonged to the new write.

The current benchmark scene remains ignored beneath `Assets/_LocalTrials`.
Consequently its sidecar must report `contentTrackingStatus` as
`local_ignored_content`, `benchmarkContentTracked=false`, and
`repositoryReproducible=false`. The content fingerprint can prove that Release
and Diagnostic builds used identical local content; it does not make that
content reproducible from the repository.

B5B1 does not pass build provenance into runtime reports and does not make a run
pairing-eligible. Official paired measurement remains blocked until B5B2 adds
run-level provenance binding and a later pairing gate verifies both authority
members.

Valid and invalid runs are written to separate CSV tables. A runtime manifest
starts as `incomplete`, becomes `awaiting_offline_validation` only after normal
runtime completion, and becomes `valid` only after the offline runner verifies
the child exit code, report, hardware/build metadata, resolution, and NVIDIA
telemetry. The validator accepts report and screenshot paths only inside the
current run directory, rejects null/NaN/Infinity metrics, and never rewrites a
manifest whose run ID does not match. A timed-out process is invalid and its
exact process tree is terminated and verified; no process-name-wide cleanup is
used.
