# Magical Worlds — Milestone 7: Journey Mode MVP

**Status:** Implemented and Unity Editor verified, 2026-10-16.  
**Project:** `BlockBlastClone`, Unity 6.3 LTS, 1080 × 1920 portrait.

## Core functionality

- Journey Map: six authored numbered level nodes with lock/unlock status and stars.
- Level 1 is unlocked by default; completing a level unlocks the next level.
- Each level is a separate editable `JourneyLevelDefinition` ScriptableObject with
  level title, move limit, objective, score target, line-clear target and deterministic seed.
- Goal types: ReachScore, ClearLines, ScoreAndLines.
- JourneyGameplay reuses the existing Classic board, three-piece hand, placement
  validation, scoring and line clearing. The Journey session wraps the Classic model.
- Journey has limited moves, objective progress, winning and losing result panels,
  1–3 stars, Next Level, Retry, Return to Map.
- Win takes precedence if the objective is met on the last permitted move.
- The no-legal-move result is distinct from an out-of-moves result.
- Journey progression uses its own PlayerPrefs keys, leaving the Classic saved
  session untouched.
- Optional `WATCH AD: +3 MOVES (SOON)` is visibly disabled; no advertisement
  or reward can be issued until a verified rewarded-ad provider is integrated.
  The option is hidden on no-space losses, where extra moves cannot help.

## Editable hierarchy and UI

- `Assets/Scenes/JourneyMap.unity`
- `Assets/Scenes/JourneyGameplay.unity`
- `Assets/MagicalWorlds/Prefabs/Journey/JourneyMapRoot.prefab`
- `Assets/MagicalWorlds/Prefabs/Journey/JourneyLevelNode.prefab`
- `Assets/MagicalWorlds/Prefabs/Journey/JourneyGameplayRoot.prefab`
- `Assets/MagicalWorlds/Prefabs/Classic/ClassicGame.prefab` (shared, not replaced)
- `Assets/Resources/MagicalWorlds/Journey/LevelCatalog.asset`
- `Assets/Resources/MagicalWorlds/Journey/Level_01.asset` through `Level_06.asset`

On the map, expand `JourneyMapRoot / Journey Map Canvas / 03 - Level Path`
to edit individual level nodes. Locked nodes still show their level numbers.

On gameplay, expand `JourneyGameplayRoot / Journey HUD Canvas`:
- `01 - Level Objective and Moves` includes editable title, objective,
  progress track, remaining moves and Map button.
- `02 - Result Overlay` includes win/loss text, stars, Next, Retry,
  Map and disabled rewarded continue.
- `Classic Board - Shared Rules` is the nested reusable Classic prefab.

Journey Canvas sorting order = 80; Classic Canvas = 50.
`02 - Header and Score` on the nested Classic Canvas is hidden in
Journey only. All board, piece, Pause and shared reference components remain
intact. The Journey header hides while Pause/How-to-Play is open and
returns when Continue is pressed.

The existing approved fantasy environment background remains in use.
Further visual matching to the locked reference screens is deferred.

## Level authoring

Open a `Level_XX.asset` and edit `moveLimit`, `goal`,
`targetScore`, `targetLines`, `randomSeed`, or `levelName`.
Catalog order and the `levelNumber` field must remain aligned; map nodes
1–6 use explicit `JourneyLevelNode.LevelNumber`.

The initial six levels are demonstration content. Their final difficulty,
star thresholds, and RNG fairness require player playtesting and balancing.

## Validation performed

- Unity Editor compiled with no new C# errors.
- Journey Data QA passed: objective validity, six unique level nodes,
  move-limit failure, no-space failure, win priority and optional move extension.
- Journey Presentation QA passed: both canvases, correct sorting, authored
  header suppression, serialized header reference, Classic UI reference integrity.
- Journey end-to-end Play Mode smoke test passed using actual authored button
  callbacks:
  - Map Level 1 → gameplay.
  - Pause → Journey header hidden; Continue → header restored.
  - Complete Level 1 objective → stars saved, Level 2 unlocked, victory.
  - Next Level button → Level 2.
  - No-space loss → loss panel; rewarded +3 option not interactable.
  - Retry button → Level 2 restarted.
  - Return to Map button → Journey Map.
  - Previous Journey PlayerPrefs values restored after test.

To rerun: open `JourneyMap.unity` in Edit Mode and use
**Tools → Magical Worlds → Journey → Run Gameplay Smoke Test**
(or **Ctrl+Alt+J**).

## Production considerations and deferred work

- Rewarded ad integration requires a real ad SDK and a verified callback.
- Player level attempts are not resumed after leaving a level; only completed
  levels and stars persist. Consider a leave-confirmation dialog or attempt save.
- Manual interaction, small-screen safe-area layout, frame-time profiling and
  a physical Android/iOS QA pass are still required.
- Star glyph rendering on all chosen device fonts and final art matching
  remain to be visually tested.
- The automated smoke test uses programmatic button invocation; it does not
  substitute for testing physical taps and drag input on hardware.
- Only the first six authored levels exist. Expansion to a larger Journey,
  economic rewards and progression tuning are separate milestones.

## Files changed in this polish pass

- `Assets/Scripts/JourneyLevelNode.cs`
- `Assets/Scripts/JourneySession.cs`
- `Assets/Scripts/BlockBlastGameFlow.cs`
- `Assets/Editor/JourneyGameplayEditor.cs`
- `Assets/Editor/JourneyPresentationUpgrade.cs`
- `Assets/Editor/JourneyPresentationQA.cs`
- `Assets/Editor/JourneyRuntimeQA.cs`
- `Assets/MagicalWorlds/Prefabs/Journey/JourneyGameplayRoot.prefab`

Backups created before modification:
`ProjectBackups/JourneyUIPolish_20261016/`.

