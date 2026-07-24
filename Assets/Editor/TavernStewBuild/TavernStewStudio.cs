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
    bool showDishSize;

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

    static Image PortraitImage()
    {
        var bust = Busts().FirstOrDefault();
        return bust ? new SerializedObject(bust).FindProperty("portraitImage").objectReferenceValue as Image : null;
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

        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawCharacterLevel();
        DrawIngredients();
        EditorGUILayout.EndScrollView();
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

        // Portrait proportions (as it sits in the tavern bust)
        showPortraitSize = EditorGUILayout.Foldout(showPortraitSize, "Portrait size & proportions (tavern bust)", true);
        if (showPortraitSize) ProportionControls(PortraitImage(), c.portrait);

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
            ProportionControls(DishImage(), dishSprite);
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

    // ---------- reusable image proportion controls ----------
    // Works on any UI Image in the scene (portrait, dish, jar icon). All Undo-able, live in edit mode.
    void ProportionControls(Image img, Sprite refSprite)
    {
        if (!img) { EditorGUILayout.HelpBox("That image isn't in the open scene right now.", MessageType.None); return; }
        var rt = img.rectTransform;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();
            bool pa = GUILayout.Toggle(img.preserveAspect, "Preserve Aspect", EditorStyles.miniButton, GUILayout.Width(120));
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(img, "Preserve aspect"); img.preserveAspect = pa; EditorUtility.SetDirty(img); Dirty(); }

            if (GUILayout.Button("Native Size", EditorStyles.miniButton, GUILayout.Width(90)))
            { Undo.RecordObject(rt, "Native size"); img.SetNativeSize(); EditorUtility.SetDirty(rt); Dirty(); }

            using (new EditorGUI.DisabledScope(!refSprite))
                if (GUILayout.Button("Match aspect (keep height)", EditorStyles.miniButton))
                    MatchAspect(rt, refSprite);
        }

        EditorGUI.BeginChangeCheck();
        var size = EditorGUILayout.Vector2Field("Size (W×H px)", rt.sizeDelta);
        if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(rt, "Resize"); rt.sizeDelta = size; EditorUtility.SetDirty(rt); Dirty(); }

        EditorGUI.BeginChangeCheck();
        float s = EditorGUILayout.Slider("Uniform scale", rt.localScale.x, 0.1f, 3f);
        if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(rt, "Scale"); rt.localScale = new Vector3(s, s, 1f); EditorUtility.SetDirty(rt); Dirty(); }
    }

    static void MatchAspect(RectTransform rt, Sprite sprite)
    {
        var r = sprite.rect;
        if (r.height <= 0) return;
        Undo.RecordObject(rt, "Match aspect");
        rt.sizeDelta = new Vector2(rt.sizeDelta.y * (r.width / r.height), rt.sizeDelta.y);
        EditorUtility.SetDirty(rt);
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

    // ---------- Kitchen ingredients ----------
    void DrawIngredients()
    {
        Header("Kitchen — ingredient art, jar placement & proportions");
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

                if (icon)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        bool pa = GUILayout.Toggle(icon.preserveAspect, "Preserve Aspect", EditorStyles.miniButton, GUILayout.Width(120));
                        if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(icon, "Preserve aspect"); icon.preserveAspect = pa; EditorUtility.SetDirty(icon); Dirty(); }
                        if (GUILayout.Button("Native Size", EditorStyles.miniButton, GUILayout.Width(90)))
                        { Undo.RecordObject(icon.rectTransform, "Native size"); icon.SetNativeSize(); EditorUtility.SetDirty(icon.rectTransform); Dirty(); }
                        using (new EditorGUI.DisabledScope(!ing.jarSprite))
                            if (GUILayout.Button("Match aspect", EditorStyles.miniButton, GUILayout.Width(100)))
                                MatchAspect(icon.rectTransform, ing.jarSprite);
                    }
                }
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

    static void Header(string t)
    {
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField(t, EditorStyles.boldLabel);
        var r = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(r, new Color(0, 0, 0, 0.25f));
    }
}
