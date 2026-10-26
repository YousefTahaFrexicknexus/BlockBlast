# Magical Worlds — Milestone 13: Cosmetic Shop

**Status:** Implemented and verified in Unity Editor, 17 October 2026.
**Project:** C:\Users\PC\Documents\GitHub\BlockBlast
**Unity:** 6.3 LTS (6000.3.13f1)

## Delivered

**A functioning, coin-only cosmetic Shop** on the existing authored Main Menu.
The Shop uses an editable modal panel with four individual palette cards,
color swatches, price/equip buttons, wallet balance and Back control.
The original Enchanted Garden visuals and 1080 x 1920 Canvas remain intact.

The original board appearance is included free and is always available.
The three additional cosmetic palettes are:

| Palette | Coin price | Affected visuals |
|---|---:|---|
| Enchanted Garden (original) | Free | Original empty board cells and tray backgrounds |
| Moonlight | 350 | Empty board cell and tray colors |
| Rose Quartz | 600 | Empty board cell and tray colors |
| Emerald Glade | 850 | Empty board cell and tray colors |

The appearance changes are cosmetic only: occupied block piece colors,
the approved background illustrations, placement rules, 50 Journey level
objectives, seeds and scores are unchanged. The equipped palette applies in
both Classic and Journey because they share the board renderer.

## Shop flow

- Tap SHOP or the coin+ button from the Main Menu to open the authored Shop.
- An item costs coins once. Its ownership, equipped ID and reduced balance
  are saved together in the existing wallet JSON PlayerPrefs key.
- An already owned item can be equipped without an additional charge.
- The currently equipped item shows EQUIPPED.
- An unaffordable item cannot be purchased; ownership and balance remain unchanged.
- Back and Escape close Shop and refresh Main Menu's coin counter.
- The Shop has no gem spending, energy costs, ads, or IAP integration.
- The SKINS tab remains reserved for future character/cosmetic collections.

## Data-driven editing

Editable ScriptableObject:
Assets/Resources/MagicalWorlds/Economy/JourneyShopCatalog.asset

Each palette exposes an ID, display name, coin price, empty board color,
normal tray color, and selected tray color.

Authoring menu:
Tools > Magical Worlds > Shop > Build Editable Shop Panel

The builder creates the Shop once inside the existing MainMenu.prefab and
does not overwrite later Inspector modifications. All Shop buttons, labels,
swatches and panel objects are individually editable in the prefab hierarchy.

## Save compatibility

The existing wallet key MW.Economy.Wallet.v1 is retained. Its JSON now has:
- ownedPalettes: array of owned cosmetic IDs
- equippedPaletteId: selected ID, default original

Milestone 12 saved wallets without these fields are upgraded in memory,
preserving coins, gems, energy, and all existing 50-level reward claims.

## Verification performed

- Shop QA: PASSED. Four-card authored prefab integrity, free default,
  purchase and debit, repeated purchase protection, insufficient funds,
  no-charge equip, saved inventory, painted board colors, legacy wallet
  migration, and unaffected Journey coin rewards.
- Economy Menu QA: PASSED. Actual authored Main Menu Play Mode test opened
  Shop and clicked a purchase button; verified exact coin deduction,
  equipped palette, button state, and refreshed header after closing.
- Journey Runtime QA: PASSED after all Shop changes. Verified level map
  pagination, Pause/Continue, Level 1 victory and coin rewards, star/unlock
  progression, Level 2 loss and Retry/Return callbacks.
- Unity C# compile and runtime smoke passes; no recent compilation errors
  observed.
- PlayerPrefs values used by QA were restored after each test.

## Files

New:
- Assets/Scripts/JourneyShopCatalog.cs
- Assets/Scripts/JourneyBoardPalette.cs
- Assets/Scripts/MagicalWorldsShopUI.cs
- Assets/Editor/JourneyShopCatalogAuthoring.cs
- Assets/Editor/MagicalWorldsShopPrefabAuthoring.cs
- Assets/Editor/MagicalWorldsShopQA.cs
- Assets/Resources/MagicalWorlds/Economy/JourneyShopCatalog.asset

Updated:
- Assets/Scripts/JourneyWallet.cs
- Assets/Scripts/MagicalWorldsMenuController.cs
- Assets/Scripts/BlockBlastPrototype.cs
- Assets/Editor/JourneyEconomyMenuQA.cs
- Assets/MagicalWorlds/Prefabs/MainMenu/MainMenu.prefab

Pre-change copies:
ProjectBackups/Milestone13_20261017/

## Remaining work

- Give the Shop custom final illustration assets and polish through
  PhotoCraft/VectorCraft, using the approved Magical Worlds reference art.
- Confirm visual legibility, button hit targets, safe areas, performance and
  wallet persistence on actual Android/iOS devices.
- Gather playtest/economy telemetry before tuning item prices.
- Consider player-friendly optional helpers only after deciding whether
  they alter the strategic identity of Journey's limited-move puzzles.
- Coins are currently local-only PlayerPrefs: not secure for real-money sales.
