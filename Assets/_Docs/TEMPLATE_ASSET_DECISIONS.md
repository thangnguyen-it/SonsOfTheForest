# Template Asset Decisions

This audit records the disposition of the remaining Unity HDRP template files after the R0-A foundation commit.

## 1. Required Foundation Assets

| Path | Decision | Reason | Committed in this task |
| --- | --- | --- | --- |
| `Assets/Settings.meta` | `REQUIRED_FOUNDATION` | Folder metadata for the required HDRP settings tree; it preserves the folder GUID used by Unity. | Yes |
| `Assets/Settings/**` | `REQUIRED_FOUNDATION` | `QualitySettings.asset` directly references the Balanced, High Fidelity, and Performant HDRP assets. `GraphicsSettings.asset` directly references `HDRenderPipelineAsset` and `HDRenderPipelineGlobalSettings`; the global settings asset depends on the default LookDev and volume profiles. `SkyandFogSettingsProfile` is a dependency of the temporarily required build scene. | Yes |
| `Assets/InputSystem_Actions.inputactions` | `REQUIRED_FOUNDATION` | Registered as `com.unity.input.settings.actions` in `ProjectSettings/EditorBuildSettings.asset`; it is the configured default Input System action asset. | Yes |
| `Assets/InputSystem_Actions.inputactions.meta` | `REQUIRED_FOUNDATION` | Supplies GUID `35845fe01580c41289b024647b1d1c53`, which `EditorBuildSettings.asset` references. | Yes |
| `Assets/OutdoorsScene.unity` | `REQUIRED_FOUNDATION` | Transitional exception: it is currently the active scene, the sole enabled scene in `EditorBuildSettings.asset`, and `ProjectSettings.asset` names it as the template default scene. Omitting it would leave the committed project configuration pointing to a missing build scene. It is not approved as the long-term game scene. | Yes |
| `Assets/OutdoorsScene.unity.meta` | `REQUIRED_FOUNDATION` | Supplies GUID `8124e5870f4fd4c779e7a5f994e84ad1`, which `EditorBuildSettings.asset` references. | Yes |

## 2. Template Reference Only

| Path | Decision | Reason | Committed in this task |
| --- | --- | --- | --- |
| `.vsconfig` | `TEMPLATE_REFERENCE_ONLY` | Optional Visual Studio workload recommendation; Unity project configuration and builds do not depend on it. | No |

## 3. Discard Later

| Path | Decision | Reason | Committed in this task |
| --- | --- | --- | --- |
| `Assets/Readme.asset` | `DISCARD_LATER` | HDRP template welcome/readme content, not part of the rebuild foundation. | No |
| `Assets/Readme.asset.meta` | `DISCARD_LATER` | Metadata belonging only to the template readme asset. | No |
| `Assets/TutorialInfo/` | `DISCARD_LATER` | Template tutorial icons, layout, and readme editor scripts; no runtime or project setting depends on them. | No |
| `Assets/TutorialInfo.meta` | `DISCARD_LATER` | Metadata belonging only to the template tutorial folder. | No |

## 4. Unknown / Needs Human Decision

None. All listed template assets had sufficient serialized-reference and AssetDatabase dependency evidence for classification.

## Foundation rules

- HDRP settings required by `ProjectSettings` must be committed.
- `TutorialInfo` and Readme assets should not be part of the long-term game foundation unless explicitly approved.
- The rebuild should eventually use an official scene under `Assets/_Game/Scenes/`.
- Do not use the template `OutdoorsScene` as the long-term gameplay scene.
