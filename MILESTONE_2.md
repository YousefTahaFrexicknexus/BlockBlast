# Block Puzzle — Milestone 2: Gameplay Feel and UX

## Status
- Implemented in BlockBlastClone on top of the accepted Milestone 1 rules engine.
- Screen orientation and Canvas reference: 1080 x 1920 portrait.
- Unity C# project compile: verified with project assembly references. Allow Unity Editor to import scripts.
- Automated game-rule simulation: 150 deterministic seeded games, 1,887 legal moves and 33,974 assertions passed (headless harness, no Unity renderer).
- Unity Editor interactive gameplay/touch, animation timing and device haptics are pending manual visual QA. C# compilation is not a play-through validation.

## Implemented interaction changes
- Smoothly followed drag ghost at full board-cell size, 185px above the fingertip (portrait reference units).
- Live position validation with valid and invalid footprint previews.
- Gold row and column highlighting when a legal placement would complete a line.
- Tray graphics remain stable between frame/UI refreshes instead of rebuilding every draw.
- Drag/drop and select-then-tap placement both remain supported.

## Added feedback
- Procedural rounded UI sprite for cells, tray and buttons.
- Placement pop scale/opacity animation.
- Line-clear flash and lightweight board impact shake.
- Animated score count, floating score reward, combo punch and staggered hand refill.
- Delayed game-over overlay reveal and smooth fade.
- Procedural audio cues for pick, place, clear, and invalid placement.
- Settings in top corners: SFX ON/OFF and VIBE ON/OFF, saved with PlayerPrefs.
- Vibration is disabled by default and only applies on Android/iOS line clears when enabled.
- Temporary visual effects are cleaned up on replay to prevent stale UI objects.

## Changed files
- Assets/Scripts/BlockBlastPrototype.cs — Milestone 2 presentation, input and effects updates.
- Assets/Editor/BlockBlastSmokeTests.cs — additional combo, game-over and piece refill tests.
- MILESTONE_2.md — implementation and QA guide (this file).
- Assets/Scripts/BlockBlastModel.cs — unchanged from the accepted mechanics.

## Run
1. In Unity 6.3.13f1, open Assets/Scenes/SampleScene.unity.
2. Use a portrait Game view, ideally 1080 x 1920.
3. Press Play. The gameplay UI bootstraps at runtime.
4. Drag tray shapes onto the board; verify the lifted ghost follows the pointer.
5. Hold a piece where it would finish a row/column. The line should display a gold preview. Drop it: it should clear, award score, play a flash, and briefly move the board.
6. Test a rejected placement near an edge or over occupied cells; it must leave the board/score unchanged.
7. Check VIBE and SFX toggles. SFX should mute audio; vibration is off by default.
8. Finish a game, verify the delayed overlay and Play Again. Check effects disappear and the score resets.
9. Run Tools > Block Puzzle > Run Rules Smoke Tests in the Unity Editor.

## Non-goals and remaining verification
- No changes to piece generation/scoring weights; hidden reference-game probabilities/score math are not claimed to be identical.
- No levels, limited-moves mode, rewarded ads, purchases or analytics.
- No final production artwork, sound design, localization or platform builds.
- Before accepting Milestone 2: visually inspect desktop drag behavior, various portrait devices, audio/haptics on physical device, rapid restarts, 30+ placements and full-line previews.
