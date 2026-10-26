using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Explicit editor command; no automatic prefab mutation on import or domain reload.
public static class MagicalWorldsVectorUIAuthoring
{
    const string Art = "Assets/MagicalWorlds/Art/VectorUI_v2/";
    static readonly string[] Targets = {
        "Assets/MagicalWorlds/Prefabs/MainMenu/MainMenu.prefab",
        "Assets/MagicalWorlds/Prefabs/Classic/ClassicGame.prefab",
        "Assets/MagicalWorlds/Prefabs/Journey/JourneyLevelNode.prefab",
        "Assets/MagicalWorlds/Prefabs/Journey/JourneyMapRoot.prefab",
        "Assets/MagicalWorlds/Prefabs/Journey/JourneyGameplayRoot.prefab"
    };
    static readonly string[] Primary = {"Dialog Primary", "Next Level Button", "Restart", "Button_Play"};

    [MenuItem("Tools/Magical Worlds/Vector UI/Apply v2 Artwork")]
    public static void Apply()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Wait for Unity to finish importing and stop Play Mode before applying artwork.");
        foreach (string p in Targets)
            if (AssetDatabase.LoadAssetAtPath<GameObject>(p) == null) throw new InvalidOperationException("Missing prefab: " + p);
        foreach (string s in new[]{"Panel_Modal","Panel_Card","Panel_HUD","Panel_Board","Panel_Tray","Logo_MagicalWorlds","CTA_Play_Decoration","CTA_Journey_Decoration","Level_completed","Level_locked"}) Sprite(s);
        string backup = "ProjectBackups/VectorUI_v2_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
        foreach (string p in Targets) {
            string dst = Path.Combine(backup,p); Directory.CreateDirectory(Path.GetDirectoryName(dst)); File.Copy(p,dst,false);
        }
        try {
            foreach (string p in Targets) {
                GameObject root=PrefabUtility.LoadPrefabContents(p);
                try {
                    SkinCommon(root);
                    if (p==Targets[0]) SkinMenu(root);
                    if (p==Targets[2] || p==Targets[3]) SkinNodes(root);
                    if (PrefabUtility.SaveAsPrefabAsset(root,p)==null) throw new InvalidOperationException("Failed to save " + p);
                } finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets(); Validate();
            File.WriteAllText("ProjectReports/VectorUI_v2_Apply.json",JsonUtility.ToJson(new Receipt {status="APPLIED_AND_VALIDATED",backup=backup,utc=DateTime.UtcNow.ToString("o"),prefabs=Targets},true));
            Debug.Log("[Vector UI v2] Applied artwork and validated references. Backup: " + backup);
        } catch {
            foreach (string p in Targets) File.Copy(Path.Combine(backup,p),p,true);
            AssetDatabase.Refresh(); throw;
        }
    }
    [Serializable] class Receipt { public string status, backup, utc; public string[] prefabs; }

