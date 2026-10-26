using MagicalWorlds.Journey;
using UnityEngine;
using UnityEngine.UI;
public sealed partial class BlockBlastPrototype
{
    private bool classicBlockLook;
    private bool collectionBoardTint;
    private void ApplyCollectionCosmetics()
    {
        classicBlockLook=JourneyCosmetics.Selected(true)?.id=="classic";
        collectionBoardTint=JourneyWallet.EquippedPaletteId!="original";
        var background=JourneyCosmetics.Selected(false);
        if(background==null||background.id=="garden"||background.background==null)return;
        foreach(var image in canvas.GetComponentsInChildren<Image>(true))
        {
            if(image.name!="Background" || image.transform.parent.name!="01 - Background")continue;
            image.sprite=background.background;image.color=Color.white;
            var fit=image.GetComponent<AspectRatioFitter>();if(fit!=null)fit.aspectRatio=background.background.rect.width/background.background.rect.height;
        }
    }
}


