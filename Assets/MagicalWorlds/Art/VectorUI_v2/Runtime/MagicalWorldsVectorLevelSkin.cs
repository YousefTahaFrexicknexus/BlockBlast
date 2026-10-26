using UnityEngine;

// Presentation only: JourneyLevelNode still owns unlocks, stars and click actions.
[DisallowMultipleComponent]
public sealed class MagicalWorldsVectorLevelSkin : MonoBehaviour
{
    public UnityEngine.UI.Button button;
    public UnityEngine.UI.Text number;
    public UnityEngine.UI.Text stars;
    public UnityEngine.UI.Text status;
    public Sprite unlocked, current, completed, locked;
    public bool pathLayout;
    public UnityEngine.UI.Image[] ratingStars;
    public UnityEngine.UI.Image lockIcon;
    public Sprite goldStar, emptyStar;
    private Sprite last;
    private JourneyLevelNode pathNode;
    private int pathHighestUnlocked;
    private bool pathRendered, lastPathOpen;
    private string lastPathStars, lastPathStatus;

    private void OnEnable()
    {
        last = null;
        pathRendered = false;
        if (pathLayout)
        {
            pathNode = GetComponent<JourneyLevelNode>();
            pathHighestUnlocked = MagicalWorlds.Journey.JourneyProgress.HighestUnlocked;
        }
        Refresh();
    }
    private void LateUpdate() { Refresh(); }
    private void RefreshPath(bool open)
    {
        string sourceStars = stars != null ? stars.text : string.Empty;
        string sourceStatus = status != null ? status.text : string.Empty;
        if (pathRendered && open == lastPathOpen &&
            sourceStars == lastPathStars && sourceStatus == lastPathStatus) return;

        // Configure can run after OnEnable, so compare its source values until they settle.
        pathRendered = true;
        lastPathOpen = open;
        lastPathStars = sourceStars;
        lastPathStatus = sourceStatus;
        int earned = 0;
        for (int i = 0; i < sourceStars.Length; i++)
            if (sourceStars[i] == '★') earned++;
        bool next = pathNode != null && pathNode.LevelNumber == pathHighestUnlocked;
        Sprite desired = !open ? locked : earned > 0 ? completed : next ? current : unlocked;
        if (desired != null) { button.image.sprite = desired; button.image.overrideSprite = null; }
        button.image.color = Color.white;
        if (number != null) {
            number.enabled = true; number.fontSize = 48; number.resizeTextForBestFit = false;
            number.rectTransform.anchoredPosition = new Vector2(0, 12);
        }
        if (stars != null) stars.enabled = false;
        if (status != null) status.enabled = false;
        if (lockIcon != null) lockIcon.enabled = !open;
        if (ratingStars != null) for (int i = 0; i < ratingStars.Length; i++) {
            if (ratingStars[i] == null) continue;
            ratingStars[i].enabled = open;
            ratingStars[i].sprite = i < earned ? goldStar : emptyStar;
        }
    }
    public void Refresh()
    {
        if (button == null || button.image == null) return;
        bool open = button.interactable;
        if (pathLayout) { RefreshPath(open); return; }
        Sprite desired = !open ? locked : stars != null && stars.text.IndexOf('★') >= 0 ? completed : current;
        if (desired == null) desired = unlocked;
        if (desired == null || desired == last) return;
        if (number != null) {
            number.enabled = true;
            number.resizeTextForBestFit = false;
            number.fontSize = desired.name.StartsWith("level_") ? 78 : open ? 78 : 36;
            number.rectTransform.anchoredPosition = new Vector2(0,desired.name.StartsWith("level_") ? 15 : open ? 15 : 98);
        }
        button.image.sprite = desired;
        button.image.overrideSprite = null;
        button.image.color = Color.white;
        last = desired;
    }
}
