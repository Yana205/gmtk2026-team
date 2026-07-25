using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tavern Stew ▸ Studio  (Cmd/Ctrl+Shift+T)
// One window to run the game's art side WITHOUT the Unity inspector / hierarchy and WITHOUT Play mode:
//   • Flip Tavern ⇄ Kitchen (the switch you do constantly) from a sticky toolbar.
//   • Treat each visitor as a LEVEL: pick them and their whole level loads into the open scene —
//     portrait, order (their preferences), napkin note, unlocks — all editable right here.
//   • Adjust how any hand-drawn image sits on screen: preserve-aspect, native size, match-aspect,
//     width/height, uniform scale — no more fighting distorted sprites in the RectTransform inspector.
//   • Fix a mis-imported sprite (wrong texture/sprite mode) with one button.
// Everything writes to the open scene / the SO (Undo-able) so you see it immediately, before pressing Play.
public class TavernStewStudio : EditorWindow
{
    [MenuItem("Tavern Stew/Studio %#t")]
    static void Open() => GetWindow<TavernStewStudio>("Tavern Stew Studio");

    Vector2 scroll;
    int visitorIndex;
    float nudge = 20f;
    bool showUnlocks;
    bool showPortraitSize = true;
    bool showKitchenPortraitSize;
    bool showDishSize;
    bool showTextTools = true;
    int textIndex;
    float bulkJarPct = 100f;
    float bulkJarFont = 24f;

    // Top-level tabs — one page at a time so you never wade through one giant scroll.
    static readonly string[] Tabs = { "Level", "Reactions & Dish", "Effects", "Night & Pace", "Kitchen", "Text" };
    int tab;
    float candlePreview = 1f;