    static Sprite Sprite(string name) {
        Sprite s=AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png");
        if (s==null) throw new InvalidOperationException("Missing imported vector sprite: " + name);
        return s;
    }
    static Transform Find(Transform parent,string name) {
        if (parent.name==name) return parent;
        foreach(Transform child in parent) { Transform t=Find(child,name); if(t!=null) return t; }
        return null;
    }
    static void Record(UnityEngine.Object obj) {
        EditorUtility.SetDirty(obj);
        if(PrefabUtility.IsPartOfPrefabInstance(obj)) PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
    }
    static void Surface(UnityEngine.UI.Image image,string sprite,bool sliced=true) {
        if(image==null) return;
        image.sprite=Sprite(sprite); image.overrideSprite=null; image.color=Color.white;
        image.type=sliced ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
        Record(image);
        foreach(var edge in image.GetComponents<UnityEngine.UI.Outline>()) { edge.enabled=false; Record(edge); }
    }
    static void SkinButton(UnityEngine.UI.Button b,string variant="violet") {
        if(b.image==null) return;
        Surface(b.image,"Button_"+variant+"_normal");
        b.image.pixelsPerUnitMultiplier=Mathf.Max(1f,b.image.sprite.rect.height/Mathf.Max(1f,b.image.rectTransform.rect.height)); Record(b.image);
        b.targetGraphic=b.image; b.transition=UnityEngine.UI.Selectable.Transition.SpriteSwap;
        b.spriteState=new UnityEngine.UI.SpriteState {
            highlightedSprite=Sprite("Button_"+variant+"_normal"), pressedSprite=Sprite("Button_"+variant+"_pressed"),
            selectedSprite=Sprite("Button_"+variant+"_selected"),disabledSprite=Sprite("Button_"+variant+"_disabled")
        }; Record(b);
        foreach(var label in b.GetComponentsInChildren<UnityEngine.UI.Text>(true)) {
            label.color=variant=="gold" ? new Color(.35f,.15f,.025f) : new Color(1f,.96f,.9f); label.raycastTarget=false; Record(label);
        }
    }
    static void SkinCommon(GameObject root) {
        var over=Find(root.transform,"Game Over Overlay");
        if(over!=null && over.Find("Game Over Card")==null) {
            var go=new GameObject("Game Over Card",typeof(RectTransform),typeof(UnityEngine.UI.Image));go.transform.SetParent(over,false);go.transform.SetAsFirstSibling();
            var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(1010,1000);
            go.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
        }
        foreach(var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true)) {
            string n=image.name;
            if(n=="Popup_Frame" || n=="Shop - Frame" || n=="Result Card" || n=="Dialog Card" || n=="Game Over Card") { Surface(image,"Panel_Modal"); Crown(image.transform); }
            else if(n.StartsWith("Shop - ") && new[]{"Shop - Enchanted Garden","Shop - Moonlight","Shop - Rose Quartz","Shop - Emerald Glade"}.Contains(n)) Surface(image,"Panel_Card");
            else if(n=="Objective Background") Surface(image,"Panel_HUD");
            else if(n=="Board Shadow") Surface(image,"Panel_Board");
            else if(n.StartsWith("Piece Tray ")) Surface(image,"Panel_Tray");
            else if(n=="Progress Track") Surface(image,"Progress_Track");
            else if(n=="Progress Fill") { Surface(image,"Progress_Fill"); image.type=UnityEngine.UI.Image.Type.Filled; image.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal; Record(image); }
        }
        foreach(var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)) {
            if(b.name.StartsWith("Plus - ") || b.name.Contains("Backdrop") || b.name.Contains("Overlay")) continue;
            if(b.GetComponent("JourneyLevelNode")!=null) continue;
            SkinButton(b,Primary.Contains(b.name)?"gold":b.name.Contains("Menu") || b.name.Contains("Map")?"blue":"violet");
        }
        foreach(var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true)) {
            if(text.name=="Journey Heading" || text.name=="Collection Progress" || text.name=="Page Count") Backplate(text);
        }
    }
    static void ChildImage(Transform parent,string name,string sprite) {
        Transform t=parent.Find(name);
        if(t==null) { var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); t=go.transform; t.SetParent(parent,false); }
        var r=t.GetComponent<RectTransform>(); r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;
        var image=t.GetComponent<UnityEngine.UI.Image>();Surface(image,sprite,false);image.raycastTarget=false;Record(image);Record(r);
    }
    static void Crown(Transform parent) {
        string name="Vector UI - Floral Crown";
        Transform t=parent.Find(name);
        if(t==null) {var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));t=go.transform;t.SetParent(parent,false);}
        var r=t.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=new Vector2(.5f,1f);r.pivot=new Vector2(.5f,.5f);
        r.anchoredPosition=new Vector2(0,-8);r.sizeDelta=new Vector2(Mathf.Max(100,((RectTransform)parent).rect.width-80),110);
        var image=t.GetComponent<UnityEngine.UI.Image>();Surface(image,"Ornament_Crown",false);image.raycastTarget=false;Record(image);Record(r);
    }
    static void Backplate(UnityEngine.UI.Text text) {
        var source=text.rectTransform;string name=text.name+" - Vector Backplate";var parent=source.parent;
        Transform t=parent.Find(name);
        if(t==null) {var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));t=go.transform;t.SetParent(parent,false);}
        var r=t.GetComponent<RectTransform>();r.anchorMin=source.anchorMin;r.anchorMax=source.anchorMax;r.pivot=source.pivot;r.anchoredPosition=source.anchoredPosition;r.sizeDelta=source.sizeDelta+new Vector2(32,0);
        int insertion=source.GetSiblingIndex();if(t.GetSiblingIndex()<insertion) insertion--;t.SetSiblingIndex(insertion);
        var image=t.GetComponent<UnityEngine.UI.Image>();Surface(image,"Panel_Card");image.pixelsPerUnitMultiplier=Mathf.Max(1f,164f/Mathf.Max(1f,r.rect.height));image.raycastTarget=false;Record(image);Record(r);
    }
    static void Label(Transform parent,string content,Vector2 pos,Vector2 size,int fontSize,Color color) {
        Transform t=parent.Find("Vector UI - Live Label");
        if(t==null) { var go=new GameObject("Vector UI - Live Label",typeof(RectTransform),typeof(UnityEngine.UI.Text));t=go.transform;t.SetParent(parent,false); }
        var r=t.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;
        var label=t.GetComponent<UnityEngine.UI.Text>();label.text=content;label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.fontSize=fontSize;label.fontStyle=FontStyle.Bold;label.alignment=TextAnchor.MiddleCenter;label.color=color;label.raycastTarget=false;Record(label);Record(r);
    }
    static void CTA(Transform root,string name,string prefix,string text,Vector2 pos,Vector2 size,int fontSize,Color color) {
        Transform t=Find(root,name);if(t==null) throw new InvalidOperationException(name+" missing");
        var b=t.GetComponent<UnityEngine.UI.Button>();Surface(b.image,prefix+"_Decoration",false);
        b.transition=UnityEngine.UI.Selectable.Transition.SpriteSwap;
        b.spriteState=new UnityEngine.UI.SpriteState {highlightedSprite=Sprite(prefix+"_Decoration"),pressedSprite=Sprite(prefix+"_pressed"),selectedSprite=Sprite(prefix+"_selected"),disabledSprite=Sprite(prefix+"_disabled")};Record(b);
        Label(t,text,pos,size,fontSize,color);
    }
    static void SkinMenu(GameObject root) {
        Surface(Find(root.transform,"Logo_MagicalWorlds").GetComponent<UnityEngine.UI.Image>(),"Logo_MagicalWorlds",false);
        foreach(var pair in new[]{new[]{"Currency_Coins","Currency_coin_Blank"},new[]{"Currency_Gems","Currency_gem_Blank"},new[]{"Currency_Energy","Currency_energy_Blank"}})
            Surface(Find(root.transform,pair[0]).GetComponent<UnityEngine.UI.Image>(),pair[1],false);
        CTA(root.transform,"Button_Play","CTA_Play","Play",new Vector2(56,-30),new Vector2(414,132),93,new Color(.36f,.14f,.015f));
        CTA(root.transform,"Button_Journey","CTA_Journey","Journey",new Vector2(20,-8),new Vector2(415,98),55,new Color(1,.95f,1));
        foreach(string name in new[]{"Shop","Skins","Events","Settings"}) {
            Transform t=Find(root.transform,"Navigation_"+name); var b=t.GetComponent<UnityEngine.UI.Button>();
            Surface(b.image,"Nav_Surface_normal",false);
            b.spriteState=new UnityEngine.UI.SpriteState {highlightedSprite=Sprite("Nav_Surface_normal"),pressedSprite=Sprite("Nav_Surface_pressed"),selectedSprite=Sprite("Nav_Surface_selected"),disabledSprite=Sprite("Nav_Surface_disabled")};Record(b);
            ChildImage(t,"Vector UI - Icon and Label","Nav_"+name+"_IconLabel");
        }
        Transform settings=Find(root.transform,"Header_Settings");
        var sb=settings.GetComponent<UnityEngine.UI.Button>();Surface(sb.image,"Nav_Surface_normal",false);
        sb.spriteState=new UnityEngine.UI.SpriteState {highlightedSprite=Sprite("Nav_Surface_normal"),pressedSprite=Sprite("Nav_Surface_pressed"),selectedSprite=Sprite("Nav_Surface_selected"),disabledSprite=Sprite("Nav_Surface_disabled")};Record(sb);
        ChildImage(settings,"Vector UI - Gear","Icon_settings");
    }
    static void SkinNodes(GameObject root) {
        foreach(var node in root.GetComponentsInChildren<MonoBehaviour>(true).Where(x=>x!=null && x.GetType().Name=="JourneyLevelNode")) {
            var s=new SerializedObject(node);
            var b=(UnityEngine.UI.Button)s.FindProperty("selectButton").objectReferenceValue;
            var number=(UnityEngine.UI.Text)s.FindProperty("numberText").objectReferenceValue;
            var stars=(UnityEngine.UI.Text)s.FindProperty("starsText").objectReferenceValue;
            var status=(UnityEngine.UI.Text)s.FindProperty("stateText").objectReferenceValue;
            Surface(b.image,"Level_unlocked",false); b.image.preserveAspect=true; b.transition=UnityEngine.UI.Selectable.Transition.None;Record(b.image);Record(b);
            var skin=node.GetComponent<MagicalWorldsVectorLevelSkin>(); if(skin==null) skin=node.gameObject.AddComponent<MagicalWorldsVectorLevelSkin>();
            skin.button=b;skin.number=number;skin.stars=stars;skin.status=status;
            skin.unlocked=Sprite("Level_unlocked");skin.current=Sprite("Level_current");skin.completed=Sprite("Level_completed");skin.locked=Sprite("Level_locked");Record(skin);
            foreach(var item in new[]{number,stars,status}) { item.raycastTarget=false; Record(item); }
            number.rectTransform.anchoredPosition=new Vector2(0,15);stars.rectTransform.anchoredPosition=new Vector2(0,-78);status.rectTransform.anchoredPosition=new Vector2(0,-125);
            Record(number.rectTransform);Record(stars.rectTransform);Record(status.rectTransform);
        }
    }
    [MenuItem("Tools/Magical Worlds/Vector UI/Validate v2 Artwork")]
    public static void Validate() {
        foreach(string p in Targets) {
            GameObject root=AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if(root==null) throw new InvalidOperationException(p);
            foreach(var canvas in root.GetComponentsInChildren<Canvas>(true)) {
                var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                if(scaler==null || scaler.referenceResolution!=new Vector2(1080,1920)) throw new InvalidOperationException("Canvas reference changed: "+canvas.name);
            }
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) throw new InvalidOperationException("Missing script: "+t.name);
        }
        var menu=AssetDatabase.LoadAssetAtPath<GameObject>(Targets[0]);
        foreach(string n in new[]{"Button_Play","Button_Journey","Navigation_Shop","Navigation_Settings"}) {
            var b=Find(menu.transform,n).GetComponent<UnityEngine.UI.Button>();
            if(!AssetDatabase.GetAssetPath(b.image.sprite).StartsWith(Art)) throw new InvalidOperationException("Unskinned control: "+n);
            if(b.spriteState.disabledSprite==null || b.spriteState.pressedSprite==null) throw new InvalidOperationException("Incomplete states: "+n);
        }
        Debug.Log("[Vector UI v2] Structural checks passed. Run existing Main Menu, economy/shop and Journey runtime smoke checks next.");
    }
}
