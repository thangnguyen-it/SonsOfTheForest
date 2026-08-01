# R2-PERF1 Controlled Revalidation Plan

## Current decision baseline

Evidence classification: **VERIFIED**.

- Paired evidence: `Benchmarks/R2_PERF1_Pairs/pair_20260801T164618969Z`
- `evidenceValidity = VALID`
- Authority configuration: Release performance member, High Fidelity, TAA,
  100% render scale, 1280x720, Direct3D 11.

The verified Release `empty_hdrp_camera` result is:

- 238.6 FPS average
- 4.2 ms frame average
- 6.0 ms p95
- 4.2 ms CPU average
- 3.0 ms GPU average
- `performanceBudgetStatus = PASS`

The paired Diagnostic GC result is:

- 1820 measured frames
- exactly one allocation event
- 48-byte peak and 48-byte total inferred from the single event
- 0.02637362666428089 B/frame average
- strict `gcBudgetStatus = FAIL`

This verifies only the empty HDRP baseline. It does **not** demonstrate that
the full forest reaches 60 FPS.

`Assets/_Docs/Research/R2_PERF0_PRODUCTION_PERFORMANCE_BASELINE.md` remains
preserved as historical evidence, but is classified here as
**HISTORICAL / SUPERSEDED FOR CURRENT DECISIONS**.

## GC observation backlog

`GC-OBS-001`: retain the observed one-time 48-byte allocation as an open
observation. The zero-allocation product gate remains unchanged; no tolerance
or permitted-allocation exception is introduced.

Investigate again only if the allocation repeats over time, grows during
gameplay, or occurs multiple times per frame. The current observation remains
a strict GC budget failure until stronger evidence resolves it.

## Canonical scenario mapping

Historical `baseline_orbit` maps to current canonical `full_forest` for
controlled R2-PERF1 screening. The historical token belongs to the legacy
multi-scenario runner and must not be restored as an alias.

The current `full_forest` selector preserves the benchmark scene's complete
active composition: neutral ground, key lighting, atmosphere volume, near 10,
mid 50, far 300, and broadleaf accent 24 forest tiers. The Release artifact at
`Builds/Benchmarks/R2_PERF1_Release/SOTF_R2_PERF1.exe` contains the canonical
`full_forest` contract and has a valid `release_performance` provenance sidecar
for the current source commit.

## Controlled screening order

Run Release performance authority only, in this order:

1. `ground_only`
2. `full_forest`
3. `empty_hdrp_camera` as the end-of-run control

Every initial command uses one independent run. Repeat a scenario three times
only when its result is near a gate, telemetry reports thermal invalidation, or
the end control shows material drift from the verified empty baseline. Do not
run a paired Diagnostic GC member for every screening scenario.

Common contract: High Fidelity, TAA, 100% render scale, 1280x720, windowed,
Direct3D 11, Release/non-Development, `release_performance`, 10-second natural
warm-up, 8-second sample, screenshots disabled.

## Release commands

These are the real commands approved for manual execution. They were validated
with the identical arguments plus `-DryRun`; no real benchmark has been run.

```powershell
.\Tools\Performance\Invoke-R2Perf1Offline.cmd -ExecutablePath "Builds/Benchmarks/R2_PERF1_Release/SOTF_R2_PERF1.exe" -Quality "High Fidelity" -Scenario ground_only -Antialiasing TAA -RenderScalePercent 100 -Upscaler CatmullRom -BuildKind release -MeasurementRole release_performance -MeasurementSetId r2_perf1c0_ground_only -Runs 1 -TimeoutSeconds 180 -WarmupSeconds 10 -SampleSeconds 8 -Width 1280 -Height 720

.\Tools\Performance\Invoke-R2Perf1Offline.cmd -ExecutablePath "Builds/Benchmarks/R2_PERF1_Release/SOTF_R2_PERF1.exe" -Quality "High Fidelity" -Scenario full_forest -Antialiasing TAA -RenderScalePercent 100 -Upscaler CatmullRom -BuildKind release -MeasurementRole release_performance -MeasurementSetId r2_perf1c0_full_forest -Runs 1 -TimeoutSeconds 180 -WarmupSeconds 10 -SampleSeconds 8 -Width 1280 -Height 720

.\Tools\Performance\Invoke-R2Perf1Offline.cmd -ExecutablePath "Builds/Benchmarks/R2_PERF1_Release/SOTF_R2_PERF1.exe" -Quality "High Fidelity" -Scenario empty_hdrp_camera -Antialiasing TAA -RenderScalePercent 100 -Upscaler CatmullRom -BuildKind release -MeasurementRole release_performance -MeasurementSetId r2_perf1c0_empty_control -Runs 1 -TimeoutSeconds 180 -WarmupSeconds 10 -SampleSeconds 8 -Width 1280 -Height 720
```

Before manual execution, close Unity and VS Code, connect AC power, select the
high-performance Windows power plan, and preserve the current Release artifact
and provenance sidecar as the measurement authority.
