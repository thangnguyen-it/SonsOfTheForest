# R2-FOREST1F — Tree Harvest Quality Reconstruction

Status: `READY_FOR_USER_VISUAL_REVIEW`

Branch: `codex/r2-forest1f-harvest-quality`
Base: `3a5823948661c006f919000dfa0883e7381e8c0c`

This milestone reconstructs the existing tree-harvest prototype as a continuous playable loop. Automated and controlled-runtime gates are complete; the visual result has deliberately not been labelled user-approved.

## Evidence classification and dispositions

| Source | Evidence / licence | Disposition |
|---|---|---|
| Solidbot firefighter axe | Supplied binary hash and geometry were verified, but the exact listing, acquisition receipt and granted licence remain `UNKNOWN` | `CONDITIONAL / LOCAL-ONLY`; not copied or derived into the public repository |
| DJMaesen FP Arms | Supplied model is Creative Commons Attribution-NonCommercial | `REJECT` as a distributable production dependency; retained only as local visual/rigging reference |
| LOLIPOP Pine pack | Pine pack by LOLIPOP, CC BY 4.0; source and attribution are recorded in the project | `ACCEPT`; `Pine_large_1` is the first vertical-quality harvest species |
| ambientCG TreeEnd003 | ambientCG / Lennart Demes, CC0 1.0 | `ACCEPT` only for end-grain/cut-face material input |

Exact source URLs, hashes, mesh statistics and acquisition evidence are recorded in `R2_FOREST1F_ASSET_INTAKE_AND_LICENSE.md`. Source/original intake files were not edited.

## Root cause and representation handoff

The disappearing-tree defect was not an LOD or static-renderer failure. Promotion correctly applied the baked transform to the interactive `Transform`, but its pooled kinematic `Rigidbody` retained a stale world pose at the origin. On the next physics synchronization the body moved the promoted tree away from its static location, making it appear to vanish nearby and return after demotion.

The fix resets the lease local pose, applies the baked placement, synchronizes the Rigidbody pose, validates identity and visible renderer readiness, and only then hides the static visual. Demotion restores and validates the static visual before returning the lease. A 30-boundary-crossing regression verifies that neither representation is simultaneously hidden or visibly duplicated and that identity, variant, transform and persistent lifecycle state survive the handoff.

## Dependency map

```text
Player input
  -> PlayerAxeHarvestController (action state, range/facing/target validation)
  -> AxeViewmodelAnimator (synchronised hands + axe, one timed contact marker)
  -> TreeDamageEvent
  -> ForestInteractiveTree (damage/notch/fall lifecycle)
  -> ForestFellingKitDefinition / ForestHarvestProfile
  -> ForestHarvestPresentationPool (bounded chips/camera feedback)
  -> ForestTreeDeltaStore (sparse changed-tree session state)

ForestCellRuntime static bindings
  -> ForestCellInteractionCoordinator (hysteresis + atomic handoff)
  -> interactive lease
  -> felling representation
  -> stump + species-matched log outputs
  -> unload/reload delta restoration
```

## Viewmodel and contact contract

The distributable fallback is a project-owned procedural two-arm survival-axe viewmodel. Axe and hands share one animation root; the axe is not animated independently. Required clips are:

- `Axe_Idle`
- `Axe_Equip`
- `Axe_Unequip`
- `Axe_Chop_Left`
- `Axe_Chop_Right`
- `Axe_Chop_Heavy`
- `Axe_Recovery`

Each ordinary chop follows windup → one contact marker → recoil/recovery. Input changes the action state but cannot directly damage a tree. At the marker the controller evaluates a stable camera-space blade segment, performs a non-allocating overlap, then verifies range, facing, target type and lifecycle before emitting one `TreeDamageEvent` per swing sequence. Heavy chop is used for the final felling transition. Tests cover delayed contact, single dispatch and contact against the real animated blade geometry.

## Pine continuity and physics

`variant.pine.large.1` supplies the canonical trunk diameter, bark, cut plane and scale for the first complete kit. Authored states are intact/Notch_01/Notch_02/Notch_03/ReadyToFall, falling upper tree, ground-pivot stump and three length/radius log definitions. Runtime uses four output segments where the species profile requires four logs. TreeEnd003 is restricted to cut faces; bark remains on cylindrical surfaces.

