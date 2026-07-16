# Old Project Migration Audit

## 1. Purpose

The old project is reference material only. This audit decides what may be reused conceptually, what data shape may be migrated deliberately, and what must be rewritten against the new SonsOfTheForest contracts.

No old script, asset, scene, prefab, Resources folder, or Netcode setup was copied into the rebuild.

## 2. Source

- Repository: https://github.com/thangnguyen-it/TheForest
- Local read-only source: E:/knee_project/TheForest_Unity
- The requested E:/knee_project/TheForest path was absent; the sibling clone above contains the expected Assets/Scripts tree and matching remote.
- Branch: master
- Commit: 7d8739ef6d9a34f6945672e3b29e74293579a1fb (7d8739e, Scripts only)
- Audit time: 2026-07-16T11:39:14+07:00
- Scope: 197 C# files under Assets/Scripts. Old serialized scenes, prefabs, Resources assets, animations, and Inspector wiring were not opened or imported.

| Old folder | Files | Old folder | Files |
|---|---:|---|---:|
| AI | 30 | Building | 51 |
| Data | 18 | Editor | 5 |
| Emotes | 4 | Events | 13 |
| FSM | 4 | Interaction | 6 |
| Items | 6 | Multiplayer | 7 |
| Navigation | 2 | Persistence | 4 |
| Player | 20 | UI | 7 |
| World | 20 | **Total** | **197** |

## 3. High-Level Finding

The old project contains valuable gameplay rules, state names, thresholds, data categories, and feature-decomposition ideas. It is also prototype-heavy. Many classes combine domain state, Unity lifecycle, input, physics, presentation, scene lookup, Resources loading, persistence, and Netcode authority.

Old scripts must not be copied wholesale. Migration should be feature-by-feature: preserve approved behavior as tests and pure domain logic, then add Unity, presentation, persistence, and networking adapters around it.

The strongest reference material is the survival causal model; item IDs and stack rules; interaction focus/prompt flow; campfire, cooking, water, drying, and storage state machines; building topology vocabulary; AI perception/aggression ideas; companion state ideas; and versioned snapshot/file-backup concepts.

## 4. Domain Summary Table

| Domain | Valuable Old Systems | Main Risk | Migration Strategy |
|---|---|---|---|
| Player | movement states, crouch, stamina gating, ordered damage | input, motor, survival, noise, combat, AI, and UI mixed in components | rewrite player state and thin adapters first |
| Survival | energy-limited stamina, cold/wet/frost, hunger/thirst causality | one MonoBehaviour owns simulation, death, difficulty, items, and HUD events | split pure policies and publish state changes |
| Interaction | focus lifecycle, prompt changes, press/hold | raycaster owns stealth, chopping, input, and HUD strings | rewrite adapter around new interaction contracts |
| Items / Inventory | stable IDs, stacking, overflow, recipes | ScriptableObject identity and Resources paths in runtime | port schemas; use ItemId and ItemStack |
| Cooking / Processing | explicit fuel/cook/burn/boil/dry/water states | interaction, visuals, network, and persistence fused | pure state machines plus separate adapters |
| Building | costs, snapping, support graph, topology, repairs | giant scene root, static bus, physics and prefab coupling | data first; postpone runtime until R8 |
| World | clock, seasons, weather, water, spawn budgets | singletons, scene searches, nondeterministic Update | tickable services with injected queries/RNG |
| AI | perception, noise, aggression, fear, roles, groups | monolithic Update/coroutine FSMs | perception -> memory -> decision -> action layers |
| Companion | commands, memory, revive/permadeath, event-driven intent | FSM owns navigation, stats, gather, save, animation | pure state/command model and adapters |
| Persistence / Multiplayer | versions, backup, snapshots, host authority | global scene scans and direct domain restoration | IPersistentObject snapshots; network only in R10 |
| UI / Tooling | observer intent and authoring references | direct scene components and asset/scene mutation | rewrite UI; discard old seed/setup automation |

## 5. Script Classification Table

