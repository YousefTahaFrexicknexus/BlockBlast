using System;
using MagicalWorlds.Journey;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MagicalWorldsSkinsUI:MonoBehaviour
{
    [Serializable] public sealed class Card
    {
        public int category,index;
        public Button button;
        public Image frame,fill,badge;
        public Text title,status,detail;
    }
    public GameObject overlay;
    public RectTransform layout;
    public Button closeButton;
    public Button[] tabs;
    public GameObject[] pages;
    public ScrollRect[] scrolls;
    public Card[] cards;
    public Text footer;
    public Sprite normalCard,selectedCard,normalTab,selectedTab,checkIcon,lockIcon;
    public int CurrentTab {get;private set;}=2;
    public bool IsOpen=>overlay!=null&&overlay.activeSelf;
    private Rect lastSafe;private Vector2Int lastSize;private float lastScale;
    private void Awake()
    {
        closeButton.onClick.AddListener(()=>GetComponent<MagicalWorldsMenuController>().ClosePopup());
        for(int i=0;i<tabs.Length;i++){int tab=i;tabs[i].onClick.AddListener(()=>SelectTab(tab));}
        foreach(var card in cards){var selected=card;card.button.onClick.AddListener(()=>Choose(selected));}
        overlay.SetActive(false);
    }
    public void Show(){overlay.SetActive(true);ApplySafeArea();SelectTab(CurrentTab);}
    public void Hide(){overlay.SetActive(false);}
    public void SelectTab(int index)
    {
        CurrentTab=Mathf.Clamp(index,0,2);
        for(int i=0;i<3;i++){pages[i].SetActive(i==CurrentTab);tabs[i].image.sprite=i==CurrentTab?selectedTab:normalTab;tabs[i].interactable=i!=CurrentTab;}
        Refresh();Canvas.ForceUpdateCanvases();scrolls[CurrentTab].verticalNormalizedPosition=1;
    }
    private void Choose(Card card)
    {
        if(card.category==0){var item=JourneyShopCatalog.Load()?.Get(card.index);if(item!=null)JourneyWallet.TrySelectPalette(item.id,out _);}
        else{var catalog=JourneyCosmetics.Catalog;var items=card.category==1?catalog.blocks:catalog.backgrounds;if(card.index<items.Length)JourneyCosmetics.Equip(card.category==1,items[card.index].id);}
        Refresh();
    }
    public void Refresh()
    {
        var catalog=JourneyCosmetics.Catalog;var boards=JourneyShopCatalog.Load();var wallet=JourneyWallet.GetView();
        foreach(var card in cards)
        {
            bool equipped=false,owned=false;float progress=1;string status="",detail="";
            if(card.category==0)
            {
                var item=boards.Get(card.index);equipped=JourneyWallet.EquippedPaletteId==item.id;owned=JourneyWallet.OwnsPalette(item.id);
                card.title.text=item.displayName;status=equipped?"EQUIPPED":owned?"EQUIP":item.coinPrice.ToString("N0")+" COINS";
                detail=owned?"BOARD AURA":"UNLOCK WITH COINS";card.button.interactable=!equipped&&(owned||wallet.Coins>=item.coinPrice);progress=owned?1:Mathf.Clamp01((float)wallet.Coins/Mathf.Max(1,item.coinPrice));
            }
            else
            {
                var item=(card.category==1?catalog.blocks:catalog.backgrounds)[card.index];owned=JourneyCosmetics.Unlocked(item);equipped=JourneyCosmetics.Selected(card.category==1)?.id==item.id;
                card.title.text=item.displayName;status=equipped?"EQUIPPED":owned?"EQUIP":"LOCKED";detail=item.unlockLevel<=1?"INCLUDED":owned?"JOURNEY REWARD":"REACH LEVEL "+item.unlockLevel;
                if(!owned){int completed=0;for(int level=Mathf.Max(1,item.unlockLevel-10);level<item.unlockLevel;level++)if(JourneyProgress.Stars(level)>0)completed++;progress=completed/10f;status=completed+" / 10";}
                card.button.interactable=owned&&!equipped;
            }
            card.status.text=status;card.detail.text=detail;card.fill.fillAmount=progress;card.fill.color=owned?new Color(.25f,1f,.3f):new Color(.2f,.85f,1f);
            card.frame.sprite=equipped?selectedCard:normalCard;card.badge.enabled=equipped||!owned;card.badge.sprite=equipped?checkIcon:lockIcon;
        }
        footer.text=CurrentTab==0?"YOUR COINS  "+wallet.Coins.ToString("N0"):CurrentTab==1?"EARN BLOCK STYLES THROUGH JOURNEY":"REACH NEW JOURNEY CHAPTERS TO UNLOCK BACKGROUNDS";
    }
    private void LateUpdate(){if(IsOpen)ApplySafeArea();}
    public void ApplySafeArea()
    {
        var canvas=layout.GetComponentInParent<Canvas>().rootCanvas;var safe=Screen.safeArea;var size=new Vector2Int(Screen.width,Screen.height);float scale=canvas.scaleFactor;
        if(safe==lastSafe&&size==lastSize&&Mathf.Approximately(scale,lastScale))return;
        lastSafe=safe;lastSize=size;lastScale=scale;
        FitToSafeArea(safe);
    }
    public void FitToSafeArea(Rect safe)
    {
        var canvas=layout.GetComponentInParent<Canvas>().rootCanvas;float scale=canvas.scaleFactor;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)layout.parent,safe.center,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var center);
        layout.anchoredPosition=center;float fit=Mathf.Min(safe.width/(1080f*scale),safe.height/(1920f*scale));layout.localScale=Vector3.one*fit;
    }
}