Log mass is relative to segment volume instead of a single arbitrary value. The Pine reference radii are 0.34 m, 0.295 m and 0.25 m; the base mass scale is 30 kg at the reference segment. Logs use dynamic/static friction 0.58/0.72, bounciness 0.02, linear/angular damping 0.55/1.15, continuous dynamic collision, 12 m/s maximum linear velocity, 14 rad/s maximum angular velocity and 0.12 sleep threshold. These values are provisional engineering tuning, not claimed measurements from *Sons of the Forest*.

Felling locks direction from accumulated valid hit direction, with a stable-identity deterministic fallback. It includes anticipation, continuous falling motion, impact/settling, non-overlapping log placement and sparse delta update. Reload cannot resurrect Falling/Felled trees as Standing.

## Validation and evidence

Validation scene: `Assets/_Game/Scenes/Validation/SCN_Validation_ForestCell.unity`

Selected evidence:

- `Artifacts/R2_FOREST1F/Evidence/axe_idle_target.png`
- `Artifacts/R2_FOREST1F/Evidence/axe_windup.png`
- `Artifacts/R2_FOREST1F/Evidence/axe_contact.png`
- `Artifacts/R2_FOREST1F/Evidence/notch_stage_01.png`
- `Artifacts/R2_FOREST1F/Evidence/notch_stage_02.png`
- `Artifacts/R2_FOREST1F/Evidence/falling_transition.png`
- `Artifacts/R2_FOREST1F/Evidence/stump_and_logs.png`
- `Artifacts/R2_FOREST1F/Evidence/logs_species_continuity.png`

Controlled runtime validation observed one Pine tree reach 100 damage, enter Falling, settle as Felled, keep its stump, and produce four species-matched logs. All four Rigidbody outputs entered sleep without an explosion or continued drift. Automated validation covers atomic handoff, 30 boundary crossings, failed-promotion rollback, damage persistence, lifecycle reload, harvest identity/geometry, one-contact damage and log physics contracts.

Final automated results were 7/7 focused EditMode, 377/377 complete EditMode and 8/8 focused PlayMode, with zero failures or skips. Unity compiled with no project errors or new project warnings; `SCN_Foundation` remained active and clean.

The latest approved standalone rendering authority remains the R2-PERF1 `full_forest` result: 164.5244 FPS average, 6.0781 ms average, 6.6691 ms p95 and 7.0506 ms p99 on NVIDIA MX550 at 1280x720/D3D11. This milestone preserved the same static forest composition, added only proximity-bound interactive leases/presentation, and introduced no per-tree update. A milestone-specific Foundation Release smoke attempt did not produce valid frame-time evidence: an auxiliary sampler lifecycle stalled and was removed instead of weakening the timeout or claiming its output. Therefore the existing >60 FPS render baseline is preserved, while the exact integrated harvest workload remains to be revalidated under the approved standalone harness after visual acceptance.

## Legacy and known limitations

- The old capsule/legacy axe presentation remains recoverable but is no longer the production harvest viewmodel reference.
- Raw third-party intake remains outside tracked production content.
- Because the supplied external arms and axe cannot presently be distributed, the checked-in procedural viewmodel is intentionally a legal fallback and remains below final realistic art quality.
- No dedicated authored creak, axe-impact or tree-impact audio source was supplied; the milestone does not invent copyrighted audio.
- Wood chips are bounded project-owned mesh VFX, not fracture simulation.
- Session delta persistence is validated; a durable save-backend integration remains deferred.
- Performance validation establishes the current controlled scene/runtime budget only; it does not prove the eventual complete game remains above 60 FPS.
- Visual quality requires user review; automated gates do not establish taste or parity.

## User review

Open `SCN_Validation_ForestCell`, enter Play Mode and use:

- `1`: equip the axe and both arms;
- `2`: unequip;
- left mouse: alternate left/right chops through the timed contact state.

Approach a Pine and cross the near/far boundary repeatedly to inspect handoff, then chop through notch stages and review the fall, stump, cut faces, log scale and settling. The production player/prefab integration is also present in the Foundation composition; the `SCN_Foundation` scene asset itself remains unchanged by this milestone.
