# Magical Worlds — Milestone 8: Journey Attempt Persistence and First-Pass Balancing

**Status:** Implemented and verified in Unity Editor on October 16, 2026.  
**Project:** BlockBlastClone, Unity 6.3 LTS.  
**Scene left ready for review:** Assets/Scenes/JourneyMap.unity.

## What changed

### Resume unfinished Journey levels

- Added `Assets/Scripts/JourneyAttemptStore.cs`.
- Journey stores one independent JSON attempt per level under
  `MW.Journey.Attempt.v1.<level number>` using Unity PlayerPrefs.
- Reuses the existing validated `BlockBlastSnapshot` rather than introducing
  separate board/hand serialization.
- Persists the board, current pieces, score, moves, combo and hand counter,
  as well as Journey's line progress and any rewarded continuation state.
- A valid placement automatically saves while the objective is still pending.
- The Map button and app pause/focus-loss paths also persist ongoing progress.
- Selecting that level again restores the unfinished attempt.
- Level-map nodes show CONTINUE for a valid attempt; completed levels without
  an attempt show REPLAY; locked nodes retain their level number.
- Restart Level discards its own attempt and creates a fresh board.
- Victory and failure clear that level's attempt. Earned stars and unlocked
  levels remain saved separately.
- A saved attempt is rejected if malformed or incompatible with the level
  definition/seed. The Classic session has a different save key and is
  unaffected.

### Journey-specific restart interface

The shared Pause UI now shows RESTART LEVEL on Journey, with a confirmation
explaining that the current attempt resets while completed levels/stars remain.
Classic continues to use its original NEW GAME wording and behavior.

### Initial six-level difficulty calibration

The following edits were applied to the existing level assets **only while
they still matched the original generated defaults** and no saved attempt for
the target level was present:

- Level 4, Crystal Cascade: score goal 80 -> 125. Move limit remains 13.
- Level 6, Garden Guardian: score goal 105 -> 225, line goal 1 -> 3.
  Move limit remains 18.

Newly generated catalogs use these updated defaults.

The balancing Editor simulation ran 18 stochastic random-move trials and
18 line-seeking trials per level, using each level's fixed seed. The
measurements for the calibrated assets were:

| Level | Objective | Moves | Random wins / 18 | Line-seeking wins / 18 | Avg winning strategic moves |
| --- | --- | ---: | ---: | ---: | ---: |
| 1 - First Steps | Score 12 | 8 | 18 | 18 | 3.0 |
| 2 - Moonlit Clearing | Score 30 | 10 | 15 | 18 | 4.2 |
| 3 - Blooming Path | Clear 1 line | 12 | 12 | 18 | 4.8 |
| 4 - Crystal Cascade | Score 125 | 13 | 8 | 18 | 7.4 |
| 5 - Starlight Bridge | Clear 2 lines | 15 | 6 | 18 | 7.6 |
| 6 - Garden Guardian | Score 225 + clear 3 lines | 18 | 5 | 18 | 9.4 |

These are simple heuristics for detecting clearly misplaced difficulty
targets, **not real player success-rate estimates**. Fine balancing still
requires human playtests.

## Regression and QA results

- Unity C# compiled without errors after the changes.
- Journey Attempt QA: PASSED. Verified JSON save/restore, board cells,
  score/moves, valid CONTINUE state, corrupt-save rejection, changes to level
  definitions invalidating incompatible attempts, and clearing saves.
- Journey Resume Runtime QA: PASSED. Verified actual Map -> Level 1 ->
  placement -> Map with CONTINUE -> resumed score/moves -> Pause ->
  Restart Level confirmation -> fresh model/no saved attempt.
- Journey progression Play Mode smoke test: PASSED after fixing the test's
  data isolation. Verified Pause/Continue UI, victory/stars/unlock, Level 2
  loss panel, rewarded-continue disabled, Next, Retry, Return to Map.
- Tests now preserve and restore previous PlayerPrefs for selected level,
  unlocks, stars and any involved incomplete attempts.
- The heavier balance simulator and attempt validator run through explicit
  Editor menu commands only; they no longer run on every compilation.
- The temporary one-shot resume QA launcher was deleted afterward.

## Files

Runtime:
- Assets/Scripts/JourneyAttemptStore.cs (new)
- Assets/Scripts/JourneySession.cs
- Assets/Scripts/JourneyMapController.cs
- Assets/Scripts/JourneyLevelNode.cs
- Assets/Scripts/BlockBlastPrototype.cs
- Assets/Scripts/BlockBlastGameFlow.cs

Data:
- Assets/Resources/MagicalWorlds/Journey/Level_04.asset
- Assets/Resources/MagicalWorlds/Journey/Level_06.asset

Editor:
- Assets/Editor/JourneyAttemptQA.cs
- Assets/Editor/JourneyResumeRuntimeQA.cs
- Assets/Editor/JourneyBalanceQA.cs
- Assets/Editor/JourneyInitialBalanceTuning.cs
- Assets/Editor/JourneyCatalogCreator.cs
- Assets/Editor/JourneyRuntimeQA.cs (test isolation fix)

Source backups:
- ProjectBackups/JourneyAttempts_20261016/

## Manual review in Unity

1. Open `Assets/Scenes/JourneyMap.unity`, press Play.
2. Select Level 1. Place a block without finishing the goal.
3. Tap Map. Level 1 should display CONTINUE.
4. Tap Level 1 to reopen it. Verify that the board, pieces, move counter,
   score and objective progress are preserved.
5. Pause -> Restart Level -> confirm. Check that the board resets.
6. Finish a level and verify that Map displays REPLAY, stars and the next
   unlocked node.

Editor testing commands under Tools -> Magical Worlds -> Journey:
- Validate Attempt Save and Resume
- Simulate Level Difficulty
- Run Resume and Restart Test
- Run Gameplay Smoke Test

## Remaining production limitations

- PlayerPrefs is suitable for the local MVP. Cloud saving and protection
  against cross-device progress loss are separate work.
- The snapshot preserves the current board/hand exactly, but it does not
  serialize the internal System.Random generator state. Subsequent hands
  after a reload may therefore differ from an uninterrupted run, even though
  the currently saved hand is restored. Persisting deterministic RNG state
  would require an explicit RNG implementation/change to the model.
- The six-level difficulty samples are heuristic only; physical-device
  trials and player telemetry are needed before release tuning.
- Android/iOS tap/drag ergonomics, device safe areas, orientation and frame
  performance have not yet been tested on real hardware.
- Approved illustration assets are preserved; final PhotoCraft/VectorCraft
  graphics refinement is intentionally deferred.
- Rewarded +3 moves remains disabled until a real ad SDK and verified reward
  callback are integrated.
