# Contracts Foundation

## 1. Purpose

R0-B establishes stable contracts before gameplay implementation. These types define domain language, dependency boundaries, identifiers, results, and integration seams without choosing concrete runtime systems.

## 2. Namespace Rules

- All new code uses `SonsOfTheForest.*`.
- The old namespace `TheForest` is forbidden in the rebuild.

## 3. Contract Groups

- Core result and stable IDs
- Event contracts
- Interaction contracts
- Item contracts
- Inventory contracts
- Survival contracts
- Persistence contracts

## 4. Dependency Rules

- Core depends on nothing.
- Interaction may depend on Core and UnityEngine.
- Items depend on Core.
- Inventory depends on Core and Items.
- Survival depends on Core and UnityEngine only for numeric helpers if needed.
- Persistence depends on Core.
- No domain should depend on UI.
- No domain should directly depend on concrete scene objects.
- No domain should call Unity `FindObjectOfType` as part of core logic.

## 5. What R0-B Does NOT Do

- No player movement
- No inventory implementation
- No item assets
- No interaction raycaster
- No save manager
- No UI
- No gameplay scene population

## 6. Next Step

R0-C should create bootstrap/service contracts and validation helpers, still avoiding gameplay implementation.
