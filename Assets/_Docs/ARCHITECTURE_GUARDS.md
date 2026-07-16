# Architecture Guards

## 1. Purpose

R0-D adds automated guardrails before gameplay starts. The EditMode suite verifies contract behavior and protects the foundation's architectural boundaries.

## 2. Why This Exists

The old project became prototype-heavy partly because scene objects, managers, UI, gameplay, save, and networking mixed too early. The rebuild must prevent that structurally so dependencies remain explicit and domain logic remains testable.

## 3. Guard Categories

- Contract behavior tests
- Stable ID tests
- Validation report tests
- Domain contract tests
- Persistence/bootstrap tests
- Namespace and dependency guard tests

## 4. Dependency Rules Enforced

- No TheForest namespace under _Game.
- Core should not depend on UnityEngine.
- Runtime assemblies should not reference test assemblies.
- Foundation contracts should not create MonoBehaviour managers.
- No FindObjectOfType or GameObject.Find calls in foundation contracts.

Scene-file protection remains a repository workflow guard: the EditMode suite does not invoke Git. Scene and prefab changes must be rejected by staging and CI path checks.

## 5. What R0-D Does NOT Do

- No gameplay implementation
- No player
- No inventory implementation
- No AI
- No save manager
- No scene modification
- No prefabs

## 6. Future Rules

- R1 Player Foundation must add tests with each runtime implementation.
- Every domain implementation should have EditMode tests for pure logic.
- PlayMode tests should be introduced only when scene/runtime composition exists.
