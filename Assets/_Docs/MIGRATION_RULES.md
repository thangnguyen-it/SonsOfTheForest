# Migration Rules

The old project is reference only.

## Prohibited bulk migration

- Do not copy the old `Scripts` folder wholesale.
- Do not copy the old scene.
- Do not copy the old Player prefab.
- Do not copy the old HUD Canvas.
- Do not copy the old BuildingSystem object.
- Do not copy the old `Resources` folder wholesale.

## Feature review

Every migrated feature must be reviewed by domain and assigned one outcome:

- Keep idea
- Rewrite clean
- Port data only
- Discard

Migration must happen feature by feature, not folder by folder.
