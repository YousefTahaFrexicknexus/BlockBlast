# Magical Worlds - Unity Hierarchy and Prefab Guide

## Where to start

Scene: Assets/Scenes/ClassicGameplay.unity
Root prefab: Assets/MagicalWorlds/Prefabs/Classic/ClassicGame.prefab
Other prefabs: Assets/MagicalWorlds/Prefabs/Classic/
Shared rounded sprite: Assets/Resources/MagicalWorlds/UI/RoundedPanel.png

Open ClassicGameplay from the Project window, or use:
Tools > Magical Worlds > Hierarchy > Open Classic Gameplay Scene.

Expand Classic Game > Block Blast Canvas. The hierarchy is organized into:

1. 01 - Background
2. 02 - Header and Score
3. 03 - Board
4. 04 - Controls and Trays
5. 05 - Dialogs

The root scene object is a prefab instance. Its reusable elements include
the 8x8 board, three trays, four UI buttons, two popup panels and five
organizational section prefabs. Change their graphics, transforms and UI
properties in the Unity Inspector or in Prefab Mode.

## Inspector connections

Select Block Blast Canvas. The BlockBlastUIReferences component exposes
serialized Inspector references for 64 board cells, 64 preview cells, the
three trays, HUD text, sound/haptics/pause controls and both dialog panels.
Reassign references in the Inspector after intentional hierarchy changes.

The context-menu action "Rebuild UI References from Hierarchy" works only
with the original element names; use direct Inspector assignment instead
if you have renamed GameObjects. Permanent UI no longer depends on runtime
hierarchy-name searches when its Inspector reference component is complete.

## Prefabs to edit

ClassicGame.prefab: full Classic presentation and controller.
GameBoard_8x8.prefab: permanent cell grid and placement previews.
PieceTray_01/02/03.prefab: piece tray containers.
Button_Sound/Haptics/Pause/Restart.prefab: interactive controls.
Popup_GameOver.prefab and Popup_Dialog.prefab: game flow panels.
Section_01 through Section_05.prefab: organized screen hierarchy.

## Dynamic objects that stay dynamic

Current hand squares, the piece being dragged, particle effects and score
feedback are temporary gameplay instances. The board, trays, HUD, menu
controls and dialogs remain visible in Edit Mode.

## Important maintenance guidance

Canvas reference resolution is 1080 x 1920, portrait.
Board size and cell-spacing/hit calculations are still constants in
BlockBlastPrototype.cs. Moving the full board is supported, but altering
individual grid geometry requires coordinated gameplay math changes.

The approved Magical Worlds art direction is unchanged; detailed
graphics and production sprites can be refined separately.

SampleScene.unity was preserved. The original runtime UI constructor
remains as a compatibility fallback for older scenes without authored UI.

## Editor tools

Tools > Magical Worlds > Hierarchy:
- Open Classic Gameplay Scene
- Validate Classic Prefabs
- Generate Classic Prefabs and Scene (will not overwrite existing prefabs)
- Repair Canvas Scale

## QA performed, October 16 2026

Unity 6.3 imported the authored scene and nested prefab assets.
The missing-script issue was fixed by moving BlockBoardPointer and
BlockTrayPointer to correctly named source files and repairing prefab
references. The pre-repair generated assets were backed up under
ProjectBackups/ClassicHierarchyFirstPass_20261016.

Unity validator passed: full 1080x1920 Canvas, scale 1, five sections,
64 board cells, 64 previews, Inspector UI references and no missing
scripts. Existing BlockBlast rules smoke tests passed.

ClassicGameplay was also run in Play Mode to verify the HUD, background,
grid and trays render and bind with the new serialized references. No
startup exceptions were reported. Unity was returned to Edit Mode.
