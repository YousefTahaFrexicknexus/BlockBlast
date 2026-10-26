# Magical Worlds — Milestone 9: Portrait Mobile Readiness

**Status:** Unity Editor implementation and automated regressions passed, October 16, 2026.  
**Project:** BlockBlastClone / Magical Worlds, Unity 6.3 LTS (6000.3.13f1).  
**Canonical design resolution:** 1080 × 1920, portrait.

## Implemented

### 1. Display safe-area handling

Added an editable component: `Assets/Scripts/MobileSafeAreaDock.cs`.

Each dock preserves the authored RectTransform position and dynamically applies
the inset from `Screen.safeArea`. It accounts for changes in pixel resolution,
Canvas scale, and logical Canvas height. OnDisable restores the original position.
A per-dock Inspector enum controls Top/Bottom/Left/Right, plus maximum cutout
displacement, influence, and viewport-edge tracking.

Updated the existing, human-editable prefabs **without replacing artwork or
rebuilding prefab hierarchies**:

| Prefab | Dock targets | Edge |
|---|---|---|
| MainMenu.prefab | 02 - Currency and Settings Bar | Top |
| MainMenu.prefab | 05 - Bottom Navigation | Bottom |
| JourneyMapRoot.prefab | 02 - Map Header | Top |
| JourneyMapRoot.prefab | 04 - Navigation | Bottom |
| JourneyGameplayRoot.prefab | 01 - Level Objective and Moves | Top |
| ClassicGame.prefab | Title | Top |
| ClassicGame.prefab | Subtitle | Top |
| ClassicGame.prefab | Sound Toggle | Top |
| ClassicGame.prefab | Haptics Toggle | Top |

**Important:** the 8×8 puzzle board, full-screen fantasy backgrounds, block trays,
popups and score display are not reparented or warped by these docks.

### 2. Responsive portrait Canvas fit

Added `Assets/Scripts/MobilePortraitCanvasFit.cs`, attached to the
Canvas components in the four authored prefabs. It maintains the
1080×1920 `CanvasScaler.referenceResolution`, but updates its scaling blend
at runtime based on actual portrait aspect ratio:

- 1080×1920 or proportional sizes: matches the approved blend of 0.5.
- Taller displays (e.g., 1080×2400): prioritizes screen width (blend 0),
  preventing the puzzle board from growing beyond the available width.
- Shorter displays (e.g., 1080×1600): prioritizes screen height (blend 1),
  helping keep the top/bottom controls in view.

Safe-area docks also track half of any extra logical viewport height, so
top and bottom navigation remain near the phone's edges rather than drifting
toward the center of a very tall screen. This behavior is tunable in prefabs.

### 3. Mobile input and target frame rate

Updated `Assets/Scripts/MagicalWorldsMobileDefaults.cs`.

On Android and iOS **only**, at runtime:

- `Screen.orientation = ScreenOrientation.Portrait`.
- `Application.targetFrameRate = 60`; `QualitySettings.vSyncCount = 0`.
- On every loaded scene, tune the active `EventSystem.pixelDragThreshold` to
  `clamp(round(0.014 * min(screenWidth, screenHeight)), 12, 24)`.
- This reduces accidental small finger drags on dense displays without
  modifying the model's placement rules, desktop input, or pointer IDs.

Journey Map now handles `InputSystem.Keyboard.current.escapeKey` Back/Escape
to return to Main Menu when not loading a scene. Classic already handled
Escape in its Pause/dialog flow.

### 4. Art and texture review

Inspected the 18 Enchanted Garden PNGs in the existing art folders.
They already have mipmap generation disabled and a max texture size of 2048.
No compression, imagery, sprite import dimensions or atlas layout was altered.
Final graphics remain deferred as previously agreed.

## Regression results

The Unity Editor logged:

- `[Mobile Layout QA] PASSED`: 8 synthetic safe-area inset tests,
  7 portrait aspect/viewport tests, 5 adaptive drag-threshold tests,
  9 docked elements across 4 prefab roots, full-screen art/board preservation,
  and essential Button RectTransform size checks.
- `[Journey Runtime QA] PASSED`: Pause/Continue, Level 1 victory,
  stars/unlock, Level 2 loss, disabled reward continuation, Next,
  Retry, and return-to-map prefab button callbacks with original
  progression state restored.
- The C# changes compiled; no new C# errors were reported in the final Editor logs.
- Regular Android and iOS builds and physical-device performance tests
  were **not** performed as part of this milestone.

### Editor validation menu

`Tools → Magical Worlds → Mobile → Validate Safe Area and Touch Targets`

`Tools → Magical Worlds → Mobile → Apply Safe Area to UI Prefabs`

The upgrader only adds components where absent and retains authored Inspector
adjustments. Auto-validation runs after migration to avoid false first-import
warnings.

## Files and backups

**New:**
- Assets/Scripts/MobileSafeAreaDock.cs
- Assets/Scripts/MobilePortraitCanvasFit.cs
- Assets/Editor/MagicalWorldsSafeAreaPrefabUpgrade.cs
- Assets/Editor/MagicalWorldsMobileQA.cs

**Changed:**
- Assets/Scripts/MagicalWorldsMobileDefaults.cs
- Assets/Scripts/JourneyMapController.cs
- Assets/MagicalWorlds/Prefabs/MainMenu/MainMenu.prefab
- Assets/MagicalWorlds/Prefabs/Classic/ClassicGame.prefab
- Assets/MagicalWorlds/Prefabs/Journey/JourneyMapRoot.prefab
- Assets/MagicalWorlds/Prefabs/Journey/JourneyGameplayRoot.prefab

**Pre-change backups:**
`ProjectBackups/MobileReadiness_20261016/`.

## Outstanding mobile QA

1. Test on a notch-equipped Android and iPhone, including different system
   gesture bars and cutout inset sizes. The dock maximum shift is capped
   for intentional layout reasons: unusually large cutouts could still need
   an extra per-device adjustment.
2. Confirm UI raycasts/drag-and-drop and gesture interactions on actual
   touchscreens. Button rectangle sizes were checked in reference Canvas
   units, not guaranteed physical millimeters.
3. Profile FPS, batching/draw calls, texture memory, GC allocations, and
   device temperature on an entry-level Android phone. Targeting 60 FPS is
   a ceiling/request, not a measured performance result.
4. Run a device orientation, pause/background/foreground, and interrupted
   Journey-save/resume matrix on physical Android and iOS builds.
5. Check visual layout on 19.5:9, 20:9, and 2.4:1 portrait devices and
   adjust the per-section maximum safe-area shifts if needed.

## Next milestone

Conduct a device-validation pass before starting irreversible art/asset
compression or major pooling/batching refactors. Continue preserving the
approved Magical Worlds art direction and the current prefab editing workflow.
