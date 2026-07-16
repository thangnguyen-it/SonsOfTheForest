# Sons of the Forest Locomotion Analysis

## 1. Analysis status

NOT STARTED — awaiting current-build measurements.

## 2. Locked build

See [SOTF_BUILD_REFERENCE.md](SOTF_BUILD_REFERENCE.md). No local build is locked in R1-C0, so quantitative analysis cannot begin.

## 3. Speed analysis

| Direction/mode | Build ID | Trials | Mean (m/s) | SD | CV | Status |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| Forward | — | — | — | — | — | Awaiting measurement |
| Backward | — | — | — | — | — | Awaiting measurement |
| Strafe | — | — | — | — | — | Awaiting measurement |
| Diagonal | — | — | — | — | — | Awaiting measurement |
| Sprint | — | — | — | — | — | Awaiting measurement |
| Crouch | — | — | — | — | — | Awaiting measurement |

## 4. Acceleration model comparison

| Model | Parameters | RMSE | Residual Pattern | Accepted |
| --- | --- | --- | --- | --- |
| Linear | — | — | — | No decision |
| Exponential | — | — | — | No decision |
| Capped delta velocity | — | — | — | No decision |
| Force/drag | — | — | — | No decision |

## 5. Jump analysis

| Field | Result |
| --- | --- |
| Measured apex height | — |
| Apex time | — |
| Total airtime | — |
| Inferred ascent gravity | — |
| Inferred descent gravity | — |
| Inferred takeoff velocity | — |
| Hold/tap difference | — |
| Horizontal momentum retention | — |

The standard ballistic model is only a candidate to test against measurements; it is not confirmed Sons of the Forest behavior.

## 6. Sprint analysis

| Field | Result |
| --- | --- |
| Start conditions | — |
| Direction restrictions | — |
| Input threshold | — |
| Stamina drain | — |
| Recovery delay | — |
| Speed transition | — |

## 7. Crouch analysis

| Field | Result |
| --- | --- |
| Transition time | — |
| Speed ratio | — |
| Camera offset | — |
| Blocked stand-up | — |
| Clearance recovery | — |

## 8. Ground and slope analysis

| Field | Result |
| --- | --- |
| Stable ground | — |
| Edge behavior | — |
| Steps | — |
| Slope thresholds | — |
| Sliding | — |
| Dynamic surfaces | — |

## 9. Look analysis

| Field | Result |
| --- | --- |
| Mouse sensitivity curve | — |
| Degrees per count | — |
| Gamepad deadzone | — |
| Gamepad response curve | — |
| Acceleration | — |
| Pitch bounds | — |

## 10. Physics-driver decision gate

No Rigidbody-versus-CharacterController decision may be approved until:

1. current-build locomotion behavior has been measured;
2. current-build physics structure has been investigated in a separately
   authorized task or both candidate adapters have been benchmarked;
3. flat movement, slope/step and dynamic-object tests have been compared;
4. the decision includes evidence, tradeoffs and migration cost.

Current decision:

OPEN.

## 11. R1-C implementation gate

BLOCKED until quantitative movement, jump and look baselines have sufficient
evidence.
