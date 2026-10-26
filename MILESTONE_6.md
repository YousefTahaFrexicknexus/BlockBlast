# Magical Worlds — Milestone 6: Classic Gameplay Feel

**Status:** Implemented and verified in Unity Editor (2026-10-16)  
**Unity:** 6.3 LTS (6000.3.13f1)  
**Scene:** `Assets/Scenes/ClassicGameplay.unity`

## Goals

Improve touch placement feel and editor maintainability, retain the gameplay rules,
preserve the 1080×1920 uGUI Canvas, and leave existing prefabs editable.

## Implemented

### Board-local placement

- Added `Assets/Scripts/BlockBlastDragMath.cs`, a reusable, small pure helper
  that calculates a piece's grid origin from its center in board-local coordinates.
- Pointer positioning now converts the lifted ghost target through Unity
  transforms, from Canvas coordinates to the board's own coordinates.
- Moving or scaling the parent UI groups no longer requires hand-maintained
  offsets in the drag-to-cell mapping.
- Drop validity still comes from the existing BlockBlastModel.CanPlace rules.

### Multi-touch safety and responsiveness

- Each drag tracks the initiating `PointerEventData.pointerId`.
- Movement and release events from unrelated pointers are ignored.
- Preview positions follow the actual pointer target rather than the
  smoothed ghost sprite position.
- The board is no longer redrawn and its 64 placement previews reset on every
  pointer event. Only a change in the hovered cell/validity replaces the preview.
- Preview cells gently pulse; the dragged piece smoothly follows and scales.

### Inspector tuning

Select `Classic Game` in the ClassicGameplay hierarchy and expand
**Classic Feel - Drag & Placement** on `BlockBlastPrototype`.

| Parameter | Default | Meaning |
| --- | ---: | --- |
| Drag Lift | 185 | Portrait UI units that lift piece above finger |
| Drag Follow Speed | 30 | Exponential visual smoothing speed |
| Drag Start Scale | 0.94 | Initial picked-up piece scale |
| Drag Valid Scale | 1.02 | Piece scale when placement is valid |
| Drag Invalid Scale | 0.97 | Piece scale for invalid board placement |
| Preview Pulse Speed | 9 | Placement hint highlight pulse rate |
| Invalid Nudge Scale | 1.035 | Tray feedback multiplier |
| Invalid Nudge Duration | 0.11 | Tray nudge duration in seconds |

The values above are actually present in Unity's serialized prefab, as
confirmed by Editor QA.

### Feedback and interruptions

- Distinct invalid drop text for an outside-board drop versus a piece that
  intersects occupied cells or is partially off-grid.
- Debounced tray nudge so rapid bad drops cannot overlap animation coroutines.
- Board impact stops any previous impact before starting another and
  restores board position when finished.
- Pause, device focus loss, and application backgrounding cancel the active
  ghost and placement preview; existing session saving still runs.
- Restart clears pointers, preview registrations and animation handles.
- Sound, haptics, scoring, clears, combo and progression logic remain intact.

## QA performed

- Unity C# compiled with no errors.
- Existing rules smoke tests: **passed**.
- Prefab validation: **passed**, including 64 cells and previews and
  no missing MonoBehaviour scripts.
- `MagicalWorldsClassicFeelQA`: **passed**. Validates snap geometry for
  differently shaped pieces, board boundary checks, nested/moved/scaled
  board transform math, and serialized tuning fields.
- `MagicalWorldsNavigationRoundtripQA`: **passed**.
  Actual Play Mode flow: Main Menu → Play → ClassicGameplay,
  then Pause → Main Menu without a duplicate Classic controller.
- Live interaction regression in the above test: **passed**.
  Programmatically performed a drag pickup, checked snapped row/column,
  rejected a second pointer, and verified Pause cancelled the drag and
  cleared active preview registrations. This did not place any pieces or
  modify the board.

### Limitations and next checks

- These are Editor Play Mode and code-level checks, not physical-device
  touch testing. Validate on a real Android phone in a later mobile QA pass.
- Existing gameplay art remains temporary; no alternate visual concepts
  were introduced.
- The board is still 8×8 with its original gameplay-model rules. Altering
  grid dimensions or per-cell geometry requires coordinated changes.
- Dedicated touch-perf profiling at low frame rates is still pending.

