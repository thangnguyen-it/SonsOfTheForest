# R2-FOREST1D — Interactive Tree Promotion and Felling

## Targeted research conclusion

1. `VERIFIED` user-supplied gameplay imagery and research show that standing trees are hit at a localized trunk position and can transition into a falling tree.
2. `VERIFIED` the visible result must retain the same species-scale tree representation rather than replace it with a generic interaction marker.
3. `VERIFIED` fallen-tree output belongs to the survival resource loop, but stump creation, log segmentation, harvesting quantities, audio, particles, and axe animation are deliberately deferred.
4. `INFERRED` the visually continuous standing-to-falling object should retain one stable world identity; no evidence supports changing identity merely because render/physics representation changes.
5. `UNKNOWN` exact current-build tree health, per-tool damage, fall impulse, settling thresholds, promotion distance, and direction-selection rules.
6. Those unknown values therefore remain `PROVISIONAL` serialized policy on the cell coordinator.
7. A production weapon system does not yet exist, so the runtime exposes `ITreeDamageReceiver` and does not bind chopping to the generic Interact action.
8. A validation-only driver can invoke that contract without becoming gameplay input architecture.
9. Static forest rendering must remain free of per-tree scripts, colliders, rigidbodies, and updates.
10. Only promoted leases own physics. Unchanged standing leases may demote; any damaged, falling, or felled delta prevents restoration of the static standing tree.
11. Sparse session deltas are keyed by `ForestCellId + TreeInstanceId`, preparing a save adapter without implementing a save backend.
12. Edge cases covered by tests include invalid damage, identity preservation, unchanged demotion, damaged non-demotion, falling/felled terminal behavior, and cell unload/reload in the same session.

## Implementation boundary

`ForestCellInteractionCoordinator` is the single cell-level proximity owner. It evaluates already-baked bindings without scene searches or per-tree static callbacks. A promoted tree hides exactly one static visual and activates a reusable, tree-specific lease containing a project-owned wrapper, one Rigidbody, one capsule collider, and the matching species variant.

The state contract is representation-independent:

`Standing → Damaged → Falling → Felled`

State changes update `ForestTreeDeltaStore`. A zero-damage Standing record is removed rather than stored, so unchanged trees consume no delta entry. Falling uses the hit direction projected to the ground; an identity-derived deterministic direction is the fallback.

## Provisional policy and gaps

- Promotion/demotion radii: `18 m / 24 m`.
- Damage threshold: `100`; validation hit: `55`.
- Fall impulse: `8`; settle window: `0.6 s`; maximum fall duration: `8 s`.
- Interactive trunk capsule is estimated from visual height and is not final species collision authoring.
- Persistence is in-memory only. Destroying the cell owner ends the session store.
- Final tree art, weapon/tool damage profiles, cut marks, stump/log output, sound, particles, animation, streaming, and multiplayer replication remain deferred.