Decision legend: KEEP_IDEA, REFACTOR_LATER, PORT_DATA_ONLY, REWRITE_CLEAN, DISCARD, UNKNOWN.

The Reason column records dependencies and coupling risks. The Future Target column records the intended new domain and relevant existing contracts.

| Old Path(s) | Class(es) | Old Namespace | Domain | Decision | Reason / Coupling | Future Target / Existing Contracts |
|---|---|---|---|---|---|---|
| Player/PlayerController.cs | PlayerController | TheForest.Player | Player | REWRITE_CLEAN | InputSystem, CharacterController, SurvivalStats, block, stealth and NoiseSystem mixed in Update | Gameplay/Player/Movement; future input/motor contracts, ITickableGameSystem |
| Player/PlayerLook.cs | PlayerLook | TheForest.Player | Camera | KEEP_IDEA | useful yaw/pitch clamp; cursor and camera Transform are adapter concerns | Gameplay/Player/Camera plus Presentation/Input |
| Player/SurvivalStats.cs | SurvivalStats | TheForest.Player | Survival | KEEP_IDEA | valuable causal rules; Time, damage, death, difficulty singleton, ItemData and HUD events mixed | Gameplay/Survival; StatValue, ISurvivalStatsReader, ITickableGameSystem, events |
| Player/EquipmentController.cs; EquipHotkeys.cs | EquipmentController; EquipHotkeys | TheForest.Player | Equipment | REWRITE_CLEAN | Inventory/ItemData identity, input, prefab spawning, animator and pose coupling | Gameplay/Equipment; ItemId and IInventoryReader; presentation adapter |
| Player/WeaponSwinger.cs; PlayerBlock.cs; PlayerDamageReceiver.cs | WeaponSwinger; PlayerBlock; PlayerDamageReceiver | TheForest.Player | Combat | KEEP_IDEA | swing/block/damage ordering is useful; physics, VFX, NavMesh, CannibalAI and UI are direct | Gameplay/Combat pure resolvers and typed events |
| Player/BowController.cs; FirearmController.cs | BowController; FirearmController | TheForest.Player | Ranged combat | REWRITE_CLEAN | Resources ammo scans, input, physics/projectiles, inventory, audio | Gameplay/Combat/Ranged after R3 |
| Player/PlayerLogCarry.cs | PlayerLogCarry | TheForest.Player | Carry | REFACTOR_LATER | two-world-log rule useful; Camera.main, keyboard fallback, Rigidbody and building types | Gameplay/Player/Carry after building ownership |
| Player/StealthKillController.cs; PlayerMudCamo.cs | StealthKillController; PlayerMudCamo | TheForest.Player | Stealth | REFACTOR_LATER | useful eligibility/noise modifiers; physics, AI execution and global noise coupled | Gameplay/Player/Stealth and Combat after perception |
| Player/PlayerWarpaint.cs | PlayerWarpaint | TheForest.Player | Cosmetic | DISCARD | no verified foundation behavior; old comments reduce it to cosmetics | Presentation/Cosmetics only if approved |
| Interaction/IInteractable.cs | IInteractable | TheForest.Interaction | Interaction | DISCARD | superseded; old API passes GameObject and raw strings | existing new IInteractable, InteractionContext/Result |
| Interaction/IHoldInteractable.cs | IHoldInteractable | TheForest.Interaction | Interaction | KEEP_IDEA | opt-in hold semantics are useful but need no parallel old interface | InteractionInputKind and InteractionPrompt.Hold |
| Interaction/InteractionRaycaster.cs | InteractionRaycaster | TheForest.Interaction | Interaction | REWRITE_CLEAN | good focus flow; owns input, stealth priority, auto-swing, physics and HUD text | Gameplay/Interaction runtime adapter |
| Interaction/TestInteractable.cs | TestInteractable | TheForest.Interaction | Demo | DISCARD | prototype fixture implementing obsolete contract | new tests/fixtures only |
| Items/ItemData.cs and Data/*ItemData.cs | ItemData and variants | TheForest.Items | Item data | PORT_DATA_ONLY | ID/name/category/stack useful; presentation, equip, block and combat fields overloaded | Gameplay/Items definitions implementing IItemDefinition, split by concern |
| Player/Inventory.cs | Inventory | TheForest.Player | Inventory | KEEP_IDEA | stack/overflow/count/remove useful; MonoBehaviour holds SO references and calls equipment | pure Gameplay/Inventory using ItemId, ItemStack, reader/writer contracts |
| Items/ItemPickup.cs; WorldConsumable.cs | ItemPickup; WorldConsumable | TheForest.Items | Items/interaction | REWRITE_CLEAN | direct GetComponent, SurvivalStats/Inventory and Destroy | new IInteractable adapters with GameResult |
| Building/CraftingRecipe.cs | CraftingRecipe | TheForest.Crafting | Crafting data | PORT_DATA_ONLY | ingredient amount/consume/result schema useful | Gameplay/Crafting definitions keyed by ItemId |
| Building/CraftingSystem.cs | CraftingSystem | TheForest.Crafting | Crafting | KEEP_IDEA | exact-match logic useful; Resources, ItemData dictionary, UI events and MonoBehaviour | pure crafting service using inventory contracts |
| World/CampfireController.cs | CampfireController | TheForest.World | Campfire | REWRITE_CLEAN | strong state rules; NetworkBehaviour owns inventory, carry, visuals, audio, RPC, Resources and JSON | Campfire state/fuel/cooking models; IInteractable, ITickableGameSystem, IPersistentObject |
| World/CookingPotController.cs | CookingPotController | TheForest.World | Cooking/water | REWRITE_CLEAN | useful water/boil states; physics, singleton weather, stats, visual, RPC and persistence fused | Gameplay/Cooking/Water models plus adapters |
| Building/DryingRack.cs; RainCatcher.cs; StorageContainer.cs | DryingRack; RainCatcher; StorageContainer | TheForest.Building | Processing/storage | KEEP_IDEA | slot/capacity/time rules useful; Netcode, Resources, inventory, visuals and persistence mixed | Processing/Water/Storage models using ItemId and IPersistentObject |
| Building/BuildingController.cs | BuildingController | TheForest.Building | Building composition | REWRITE_CLEAN | 15 scene subsystem refs, execution order, static EventBus, scene scans and prefab restore | later Infrastructure/Building composition through SceneBootstrap/services |
| Building/FreeformPlacementSystem.cs | FreeformPlacementSystem | TheForest.Building.Systems | Placement | REWRITE_CLEAN | input, physics, ghosts, inventory/log carry and event bus fused | pure placement request/result plus physics/presentation adapters |
| Building/PlacementIndicatorController.cs | PlacementIndicatorController | TheForest.Building.Systems | Placement UI | REFACTOR_LATER | snap search and indicators are physics/presentation concerns | Presentation/Building after placement domain |
| Building/BlueprintSystem.cs | BlueprintSystem | TheForest.Building.Systems | Blueprints | REWRITE_CLEAN | input, guide state, ghosts, physics, scene queries and Kelvin hooks | Building/Blueprints; data then runtime |
| Building/MaterialDatabase.cs and *Config.cs | MaterialDatabase and configs | TheForest.Building.Data/Config | Building data | PORT_DATA_ONLY | IDs, costs and tuning useful; static Active and prefab ownership must go | Building/Definitions with stable IDs |
| Building/StructuralDependencyGraph.cs | StructuralDependencyGraph | TheForest.Building.Systems | Structure | KEEP_IDEA | support/dependent/strut graph useful; currently GameObject and physics driven | pure graph keyed by PersistentId |
| Building/WallBuilder.cs; RoofBuilder.cs | WallBuilder; RoofBuilder | TheForest.Building.Systems | Structure rules | KEEP_IDEA | topology/shelter recognition useful; static scene events currently drive it | pure building topology rules |
| Building/DoorWindowSystem.cs; ElectricitySystem.cs; TarpRopeSystem.cs | named systems | global namespace | Building features | REFACTOR_LATER | valuable later features; physics, prefabs and runtime objects dominate | Building/Openings, Electricity, Ropes after core R8 |
| Building/RepairDismantleSystem.cs; StoneBuildingSystem.cs | named systems | TheForest.Building.Systems | Building lifecycle | KEEP_IDEA | repair/refund/grid rules useful; input, physics, inventory and bus coupled | Building/Maintenance and Stone after persistence |
| Building/KelvinBuildingCommands.cs | KelvinBuildingCommands | TheForest.Building.Systems | Companion/building | REWRITE_CLEAN | Find, NavMesh, coroutines, physics, inventory and building events cross domains | Companion/Building adapter after R8/R9 |
| Building/WorldLog.cs; LogHolder.cs | WorldLog; LogHolder | TheForest.Building | Building resources | REFACTOR_LATER | world-log/capacity concepts needed; old interaction, physics, Find and visuals | Building/Resources and Storage |
| Building/LogSled.cs | LogSled | global namespace | Transport | DISCARD | small duplicate prototype with weak ownership | redesign only if R8 requires |
| Building/TurtleShellSled.cs; AnimalTrap.cs; RabbitCage.cs; BedController.cs | named classes | TheForest.Building | World/building | REFACTOR_LATER | feature rules useful; input/physics/animals/sleep/save are cross-domain | later Vehicles, Traps, Husbandry, Sleep |
| World/DayNightCycle.cs | DayNightCycle | TheForest.World | World time | KEEP_IDEA | clock/rollover useful; sunlight mutation belongs to presentation | World/Time service implementing ITickableGameSystem |
| World/SeasonSystem.cs; WeatherSystem.cs | SeasonSystem; WeatherSystem | TheForest.World | World simulation | KEEP_IDEA | modifiers/phases useful; singleton, Find and random Update must go | deterministic services with injected clock/RNG and events |
| World/NaturalWaterSource.cs | NaturalWaterSource | TheForest.World | Water | REWRITE_CLEAN | dirty/clean/frozen rules useful; direct stats and season scene lookup | World/Water with interaction adapter |
| World/TreeCutting.cs | TreeCutting | TheForest.World | Harvesting | REFACTOR_LATER | chop/fell/drop idea useful after combat/items/persistence | World/Harvesting |
| AI/FishSpawner.cs; AnimalSpawner.cs | FishSpawner; AnimalSpawner | TheForest.AI | Spawning | REWRITE_CLEAN | budgets/weights useful; scene counts, NavMesh and prefab spawning nondeterministic | AI/Spawning with explicit query/RNG |
| World/WorldSurvivalBootstrap.cs | WorldSurvivalBootstrap | TheForest.World | Bootstrap | DISCARD | RuntimeInitialize creates manager objects through Find and DontDestroyOnLoad | existing SceneBootstrap contracts |
| World/DifficultyDatabase.cs | DifficultyDatabase | TheForest.World | Difficulty data | PORT_DATA_ONLY | profile catalog useful; values require approval | Gameplay/Difficulty definitions |
| AI/CannibalAI.cs | CannibalAI | TheForest.AI | Enemy AI | REWRITE_CLEAN | 860-line perception/decision/NavMesh/combat/corpse/group/animation monolith | AI Perception, Memory, Decisions and Actions |
| AI/CannibalHealth.cs; CannibalAttack.cs | named classes | TheForest.AI | Enemy combat | REFACTOR_LATER | health/attack ideas useful; animation, projectiles and SurvivalStats direct | AI/Combat after damage contracts |
| AI/AnimalAI.cs; AnimalHealth.cs; FishAI.cs | named classes | TheForest.AI | Animal AI | REWRITE_CLEAN | behavior vocabulary useful; player Find, NavMesh, Resources and loot coupled | shared AI perception/action architecture |
| AI/NoiseSystem.cs | NoiseSystem | TheForest.AI | Perception | KEEP_IDEA | position/loudness fact is a strong seam | typed IGameEvent via IEventPublisher |
| AI/FireZone.cs | FireZone | TheForest.AI | Hazard | REFACTOR_LATER | periodic hazard useful; physics and concrete health types are adapters | World/Hazards plus damage command |
| AI/AggressionManager.cs | AggressionManager | TheForest.AI | AI world state | KEEP_IDEA | zone aggression/decay/unlock useful; singleton/channel implementation not | persistent tickable AI/Aggression service |
| FSM/CompanionFSM.cs | CompanionFSM | Companion.FSM | Companion | REWRITE_CLEAN | good event-driven states/memory/revive; owns NavMesh, gather, stats, save and time | pure Companion state machine plus adapters |
| FSM/CompanionCommandRouter.cs | CompanionCommandRouter | Companion.FSM | Commands | KEEP_IDEA | narrow command entry is useful; replace SO channel | Companion commands via IEventPublisher |
| FSM/CompanionSaveData.cs; CompanionStatRuntime.cs | named classes | Companion.FSM | Companion data/stats | PORT_DATA_ONLY | snapshot fields and clamp/decay ideas useful; formal identity/version needed | Companion pure stats and PersistenceSnapshot |
| Emotes/CompanionAnimatorBridge.cs | CompanionAnimatorBridge | Companion.FSM | Presentation | REFACTOR_LATER | good bridge intent; Animator/NavMesh translation is presentation | Presentation/Companion |
| AI/VirginiaAI.cs; VirginiaHealth.cs | named classes | TheForest.AI | Companion AI | REWRITE_CLEAN | bonding/revive ideas useful; singleton registry, Find, NavMesh, physics and interaction fused | shared companion/perception foundations |
| Data/Cmd_*.cs | command definitions | Companion.Data | Companion data | PORT_DATA_ONLY | command vocabulary useful; use stable command IDs/payloads | Companion/Commands definitions |
| Persistence/SaveGameManager.cs | SaveGameManager | TheForest.Persistence | Persistence | REWRITE_CLEAN | host rules useful; singleton scans/loads scenes, Resources, restores every domain | persistence coordinator using IPersistentObject and services |
| Persistence/SaveFileStore.cs | SaveFileStore | TheForest.Persistence | File storage | KEEP_IDEA | temp write, backup, fallback, version and slot sanitation valuable | Infrastructure/Persistence file-store interface |
| Persistence/SaveGameData.cs | save DTOs | TheForest.Persistence | Persistence data | PORT_DATA_ONLY | domain categories useful; replace monolithic schema with versioned snapshots | per-domain PersistenceSnapshot |
| Persistence/IPersistentStateParticipant.cs | old interface/helper | TheForest.Persistence | Persistence | DISCARD | superseded; hierarchy/sibling-index hash is unstable | IPersistentObject, PersistentId, PersistenceSnapshot |
| Multiplayer/NetworkWorldInteraction.cs; NetworkWorldObjectState.cs | named classes | TheForest.Multiplayer | Multiplayer/world | REWRITE_CLEAN | authority/snapshot ideas useful; Netcode directly wraps old participants | R10 adapters around PersistenceSnapshot |
| Multiplayer/NetworkPlayerStateSync.cs; NetworkPlayerOwnership.cs | named classes | TheForest.Multiplayer | Multiplayer/player | REWRITE_CLEAN | concrete old vitals/inventory/Resources/input ownership coupled | R10 replication and presentation adapters |
| Multiplayer/CoopSessionManager.cs | CoopSessionManager | TheForest.Multiplayer | Session | REWRITE_CLEAN | lifecycle/approval/identity ideas useful; singleton auto-bootstrap/prefab setup not | Infrastructure/Multiplayer service in R10 |
| UI/HUDManager.cs; InventoryUI.cs | named classes | TheForest.UI | UI | REWRITE_CLEAN | concrete domain components and input/slot construction coupled | Presentation read models/events after domains |
| UI/StatBar.cs; BurningHUD.cs | named classes | TheForest.UI | UI | REFACTOR_LATER | small visual behavior only; Find/direct stats subscription must go | Presentation/Common/HUD |
| Editor/MultiplayerProjectSetup.cs; SonsOfTheForestDataSeeder.cs; WorldSurvivalSceneSetup.cs | named tools | TheForest.EditorTools | Setup | DISCARD | mutate old project data, prefabs, network components and scenes | no migration; future validated tooling task only |
| Editor/ScatterManagerEditor.cs | ScatterManagerEditor | TheForest.EditorTools | Tooling | REFACTOR_LATER | may be reconsidered after world-authoring workflow exists | Editor/World |
| Editor/TexturePackerEditor.cs | TexturePackerEditor | global namespace | Utility | DISCARD | unrelated generic prototype utility | external art pipeline if justified |

## 6. Deep Audit Notes

### Player / Survival

Preserve hunger/thirst decay; starvation/dehydration damage; wetness-adjusted temperature; delayed frost damage while cold and still; hunger/cold energy drain; energy-limited stamina; sprint cost; timed burning/extinguish; and explicit food, drink, medicine, sleep, damage and death commands.

Split SurvivalStats into pure stat storage, hunger/thirst policy, thermal exposure, energy/stamina policy, health/status effects, difficulty modifiers, persistence adapter, and presentation events. The pure model should receive GameTickContext plus explicit activity/environment inputs and expose StatValue through ISurvivalStatsReader. HUD and grey-zone effects subscribe through events only.

R1 should migrate only move/look/sprint/crouch/jump intent and motor/camera mapping. Combat, inventory, AI noise, and survival drain must stay out of the first controller.

### Interaction

Preserve camera-forward acquisition, target changes, focus gain/loss, prompt updates, press/hold intent, and eligibility checks at execution. A future adapter builds InteractionContext, calls the new IInteractable, uses IFocusableInteractable, and returns InteractionResult.

Do not copy stealth-kill priority, chopping auto-swing, Camera.main fallback, raw HUD strings, GetComponentInParent domain discovery, or InputSystem callbacks into domain logic.

### Items / Inventory / Crafting

Preserve stable string identity, display/category/max stack, stack fill/overflow/count/remove, exact recipe matching, and non-consumed tools. Later ScriptableObject definitions may implement IItemDefinition, but identity, presentation, consumable, equipment, combat, and recipe data should be separate concerns.

Runtime inventory stores ItemId and ItemStack, not ScriptableObject references. Pickups and uses call IInventoryWriter/survival contracts and return InteractionResult. Avoid Resources.LoadAll, object-reference identity, and UI-owned craft state.

### Campfire / Cooking / Water

Preserve campfire construction, held ignition, lit/reinforced state, explicit fuel, cooking slots, raw/cooked/burnt thresholds, extinguish policy, pot empty/dirty/clean state, servings, dirty-water consequence, heat-gated boiling, and distinct rain/natural sources.

Split into campfire/fuel, cooking slots/process, water container/boiling, interaction, inventory/effects, world queries, visuals/audio, persistence, and later network adapters. Pure processes may implement ITickableGameSystem; world objects later expose IPersistentObject. Do not copy NetworkBehaviour/RPC, Resources, direct Stats/Inventory, or visual fields.

### Building

Valuable ideas are blueprint costs/progress, placement and snapping, stable material/piece IDs, support graph and struts, wall/roof/shelter topology, openings, damage/repair/dismantle, and modular storage/electricity/rope/companion tasks.

BuildingController is too scene-wired: it is composition root, event relay, diagnostics, API, persistence coordinator, prefab spawner, and validator. Future R8 order should be data IDs/costs -> pure placement -> physics adapter -> persistent piece identity -> structural graph -> log integration -> blueprints -> maintenance -> topology -> optional electricity/ropes/traps/Kelvin. Nothing should migrate before player, interaction, inventory, survival and persistence stabilize.

### World Simulation

Day, season, and weather become separate tickable services. Inject clock and deterministic RNG. Sun/sky visuals consume world state in Presentation. Water, trees, and spawners use explicit queries/events. Avoid singletons, runtime manager creation, Find, scene counts, and implicit Update order.

### AI

CannibalAI offers useful perception, patrol/investigate/chase/attack/fear/stun/knockdown/mourn, aggression, group surround, tribe/role, ally revive, corpse and climbing ideas. Rewrite as perception facts -> memory/blackboard -> utility/decision policy -> navigation/animation/combat actions -> group coordination -> persistence/spawning.

Noise should be a typed IGameEvent. AI decision code must not reference PlayerMudCamo, SurvivalStats, tags, global registries or Animator triggers. No enemy AI belongs in R1.

### Companion

Keep command-driven transitions, one-time terminal death, revive window, selective memory, nonzero low-energy movement, save state, and animation-bridge intent. Rewrite pure state/command logic; navigation, gathering, animation, resources, health, and persistence are adapters. Virginia and Kelvin share foundations but retain character policies. Companion waits until player, interaction, inventory, persistence and basic perception exist.

### Persistence / Multiplayer

Preserve participant identity, per-domain snapshots, versions/upgrades, temp write and backup, fallback read, host-authoritative world saves, and separate profiles/world state. Map to PersistentId, PersistenceScope, ContractVersion, PersistenceSnapshot, IPersistentObject, and GameResult.

Do not copy SaveGameManager.Instance, runtime auto-creation, scene loading/scans, Resources lookup, direct building spawn, or Netcode checks into persistence core. Multiplayer is adapter-only in R10; domains must work offline.

### UI / Presentation

Observer intent is useful, but old UI must be rewritten against read models/events. It must never depend on concrete domain scene components. Cursor and input-mode ownership need a presentation/input service.

### Editor / Tooling

Old seeders and setup scripts are explicit non-migration items because they create old Resources data, prefabs, network components and placeholder scene objects. Small tools require a separate approved authoring workflow.

## 7. First Migration Recommendation

R1 should start with Player Foundation:

1. define player input intent and motor/read-state boundaries;
2. test camera yaw/pitch and movement-state mapping without inventory, combat, AI, or survival ownership;
3. add a thin chosen motor adapter after pure tests;
4. add the interaction raycast adapter later when actor/camera context is stable;
5. add item definitions and inventory only after player foundation is stable.

Do not start with building, campfire, cooking, companion, AI, or multiplayer.

## 8. Explicit Non-Migration List

Do not copy the old OutdoorsScene, Player prefab, HUD_Canvas, BuildingSystem scene object, whole Scripts folder, whole Resources folder, old Netcode setup, editor seeder/setup scripts, runtime-created managers, old namespace/assembly layout, or hierarchy-derived persistent IDs.

## 9. Proposed Migration Order

1. R1 Player Foundation
2. R2 Interaction Runtime
3. R3 Item Data + Inventory Runtime
4. R4 Survival Runtime
5. R5 Persistence Runtime
6. R6 Basic AI Perception
7. R7 Campfire/Cooking
8. R8 Building
9. R9 Companion
10. R10 Multiplayer adapters

Each phase should add pure EditMode tests before scene composition or presentation.

## 10. Risks and Open Questions

- The exact requested folder was absent; this audit used the matching sibling clone at the recorded commit.
- This was script-only; old Inspector references, scenes, prefabs, Resources assets, animations, and balance data were not validated.
- Comments claiming design fidelity and all numeric values require human/GDD approval.
- R1 needs decisions on motor technology, input/camera ownership, and multiplayer-independent player identity.
- Item definition boundaries require approval before any ScriptableObject assets.
- Persistent IDs must survive hierarchy and prefab changes.
- Save format, integrity, async I/O, slot policy, and platform constraints remain open.
- World clocks/RNG need deterministic rules before multiplayer.
- AI perception, navigation, authoring, and behavior priority remain open.
- Building needs a product-approved minimum slice.
- Multiplayer authority/prediction/reconnect must not shape domains prematurely.