## Important paths

- `Assets/Scripts/BlockBlastPrototype.cs`
- `Assets/Scripts/BlockBlastGameFlow.cs`
- `Assets/Scripts/BlockBlastDragMath.cs`
- `Assets/Editor/MagicalWorldsClassicFeelQA.cs`
- `Assets/Editor/MagicalWorldsNavigationRoundtripQA.cs`
- `Assets/MagicalWorlds/Prefabs/Classic/ClassicGame.prefab`
- `ProjectBackups/ClassicFeelBeforeM6_20261016/` — source scripts before M6

## How to test manually

1. Open `Assets/Scenes/ClassicGameplay.unity`.
2. Select **Classic Game** and inspect/tune the **Classic Feel** section.
3. Press Play, resume or start a Classic run.
4. Drag pieces from trays to different board cells; check preview snap and
   ghost smoothing. Test invalid drops onto occupied cells and outside board.
5. Test tap-to-select and tap-to-place.
6. During a drag, open Pause and verify the ghost/preview disappears.
7. Resume, clear lines, watch score animation, combo, impact, and feedback.
8. Use Pause → Main Menu to return to the existing hub.

Unity menu: **Tools → Magical Worlds → Classic Gameplay → Run Drag Geometry Checks**.

## Hotfix — Stuck Placement Highlights (2026-10-16)

**User-reported issue:** placement highlighting remained visible after dragging
away or releasing an unplaced piece, until another successful placement
refreshed the board.

**Root cause:** ShowPlacementPreview and ShowLineHint directly enabled preview
Image GameObjects, but ClearDragPreviews only disabled images registered in
visibleDragPreviews. These two paths were not using SetDragPreview and
therefore the cleanup list was empty.

**Fix:**
- Both piece-footprint previews and row/column completion hints now register
  through SetDragPreview.
- On pickup, release and pause/cancellation, ClearDragPreviews(true) also
  performs a single 8×8 sweep for leftover untracked highlights.
- Normal per-cell drag movement still uses tracked-only cleanup, retaining
  the Milestone 6 efficient preview-update behavior.
- Gameplay placement, scoring and piece selection rules were not changed.

**Verification:** Unity Editor import and C# compilation passed. The
MagicalWorldsClassicFeelQA preview cleanup test passed, including actual
prefab images for piece highlights, full-line hints, leaving a hover, and an
intentionally orphaned old-style highlight. Existing rules, board snapping
and prefab tests also passed. The fix is ready for manual in-Game-view
confirmation with the user's exact input sequence.

**Modified:**
- Assets/Scripts/BlockBlastPrototype.cs
- Assets/Scripts/BlockBlastGameFlow.cs
- Assets/Editor/MagicalWorldsClassicFeelQA.cs
- Assets/Editor/MagicalWorldsNavigationRoundtripQA.cs (added a Play Mode
  invalid-release regression to run during its next MainMenu roundtrip)
- Backup: ProjectBackups/PreviewCleanupBugfix_20261016/
## Classic feel follow-up — Placement-anchored score feedback

Implemented: The floating +score reward now appears near the actual placed
piece rather than at a hard-coded screen location.

- Computes the average location of the recently occupied cells.
- Converts through the authored Board and Canvas transforms; moving, nesting
  or scaling the board preserves the popup position.
- Clamps placement popup position within the portrait gameplay area.
- Added three Inspector fields in Classic Feel - Scoring Feedback:
  Floating Score Lift = 55, Rise = 112, Duration = 0.8 seconds.
- Debounces overlapping combo punch animations.
- Preserves TAP THE GRID TO PLACE THE PIECE when a temporary message expires
  but the player still has a selected piece.
- After the selection is cleared, restores DRAG A PIECE TO PLACE IT.

Changed: Assets/Scripts/BlockBlastPrototype.cs
New tests: Assets/Editor/MagicalWorldsClassicPolishQA.cs
Backup: ProjectBackups/ClassicPolishFeedback_20261016/

Verification: Unity compilation, Classic Polish QA (placement coordinates,
nested board transforms, context-sensitive instructions), puzzle rules,
preview cleanup, prefab validation, and menu navigation passed.
The menu navigation test restored a finished Classic run and skipped its
live drag check. Android device testing and hands-on visual inspection
of the new score popup remain outstanding.