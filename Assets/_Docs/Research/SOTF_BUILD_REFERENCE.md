# Sons of the Forest Build Reference

Research date: 2026-07-16

Online source access time: 2026-07-16T08:26:32Z

## 1. Purpose

This reference locks locomotion evidence to an identifiable Sons of the Forest build. Every measured movement value must be tied to an exact game build; values without that identity cannot be treated as current-build direct evidence.

## 2. Evidence grading

### Grade A — Current-build direct evidence

Examples:

- controlled measurement performed on the locally installed build;
- locally observed runtime behavior with reproducible conditions;
- local Steam appmanifest identifying the installed build.

### Grade B — Current-build metadata evidence

Examples:

- current-build serialized/runtime metadata;
- executable/product metadata;
- trustworthy current depot/build metadata.

Build IDs from third-party trackers must include limitations and may not be treated as proof that the same build is installed locally.

### Grade C — Official product or patch evidence

Examples:

- official Steam store description;
- official Endnight/Steam patch notes;
- official release information.

Official descriptions may establish product goals or announced mechanics, but not hidden numerical values.

### Grade D — Controlled visual measurement

Examples:

- frame-by-frame gameplay video with known frame rate;
- known-distance traversal recording;
- controlled input and sensitivity recording.

### Grade E — Historical or community lead

Examples:

- old IL2CPP dump;
- old trainer/mod field access;
- community wiki;
- forum observation;
- uncontrolled gameplay clip.

Grade E is useful for discovering what to measure. Grade E must never lock a production default by itself.

### Grade applicability

- Grades A–E apply only to evidence concerning Sons of the Forest.
- A row with no evidence yet uses Grade `N/A`.
- `N/A` is not an evidence grade.
- Current-project contracts are design context, not SOTF evidence, and must not receive Grades A–E.

### Evidence Type vocabulary

Allowed Evidence Type values are:

- `Direct Measurement`;
- `Local Manifest`;
- `Current Metadata`;
- `Official Statement`;
- `Historical Dump`;
- `Historical Community Tool`;
- `Visual Observation`;
- `Engineering Hypothesis`;
- `Research Question`.

`Research Question` means that no current supporting evidence exists. The required future acquisition method belongs in `Next Verification`. A row cannot use `Direct Measurement` unless a measurement has actually been performed and recorded.

## 3. Local installed build

| Property | Result |
| --- | --- |
| Status | NOT FOUND |
| Local evidence status | BLOCKED |
| App ID sought | 1326470 |
| Manifest | `appmanifest_1326470.acf` was not present in any discovered Steam library |
| Build ID | Unknown |
| Beta branch | Unknown |
| Last updated UTC/local | Unknown |
| Install directory | Unknown |
| Executable/product version | Unknown |
| Relevant file timestamps | Not available; no installation was found |
| Selected SHA-256 hashes | Not available; no installation was found |

Read-only discovery checked these locations:

- `C:\Program Files (x86)\Steam\steamapps\appmanifest_1326470.acf`
- `D:\SteamLibrary\steamapps\appmanifest_1326470.acf`

Steam registry paths and `libraryfolders.vdf` were readable. They resolved only the Steam root and `D:\SteamLibrary`; neither contained the requested manifest. No game was launched, no process memory was read, and no Steam or game file was changed. Consequently, executable metadata, timestamps, hashes, branch, installation directory, and exact installed build cannot be established in R1-C0.

## 4. Public Steam identity

The [official Steam product page](https://store.steampowered.com/app/1326470/Sons_Of_The_Forest/) provides these official developer/store facts:

- App ID: 1326470;
- title: Sons Of The Forest;
- developer: Endnight Games Ltd;
- publisher: Newnight;
- full release date: 22 February 2024;
- Early Access release date: 23 February 2023;
- official description: open-world survival horror playable alone or with friends;
- official feature support: single-player and online co-op.

The same Steam page lists `First-Person` as a popular user-defined tag. That tag is store/user metadata, not an official developer statement. Store tags are useful experience-context evidence but do not provide hidden algorithms or numerical defaults.

The official facts above are Grade C product evidence. Neither those facts nor the user-defined tag establishes locomotion algorithms or numerical movement defaults.

## 5. Public build metadata

The fixed [SteamDB App 1326470 record](https://steamdb.info/app/1326470/patchnotes/) showed at access time:

- App ID 1326470, Windows game;
- detected technologies Unity Engine and UnityIL2CPP;
- last record update 10 July 2026 03:48:13 UTC;
- full controller support metadata;
- Steam Machine tested build ID `20228174`, test timestamp 29 June 2026 00:00:00 UTC.

This tracker metadata is Grade B only for the metadata shown. SteamDB is not Valve, the tested build is not proof of the current public branch, and it is not proof of a locally installed build. Technology detection does not identify the current locomotion implementation.

## 6. Historical dump reference

Historical structural lead:

- repository: [NeuralBinary/Sons-of-The-Forest-Dump](https://github.com/NeuralBinary/Sons-of-The-Forest-Dump);
- pinned commit: `d20b8ab1b7a28ce1e4a48f5dc74c1ecbcedea2cb`;
- commit message: `Update: 6-23-2023`;
- commit author timestamp: 2023-06-24T02:23:39Z;
- file: [`Sons/FirstPersonCharacter.cs`](https://github.com/NeuralBinary/Sons-of-The-Forest-Dump/blob/d20b8ab1b7a28ce1e4a48f5dc74c1ecbcedea2cb/Sons/FirstPersonCharacter.cs).

The commit is from the Early Access period and predates the 22 February 2024 full release. The IL2CPP dump exposes type, member, serialized-field, and native-address structure, but its C# method bodies are empty/default stubs. Serialized declarations show that fields existed; they do not reveal prefab-assigned numerical values or the native algorithm.

A pinned [uniref historical community-tool example](https://github.com/in1nit1t/uniref/blob/82b06a043d7495c6ff57018b693bfae45c384453/examples/windows/il2cpp/Sons%20Of%20The%20Forest.py) corroborates access to fields named `_runSpeed`, `_swimSpeed`, `crouchSpeed`, `_baseFallDamage`, `_fallDamagePower`, and `_fallDamageVelocity`. It modifies runtime values and does not reveal original values in its source. Both sources are Grade E, HISTORICAL, and unsuitable for current-build defaults.

## 7. Build identity rules

- Measurements from different build IDs cannot be silently combined.
- A new build invalidates unverified numerical assumptions.
- Every raw measurement row must contain `build_id`.
- An unknown build ID means the row cannot receive Grade A.
- BUILD-005 remains Status `BLOCKED` and Grade `N/A` while no local manifest exists; it becomes Grade A only after direct local-manifest evidence exists.
- Tracker build IDs and historical commit IDs must never be substituted for the local manifest build ID.

## 8. Current conclusion

The exact local Sons of the Forest build is unknown because no App 1326470 manifest or installation was found in the discovered Steam libraries. Current-build locomotion measurement must not proceed as Grade A until an installed build is identified and locked. Public identity, tracker metadata, historical structure, the measurement protocol, and empty analysis templates may proceed; executable verification and all current-build quantitative experiments remain blocked.
