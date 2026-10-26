# Magical Worlds — Milestone 5: Main Menu & Navigation

Status: Implemented and Unity Editor tested on 2026-10-16.
Unity project: BlockBlastClone, Unity 6.3 LTS.

## Entry Point

- Assets/Scenes/MainMenu.unity — first enabled scene in Build Settings.
- Assets/Scenes/ClassicGameplay.unity — existing endless Classic mode.
- Assets/Scenes/SampleScene.unity — preserved prototype fallback.

Open MainMenu in the Editor. Expand the **Magical Worlds - Main Menu**
prefab instance and **Canvas - Main Menu 1080x1920**. All major art
and interactive elements are real objects (not runtime-created).

## Hierarchy

Magical Worlds - Main Menu
  Canvas - Main Menu 1080x1920
    01 - Enchanted Garden Background
    02 - Currency and Settings Bar
      Header_Settings
      Currency_Coins / Coins - Editable Value / Plus - Coins
      Currency_Gems / Gems - Editable Value / Plus - Gems
      Currency_Energy / Energy - Editable Value / Plus - Energy
    03 - Magical Worlds Logo
    04 - Primary Navigation
      Button_Play
      Button_Journey
    05 - Bottom Navigation
      Navigation_Shop
      Navigation_Skins
      Navigation_Events
      Navigation_Settings
    06 - Popup Container
      Popup Overlay (inactive until opened)
        Popup_Frame
        Popup_Title / Popup_Description
        Popup_SoundToggle / Popup_HapticsToggle
        Popup_Close
  EventSystem (separate scene root)

The menu uses Unity uGUI with CanvasScaler reference resolution 1080x1920,
Scale With Screen Size, match width/height 0.5.

## Editable Assets

- Root prefab: Assets/MagicalWorlds/Prefabs/MainMenu/MainMenu.prefab
- 15 reusable prefabs: Play / Journey, four bottom navigation controls,
  three currency pill art objects, shared Popup, and header/section prefabs.
- Imported approved art: Assets/MagicalWorlds/Art/EnchantedGarden/Menu
  (separate background, logo, currency artwork and buttons).
- Runtime script: Assets/Scripts/MagicalWorldsMenuController.cs
- Scene authoring and QA: Assets/Editor/MagicalWorldsMainMenuBuilder.cs,
  MagicalWorldsMainMenuRepair.cs, MagicalWorldsMenuRuntimeQA.cs,
  MagicalWorldsNavigationRoundtripQA.cs.

Some UI sprites are cut from the approved raster reference and may have minor
edge/background artifacts. Their graphics can later be replaced via the Image
component in Inspector without remaking navigation or prefabs.

## Functional Navigation

- Play: loads ClassicGameplay.unity, preserving Classic's saved run.
- Journey: opens clearly labeled Journey placeholder panel.
- Shop, Skins, Events: open their own clearly labeled placeholder panels.
- Settings top icon / Settings bottom tab: opens sound/vibration toggles,
  using the same PlayerPrefs keys as existing Classic gameplay.
- Green currency plus icons: open Shop placeholder.
- Shared popup Close / backdrop tap / Escape: dismisses popup.

Currency numbers are placeholders for the upcoming economy milestone:
Coins = 1,250; Gems = 80; Energy = 5/5.
They are real editable Text objects, not final economic values.

Classic gameplay:
- Auto-bootstrap now only runs in ClassicGameplay and SampleScene; it does
  not unexpectedly instantiate a game while MainMenu is open.
- Pause retains Continue, How to Play and New Game, plus a separate Main Menu
  button that saves the session and loads MainMenu.
- Game Over retains Play Again plus a new editable Main Menu button.
- Existing prefabs were upgraded non-destructively. No user-edited prefab
  was overwritten by the Main Menu builder.

## Test Summary

Unity Editor compiled without C# errors. The menu prefab validator confirmed
scene, Canvas (1080x1920, scale 1), navigation references, shared popup,
and no missing scripts.

Runtime smoke test:
- Menu controller exists with EventSystem and GraphicRaycaster.
- Journey placeholder opens and closes.
- Raycast hits the Journey Image at its actual RectTransform screen position.

Navigation roundtrip:
- MainMenu -> Play button -> ClassicGameplay -> Game Over menu action
  (saved-session fallback) -> MainMenu.
- Confirmed no duplicate Classic controller in the hub afterward.
- Original Classic rules smoke tests passed.

The saved-session fallback was used for the roundtrip because Pause was not
available in that restored run. The authored Pause Main Menu button is wired
and references validated, but the full fresh-session Pause click path has not
been independently exercised yet.

## Quick Test Instructions

1. Open Assets/Scenes/MainMenu.unity and press Play.
2. Click Journey, Shop, Skins, Events and Settings. Their placeholder panels
   should display and Close should return to the menu.
3. Click Play to open the Classic mode.
4. From an active Classic run, use Pause -> Main Menu. From a completed run,
   use Game Over -> Main Menu. Both paths save and return to the hub.
5. Edit any Image sprite or Text in the Canvas prefab in Edit Mode.

## Deferred Work

Journey level progression, shops/purchases, collections, missions/events,
server-driven economy, real currency values, and final PhotoCraft/VectorCraft
art refinements are separate milestones.

Next proposed milestone: Classic gameplay feel, drag/drop polish and mobile
UI safe-area/device validation while preserving these editable prefabs.
