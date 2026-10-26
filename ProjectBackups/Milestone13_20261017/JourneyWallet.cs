using System;
using UnityEngine;

namespace MagicalWorlds.Journey
{
    [Serializable]
    public sealed class JourneyWalletData
    {
        public int version = 1;
        public int coins;
        public int gems;
        public int energy;
        public int[] rewardedStars = new int[50];
    }

    public struct JourneyWalletView
    {
        public int Coins;
        public int Gems;
        public int Energy;
        public int MaximumEnergy;
    }

    /// <summary>
    /// Local-only first release economy. The whole wallet including the per-level
    /// reward ledger is saved under one PlayerPrefs key to prevent replay farming.
    /// Never gates a level or changes its moves, objectives or piece sequence.
    /// </summary>
    public static class JourneyWallet
    {
        public const string SaveKey = "MW.Economy.Wallet.v1";
        private const string ConfigResource = "MagicalWorlds/Economy/JourneyEconomyConfig";
        private static JourneyEconomyConfig config;

        public static JourneyEconomyConfig Config
        {
            get
            {
                if (config == null)
                    config = Resources.Load<JourneyEconomyConfig>(ConfigResource);
                return config;
            }
        }

        public static JourneyWalletView GetView()
        {
            JourneyWalletData data = ReadOrInitialize();
            return new JourneyWalletView
            {
                Coins = data.coins,
                Gems = data.gems,
                Energy = data.energy,
                MaximumEnergy = Config != null ? Mathf.Max(1, Config.maximumEnergy) : 5
            };
        }

        public static void EnsureInitialized()
        {
            ReadOrInitialize();
        }

        public static int AwardJourneyWin(int level, int stars)
        {
            if (level < 1 || level > 50 || stars < 1 || stars > 3) return 0;
            JourneyWalletData wallet = ReadOrInitialize();
            int previous = wallet.rewardedStars[level - 1];
            if (stars <= previous) return 0;

            JourneyEconomyConfig settings = Config;
            if (settings == null)
            {
                Debug.LogError("[Economy] JourneyEconomyConfig asset missing. Rewards disabled.");
                return 0;
            }

            int proposed = settings.CalculateNewCoins(level, previous, stars);
            int actual = (int)Math.Min(proposed, (long)int.MaxValue - wallet.coins);
            wallet.coins += actual;
            wallet.rewardedStars[level - 1] = stars;
            if (!Store(wallet))
            {
                Debug.LogError("[Economy] Could not save earned coins.");
                return 0;
            }
            return actual;
        }

        // Future IAP or shop integration must only call debit after
        // validating and delivering a real item; there is no purchasable
        // item, energy cost or gem spending in this milestone.
        private static bool Store(JourneyWalletData wallet)
        {
            try
            {
                PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(wallet));
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Economy] Wallet write failed: " + ex.Message);
                return false;
            }
        }

        private static JourneyWalletData ReadOrInitialize()
        {
            if (PlayerPrefs.HasKey(SaveKey))
            {
                try
                {
                    JourneyWalletData saved = JsonUtility.FromJson<JourneyWalletData>(
                        PlayerPrefs.GetString(SaveKey));
                    if (Valid(saved)) return saved;
                    Debug.LogWarning("[Economy] Stored wallet is invalid; rebuilding safely.");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Economy] Unable to parse wallet: " + ex.Message);
                }
            }

            JourneyEconomyConfig settings = Config;
            JourneyWalletData wallet = new JourneyWalletData
            {
                coins = settings != null ? Mathf.Max(0, settings.startingCoins) : 0,
                gems = settings != null ? Mathf.Max(0, settings.startingGems) : 0,
                energy = settings != null ? Mathf.Max(1, settings.maximumEnergy) : 5
            };
            // Migration: any pre-economy stars were already earned, and must
            // not trigger retroactive replay rewards on first wallet creation.
            for (int i = 1; i <= wallet.rewardedStars.Length; i++)
                wallet.rewardedStars[i - 1] = JourneyProgress.Stars(i);
            Store(wallet);
            return wallet;
        }

        private static bool Valid(JourneyWalletData data)
        {
            if (data == null || data.version != 1 || data.coins < 0 ||
                data.gems < 0 || data.energy < 0 || data.rewardedStars == null ||
                data.rewardedStars.Length != 50) return false;
            for (int i = 0; i < data.rewardedStars.Length; i++)
            {
                if (data.rewardedStars[i] < 0 || data.rewardedStars[i] > 3)
                    return false;
            }
            return true;
        }
    }
}
