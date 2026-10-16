using UnityEngine;
using UnityEngine.UI;

public sealed partial class BlockBlastPrototype
{
    private void ApplyPainterlyBackground(RectTransform backdrop)
    {
        Sprite scene = Resources.Load<Sprite>("MagicalWorlds/Backgrounds/EnchantedGarden-Gameplay");

        if (scene == null)
        {
            return;
        }

        Image image = backdrop.GetComponent<Image>();
        image.sprite = scene;
        image.color = Color.white;
        image.raycastTarget = false;

        AspectRatioFitter fitter = backdrop.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = scene.rect.width / scene.rect.height;
    }
}
