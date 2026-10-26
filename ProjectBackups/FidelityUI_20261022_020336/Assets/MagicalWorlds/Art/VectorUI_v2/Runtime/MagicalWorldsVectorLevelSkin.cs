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
    private Sprite last;

    private void OnEnable() { last = null; Refresh(); }
    private void LateUpdate() { Refresh(); }
    public void Refresh()
    {
        if (button == null || button.image == null) return;
        bool open = button.interactable;
        Sprite desired = !open ? locked : stars != null && stars.text.IndexOf('★') >= 0 ? completed : current;
        if (desired == null) desired = unlocked;
        if (desired == null || desired == last) return;
        if (number != null) {
            number.enabled = true;
            number.resizeTextForBestFit = false;
            number.fontSize = open ? 78 : 36;
            number.rectTransform.anchoredPosition = new Vector2(0,open ? 15 : 98);
        }
        button.image.sprite = desired;
        button.image.overrideSprite = null;
        button.image.color = Color.white;
        last = desired;
    }
}
