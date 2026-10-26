# Block Puzzle — Milestone 1: Classic Playable Prototype

## Status
- Gameplay implementation: complete in source.
- Unity C# compile: verified with the project assembly references (0 warnings, 0 errors).
- Editor play-through / touch QA: **not yet verified**; manually press Play and test.
- Reference resolution: 1080 x 1920 portrait.

## Run the prototype
1. Open `BlockBlastClone` in Unity 6.3.13f1 (this project is already open in the local Unity Editor).
2. Open `Assets/Scenes/SampleScene.unity`.
3. Set **Game** tab to **1080 x 1920 (portrait)**; the UI uses CanvasScaler's 1080 x 1920 reference resolution.
4. Press **Play**. A runtime bootstrapped UI is created automatically; no prefab/inspector wiring is needed.
5. Drag pieces from the bottom tray onto the board with mouse or touch. Alternatively, click a piece and then a destination square.
6. To test rules from the Editor menu, select **Tools → Block Puzzle → Run Rules Smoke Tests**.

## Rules
- 8 x 8 grid, with no falling pieces or rotation.
- Three shapes per hand; deal next hand only when all three are placed.
- Every shape cell must fit inside the board and land on an empty square.
- Clear all full rows and columns simultaneously on placement, counting intersections once.
- Continue while at least one remaining tray piece can fit anywhere.
- Lose only when no available tray piece fits; restart via Play Again.
- Placement points, clear points, multi-line bonus and short combo window are implemented as a working baseline. Precise proprietary scoring and hidden RNG weights are **not claimed** to match the hosted reference; they require observation and further tuning.

## Files
- `Assets/Scripts/BlockBlastModel.cs` — independent rule engine, hand generation, scoring, legality, clear and game-over logic.
- `Assets/Scripts/BlockBlastPrototype.cs` — programmatic Unity uGUI portrait screen, pointer/touch drag previews, invalid placement feedback, best score, restart and clear flash.
- `Assets/Editor/BlockBlastSmokeTests.cs` — deterministic rule checks via Editor menu.
- `ProjectSettings/ProjectSettings.asset` — portrait orientation and 1080 x 1920 standalone starting resolution.

## Deferred beyond this milestone
- Full art/theme integration, audio polish and effects.
- Analytics, monetization, rewarded moves and purchases.
- Limited-moves variant, goals/levels and meta progression.
- Measuring the hosted game's secret piece-generation weights and full score math.

## Acceptance checklist (manual QA)
- [ ] Start in Editor portrait mode with board and 3 pieces visible.
- [ ] Drag each shape into empty cells; occupied and out-of-bounds placements are rejected.
- [ ] Complete a row, column and cross; completed lines clear immediately.
- [ ] Three placements refill the tray; one/two placements do not.
- [ ] Use a board with no legal placements: Game Over overlay appears.
- [ ] Play Again clears the board and restores 3 pieces; best score remains.
- [ ] Verify mouse on desktop and finger drag in Android portrait build.
