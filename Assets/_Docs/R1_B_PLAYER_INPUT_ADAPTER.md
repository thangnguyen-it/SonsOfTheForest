# R1-B Player Input Adapter

## 1. Purpose

R1-B translates the approved Player input actions into Player-domain intent values. It adds the missing intent-source boundary, a pure input buffer, and a thin Unity Input System adapter without implementing player motion or camera application.

## 2. Approved architecture

`PlayerInputAdapter` receives Input System callbacks and writes only to `PlayerInputBuffer`. Consumers pull `MovementIntent` and `LookIntent` through `IPlayerIntentSource`. Input, movement calculation, collision application, and camera presentation remain separate responsibilities.

Input System does not enter `Gameplay.Player`. R1-B does not move or rotate the player.

## 3. Input asset used

The adapter uses the existing `Assets/InputSystem_Actions.inputactions` asset. R1-B does not modify the input action asset. The `Player` and `UI` maps remain separate.

## 4. Action ownership

`PlayerInputAdapter` uses `playerInput.actions` and owns callbacks only for `Player/Move`, `Player/Look`, `Player/Sprint`, `Player/Crouch`, and `Player/Jump`. Attack, Interact, Previous, Next, UI actions, control-scheme selection, and binding overrides remain outside this adapter.

## 5. Movement intent semantics

Move stores the latest `Vector2`; `MovementIntent` applies final unit-magnitude clamping. Sprint is continuous held state. The adapter reports intent only and does not apply velocity, gravity, collision, or transforms.

## 6. Crouch Hold and Toggle semantics

Hold sets crouch desired on performed and clears it on canceled. Toggle inverts crouch desired exactly once on performed and ignores canceled. The policy defaults to Hold and can be changed explicitly.

## 7. Jump one-shot queue

Jump performed latches one request. Repeated performed callbacks before consumption still produce one request. Consumption clears the latch, while jump canceled never removes an already queued request.

## 8. Mouse Delta versus Gamepad Rate

Pointer input is `LookInputKind.Delta`: the latest callback replaces the previous delta, consumption clears it, and pointer delta is not multiplied by `deltaTime`. Non-pointer look is `LookInputKind.Rate`: gamepad look rate remains continuous across consumes until a zero or canceled value clears it.

## 9. Suspend versus Full Reset

Gameplay suspension clears move, sprint, jump, and both look paths while retaining crouch desired. Disabled consumption returns `None`; re-enable rehydrates only current Move and Sprint values. Full reset also clears crouch desired. Neither operation enables, disables, or switches action maps.

## 10. Control-scheme change behavior

`PlayerInput.onControlsChanged` clears pointer delta, gamepad rate, and queued jump. It preserves crouch desired and, when gameplay input is enabled, rehydrates Move and Sprint from the cached actions without synthesizing jump or look input.

## 11. Assembly/dependency rules

`SonsOfTheForest.Presentation.Input` references only `SonsOfTheForest.Gameplay.Player` and `Unity.InputSystem`. `Gameplay.Player` remains independent of Input System. Runtime assemblies do not reference the dedicated input test assembly.

## 12. Test coverage

EditMode tests cover the pure buffer semantics, virtual keyboard/mouse/gamepad behavior through `InputTestFixture`, control changes, suspend/reset behavior, action-map preservation, and architecture/dependency guards. Tests use an isolated copy of the existing input asset and do not save it.

## 13. Explicit exclusions

R1-B creates no PlayerController, motor, CharacterController adapter, camera controller, cursor behavior, UI runtime, prefab, or scene composition. It does not call Survival, Interaction, Combat, Inventory, AI, Persistence, or networking systems.

## 14. Next step

R1-C should implement pure player motor and look logic with tests, not Unity collision yet. CharacterController application, camera transforms, prefab creation, and scene composition remain deferred to their approved tasks.

## External Review Corrections

- Source-specific look clear operations clear only their value and do not switch the active look kind.
- Full reset restores the buffer to its initial enabled state and clears all retained input.
- Suspend remains distinct from full reset: it disables gameplay input, clears movement and transient input, and preserves crouch desired.
- Integration tests use Unity's real component lifecycle after normal GameObject activation rather than invoking Unity messages through reflection.
- Cross-source cancel selection is covered deterministically at the pure-buffer boundary. The existing single `Look` Value action and PlayerInput control-scheme switching do not expose deterministic per-binding cancel callbacks while the other look binding remains active, so no production-only test hook was added.
