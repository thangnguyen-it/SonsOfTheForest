# R1 Playable Player Foundation

Status: implementation baseline, 2026-07-17

## Targeted evidence conclusion

1. The official Steam page identifies *Sons of the Forest* as the released product, with first-person context supplied by store metadata (`VERIFIED`).
2. It does not publish the current movement driver, collision queries, locomotion equations, or numerical tuning (`UNKNOWN`).
3. Official Small Patch notes dated 2024-05-13 fixed failure to un-crouch near some colliders (`VERIFIED`).
4. That supports an explicit safe-standing clearance seam; it does not prove a particular overlap shape or recovery policy.
5. The pinned `d20b8ab1b7a28ce1e4a48f5dc74c1ecbcedea2cb` dump is Early Access-era structural evidence only (`HISTORICAL`).
6. It exposes `Rigidbody`, `CapsuleCollider`, fixed-update, collision, slope, grounding, crouch-clearance, and step-helper concepts.
7. Empty IL2CPP bodies and serialized field declarations reveal neither current algorithms nor prefab-assigned values.
8. The audited old TheForest project remains a requirements reference only; no code or tuning was copied.
9. Unity 6.3 documentation states that `CharacterController.Move` is collision-constrained and does not apply gravity automatically (`VERIFIED`).
10. Unity also documents that CharacterController interaction does not automatically push rigidbodies (`VERIFIED`).
11. Unity 6.3 recommends forces for continuous Rigidbody motion and identifies direct velocity changes as suitable for immediate changes such as jumping (`VERIFIED`).
12. Unity documents Rigidbody interpolation for visibly followed bodies and continuous collision detection for fast dynamic colliders (`VERIFIED`).
13. Current-release SOTF walk, sprint, crouch, jump, slope, step, acceleration, and camera values remain `UNKNOWN`.
14. Current-release SOTF use of Rigidbody versus CharacterController also remains `UNKNOWN`.
15. Therefore all numerical values in `CFG_PlayerFoundation` are `PROVISIONAL` and replaceable.

## Candidate-driver experiment

- Environment: Unity 6000.3.10f1, isolated additive unsaved scene, fixed step 0.02 seconds.
- Candidates: CharacterController and non-kinematic Rigidbody with a capsule.
- Obstacles: 0.25 m step, 0.45 m step, 20-degree ramp, and a 10 kg dynamic box.
- CharacterController: low step `(0.035, 7.000)`; high step `(0.035, 0.840)`; ramp `(2.726, 5.000)`.
- Rigidbody: low step `(0.000, 6.837)`; high step `(0.000, 0.850)`; ramp `(2.570, 4.667)`.
- CharacterController push: player Z `0.615`, box Z `1.500`.
- Rigidbody push: player Z `6.901`, box Z `7.736`.
- Both candidates cleared the lower step, rejected the higher step, and climbed the tested ramp.
- Only the Rigidbody candidate produced useful native dynamic-body interaction in this experiment.
- These results compare this project's candidates; they do not measure or identify SOTF (`INFERRED`).

## Implementation decision

- Select a non-kinematic Rigidbody adapter as the provisional driver.
- Preserve `PlayerMovementModel` as the pure authority for target locomotion state and velocity.
- Apply target-minus-current velocity through `ForceMode.VelocityChange`; do not assign velocity every physics tick.
- Disable built-in gravity because the pure model owns vertical velocity and ground snap.
- Freeze body rotation, interpolate the body, and use Continuous Dynamic collision detection.
- Use a capsule sphere cast for grounding and an overlap capsule for standing clearance.
- Keep input, pure decisions, physics, view application, animation, and visual rendering in separate assemblies/components.
- Keep physics behind `IPlayerMotionDriver` and view application behind `IPlayerLookDriver` so either adapter can be replaced.
- Drive the existing lawful Humanoid Animator with eight existing clips and no root motion.
- Render the head-hidden local body without shadows, plus a full shadow-only body; reserve the full renderer for future remote-player mode.
- Compose the system in `PRF_PlayerFoundation`; do not modify `SCN_Foundation`.
- Provide `SCN_PlayerFoundationValidation` as an isolated manual validation environment.

## Confident baseline and tests

- Confident: input intent reaches the pure models and fixed-step physics adapter.
- Confident: walk/sprint/crouch/jump/fall state is represented without scene lookups.
- Confident: a blocked stand request keeps crouch until capsule clearance returns.
- Confident: body yaw and camera pitch use the existing clamped/wrapped pure look model.
- Confident: collision, ground probing, local-body rendering, and animation selection are independently testable.
- Test edges: diagonal movement, wall collision, jump takeoff/landing, crouch clearance, look delta, asset wiring, and assembly boundaries.

## Provisional choices and fidelity gaps

- Provisional: 4/7/2 m/s walk/sprint/crouch, response rates, gravity, 1.2 m jump height, and -2 m/s ground snap.
- Provisional: 2.0/1.2 m capsule heights, 0.35 m radius, 0.30 m step target, 45-degree slope limit, and 80 kg body mass.
- Provisional: look sensitivity, 1.65/1.05 m view heights, animation cross-fade timing, and hold-to-crouch policy.
- Gap: the Rigidbody driver does not yet contain a dedicated step-helper algorithm; native capsule response is only a baseline.
- Gap: steep-slope sliding, ledge grounding, moving platforms, stairs, dynamic-body mass ratios, and high-speed tunnelling need broader experiments.
- Gap: current SOTF speed curves, momentum conservation, air control, jump timing, camera motion, and stance transition timing need release-locked measurement.
- Gap: local arm/weapon posing, procedural feet, animation blending, remote-player switching, networking, and final scene composition are later systems.

## Sources

- Official Steam product page: https://store.steampowered.com/app/1326470/Sons_Of_The_Forest/
- Official Small Patch, 2024-05-13: https://store.steampowered.com/news/app/1326470/view/5768625435411253499
- Pinned historical structure: https://github.com/NeuralBinary/Sons-of-The-Forest-Dump/blob/d20b8ab1b7a28ce1e4a48f5dc74c1ecbcedea2cb/Sons/FirstPersonCharacter.cs
- Unity CharacterController: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.html
- Unity Rigidbody linear velocity: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody-linearVelocity.html
- Unity Rigidbody interpolation: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody-interpolation.html
- Unity collision detection mode: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody-collisionDetectionMode.html
