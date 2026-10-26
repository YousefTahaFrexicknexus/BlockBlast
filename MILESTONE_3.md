# Block Puzzle — Milestone 3: Game Flow and Saved Progress

## Status
- Implementation added to the connected BlockBlastClone Unity 6.3.13f1 project.
- Accepted 8x8 Classic Mode rules are unchanged.
- Portrait UI CanvasScaler reference is still 1080x1920.
- C# assembly compilation: passed, 0 errors / 0 warnings (temporary references added to generated csproj for validation, removed afterward).
- Headless model simulation: 150 seeded games, 1,887 legal moves, 34,050 assertions passed. Includes snapshot round-trip and corrupted-state rejection.
- Editor interactive visual/touch QA is not verified: the local Unity control bridge is unavailable.

## Delivered
1. In-progress Classic run is saved after every valid placement, when paused, on app focus loss, and on quit.
2. Saved state restores occupied board cells, exact remaining tray shapes and colors, score, combo streak, move count, hand number and game-over status.
3. Versioned Unity JsonUtility snapshot DTO is validated before any changes are applied; invalid/corrupt saves are discarded and a new run starts.
4. On first launch, a How to Play overlay explains placing the three nonrotating shapes and clearing complete rows/columns.
5. The PAUSE button below the board opens Resume / How to Play / New Game, with in-menu SFX and vibration toggles.
6. New Game requires a second confirmation and does not reset the persistent Best score.
7. Android back and desktop Escape map to menu back / pause.
8. All game input is blocked while dialogs are displayed.
9. Game Over save restores the game-over screen and allows Play Again.

## Source files
- Assets/Scripts/BlockBlastModel.cs: now a partial class for modular save-state extension; unchanged game rules.
- Assets/Scripts/BlockBlastSnapshot.cs: serializable DTO, ExportSnapshot, TryRestore.
- Assets/Scripts/BlockBlastPrototype.cs: game-flow integration, pause button, runtime input guards, save triggers.
- Assets/Scripts/BlockBlastGameFlow.cs: persistence, first-run guide, pause / restart confirmation overlays, Escape/back handling.
- Assets/Editor/BlockBlastSmokeTests.cs: saved-state, JSON round-trip, invalid data, combo and gameplay tests.
- MILESTONE_3.md: this guide.

## Test in Unity
1. Open Assets/Scenes/SampleScene.unity in Unity 6.3.13f1 and select 1080x1920 portrait in Game view.
2. Press Play and follow the first-run How to Play overlay. (Tutorial is shown once per PlayerPrefs profile; use a fresh test profile to test it again.)
3. Place 1 or 2 pieces. Note the exact board cells, colors and remaining shapes.
4. Stop Play, then Play again. Confirm board, remaining pieces and score resume exactly.
5. Pause with the PAUSE button below the board, choose How to Play, return, choose New Game, CANCEL, then confirm that the current board is intact. Confirming New Game should reset the board and score while preserving Best.
6. Press Escape with the Game tab focused: pause/resume should work.
7. Simulate full line clearing and game-over. Stop and Play again to verify the game-over state resumes.
8. Verify mouse drag/touch placement remains blocked behind guide, pause, and confirmation dialogs.
9. In the Editor run Tools > Block Puzzle > Run Rules Smoke Tests. Check Console output.

## Caveats and deferred work
- Unity UI rendering and physical-device touch handling must still be manually verified; C# compilation and headless rules tests are not end-to-end Play Mode tests.
- This uses PlayerPrefs for a small prototype save. For production/mobile release, migrate to atomic disk writes and optionally cloud backup.
- Future random hands are sampled from the standard generator after resume; no attempt is made to replicate an original proprietary PRNG state.
- No ads, purchases, levels, limited-moves mode, analytics, account sync or external asset integration were added.
- No distributable Android/WebGL build was generated.

## QA acceptance criteria
- [ ] Intro appears on fresh profile and closes cleanly.
- [ ] Saved board and exact remaining piece shapes restore after Play Mode or app relaunch.
- [ ] Bad saved JSON is rejected without crash.
- [ ] Pause/resume/back work; gameplay stays disabled behind overlays.
- [ ] Restart confirmation cannot erase progress accidentally.
- [ ] Best is retained after restart.
- [ ] Portrait layout remains usable across common aspect ratios and device safe areas.