    // ---------- asset lookups ----------
    static List<T> LoadAll<T>(string folder) where T : Object =>
        AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder })
            .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(x => x != null).ToList();

    static List<CharacterSO>  Characters()  => LoadAll<CharacterSO>("Assets/Data/Characters");
    static List<IngredientSO> Ingredients() => LoadAll<IngredientSO>("Assets/Data/Ingredients")
                                               .OrderBy(i => i.slot).ThenBy(i => i.displayName).ToList();

    static T[] Scene<T>() where T : Object =>
        Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    static IngredientJar[] Jars() => Scene<IngredientJar>();
    static BustDresser[]   Busts() => Scene<BustDresser>();

    static Image DishImage()
    {
        var gm = Scene<GameManager>().FirstOrDefault();
        if (!gm) return null;
        var go = new SerializedObject(gm).FindProperty("dishOnCounter").objectReferenceValue as GameObject;
        return go ? go.transform.Find("Dish")?.GetComponent<Image>() : null;
    }

    // Bust portrait images for ONE screen. Tavern (the main interaction) and Kitchen (a small corner
    // portrait) size independently, so we filter busts by which screen root they live under.
    static Image[] PortraitImages(bool tavern)
    {
        string key = tavern ? "TavernScreen" : "CookingScreen";
        return Busts().Where(b => PathOf(b.transform).Contains(key))
                      .Select(b => new SerializedObject(b).FindProperty("portraitImage").objectReferenceValue as Image)
                      .Where(i => i != null).Distinct().ToArray();
    }

    static TMP_FontAsset UiFont()     => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/CormorantGaramond SDF.asset");
    static TMP_FontAsset NapkinFont() => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Caveat SDF.asset");

    static string PathOf(Transform t)
    {
        string p = t.name;
        while (t.parent) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }

    // ---------- view switching (Tavern ⇄ Kitchen) ----------
    static ScreenManager Sm() => Scene<ScreenManager>().FirstOrDefault();
    static GameObject ScreenGO(ScreenManager sm, string field) =>
        sm ? new SerializedObject(sm).FindProperty(field).objectReferenceValue as GameObject : null;

    static void ShowView(bool tavern)
    {
        var sm = Sm(); if (!sm) return;
        var t = ScreenGO(sm, "tavernScreen");
        var k = ScreenGO(sm, "cookingScreen");
        if (t) { Undo.RecordObject(t, "Switch view"); t.SetActive(tavern); }
        if (k) { Undo.RecordObject(k, "Switch view"); k.SetActive(!tavern); }
        Dirty();
    }

    static void Dirty() { if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); }

    // ---------- core: push data → scene in edit mode ----------
    public static void ApplyIngredientArt()
    {
        foreach (var j in Jars())
        {
            var ing = j.Ingredient;
            if (!ing) continue;
            var icon = j.transform.Find("Icon")?.GetComponent<Image>();
            if (icon)
            {
                Undo.RecordObject(icon, "Apply jar art");
                icon.sprite = ing.jarSprite;
                icon.color  = ing.jarSprite ? Color.white : ing.placeholderColor;
                EditorUtility.SetDirty(icon);
            }
            var lbl = j.transform.Find("NameLabel")?.GetComponent<TMP_Text>();
            if (lbl) { Undo.RecordObject(lbl, "Apply jar art"); lbl.text = ing.displayName; EditorUtility.SetDirty(lbl); }
        }
        Dirty();
    }

    // Show one visitor on-screen (tavern bust + kitchen portrait) + preview their dish, in edit mode.
    public static void PreviewVisitor(CharacterSO c)
    {
        if (!c) return;
        foreach (var bust in Busts())
        {
            var so = new SerializedObject(bust);
            SetImage(so.FindProperty("portraitImage").objectReferenceValue as Image, c.portrait, true);
            // hide the procedural dressing layers — the portrait is the whole character now
            foreach (var f in new[] { "hatLayer", "sideLayer", "sauceLayer" })
                Enable(so.FindProperty(f).objectReferenceValue as Image, false);
            foreach (var f in new[] { "hatLabel", "sideLabel", "sauceLabel" })
                EnableText(so.FindProperty(f).objectReferenceValue as TMP_Text, false);
        }
        var dish = DishImage();
        if (dish && c.main && c.main.dishSprite) SetImage(dish, c.main.dishSprite, false);
        Dirty();
    }

    static void SetImage(Image img, Sprite s, bool enable)
    {
        if (!img) return;
        Undo.RecordObject(img, "Preview art");
        img.sprite = s;
        img.color  = s ? Color.white : img.color;
        if (enable) img.enabled = true;
        EditorUtility.SetDirty(img);
    }
    static void Enable(Image img, bool on) { if (img) { Undo.RecordObject(img, "toggle"); img.enabled = on; EditorUtility.SetDirty(img); } }
    static void EnableText(TMP_Text t, bool on) { if (t) { Undo.RecordObject(t, "toggle"); t.enabled = on; EditorUtility.SetDirty(t); } }

    // ---------- GUI ----------
    void OnGUI()
    {
        DrawToolbar();   // sticky top — not inside the scroll view
        DrawTabs();      // page selector — also sticky, keeps each area short

        scroll = EditorGUILayout.BeginScrollView(scroll);
        switch (tab)
        {
            case 0: DrawCharacterLevel(); break;
            case 1: DrawReactionsDish();  break;
            case 2: DrawEffects();        break;
            case 3: DrawNightPace();      break;
            case 4: DrawIngredients();    break;
            case 5: DrawTextTools();      break;
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawTabs()
    {
        EditorGUI.BeginChangeCheck();
        int t = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(24));
        if (EditorGUI.EndChangeCheck()) { tab = t; scroll = Vector2.zero; GUI.FocusControl(null); }
    }

    // Tavern ⇄ Kitchen switch + Apply-all, always visible at the top.
    void DrawToolbar()
    {
        var sm = Sm();
        bool tavernLive = ScreenGO(sm, "tavernScreen")?.activeSelf ?? false;

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            using (new EditorGUI.DisabledScope(sm == null))
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = tavernLive ? new Color(0.55f, 0.8f, 1f) : prev;
                if (GUILayout.Button("🍺  Tavern", EditorStyles.toolbarButton, GUILayout.Width(90))) ShowView(true);
                GUI.backgroundColor = !tavernLive && sm ? new Color(1f, 0.8f, 0.5f) : prev;
                if (GUILayout.Button("🍳  Kitchen", EditorStyles.toolbarButton, GUILayout.Width(90))) ShowView(false);
                GUI.backgroundColor = prev;
            }
            if (sm == null) GUILayout.Label("no ScreenManager in scene", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("★ Apply ALL art", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                ApplyIngredientArt();
                var cs = Characters();
                if (cs.Count > 0) PreviewVisitor(cs[Mathf.Clamp(visitorIndex, 0, cs.Count - 1)]);
            }
        }
    }

    // ---------- shared lookups for the tavern-feel tabs ----------
    static T FindSO<T>() where T : ScriptableObject =>
        AssetDatabase.FindAssets("t:" + typeof(T).Name)
            .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault();

    static ReactionFX   ReactionInScene() => Scene<ReactionFX>().FirstOrDefault();
    static CustomerView CustView()        => Scene<CustomerView>().FirstOrDefault();
    static NightClock   Clock()           => Scene<NightClock>().FirstOrDefault();

    static GameObject DishOnCounterGO()
    {
        var gm = Scene<GameManager>().FirstOrDefault();
        return gm ? new SerializedObject(gm).FindProperty("dishOnCounter").objectReferenceValue as GameObject : null;
    }
    static Image CandleFill()
    {
        var c = Clock();
        return c ? new SerializedObject(c).FindProperty("candleFill").objectReferenceValue as Image : null;
    }
    static TMP_Text ClockLabel()
    {
        var c = Clock();
        return c ? new SerializedObject(c).FindProperty("timeLabel").objectReferenceValue as TMP_Text : null;
    }

    // Undo-able slider bound straight to an SO float — the whole point of the feel tabs.
    static void SOSlider(ScriptableObject so, string label, float val, float min, float max, System.Action<float> set)
    {
        EditorGUI.BeginChangeCheck();
        float v = EditorGUILayout.Slider(label, val, min, max);
        if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(so, "Tune " + label); set(v); EditorUtility.SetDirty(so); }
    }
    static void SOToggle(ScriptableObject so, string label, bool val, System.Action<bool> set)
    {
        EditorGUI.BeginChangeCheck();
        bool v = EditorGUILayout.ToggleLeft(label, val);
        if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(so, "Toggle " + label); set(v); EditorUtility.SetDirty(so); }
    }

    // ---------- Tab: Reactions & Dish (tavern) ----------
    // See the customer's happy/sad beat and the served bowl WITHOUT Play mode, and tune the feel live.
    void DrawReactionsDish()
    {
        Header("Reaction beat — preview the customer's happy/sad moment (tavern)");
        var feel = FindSO<TavernFeelSO>();
        int cph = FindSO<GameConfigSO>()?.coinsPerHeart ?? 5;

        EditorGUILayout.LabelField("Preview on the tavern customer (edit mode — a static pose):", EditorStyles.miniLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("😞 0♥"))  PreviewReaction(0, cph);
            if (GUILayout.Button("😐 1♥"))  PreviewReaction(1, cph);
            if (GUILayout.Button("🙂 2♥"))  PreviewReaction(2, cph);
            if (GUILayout.Button("😍 3♥"))  PreviewReaction(3, cph);
            GUILayout.Space(8);
            if (GUILayout.Button("Clear", GUILayout.Width(70))) ClearReactionPreview();
        }
        EditorGUILayout.HelpBox("Shows N hearts + the coin popup over the customer. Enter Play for the full animated beat.", MessageType.None);

        Header("Served dish on the counter");
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Show served dish (current visitor's Main)", GUILayout.Height(24))) SetDishOnCounter(true);
            if (GUILayout.Button("Hide", GUILayout.Width(70), GUILayout.Height(24)))               SetDishOnCounter(false);
        }
        showDishSize = EditorGUILayout.Foldout(showDishSize, "Dish size & proportions", true);
        if (showDishSize)
        {
            var chars = Characters();
            var c = chars.Count > 0 ? chars[Mathf.Clamp(visitorIndex, 0, chars.Count - 1)] : null;
            FitScale(new[] { DishImage() }, c && c.main ? c.main.dishSprite : null);
        }

        Header("Feel — tune the reaction (writes to TavernFeelSO)");
        if (feel == null) { EditorGUILayout.HelpBox("No TavernFeelSO found in the project.", MessageType.Warning); return; }
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUI.BeginChangeCheck();
            int minH = EditorGUILayout.IntSlider("Hearts for happy face+hop", feel.happyFaceMinHearts, 1, 3);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(feel, "Tune"); feel.happyFaceMinHearts = minH; EditorUtility.SetDirty(feel); }

            SOSlider(feel, "Happy hop height (px)", feel.happyHopHeight,   0f, 100f, v => feel.happyHopHeight = v);
            SOSlider(feel, "Happy hop time (s)",    feel.happyHopSeconds,  0.05f, 1f, v => feel.happyHopSeconds = v);
            SOSlider(feel, "Sad droop (px)",        feel.sadDroopPixels,   0f, 60f,  v => feel.sadDroopPixels = v);
            EditorGUILayout.Space(4);
            SOSlider(feel, "Reaction length (s)",   feel.reactionTotalSeconds, 0.3f, 3f, v => feel.reactionTotalSeconds = v);
            SOSlider(feel, "Heart pop interval (s)",feel.heartPopInterval, 0.02f, 0.6f, v => feel.heartPopInterval = v);
            SOSlider(feel, "Heart pop scale",       feel.heartPopScale,    1f, 1.6f, v => feel.heartPopScale = v);
            SOSlider(feel, "Coin fly time (s)",     feel.coinFlySeconds,   0.1f, 2f,  v => feel.coinFlySeconds = v);
            SOSlider(feel, "Coin rise (px)",        feel.coinRisePixels,   0f, 200f, v => feel.coinRisePixels = v);
            SOSlider(feel, "Fade in (s)",           feel.fadeInSeconds,    0.05f, 2f, v => feel.fadeInSeconds = v);
            SOSlider(feel, "Fade out (s)",          feel.fadeOutSeconds,   0.05f, 2f, v => feel.fadeOutSeconds = v);
        }
    }

    void PreviewReaction(int hearts, int coinsPerHeart)
    {
        ShowView(true);
        var cv = CustView();
        if (cv)
        {
            var so = new SerializedObject(cv);
            if (so.FindProperty("group").objectReferenceValue is CanvasGroup grp)
            { Undo.RecordObject(grp, "Preview reaction"); grp.alpha = 1f; grp.interactable = true; grp.blocksRaycasts = true; EditorUtility.SetDirty(grp); }
            if (so.FindProperty("thoughtBubble").objectReferenceValue is GameObject tb)
            { Undo.RecordObject(tb, "Preview reaction"); tb.SetActive(false); EditorUtility.SetDirty(tb); }
        }
        var rf = ReactionInScene();
        if (rf)
        {
            var so = new SerializedObject(rf);
            var arr = so.FindProperty("hearts");
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue is GameObject h)
                { Undo.RecordObject(h, "Preview reaction"); h.SetActive(i < hearts); EditorUtility.SetDirty(h); }
            if (so.FindProperty("coinPopupLabel").objectReferenceValue is TMP_Text lbl)
            {
                Undo.RecordObject(lbl, "Preview reaction"); Undo.RecordObject(lbl.gameObject, "Preview reaction");
                lbl.text = "+" + (hearts * coinsPerHeart);
                lbl.gameObject.SetActive(hearts > 0);
                EditorUtility.SetDirty(lbl);
            }
        }
        Dirty();
    }

    void ClearReactionPreview()
    {
        var rf = ReactionInScene();
        if (rf)
        {
            var so = new SerializedObject(rf);
            var arr = so.FindProperty("hearts");
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue is GameObject h)
                { Undo.RecordObject(h, "Clear preview"); h.SetActive(false); EditorUtility.SetDirty(h); }
            if (so.FindProperty("coinPopupLabel").objectReferenceValue is TMP_Text lbl)
            { Undo.RecordObject(lbl.gameObject, "Clear preview"); lbl.gameObject.SetActive(false); EditorUtility.SetDirty(lbl); }
        }
        Dirty();
    }

    void SetDishOnCounter(bool on)
    {
        ShowView(true);
        var go = DishOnCounterGO();
        if (go) { Undo.RecordObject(go, "Toggle dish"); go.SetActive(on); EditorUtility.SetDirty(go); }
        if (on)
        {
            var chars = Characters();
            var c = chars.Count > 0 ? chars[Mathf.Clamp(visitorIndex, 0, chars.Count - 1)] : null;
            var dish = DishImage();
            if (dish && c && c.main && c.main.dishSprite) SetImage(dish, c.main.dishSprite, true);
        }
        Dirty();
    }

    // ---------- Tab: Effects ----------
    // Every animation/juice knob in one place: the customer bust's entrance/idle/exit, the kitchen
    // jar+pot juice, and how dark a locked catch-of-the-day silhouette reads. All write to the Feel
    // SOs (or the jars) and update the running tweens LIVE while you're in Play mode.
    void DrawEffects()
    {
        Header("Effects — motion & juice (drag in Play mode to feel it live)");
        var tf = FindSO<TavernFeelSO>();
        var kf = KitchenFeel();
        EditorGUILayout.HelpBox("These drive runtime animation. Press Play, then drag — the sliders retune the tweens without leaving Play.", MessageType.Info);

        EditorGUILayout.LabelField("Customer bust — entrance · idle · exit  (TavernFeelSO)", EditorStyles.miniBoldLabel);
        if (tf == null) EditorGUILayout.HelpBox("No TavernFeelSO found in the project.", MessageType.Warning);
        else using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            SOSlider(tf, "Fade in (s)",           tf.fadeInSeconds,    0.05f, 2f,  v => tf.fadeInSeconds = v);
            SOSlider(tf, "Enter rise (px)",       tf.enterRisePixels,  0f, 250f,   v => tf.enterRisePixels = v);
            EditorGUILayout.Space(3);
            SOSlider(tf, "Idle bob (px)",         tf.idleBobPixels,    0f, 30f,    v => tf.idleBobPixels = v);
            SOSlider(tf, "Idle bob cycle (s)",    tf.idleBobSeconds,   0.5f, 4f,   v => tf.idleBobSeconds = v);
            EditorGUILayout.Space(3);
            SOSlider(tf, "Fade out (s)",          tf.fadeOutSeconds,   0.05f, 2f,  v => tf.fadeOutSeconds = v);
            SOSlider(tf, "Exit drift (px)",       tf.exitDriftPixels,  0f, 250f,   v => tf.exitDriftPixels = v);
            EditorGUILayout.LabelField("Happy drifts up, unhappy slumps down as they fade.", EditorStyles.miniLabel);
            EditorGUILayout.Space(3);
            SOSlider(tf, "Happy hop height (px)", tf.happyHopHeight,   0f, 100f,   v => tf.happyHopHeight = v);
            SOSlider(tf, "Happy hop time (s)",    tf.happyHopSeconds,  0.05f, 1f,  v => tf.happyHopSeconds = v);
            SOSlider(tf, "Sad droop (px)",        tf.sadDroopPixels,   0f, 60f,    v => tf.sadDroopPixels = v);
            SOSlider(tf, "Sad droop time (s)",    tf.sadDroopSeconds,  0.05f, 1f,  v => tf.sadDroopSeconds = v);
        }

        Header("Kitchen juice — jar click/hover, arc into the pot, pot bounce  (KitchenFeelSO)");
        if (kf == null) EditorGUILayout.HelpBox("No KitchenFeelSO found in the project.", MessageType.Warning);
        else using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            SOSlider(kf, "Jar hover scale",    kf.jarHoverScale,    1f, 1.5f,   v => kf.jarHoverScale = v);
            SOSlider(kf, "Jar punch scale",    kf.jarPunchScale,    1f, 1.5f,   v => kf.jarPunchScale = v);
            SOSlider(kf, "Jar punch time (s)", kf.jarPunchSeconds,  0.02f, 0.5f, v => kf.jarPunchSeconds = v);
            EditorGUILayout.Space(3);
            SOSlider(kf, "Arc height (px)",    kf.arcHeight,        0f, 400f,   v => kf.arcHeight = v);
            SOSlider(kf, "Arc time (s)",       kf.arcSeconds,       0.05f, 1.5f, v => kf.arcSeconds = v);
            SOSlider(kf, "Pot bounce (px)",    kf.potBouncePixels,  0f, 40f,    v => kf.potBouncePixels = v);
            SOSlider(kf, "Pot bounce time (s)",kf.potBounceSeconds, 0.02f, 0.5f, v => kf.potBounceSeconds = v);
            EditorGUILayout.Space(3);
            SOSlider(kf, "Serve pop scale",    kf.servePopScale,    1f, 1.5f,   v => kf.servePopScale = v);
            SOSlider(kf, "Serve pop time (s)", kf.servePopSeconds,  0.02f, 0.5f, v => kf.servePopSeconds = v);
        }

        Header("Catch of the Day — locked silhouette darkness (all jars)");
        DrawSilhouetteTint();
    }

    // The catch jars' silhouette colour is a per-IngredientJar serialized field (lockedTint); edit them
    // all at once so "blacked out" reads consistently. Applied at runtime by IngredientJar.SetLocked.
    void DrawSilhouetteTint()
    {
        var jars = Jars();
        if (jars.Length == 0) { EditorGUILayout.HelpBox("No jars in the open scene.", MessageType.None); return; }
        var read = new SerializedObject(jars[0]).FindProperty("lockedTint");
        if (read == null) { EditorGUILayout.HelpBox("IngredientJar has no lockedTint field.", MessageType.None); return; }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUI.BeginChangeCheck();
            Color c = EditorGUILayout.ColorField("Locked silhouette tint", read.colorValue);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var j in jars)
                {
                    var so = new SerializedObject(j);
                    var p = so.FindProperty("lockedTint");
                    if (p == null) continue;
                    Undo.RecordObject(j, "Silhouette tint");
                    p.colorValue = c;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(j);
                }
                Dirty();
            }
            EditorGUILayout.LabelField($"→ applied to {jars.Length} jars. Re-open the kitchen (or re-enter Play) to re-tint locked jars.", EditorStyles.miniLabel);
        }
    }

    // ---------- Tab: Night & Pace ----------
    // The desk candle (night timer) + the tempo knobs. Candle preview is edit-mode only; the
    // GameConfig/DebugConfig knobs also drive Play Mode live (they write to the SO).
    void DrawNightPace()
    {
        Header("Candle — the desk night timer");
        var cfg = FindSO<GameConfigSO>();
        var dbg = FindSO<DebugConfigSO>();
        var candle = CandleFill();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            if (candle == null)
                EditorGUILayout.HelpBox("No candle Image wired on NightClock (candleFill).", MessageType.None);
            else
            {
                EditorGUI.BeginChangeCheck();
                candlePreview = EditorGUILayout.Slider("Preview fill (night left)", candlePreview, 0f, 1f);
                if (EditorGUI.EndChangeCheck()) SetCandle(candlePreview);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("🕯 Full (night start)")) { candlePreview = 1f; SetCandle(1f); }
                    if (GUILayout.Button("Half"))                   { candlePreview = 0.5f; SetCandle(0.5f); }
                    float lc = (cfg && cfg.nightSeconds > 0) ? cfg.lastCallSeconds / cfg.nightSeconds : 0.15f;
                    if (GUILayout.Button("Last call"))              { candlePreview = lc; SetCandle(lc); }
                    if (GUILayout.Button("Out (night end)"))        { candlePreview = 0f; SetCandle(0f); }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Show candle")) EnableGO(candle.gameObject, true);
                    if (GUILayout.Button("Hide candle")) EnableGO(candle.gameObject, false);
                }
            }
        }

        Header("Start / freeze the night (Play Mode behaviour)");
        if (dbg == null) EditorGUILayout.HelpBox("No DebugConfigSO found.", MessageType.None);
        else
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                SOToggle(dbg, "❄ Freeze the night — clock & patience stand still (design mode)", dbg.freezeTimers, v => dbg.freezeTimers = v);
                SOToggle(dbg, "⏩ Skip intro — the night starts the moment you press Play", dbg.skipIntro, v => dbg.skipIntro = v);
                SOSlider(dbg, "Night speed ×  (4 ≈ whole night in ~45s)", dbg.nightSpeedMultiplier, 1f, 10f, v => dbg.nightSpeedMultiplier = v);
            }

        Header("Pace & time (GameConfigSO)");
        if (cfg == null) { EditorGUILayout.HelpBox("No GameConfigSO found.", MessageType.Warning); return; }
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            SOSlider(cfg, "Night length (s)",           cfg.nightSeconds,          60f, 600f, v => cfg.nightSeconds = v);
            SOSlider(cfg, "Last call at (s left)",       cfg.lastCallSeconds,       10f, 60f,  v => cfg.lastCallSeconds = v);
            SOSlider(cfg, "Patience per customer (s)",   cfg.patienceSeconds,       5f, 60f,   v => cfg.patienceSeconds = v);
            SOSlider(cfg, "Delay between customers (s)", cfg.delayBetweenCustomers, 0f, 3f,    v => cfg.delayBetweenCustomers = v);
            int m = Mathf.FloorToInt(cfg.nightSeconds / 60f), s = Mathf.FloorToInt(cfg.nightSeconds % 60f);
            EditorGUILayout.LabelField($"→ a full night runs {m}:{s:00}", EditorStyles.miniLabel);
        }
    }

    void SetCandle(float frac)
    {
        var img = CandleFill();
        if (img) { Undo.RecordObject(img, "Preview candle"); img.type = Image.Type.Filled; img.fillAmount = frac; EditorUtility.SetDirty(img); }
        var lbl = ClockLabel();
        var cfg = FindSO<GameConfigSO>();
        if (lbl && cfg)
        {
            float secs = cfg.nightSeconds * frac;
            int m = Mathf.FloorToInt(secs / 60f), s = Mathf.FloorToInt(secs % 60f);
            Undo.RecordObject(lbl, "Preview candle"); lbl.text = m + ":" + s.ToString("00"); EditorUtility.SetDirty(lbl);
        }
        Dirty();
    }

    static void EnableGO(GameObject go, bool on)
    {
        if (!go) return;
        Undo.RecordObject(go, "Toggle");
        go.SetActive(on);
        EditorUtility.SetDirty(go);
        Dirty();
    }

    // ---------- Character = Level ----------
    void DrawCharacterLevel()
    {
        var chars = Characters();
        Header("Level — pick a visitor, edit everything about them");
        if (chars.Count == 0) { EditorGUILayout.HelpBox("No CharacterSO in Assets/Data/Characters.", MessageType.Warning); return; }

        visitorIndex = Mathf.Clamp(visitorIndex, 0, chars.Count - 1);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("◀", GUILayout.Width(30))) { visitorIndex = (visitorIndex - 1 + chars.Count) % chars.Count; LoadLevel(chars[visitorIndex]); }
            EditorGUI.BeginChangeCheck();
            visitorIndex = EditorGUILayout.Popup(visitorIndex, chars.Select(c => c.displayName).ToArray());
            if (EditorGUI.EndChangeCheck()) LoadLevel(chars[visitorIndex]);
            if (GUILayout.Button("▶", GUILayout.Width(30))) { visitorIndex = (visitorIndex + 1) % chars.Count; LoadLevel(chars[visitorIndex]); }
            if (GUILayout.Button("Load level in Scene", GUILayout.Width(140))) LoadLevel(chars[visitorIndex]);
        }

        var c = chars[visitorIndex];

        // Identity + portrait + live preview
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            var tex = c.portrait ? AssetPreview.GetAssetPreview(c.portrait) : null;
            GUILayout.Label(tex, GUILayout.Width(84), GUILayout.Height(84));
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUI.BeginChangeCheck();
                var name = EditorGUILayout.TextField("Name", c.displayName);
                var p = (Sprite)EditorGUILayout.ObjectField("Portrait", c.portrait, typeof(Sprite), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(c, "Edit visitor");
                    c.displayName = name; c.portrait = p;
                    EditorUtility.SetDirty(c); PreviewVisitor(c);
                }
                ImportFixLine(c.portrait);
            }
        }

        // Tavern portrait — the big hero, the main interaction.
        showPortraitSize = EditorGUILayout.Foldout(showPortraitSize, "Tavern portrait size (hero — switch to 🍺 to see it)", true);
        if (showPortraitSize) FitScale(PortraitImages(true), c.portrait);

        // Kitchen portrait — the small corner portrait; sized independently from the tavern one.
        showKitchenPortraitSize = EditorGUILayout.Foldout(showKitchenPortraitSize, "Kitchen portrait size (corner — switch to 🍳 to see it)", true);
        if (showKitchenPortraitSize) FitScale(PortraitImages(false), c.portrait);

        // Their order = their preferences
        EditorGUILayout.LabelField("Order — their favorite stew (the answer)", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            SlotField(c, "Main",  SlotType.Main,  () => c.main,  v => c.main = v);
            SlotField(c, "Side",  SlotType.Side,  () => c.side,  v => c.side = v);
            SlotField(c, "Sauce", SlotType.Sauce, () => c.sauce, v => c.sauce = v);
        }

        // Dish preview + proportions
        showDishSize = EditorGUILayout.Foldout(showDishSize, "Served dish size & proportions (counter)", true);
        if (showDishSize)
        {
            var dishSprite = c.main ? c.main.dishSprite : null;
            if (c.main && !dishSprite) EditorGUILayout.HelpBox($"{c.main.displayName} has no dishSprite set.", MessageType.None);
            FitScale(new[] { DishImage() }, dishSprite);
        }

        // Napkin note
        EditorGUILayout.LabelField("Napkin note (verbatim writer text)", EditorStyles.miniBoldLabel);
        EditorGUI.BeginChangeCheck();
        var napkin = EditorGUILayout.TextArea(c.napkinText, GUILayout.MinHeight(46));
        if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(c, "Edit napkin"); c.napkinText = napkin; EditorUtility.SetDirty(c); }

        // Unlocks (ingredient locks are global; surfaced here for level management)
        showUnlocks = EditorGUILayout.Foldout(showUnlocks, "Unlocks — which ingredients start locked (global)", true);
        if (showUnlocks)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                foreach (var ing in Ingredients())
                {
                    EditorGUI.BeginChangeCheck();
                    bool locked = EditorGUILayout.ToggleLeft($"{ing.displayName}  [{ing.slot}]  — starts locked", ing.startsLocked);
                    if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(ing, "Toggle unlock"); ing.startsLocked = locked; EditorUtility.SetDirty(ing); }
                }
            }
        }
    }

    void LoadLevel(CharacterSO c)
    {
        ShowView(true);      // levels are seen from the tavern first
        PreviewVisitor(c);
    }

    void SlotField(CharacterSO c, string label, SlotType slot, System.Func<IngredientSO> get, System.Action<IngredientSO> set)
    {
        EditorGUI.BeginChangeCheck();
        var v = (IngredientSO)EditorGUILayout.ObjectField(label, get(), typeof(IngredientSO), false);
        if (EditorGUI.EndChangeCheck() && (v == null || v.slot == slot))
        {
            Undo.RecordObject(c, "Set " + label);
            set(v);
            EditorUtility.SetDirty(c);
            PreviewVisitor(c);
        }
        else if (v != null && v.slot != slot)
        {
            EditorGUILayout.HelpBox($"{v.displayName} is a {v.slot}, not a {slot}.", MessageType.Warning);
        }
    }

    // ---------- reusable "keep the asset's real proportions, only resize" controls ----------
    // Drives EVERY passed image at once (e.g. both busts) so edits show live. The golden rule:
    // the rect is ALWAYS sized to the sprite's native aspect (native px × scale), so it can never
    // stretch — you only pick how big. Undo-able, writes to the open scene in edit mode.
    void FitScale(Image[] imgs, Sprite sprite)
    {
        imgs = imgs?.Where(i => i != null).ToArray() ?? new Image[0];
        if (imgs.Length == 0) { EditorGUILayout.HelpBox("That image isn't in the open scene right now.", MessageType.None); return; }
        if (!sprite) { EditorGUILayout.HelpBox("No sprite assigned yet — set one above to resize by its real proportions.", MessageType.None); return; }

        Vector2 native = new Vector2(sprite.rect.width, sprite.rect.height);
        if (native.y <= 0) return;
        float aspect = native.x / native.y;

        var rt0 = imgs[0].rectTransform;
        float curPct = rt0.sizeDelta.y > 0 ? rt0.sizeDelta.y / native.y * 100f : 100f;

        EditorGUILayout.LabelField($"Asset is {native.x:0}×{native.y:0}px  (aspect {aspect:0.00})  •  driving {imgs.Length} image(s)", EditorStyles.miniLabel);

        // Size %, keeping the asset's proportions — this is the "unstretch + only-larger" control.
        EditorGUI.BeginChangeCheck();
        float pct = EditorGUILayout.Slider("Size %", curPct, 10f, 400f);
        if (EditorGUI.EndChangeCheck()) ApplyFit(imgs, native, pct / 100f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reset to 100% (native)", EditorStyles.miniButton)) ApplyFit(imgs, native, 1f);
            if (GUILayout.Button("Fit height, keep aspect", EditorStyles.miniButton)) ApplyFit(imgs, native, rt0.sizeDelta.y / native.y);
            if (GUILayout.Button("−10%", EditorStyles.miniButton, GUILayout.Width(48))) ApplyFit(imgs, native, Mathf.Max(0.1f, curPct - 10f) / 100f);
            if (GUILayout.Button("+10%", EditorStyles.miniButton, GUILayout.Width(48))) ApplyFit(imgs, native, (curPct + 10f) / 100f);
        }
        EditorGUILayout.LabelField($"→ {rt0.sizeDelta.x:0}×{rt0.sizeDelta.y:0}px", EditorStyles.miniLabel);
    }

    // Set every image to native-aspect size × scale. preserveAspect on = belt-and-suspenders no-stretch.
    static void ApplyFit(Image[] imgs, Vector2 native, float scale)
    {
        foreach (var img in imgs)
        {
            var rt = img.rectTransform;
            Undo.RecordObject(rt, "Fit & scale");
            Undo.RecordObject(img, "Fit & scale");
            rt.sizeDelta = native * scale;
            rt.localScale = Vector3.one;
            img.preserveAspect = true;
            img.type = Image.Type.Simple;
            EditorUtility.SetDirty(rt);
            EditorUtility.SetDirty(img);
        }
        Dirty();
    }

    // Detect a sprite that was imported wrong (not a Single sprite) and offer a one-click fix.
    static void ImportFixLine(Sprite sprite)
    {
        if (!sprite) return;
        var path = AssetDatabase.GetAssetPath(sprite);
        if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
        if (ti.textureType == TextureImporterType.Sprite && ti.spriteImportMode == SpriteImportMode.Single) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("⚠ imported as " +
                (ti.textureType != TextureImporterType.Sprite ? ti.textureType.ToString() : ti.spriteImportMode + " sprite"),
                EditorStyles.miniLabel);
            if (GUILayout.Button("Fix → Single Sprite", EditorStyles.miniButton, GUILayout.Width(150)))
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.SaveAndReimport();
            }
        }
    }

    // ---------- Kitchen bulk actions (there are a lot of jars) ----------
    static KitchenFeelSO KitchenFeel() =>
        AssetDatabase.FindAssets("t:KitchenFeelSO")
            .Select(g => AssetDatabase.LoadAssetAtPath<KitchenFeelSO>(AssetDatabase.GUIDToAssetPath(g)))
            .FirstOrDefault();

    static Image JarIcon(IngredientJar jar) => jar ? jar.transform.Find("Icon")?.GetComponent<Image>() : null;
    static TMP_Text JarLabel(IngredientJar jar) => jar ? jar.transform.Find("NameLabel")?.GetComponent<TMP_Text>() : null;

    void DrawKitchenBulk()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Bulk — apply to ALL jars at once", EditorStyles.boldLabel);

            // Size % (each jar keeps its own sprite's proportions)
            using (new EditorGUILayout.HorizontalScope())
            {
                bulkJarPct = EditorGUILayout.Slider("Jar size %", bulkJarPct, 10f, 400f);
                if (GUILayout.Button("Apply size to all", GUILayout.Width(120))) BulkJarSize(bulkJarPct / 100f);
                if (GUILayout.Button("Native", GUILayout.Width(60))) BulkJarSize(1f);
            }

            // Hover effect (data-driven on KitchenFeelSO → every jar reads it at runtime)
            var feel = KitchenFeel();
            using (new EditorGUI.DisabledScope(feel == null))
            {
                EditorGUI.BeginChangeCheck();
                float hover = EditorGUILayout.Slider("Hover scale", feel ? feel.jarHoverScale : 1.1f, 1f, 1.5f);
                if (EditorGUI.EndChangeCheck() && feel)
                { Undo.RecordObject(feel, "Jar hover scale"); feel.jarHoverScale = hover; EditorUtility.SetDirty(feel); }
            }
            if (feel == null) EditorGUILayout.LabelField("(no KitchenFeelSO found — hover uses default 1.1)", EditorStyles.miniLabel);

            // Name-label font size
            using (new EditorGUILayout.HorizontalScope())
            {
                bulkJarFont = EditorGUILayout.FloatField("Jar name font size", bulkJarFont);
                if (GUILayout.Button("Apply font size to all labels", GUILayout.Width(200))) BulkJarFont(bulkJarFont);
            }
        }
    }

    void BulkJarSize(float scale)
    {
        int n = 0;
        foreach (var jar in Jars())
        {
            var icon = JarIcon(jar);
            var sprite = jar.Ingredient ? jar.Ingredient.jarSprite : null;
            if (!icon) continue;
            if (sprite) ApplyFit(new[] { icon }, new Vector2(sprite.rect.width, sprite.rect.height), scale);
            else { Undo.RecordObject(icon.rectTransform, "Bulk jar size"); icon.rectTransform.sizeDelta = new Vector2(96f, 96f) * scale; EditorUtility.SetDirty(icon.rectTransform); }
            n++;
        }
        Dirty();
        Debug.Log($"Bulk jar size {scale * 100f:0}% applied to {n} jars.");
    }

    void BulkJarFont(float size)
    {
        int n = 0;
        foreach (var jar in Jars())
        {
            var lbl = JarLabel(jar);
            if (!lbl) continue;
            Undo.RecordObject(lbl, "Bulk jar font");
            lbl.enableAutoSizing = false;
            lbl.fontSize = size;
            EditorUtility.SetDirty(lbl);
            n++;
        }
        Dirty();
        Debug.Log($"Bulk jar font size {size} applied to {n} labels.");
    }

    // ---------- Kitchen ingredients ----------
    void DrawIngredients()
    {
        Header("Kitchen — ingredient art, jar placement & proportions");
        DrawKitchenBulk();
        nudge = EditorGUILayout.FloatField("Nudge step (px)", nudge);

        foreach (var ing in Ingredients())
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var jar = Jars().FirstOrDefault(j => j.Ingredient == ing);
                var icon = jar ? jar.transform.Find("Icon")?.GetComponent<Image>() : null;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(ing.displayName + "  [" + ing.slot + "]", EditorStyles.boldLabel, GUILayout.Width(190));
                    using (new EditorGUI.DisabledScope(jar == null))
                    {
                        if (GUILayout.Button("Select jar", GUILayout.Width(80)) && jar)
                        { Selection.activeGameObject = jar.gameObject; EditorGUIUtility.PingObject(jar); SceneView.FrameLastActiveSceneView(); }
                        if (GUILayout.Button("←", GUILayout.Width(26)) && jar) NudgeJar(jar, -nudge, 0);
                        if (GUILayout.Button("→", GUILayout.Width(26)) && jar) NudgeJar(jar, nudge, 0);
                        if (GUILayout.Button("↑", GUILayout.Width(26)) && jar) NudgeJar(jar, 0, nudge);
                        if (GUILayout.Button("↓", GUILayout.Width(26)) && jar) NudgeJar(jar, 0, -nudge);
                    }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    SpriteField("Jar", ing, () => ing.jarSprite, s => ing.jarSprite = s);
                    if (ing.slot == SlotType.Main) SpriteField("Dish", ing, () => ing.dishSprite, s => ing.dishSprite = s);
                    SpriteField("Stew", ing, () => ing.stewSprite, s => ing.stewSprite = s);
                }
                ImportFixLine(ing.jarSprite);

                if (icon) FitScale(new[] { icon }, ing.jarSprite);
            }
        }
    }

    void SpriteField(string label, IngredientSO ing, System.Func<Sprite> get, System.Action<Sprite> set)
    {
        EditorGUI.BeginChangeCheck();
        var v = (Sprite)EditorGUILayout.ObjectField(label, get(), typeof(Sprite), false);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(ing, "Set " + label);
            set(v);
            EditorUtility.SetDirty(ing);
            ApplyIngredientArt();   // reflect immediately in the Scene
        }
    }

    static void NudgeJar(IngredientJar jar, float dx, float dy)
    {
        var rt = jar.GetComponent<RectTransform>();
        Undo.RecordObject(rt, "Nudge jar");
        rt.anchoredPosition += new Vector2(dx, dy);
        EditorUtility.SetDirty(rt);
        Dirty();
    }

    // ---------- Text & fonts ----------
    static TMP_Text[] SceneTexts() => Scene<TMP_Text>().OrderBy(t => PathOf(t.transform)).ToArray();

    // UI = Cormorant Garamond everywhere; anything under NotePanel = Caveat (the handwritten napkin).
    static void ApplyFontsSmart()
    {
        var ui = UiFont(); var napkin = NapkinFont();
        if (!ui || !napkin) { Debug.LogError("Font assets missing under Assets/Fonts/"); return; }
        int nUI = 0, nNap = 0;
        foreach (var t in SceneTexts())
        {
            bool isNapkin = PathOf(t.transform).Contains("/NotePanel/");
            Undo.RecordObject(t, "Apply font");
            t.font = isNapkin ? napkin : ui;
            EditorUtility.SetDirty(t);
            if (isNapkin) nNap++; else nUI++;
        }
        Dirty();
        Debug.Log($"Fonts applied — UI(Cormorant)={nUI}, Napkin(Caveat)={nNap}");
    }

    void DrawTextTools()
    {
        Header("Text & fonts");

        if (!UiFont() || !NapkinFont())
            EditorGUILayout.HelpBox("Expected Assets/Fonts/CormorantGaramond SDF.asset and Caveat SDF.asset.", MessageType.Warning);

        if (GUILayout.Button("Apply fonts to scene  (UI = Cormorant, napkin = Caveat)", GUILayout.Height(26)))
            ApplyFontsSmart();

        showTextTools = EditorGUILayout.Foldout(showTextTools, "Edit one text object (font, size, colour, spacing…)", true);
        if (!showTextTools) return;

        var texts = SceneTexts();
        if (texts.Length == 0) { EditorGUILayout.HelpBox("No TMP text in the open scene.", MessageType.None); return; }
        textIndex = Mathf.Clamp(textIndex, 0, texts.Length - 1);

        using (new EditorGUILayout.HorizontalScope())
        {
            textIndex = EditorGUILayout.Popup(textIndex, texts.Select(t => PathOf(t.transform)).ToArray());
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            { Selection.activeGameObject = texts[textIndex].gameObject; EditorGUIUtility.PingObject(texts[textIndex]); }
        }

        var txt = texts[textIndex];
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUI.BeginChangeCheck();
            var font    = (TMP_FontAsset)EditorGUILayout.ObjectField("Font", txt.font, typeof(TMP_FontAsset), false);
            float size  = EditorGUILayout.FloatField("Font size", txt.fontSize);
            bool auto   = EditorGUILayout.Toggle("Auto size", txt.enableAutoSizing);
            var color   = EditorGUILayout.ColorField("Colour", txt.color);
            var align   = (TextAlignmentOptions)EditorGUILayout.EnumPopup("Alignment", txt.alignment);
            var style   = (FontStyles)EditorGUILayout.EnumFlagsField("Style", txt.fontStyle);
            float cs    = EditorGUILayout.FloatField("Character spacing", txt.characterSpacing);
            float ls    = EditorGUILayout.FloatField("Line spacing", txt.lineSpacing);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(txt, "Edit text");
                txt.font = font; txt.fontSize = size; txt.enableAutoSizing = auto;
                txt.color = color; txt.alignment = align; txt.fontStyle = style;
                txt.characterSpacing = cs; txt.lineSpacing = ls;
                EditorUtility.SetDirty(txt);
                Dirty();
            }

            EditorGUI.BeginChangeCheck();
            var body = EditorGUILayout.TextArea(txt.text, GUILayout.MinHeight(40));
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(txt, "Edit text body"); txt.text = body; EditorUtility.SetDirty(txt); Dirty(); }
        }
    }

    static void Header(string t)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(t, EditorStyles.boldLabel);
        var r = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(r, new Color(0, 0, 0, 0.25f));
    }
}
