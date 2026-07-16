# Bootstrap Foundation

## 1. Purpose

R0-C defines lifecycle contracts for runtime ticking, services, validation, and scene bootstrap without providing any implementation.

## 2. Why This Exists

A long-term survival project must avoid random `MonoBehaviour` manager order and hidden scene dependencies. Explicit contracts make lifecycle, timing, composition, and readiness boundaries visible and testable.

## 3. Runtime Contract Groups

- `GameLoopStage`
- `GameTickContext`
- `ITickableGameSystem`

## 4. Service Contract Groups

- `IGameService`
- `IServiceResolver`
- `IServiceRegistry`
- `IGameServiceLifecycle`
- `IReadinessCheck`

## 5. Validation Contract Groups

- `ValidationIssue`
- `ValidationReport`
- `IValidatable`

## 6. Scene Bootstrap Contract Groups

- `SceneBootstrapContext`
- `ISceneBootstrapStep`
- `ISceneCompositionRoot`
- `ISceneReadinessValidator`

## 7. Dependency Rules

- Core owns service/runtime contracts.
- SceneBootstrap depends on Core only.
- Gameplay domains must not directly control scene boot order.
- Presentation/UI must not be required for domain simulation.
- Future bootstrap implementation must live in Infrastructure, not Gameplay.
- Scene bootstrap may compose systems, but domain systems must remain testable outside scenes.

## 8. What R0-C Does NOT Do

- No bootstrapper implementation
- No service registry implementation
- No tick runner
- No `MonoBehaviour` managers
- No scene modification
- No player or gameplay

## 9. Next Step

R0-D should create folder-level architecture guards and possibly test/validation scaffolding, or R1 should start Player Foundation only after the foundation is approved.
