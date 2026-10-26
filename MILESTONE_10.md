# Milestone 10 - Journey Expansion to 50 Levels

**Status:** Implemented and validated in Unity Editor, October 17, 2026.
**Project:** Magical Worlds / BlockBlastClone, Unity 6.3 LTS.
**Approved art direction:** Preserved. This milestone creates no new visual concept.

## Delivery

- 50 editable `JourneyLevelDefinition` ScriptableObjects at `Assets/Resources/MagicalWorlds/Journey/Level_01.asset` through `Level_50.asset`.
- LevelCatalog.asset has 50 references in exact level order.
- Existing Levels 1-6 kept their original names, move limits, objectives and random seeds.
- Added 44 original, named fantasy levels from Moonpetal Glade (7) through Everlasting Bloom (50).
- Each level is independently editable in the Unity Inspector; goals, moves and seeds are data-driven.
- Reused existing scoring, line clearing, combined objectives, star/unlock progression and save/resume.
- Journey Map has 50 authored prefab nodes, shown six per page (9 pages total). Page 9 contains Levels 49 and 50.
- Existing first-page map and approved painterly environment artwork remain unchanged.
- Map includes PREV, NEXT and page-range indicator. The map opens on the page containing the last selected level.
- Completed levels show REPLAY; saved unfinished attempts show CONTINUE; locked levels remain numbered.
- Legacy save compatibility: a player who completed the original Level 6 automatically has Level 7 available.

## Difficulty direction

Level 7 starts a little harder than the approved Level 6. Across the following 43 levels,
the player is given longer sessions (approximately 18 up to 32 moves), but objectives increase
more quickly. Levels rotate between point totals, line clears and combined objectives.

Selected calibrated checkpoints:

| Level | Goal | Move limit | Simulated strategic wins (8 runs) | Mean moves among wins |
|---:|---|---:|---:|---:|
| 6 (original) | 225 points and 3 lines | 18 | 8/8 | 9.4 |
| 7 | 250 points and 3 lines | 18 | 7/8 | 11.1 |
| 12 | 560 points and 5 lines | 19 | 8/8 | 15.3 |
| 30 | See complete level table | 25 | 6/8 | 19.3 |
| 40 | See complete level table | 29 | 7/8 | 24.4 |
| 45 | 3,000 points | 30 | 6/8 | 24.3 |
| 50 (finale) | 2,650 points and 11 lines | 32 | 6/8 | 25.7 |

The complete simulator ran 8 randomized and 8 line-seeking deterministic strategies per
level (800 simulated runs in all). Every level achieved at least 3/8 wins under the tested
line-seeking strategy. This supports basic attainability but does not establish optimal play,
real-player difficulty, retention, or success rate. Real-device playtesting is still required.

The final calibration refined nine unusual difficulty spikes by adjusting specific seeds
and/or objectives (Levels 11, 12, 17, 24, 34, 41, 45, 48, 50).
New assets are authored with these calibrated defaults. Existing manually edited levels
and levels with an unfinished saved attempt are protected against automatic retuning.

## Unity editing workflow

Open the map using `Assets/Scenes/JourneyMap.unity`.
In the Hierarchy, open:

`JourneyMapRoot / Journey Map Canvas / 03 - Level Path`

This contains 50 `JourneyLevelNode` components; off-page nodes are inactive but fully
editable. The first six nodes are the original authored nodes.

`JourneyMapRoot / Journey Map Canvas / 05 - Page Navigation`

Contains PREV, NEXT and the page-range Text. Edit their dimensions, colors or positions in
the prefab; the page controller references are serialized.

Level definitions are listed at:
`Assets/Resources/MagicalWorlds/Journey/Level_XX.asset`.

For the entire 50-level data table including level names, goals, moves and fixed seed,
read `JOURNEY_50_LEVELS.md` at the project root.

## Verification

- Unity detected exactly 50 level asset files and 50 catalog entries.
- Journey Data QA passed: valid goals and win/loss rules for all 50 levels.
- Journey Presentation QA passed: serialized UI components and 50 distinct level nodes.
- Journey Runtime QA passed: page 1/9 and 9/9 selection, Level 1 victory and unlock,
  Level 2 failure, Pause/Continue, Retry, Next and Return-to-Map prefab callbacks.
- Legacy progression QA passed: completion of Level 6 unlocks Level 7 using the previous
  PlayerPrefs save, while incomplete Level 6 does not.
- PlayerPrefs progression restored after the automated tests.
- Editor logs reported no new C# compile errors or runtime exceptions in the final check.

## Editing, rebuilding, and simulating

Tools / Magical Worlds / Journey:
- Ensure 50 Level Assets
- Upgrade Paged 50-Level Map
- Apply Calibrated 50-Level Difficulty
- Profile All 50 Levels
- Run Gameplay Smoke Test
- Validate Level Data
- Verify Legacy Progress Unlock

The editor upgrade is idempotent: rerunning does not recreate user-edited existing levels
or override the authored first six. A backup of the original catalog/map/scripts exists at:
`ProjectBackups/Journey50_20261016/`.

## Deferred follow-up

- Physical Android/iOS playtest with multi-session progression from Levels 1 through 50.
- Mobile touch feel, low-end performance, and safe area behavior on real hardware.
- Final star-tier economy, progression rewards, events and monetization.
- Final PhotoCraft/VectorCraft artwork refinement based on the previously approved references.
