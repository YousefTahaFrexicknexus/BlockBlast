using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Scene-authored Main Menu controller. Its buttons, labels and popup panels
/// are serialized from a real Canvas prefab, never generated during gameplay.
/// </summary>
[DisallowMultipleComponent]
public sealed class MagicalWorldsMenuController : MonoBehaviour
{
    private const string ClassicScene = "ClassicGameplay";
    private const string SoundPreference = "BlockBlastPrototypeSound";
    private const string HapticsPreference = "BlockBlastPrototypeHaptics";

    [Header("Primary Navigation")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button journeyButton;

    [Header("Bottom Navigation")]
    [SerializeField] private Button shopButton;
    [SerializeField] private Button skinsButton;
    [SerializeField] private Button eventsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button headerSettingsButton;

    [Header("Currency Add Buttons")]
    [SerializeField] private Button coinsAddButton;
    [SerializeField] private Button gemsAddButton;
    [SerializeField] private Button energyAddButton;

    [Header("Currency (placeholder values until Economy milestone)")]
    [SerializeField] private Text coinsValue;
    [SerializeField] private Text gemsValue;
    [SerializeField] private Text energyValue;
    [SerializeField] private int previewCoins = 1250;
    [SerializeField] private int previewGems = 80;
    [SerializeField] private int previewEnergy = 5;
    [SerializeField] private int previewMaximumEnergy = 5;

    [Header("Shared Popup Prefab Instance")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Text popupTitle;
    [SerializeField] private Text popupDescription;
    [SerializeField] private Button popupCloseButton;
    [SerializeField] private Button popupBackdropButton;
    [SerializeField] private Button popupSoundButton;
    [SerializeField] private Button popupHapticsButton;
    [SerializeField] private Text popupSoundLabel;
    [SerializeField] private Text popupHapticsLabel;

    private bool transitioning;

    private void Awake()
    {
        Application.targetFrameRate = 60;

        Bind(playButton, PlayClassic);
        Bind(journeyButton, ShowJourney);
        Bind(shopButton, ShowShop);
        Bind(skinsButton, ShowSkins);
        Bind(eventsButton, ShowEvents);
        Bind(settingsButton, ShowSettings);
        Bind(headerSettingsButton, ShowSettings);
        Bind(coinsAddButton, ShowShop);
        Bind(gemsAddButton, ShowShop);
        Bind(energyAddButton, ShowShop);
        Bind(popupCloseButton, ClosePopup);
        Bind(popupBackdropButton, ClosePopup);
        Bind(popupSoundButton, ToggleSound);
        Bind(popupHapticsButton, ToggleHaptics);

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }

        RefreshDisplay();
        RefreshSettings();
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            Debug.LogError("[Magical Worlds] Menu prefab missing a button reference.");
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void RefreshDisplay()
    {
        if (coinsValue != null)
        {
            coinsValue.text = Mathf.Max(0, previewCoins).ToString("N0");
        }
        if (gemsValue != null)
        {
            gemsValue.text = Mathf.Max(0, previewGems).ToString("N0");
        }
        if (energyValue != null)
        {
            energyValue.text = Mathf.Clamp(previewEnergy, 0, Mathf.Max(1, previewMaximumEnergy)) +
                "/" + Mathf.Max(1, previewMaximumEnergy);
        }
    }

    public void PlayClassic()
    {
        if (transitioning)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(ClassicScene))
        {
            Debug.LogError("[Magical Worlds] ClassicGameplay is missing from enabled Build Settings.");
            return;
        }

        transitioning = true;
        SceneManager.LoadScene(ClassicScene);
    }

    public void ShowJourney()
    {
        if (transitioning) return;
        if (!Application.CanStreamedLevelBeLoaded("JourneyMap"))
        {
            Debug.LogError("[Journey] JourneyMap is not enabled in Build Settings.");
            return;
        }
        transitioning = true;
        SceneManager.LoadScene("JourneyMap");
    }

    public void ShowShop()
    {
        OpenPopup("SHOP", "The shop opens when the economy and purchases are implemented.", false);
    }

    public void ShowSkins()
    {
        OpenPopup("SKINS", "Character and theme collections are planned for a later milestone.", false);
    }

    public void ShowEvents()
    {
        OpenPopup("EVENTS", "Seasonal events and missions will be added later.", false);
    }

    public void ShowSettings()
    {
        OpenPopup("SETTINGS", "Adjust sound and vibration preferences.", true);
    }

    private void OpenPopup(string title, string description, bool isSettings)
    {
        if (popupRoot == null)
        {
            return;
        }

        popupTitle.text = title;
        popupDescription.text = description;

        if (popupSoundButton != null)
        {
            popupSoundButton.gameObject.SetActive(isSettings);
        }
        if (popupHapticsButton != null)
        {
            popupHapticsButton.gameObject.SetActive(isSettings);
        }

        RefreshSettings();
        popupRoot.SetActive(true);
    }

    public void ClosePopup()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void ToggleSound()
    {
        bool enabled = PlayerPrefs.GetInt(SoundPreference, 1) != 0;
        PlayerPrefs.SetInt(SoundPreference, enabled ? 0 : 1);
        PlayerPrefs.Save();
        RefreshSettings();
    }

    private void ToggleHaptics()
    {
        bool enabled = PlayerPrefs.GetInt(HapticsPreference, 0) != 0;
        PlayerPrefs.SetInt(HapticsPreference, enabled ? 0 : 1);
        PlayerPrefs.Save();
        RefreshSettings();
    }

    private void RefreshSettings()
    {
        if (popupSoundLabel != null)
        {
            popupSoundLabel.text = PlayerPrefs.GetInt(SoundPreference, 1) != 0 ? "SOUND  ON" : "SOUND  OFF";
        }
        if (popupHapticsLabel != null)
        {
            popupHapticsLabel.text = PlayerPrefs.GetInt(HapticsPreference, 0) != 0 ? "VIBRATION  ON" : "VIBRATION  OFF";
        }
    }

    private void Update()
    {
        if (popupRoot != null && popupRoot.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePopup();
        }
    }
}
