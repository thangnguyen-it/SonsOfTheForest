# R2-PERF0 — Production Performance Diagnosis and Budget Baseline

Date: 2026-07-26

Scope: diagnose the current standalone HDRP forest benchmark before adding more visual density or promoting another model pack.

## Research conclusion

- VERIFIED — Unity 6.3 `FrameTimingManager` supplies frame-level CPU and GPU timing in player builds when frame timing statistics are enabled.
- VERIFIED — Unity 6.3 `ProfilerRecorder` can read player-safe CPU, GPU, render, memory, and GC counters without depending on `UnityEditor.UnityStats`.
- VERIFIED — the previous standalone report recorded 9.3 FPS baseline and 21.8 FPS at forced lowest LOD on the current machine.
- VERIFIED — the previous standalone report wrote render counters as zero because its only counter source was an Editor-only type.
- VERIFIED — the project has `runInBackground` disabled, so an auto-launched standalone benchmark must override it locally or loss of window focus can throttle and invalidate results.
- VERIFIED — 300 far conifers already use `Graphics.RenderMeshInstanced`, with realtime shadow casting and receiving disabled.
- VERIFIED — the local benchmark scene contains ground, sun, atmosphere, 10 near conifers, 50 mid conifers, 300 far instances, and 24 broadleaf accents as separable roots.
- UNKNOWN — the exact current split between CPU main thread, CPU render thread, and GPU cost until the R2-PERF0 standalone run completes.
- UNKNOWN — whether imported foliage materials, HDRP baseline, shadows, foreground meshes, or broadleaf accents dominate the remaining frame time.
- PROVISIONAL — 60 FPS is the minimum playable target requested for this project, not a claimed Sons of the Forest hardware default.
- PROVISIONAL — the frame-time gate is average >= 60 FPS, p95 <= 20 ms, and p99 <= 25 ms.
- PROVISIONAL — steady-state GC allocation must remain 0 B/frame in the benchmark sampling window.
- PROVISIONAL — the benchmark remains a controlled local trial and is not gameplay-scene content.

## Implementation decision

The benchmark records two independent timing paths:

1. `FrameTimingManager` for completed-frame CPU/GPU timing.
2. `ProfilerRecorder` for CPU timing fallback, draw calls, batches, SetPass, triangles, vertices, GC allocation, total used memory, graphics memory, and texture memory.

Unavailable counters are written as unavailable. They are never presented as a measured zero.

The report includes device, operating system, Unity version, graphics API, render pipeline, quality level, resolution, development-build state, and budget values.

`PlayerSettings.enableFrameTimingStats` is enabled and version-controlled during R2 so every standalone diagnostic build exposes the same timing capability. A later release-build audit must remeasure its overhead before final shipping configuration.

Every scenario records average, median, p95, p99, 1% low FPS, CPU/GPU timing, render counters, memory, GC, an inferred bottleneck label, and PASS/FAIL/INCOMPLETE.

## Isolation matrix

- `empty_hdrp_camera`: camera and base render pipeline only.
- `lighting_volume_only`: sun plus HDRP atmosphere, without ground or forest.
- `ground_only`: lighting, atmosphere, and neutral ground.
- `near_10_only`: ground stack plus 10 near interactive-tier tree prefabs.
- `mid_50_only`: ground stack plus 50 mid visual-only tree prefabs.
- `far_300_instanced_only`: ground stack plus 300 instanced distant trees.
- `broadleaf_24_only`: ground stack plus the broadleaf accent tier.
- `baseline_orbit`: complete benchmark composition.
- `full_without_atmosphere`: complete composition without the local fog/atmosphere volume.
- `shadows_off_orbit`: complete composition without directional realtime shadows.
- `shadow_distance_30`: complete composition with a short diagnostic shadow distance.
- `forced_lowest_lod`: complete composition with all LODGroups forced to their final LOD.
- `walkthrough_camp`: moving camera path through the complete composition.
- `empty_hdrp_camera_repeat` and `baseline_orbit_repeat`: end-of-run drift controls for focus, shader warmup, and sustained thermal behavior.

Scenario differences are diagnostic evidence; they are not added together as if GPU workloads were perfectly linear.

## Acceptance budget

- Frame budget at 60 FPS: 16.67 ms.
- Average performance: >= 60 FPS.
- p95 frame time: p95 <= 20 ms.
- p99 frame time: p99 <= 25 ms.
- Steady-state GC allocation peak: 0 B/frame.
- Compile errors: 0.
- Complete EditMode regression suite: pass.
- Foundation scene: remains clean and unmodified.

