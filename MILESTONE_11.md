# Magical Worlds / BlockBlast — Milestone 11

**Status:** Implemented and Unity Editor-validated, 17 October 2026.
**Project:** C:\Users\PC\Documents\GitHub\BlockBlast
**Editor:** Unity 6.3 LTS (6000.3.13f1)

## 1. 50-Level Playtest Dashboard

Open **Tools → Magical Worlds → Journey → Playtest Dashboard** in Unity.

- Choose any Journey level from 1 through 50 regardless of unlock status.
- Play Selected Level: opens the authored JourneyGameplay scene with an Editor-only preview override.
- Open Journey Map: returns to the authored nine-page level map, saving scene changes if requested.
- Stop Playtest: exits Play Mode.
- Inspect the selected level ScriptableObject, move budget, objective and RNG seed.
- View results by level: wins, losses, retries, abandons, win rate and average winning moves.
- Refresh Results, Export CSV and Open Report Folder controls.

Preview mode does **not** modify the real selected level, unlocked levels, star progression, or saved incomplete attempts. The original 50 levels and their difficulty parameters were not changed.

Main files: Assets/Editor/JourneyPlaytestWindow.cs and Assets/Scripts/JourneyPlaytestContext.cs.

## 2. Local playtest analytics

The Editor writes events to:
ProjectReports/JourneyPlaytestEvents.jsonl

Captured fields: UTC time, anonymous test session identifier, event type, level, score, cleared lines, used moves, move limit, earned stars, preview mode and automated-test marker.

Supported events: Start, Resume, Win, Loss, Retry and Abandon (return to map). The dashboard excludes events tagged as automated QA. This is local-only developer instrumentation; no network or player-account analytics was added.

Use **Export CSV** to generate:
ProjectReports/JourneyPlaytestSummary.csv

The initial 8 automated QA events were archived, not counted as real player sessions, at:
ProjectReports/QA/JourneyPlaytestAutomatedRuns_20261017.jsonl

## 3. Inspector-editable star awards

Every JourneyLevelDefinition ScriptableObject now has:
- Three Star Remaining Ratio: 0.50 of the original move budget by default.
- Two Star Remaining Ratio: 0.20 of the original move budget by default.

The existing star thresholds remain unchanged on all 50 levels unless adjusted in their own Inspector. A validation hook prevents the two-star requirement from being stricter than the three-star requirement.

No score goals, line-clear goals, move limits, approved seeds, artwork, prefabs or UI design were changed.

## 4. Deterministic future hands after resume

BlockBlastSnapshot now stores an RNG draw count. In seeded Journey gameplay, restoring a new snapshot advances a System.Random instance seeded with the level's existing seed to the saved draw count.

This makes future hands after a resumed save match an uninterrupted run without changing the piece generation sequence.

Older snapshots without a draw count remain loadable but keep their legacy best-effort continuation behavior. Unseeded Classic saves are unaffected; exact future-hand continuity is guaranteed only for seeded snapshots with the new count.

## 5. Tests performed

- PASS: All 50 levels retain their previous star awards for every used-move threshold.
- PASS: Seeded snapshot restore yields identical subsequent placements and generated piece hands.
- PASS: Level 50 Editor playtest when locked, including gameplay input and local telemetry, leaves player progression and unfinished attempts unchanged.
- PASS: Existing full Journey gameplay regression: map pages 1/9 and 9/9; Pause/Continue; Level 1 victory/stars/unlock; Level 2 loss; Next, Retry and Return-to-Map prefab buttons; pre-test progression restored.
- PASS: QA-generated analytics events are tagged automated and are excluded from playtest aggregates.
- PASS: Latest Unity compilation without C# errors.

The one-time automatic Play Mode launcher was removed after validation. The preview test can still be run manually under:
**Tools → Magical Worlds → Journey → Run Isolated Preview Test**.

Other commands:
- **Tools → Magical Worlds → Journey → Validate Milestone 11**
- **Tools → Magical Worlds → Journey → Run Gameplay Smoke Test** (Ctrl+Alt+J)

## 6. Source files

New:
- Assets/Scripts/JourneyPlaytestContext.cs
- Assets/Scripts/JourneyPlaytestTelemetry.cs
- Assets/Editor/JourneyPlaytestWindow.cs
- Assets/Editor/JourneyMilestone11QA.cs
- Assets/Editor/JourneyPreviewRuntimeQA.cs

Updated:
- Assets/Scripts/JourneyProgress.cs
- Assets/Scripts/JourneySession.cs
- Assets/Scripts/JourneyLevelDefinition.cs
- Assets/Scripts/BlockBlastModel.cs
- Assets/Scripts/BlockBlastSnapshot.cs
- Assets/Editor/JourneyRuntimeQA.cs
- Assets/Editor/JourneyResumeRuntimeQA.cs

Original gameplay source backups:
ProjectBackups/Milestone11_20261017/

## 7. Remaining QA / next milestone

- Playtest real players across representative levels and use actual win rates to tune difficulty.
- Verify touch controls, insets, pause/resume and frame rates on physical Android and iOS devices.
- Review per-level star awards and economy rewards once a player progression economy is designed.
- Verify the GUI Export CSV control manually: code compiles, but a native-click-through test of its Explorer/CSV opening behavior was not performed.
- No Android/iOS build was created.
