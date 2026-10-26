# Magical Worlds / BlockBlast — Milestone 12: Coins, Rewards and Economy

**Date:** 17 October 2026
**Project:** C:\Users\PC\Documents\GitHub\BlockBlast
**Status:** Implemented and tested in Unity Editor; no physical device build or purchase SDK.

## Implemented
- One local saved wallet with coins, gems, energy, and a 50-level reward-claim ledger. Stored as a JSON object under MW.Economy.Wallet.v1.
- The Main Menu preview balances (1,250 coins, 80 gems, 5/5 energy) become persisted first-install balances.
- New Journey completions grant coins once; better stars grant only the extra star reward. Replays at the same or lower stars pay zero.
- First-clear formula: 40 + 4 × level number + 20 × awarded stars, plus 100 coins on Levels 10, 20, 30, 40 and 50.
- Level 1 one-star first clear earns 64 coins; Level 50 three-star first clear earns 400 coins.
- Migrating existing stars recognizes already completed levels, avoiding duplicate first-clear rewards without erasing progress.
- Main Menu header and Shop popup display real wallet values; Shop purchases still explicitly say coming later.
- Journey win panel shows the earned coin reward. Editor-only playtests do not grant currency.
- Inspector-editable asset: Assets/Resources/MagicalWorlds/Economy/JourneyEconomyConfig.asset
- A complete 50-level reward table: JOURNEY_50_COIN_REWARDS.md.
- Theoretical total for 50 first-time three-star completions is 10,600 coins, excluding starter balance.

## Explicitly not implemented
- No in-app purchases, actual Shop items, premium gem earning, energy cost/recharge, or paid offers.
- No forced advertising or changes to the approved gameplay rules.
- No cloud wallet synchronization or anti-tamper for the MVP local wallet.

## Unity verification
- Economy wallet QA: starting balances, legacy star migration, first-clear one-time payment, checkpoint bonuses, star upgrade difference, repeat and lower-star replay protection, save and test-state restoration.
- Journey Runtime QA: actual win awarded expected coins, result panel displayed the coin gain, replay couldn't duplicate payout, and existing win/loss/navigation still passed.
- Main Menu Play Mode QA: temporary 4,321 coins / 17 gems / 3 energy displayed correctly, including Shop description; test restored wallet.
- Unity C# compilation and runtime smoke testing passed; no new errors found.

## Main files
New:
- Assets/Scripts/JourneyEconomyConfig.cs
- Assets/Scripts/JourneyWallet.cs
- Assets/Editor/JourneyEconomyAssetAuthoring.cs
- Assets/Editor/JourneyEconomyQA.cs
- Assets/Editor/JourneyEconomyMenuQA.cs
- JOURNEY_50_COIN_REWARDS.md

Changed:
- Assets/Scripts/MagicalWorldsMenuController.cs
- Assets/Scripts/JourneySession.cs
- Assets/Editor/JourneyRuntimeQA.cs

Original backups: ProjectBackups/Milestone12_20261017/

## How to test
In Unity use Tools → Magical Worlds → Economy → Validate Wallet and Rewards.
To check authored Main Menu labels use Tools → Magical Worlds → Economy → Test Main Menu Wallet Labels.
The existing Journey Gameplay Smoke Test under Tools → Magical Worlds → Journey checks the coin reward and result text in Play Mode.

## Recommended next milestone
Design meaningful coin-spending sinks, cosmetics or optional boosters and then make the Shop functional. Tune awards after real player testing, keep Gems and Energy non-gating until their purpose is defined, and validate mobile persistence on real devices.