No new forest model is promoted to gameplay while the full standalone scenario fails this gate.

If `empty_hdrp_camera` already fails, the next milestone targets HDRP/render settings and benchmark resolution before forest art.

If ground passes but a single vegetation tier fails, the next milestone targets that tier's meshes, materials, shadow policy, or renderer path.

If every isolated tier passes but the full composition fails, the next milestone targets cumulative overdraw, draw submission, visibility, and tier transition budgets.

## Measured standalone result

Final controlled run: 2026-07-26, Windows player, Unity 6000.3.10f1, Direct3D 11, HDRP High Fidelity, 1280x720, Intel i7-1255U, NVIDIA MX550 1905 MB, `runInBackground = true`.

| Scenario | Avg FPS | p95 ms | p99 ms | GPU avg ms | Draws | Tris M | GC peak B |
|---|---:|---:|---:|---:|---:|---:|---:|
| `empty_hdrp_camera` | 27.7 | 38.98 | 39.66 | 34.60 | 15 | 0.000 | 0 |
| `ground_only` | 23.2 | 44.99 | 46.09 | 42.62 | 25 | 0.000 | 0 |
| `near_10_only` | 21.1 | 49.26 | 50.17 | 46.87 | 101 | 0.153 | 0 |
| `mid_50_only` | 18.2 | 58.99 | 60.33 | 54.56 | 126 | 0.181 | 0 |
| `far_300_instanced_only` | 22.5 | 46.59 | 47.76 | 44.11 | 55 | 0.010 | 0 |
| `broadleaf_24_only` | 17.0 | 63.54 | 65.19 | 58.52 | 98 | 0.231 | 0 |
| `shadows_off_orbit` | 16.0 | 67.45 | 69.87 | 62.18 | 261 | 0.499 | 0 |
| `forced_lowest_lod` | 21.7 | 48.37 | 48.77 | 45.78 | 127 | 0.012 | 0 |
| `baseline_orbit_repeat` | 14.8 | 73.11 | 74.35 | 67.01 | 305 | 0.575 | 0 |

The first baseline sample contained a one-time 276-byte allocation; the repeated full baseline and every isolated steady-state scenario recorded 0 B/frame.

The repeated empty control measured 28.1 FPS and 34.07 ms GPU, confirming that focus loss and late-run drift were removed from the final harness.

The full baseline repeated at the same 14.8 FPS and nearly identical draw/triangle counts, confirming that scenario state restoration is stable.

## Diagnosis and next gate

- The empty HDRP High Fidelity frame already exceeds the entire 16.67 ms product budget by about 2.1 times; no forest mesh optimization alone can reach 60 FPS under this baseline.
- Neutral ground adds about 7 ms GPU over the empty control despite negligible geometry, pointing to fill-rate/material/fullscreen cost rather than triangle count.
- Of the isolated vegetation tiers, the 24 broadleaf accents are the largest added cost; the 300-tree instanced far tier is the smallest added cost.
- Removing atmosphere changes the complete scene by less than measurement noise, so the local fog volume is not the primary bottleneck.
- Disabling tree shadows improves the full scene from 14.8 to 16.0 FPS, useful but far below the required gain.
- Forcing final LOD reduces the forest to about 0.012M triangles yet reaches only 21.7 FPS, confirming that geometry is not the dominant global limit.
- Total used memory stabilizes around 740 MB; recorded graphics memory is about 504 MB and texture memory about 525 MB. These counters overlap and must not be summed.

The next milestone is `R2-PERF1 — HDRP Quality and Fullscreen Cost Reduction`.

Its first acceptance gate is an empty/ground standalone baseline below 10 ms at 1280x720, leaving approximately 6-7 ms for the complete forest composition.

It must benchmark High Fidelity, Balanced, and Performant HDRP assets; isolate fullscreen passes and anti-aliasing; evaluate render scale/upscaling; and keep near-tree realism decisions separate from distant-tier budgets.

No model pack is promoted before that base-render gate passes.

## Sources

- Unity 6.3 Script Reference — `FrameTimingManager`: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/FrameTimingManager.html
- Unity 6.3 Script Reference — `ProfilerRecorder`: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorder.html
- Unity 6 Script Reference — `Graphics.RenderMeshInstanced`: https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Graphics.RenderMeshInstanced.html
- Local measured baseline: `Assets/_Docs/Research/R2_PRODUCTION_FOREST_RENDERER.md`.
