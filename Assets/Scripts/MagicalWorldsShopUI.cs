using MagicalWorlds.Journey;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Controller for the authored Shop overlay in MainMenu.prefab.
/// No visual controls or buttons are generated at runtime.</summary>
[DisallowMultipleComponent]
public sealed class MagicalWorldsShopUI : MonoBehaviour
{
    [SerializeField] private GameObject shopOverlay;
    [SerializeField] private Button closeButton;
    [SerializeField] private Text walletLabel;
    [SerializeField] private Text feedbackLabel;
    [SerializeField] private Button[] paletteButtons;
    [SerializeField] private Text[] paletteNames;
    [SerializeField] private Text[] palettePrices;
    [SerializeField] private Image[] paletteSwatches;

    public bool IsOpen => shopOverlay != null && shopOverlay.activeSelf;

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (paletteButtons != null)
        {
            for (int index = 0; index < paletteButtons.Length; index++)
            {
                int chosen = index;
                if (paletteButtons[index] != null)
                    paletteButtons[index].onClick.AddListener(() => Choose(chosen));
            }
        }
        Hide();
    }

    public void Show()
    {
        if (shopOverlay == null) return;
        shopOverlay.SetActive(true);
        if (feedbackLabel != null)
            feedbackLabel.text = "UNLOCK A BOARD AURA WITH COINS";
        Refresh();
    }

    public void Hide()
    {
        if (shopOverlay != null) shopOverlay.SetActive(false);
    }

    private void Choose(int index)
    {
        JourneyShopCatalog catalog = JourneyShopCatalog.Load();
        BoardPaletteItem item = catalog != null ? catalog.Get(index) : null;
        if (item == null) return;
        bool purchased = JourneyWallet.TrySelectPalette(item.id, out string message);
        if (feedbackLabel != null)
            feedbackLabel.text = purchased ? message : "NOT ENOUGH COINS";
        Refresh();
    }

    public void Refresh()
    {
        JourneyShopCatalog catalog = JourneyShopCatalog.Load();
        JourneyWalletView wallet = JourneyWallet.GetView();
        if (walletLabel != null)
            walletLabel.text = "YOUR COINS  " + wallet.Coins.ToString("N0");

        if (paletteButtons == null) return;
        for (int i = 0; i < paletteButtons.Length; i++)
        {
            BoardPaletteItem item = catalog != null ? catalog.Get(i) : null;
            bool valid = item != null;
            if (paletteButtons[i] != null) paletteButtons[i].gameObject.SetActive(valid);
            if (!valid) continue;

            bool equipped = JourneyWallet.EquippedPaletteId == item.id;
            bool owned = JourneyWallet.OwnsPalette(item.id);
            if (paletteNames != null && i < paletteNames.Length && paletteNames[i] != null)
                paletteNames[i].text = item.displayName.ToUpperInvariant();
            if (paletteSwatches != null && i < paletteSwatches.Length && paletteSwatches[i] != null)
                paletteSwatches[i].color = item.emptyCellColor;
            if (palettePrices != null && i < palettePrices.Length && palettePrices[i] != null)
                palettePrices[i].text = equipped ? "EQUIPPED" : owned ? "EQUIP" :
                    item.coinPrice.ToString("N0") + " COINS";
            if (paletteButtons[i] != null)
                paletteButtons[i].interactable = !equipped && (owned || wallet.Coins >= item.coinPrice);
        }
    }
}