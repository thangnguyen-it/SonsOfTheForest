# R1-C1 Movement and Look Implementation Note

Access date for current web research: 2026-07-17. Scope was limited to on-foot movement and first-person look.

## Sources inspected

- Official Steam product page, release 2024-02-22: https://store.steampowered.com/app/1326470/Sons_Of_The_Forest/ (`VERIFIED`). It establishes the released product and experience context, not locomotion algorithms or tuning.
- Official Small Patch, 2024-05-13: https://store.steampowered.com/news/app/1326470/view/5768625435411253499 (`VERIFIED`). It fixes failure to un-crouch near certain colliders, supporting a stand-clearance seam but not its query algorithm.
- Official Hotfix, 2024-06-20: https://store.steampowered.com/news/app/1326470/view/5759622039976950252 (`VERIFIED`). Separate health/stamina regeneration settings support keeping survival policy outside the pure movement calculation.
- Official v1.0 notes, 2024-02-22: https://store.steampowered.com/news/app/1326470/view/5728086065182647154 (`VERIFIED`). Crouching affects multiplayer visibility; stealth/network consequences belong to later systems.
- Community gameplay/settings reference, retrieved 2026-07-17: https://sonsoftheforest.wiki.gg/wiki/Gameplay (`INFERRED`). It documents W/A/S/D, run, crouch, jump, crouch/sprint toggle settings, gamepad deadzone, and separate sensitivity settings. It is not controlled current-build measurement.
- Community stats reference, retrieved 2026-07-17: https://sonsoftheforest.wiki.gg/wiki/Stats?section=1 (`INFERRED`). It reports stamina use by sprint/jump and speed dependencies; exact thresholds, costs, and multipliers remain unverified.
- PCGamingWiki input table, retrieved 2026-07-17: https://www.pcgamingwiki.com/wiki/Sons_of_the_Forest (`INFERRED`). It reports separate mouse/controller sensitivities, Y inversion, and forced mouse smoothing. No primary source or measured curve was supplied.
- Targeted searches for dated 1.0/current movement footage and settings demonstrations found no reproducible quantitative trace suitable for tuning (`UNKNOWN`).
- Pinned dump `d20b8ab1b7a28ce1e4a48f5dc74c1ecbcedea2cb`, 2023-06-24: https://github.com/NeuralBinary/Sons-of-The-Forest-Dump/blob/d20b8ab1b7a28ce1e4a48f5dc74c1ecbcedea2cb/Sons/FirstPersonCharacter.cs (`HISTORICAL`).
- The dump declares separate walk/run/strafe/crouch speeds, velocity limits, gravity/jump, sprint/stamina, ground adhesion, slope, collider, and step-helper fields. Empty IL2CPP bodies and unassigned serialized values reveal neither algorithms nor defaults.
- Pinned uniref example `82b06a043d7495c6ff57018b693bfae45c384453`: https://github.com/in1nit1t/uniref/blob/82b06a043d7495c6ff57018b693bfae45c384453/examples/windows/il2cpp/Sons%20Of%20The%20Forest.py (`HISTORICAL`). Modified values are trainer choices, never original defaults.
- Audited `TheForest_Unity` player scripts (`HISTORICAL` requirements only) corroborate basis-relative movement, crouch/sprint eligibility, ground snap, ballistic jump, and separated body-yaw/camera-pitch. No code or tuning was copied.

## Conclusion and implementation impact

1. Sons of the Forest observably exposes walk/run/crouch/jump and first-person mouse/controller look; crouch and sprint can be hold/toggle upstream.
2. Stand-up clearance, stamina-gated actions, sensitivity, invert-Y, and controller deadzone have supporting evidence, but ownership remains separated by existing contracts.
3. Exact forward/back/strafe speeds, diagonal policy, acceleration curve, air control, gravity, jump height, adhesion, slope/step response, pitch limits, and smoothing are `UNKNOWN` for the current build.
4. The existing intent, config, constraint, ground, and state contracts are sufficient for pure deterministic models; no public contract change is needed.
5. Basis-relative planar movement, analog preservation, diagonal clamping, stance/sprint constraints, grounded jump, gravity, adhesion, yaw accumulation, and pitch clamping can be implemented confidently as clean system boundaries.
6. `Vector3.MoveTowards` acceleration/deceleration, equal directional speed, full air steering, immediate look response, yaw range `[-180, 180)`, and ballistic takeoff are `PROVISIONAL` replaceable baselines.
7. The provisional jump is `sqrt(2 * abs(gravity) * jumpHeight)`; config values are not claimed as Sons of the Forest defaults.
8. Conflicting smoothing evidence is resolved by omitting smoothing from this stateless model: the current contract has no curve/time constant and secondary reporting does not prove a current algorithm.
9. Tests must cover diagonal/analog invariants, pitched bases, blocked standing, unwalkable ground, eligibility, acceleration clocks, invalid vectors/time, wrapping, clamping, and finite outputs.
10. Physics queries, slope sliding, step solving, collider resizing, stamina drain/recovery, stealth, camera application, recoil, sway, headbob, FOV, animation, networking, and final tuning belong to later systems.
