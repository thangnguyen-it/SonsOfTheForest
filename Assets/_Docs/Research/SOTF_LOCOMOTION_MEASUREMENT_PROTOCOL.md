# Sons of the Forest Locomotion Measurement Protocol

This is a protocol only. No measurements were performed in R1-C0.

## 1. Build lock

Before every session record:

- build ID;
- game executable SHA-256;
- measurement date in UTC;
- Steam beta branch;
- save identifier;
- difficulty;
- player condition;
- carried/equipped items;
- armor state;
- stamina state;
- hunger, thirst, and rest states;
- environment and weather;
- surface;
- slope;
- snow depth;
- frame-rate cap;
- VSync;
- resolution;
- input device;
- mouse DPI;
- in-game sensitivity;
- controller model.

The session is invalid for Grade A if the exact build ID is unavailable. Re-lock after every update and never pool different build IDs.

## 2. Controlled save requirements

Create in a future authorized task a clean measurement save with:

- single-player;
- Normal difficulty unless evidence requires another difficulty;
- no cheats or mods;
- full health and stamina;
- neutral hunger, thirst, and rest;
- no heavy carried objects;
- a repeatable flat test area;
- a repeatable slope/step area;
- a repeatable low-ceiling crouch area.

Record a stable save ID and reset state between trials. Do not create this save in R1-C0.

## 3. Coordinate acquisition options

Ranked future methods:

| Rank | Method | Expected precision | Limitations |
| --- | --- | --- | --- |
| A | Current-build direct runtime telemetry in an explicitly authorized research task | Highest; frame-level coordinates and state | Requires separate authorization and an unmodified measurement path |
| B | Repeatable world landmarks plus video measurement | Medium; bounded by landmark calibration and perspective | Requires reliable world distances and camera alignment |
| C | Frame-count and known-distance visual measurement | Medium-low; timing can be frame accurate | Distance calibration, occlusion, and projection add error |
| D | Manual stopwatch measurement | Low | Human reaction time; useful only for coarse corroboration |

Prefer the highest-ranked lawful, non-modifying method available and record the acquisition method per source recording.

## Raw data layers

1. `SOTF_LOCOMOTION_RAW_RESULTS.csv`

   One row per completed trial. It stores trial conditions and summary metrics.

2. `SOTF_LOCOMOTION_FRAME_SAMPLES.csv`

   Zero or more ordered sample rows per trial. It stores frame/time-level input, position, velocity, look, ground, and stamina observations.

The composite key `experiment_id + trial_id` must identify the same trial in both files.

Requirements:

- acceleration-model fitting requires frame samples;
- ascent/descent gravity fitting requires frame samples;
- look acceleration/smoothing fitting requires frame samples;
- a trial without frame samples may support coarse speed corroboration but cannot validate dynamic-response models;
- `sample_index` must start at 0 and increase without duplicates per trial;
- `sample_time_seconds` must be monotonic;
- `acquisition_method` must identify telemetry, calibrated video, visual estimate, or manual observation;
- missing values remain empty, never fabricated as zero.

## 4. Speed experiments

Conditions: forward walk, backward walk, left/right strafe, diagonal walk, forward sprint, sideways sprint attempt, backward sprint attempt, crouch forward, and crouch strafe.

For every condition use a warm-up, fixed input duration, and at least 3 valid trials. Record distance and elapsed time, then calculate mean, standard deviation, and coefficient of variation (CV). Keep build, surface, slope, player state, frame cap, and input magnitude fixed. Treat left/right separately before combining.

## 5. Acceleration experiments

Test standing-to-walk, standing-to-sprint, walk-to-sprint, sprint-to-release, walk-to-release, forward-to-backward reversal, forward-to-strafe transition, and airborne input response.

Capture ordered displacement, velocity, and input samples in `SOTF_LOCOMOTION_FRAME_SAMPLES.csv`. Do not choose a response model before data collection. Future analysis must compare linear acceleration, exponential response, capped velocity delta, and force/drag response using common intervals and residuals. Trial-summary start/end values alone are insufficient for model fitting.

## 6. Jump experiments

Test standing jump, walking jump, sprint jump, short tap, full hold, jump at slope, repeated jump input before landing, and input shortly before landing.

Record takeoff, apex, and landing frames; start and apex heights; horizontal displacement; and input press/release frames. Store ordered height, velocity, grounded, and jump-input samples in `SOTF_LOCOMOTION_FRAME_SAMPLES.csv`. Candidate ballistic equations may be fitted from frame samples, but are not assumed to be confirmed behavior.

## 7. Sprint and stamina experiments

Measure minimum movement magnitude, startup delay, time to sprint speed, stamina drain per second, minimum stamina required, exhaustion behavior, recovery delay, recovery rate, reactivation threshold, crouch interaction, jump interaction, and sideways/backward behavior. Restore a controlled stamina state between trials and separate eligibility from achieved speed.

## 8. Crouch experiments

Measure stance transition time, camera height change, collider/clearance behavior, blocked stand-up, automatic versus manual stand recovery, crouch speed ratios, crouch during sprint, and crouch during jump. Use a repeatable low ceiling and never infer collider dimensions from camera motion alone.

## 9. Ground, slope and step experiments

Test flat ground, small downward step, small upward step, staircase, mild slope, steep slope, extreme slope, slippery surface, snow, and a dynamic platform if available. Record grounded flicker, sliding, step acceptance, and vertical displacement. Measure actual slope/step geometry and stratify results by surface and snow condition.

## 10. Look experiments

Mouse protocol:

- use known DPI with pointer acceleration disabled;
- test multiple sensitivity levels;
- apply fixed physical/count movement;
- measure yaw and pitch separately;
- run at least 3 trials per point.

Gamepad protocol:

- test 0%, 10%, 25%, 50%, 75%, and 100% deflection;
- hold for a fixed duration;
- measure yaw/pitch degrees per second;
- estimate deadzone, response curve, and acceleration ramp.

Keep frame cap and device identity fixed, and test step inputs to distinguish immediate response from smoothing. Store ordered look input, camera yaw, camera pitch, and sample times in `SOTF_LOCOMOTION_FRAME_SAMPLES.csv`; summary angular values alone cannot establish acceleration or smoothing.

## 11. Statistical acceptance

- Require at least 3 valid trials.
- Remove no unexplained outlier; keep exclusions with reasons in raw data.
- Report mean, standard deviation, and CV.
- Downgrade confidence when CV is high and investigate the cause.
- Do not combine different build IDs.
- Do not combine different surfaces without explicit stratification.
- Retain raw trials and source recordings so every conclusion is reproducible.

## 12. Safety and integrity

- Never modify the original game installation.
- Never commit proprietary binaries or assets.
- Never use measurements from a modified runtime as default-game evidence.
- Distinguish instrumented observation from altered behavior.
- Do not attach trainers, cheats, injectors, or dumpers during default-game measurement.
- Record limitations and failed trials rather than filling gaps with inferred defaults.
