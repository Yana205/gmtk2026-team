using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tavern Stew ▸ Studio  (Cmd/Ctrl+Shift+T)
// One window to preview REAL art in the Scene WITHOUT entering Play mode, switch the on-screen
// visitor, and tweak kitchen ingredient art / jar placement — clicks & dropdowns, no hunting the
// hierarchy + inspector. Everything writes to the open scene (Undo-able) so you see it immediately.
public class TavernStewStudio : EditorWindow
{
    [MenuItem("Tavern Stew/Studio %#t")]
    static void Open() => GetWindow<TavernStewStudio>("Tavern Stew Studio");

    Vector2 scroll;
    int visitorIndex;
    float nudge = 20f;

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
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("★  Apply ALL art to Scene (edit mode)", GUILayout.Height(34)))
        {
            ApplyIngredientArt();
            var cs = Characters();
            if (cs.Count > 0) PreviewVisitor(cs[Mathf.Clamp(visitorIndex, 0, cs.Count - 1)]);
        }
        EditorGUILayout.HelpBox("Jars set their icon at Play; this pushes every IngredientSO's art into the Scene now so you see it before pressing Play.", MessageType.None);

        DrawVisitors();
        DrawIngredients();

        EditorGUILayout.EndScrollView();
    }

    void DrawVisitors()
    {
        var chars = Characters();
        Header("Tavern — switch visitor");
        if (chars.Count == 0) { EditorGUILayout.HelpBox("No CharacterSO in Assets/Data/Characters.", MessageType.Warning); return; }

        visitorIndex = Mathf.Clamp(visitorIndex, 0, chars.Count - 1);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("◀", GUILayout.Width(30))) { visitorIndex = (visitorIndex - 1 + chars.Count) % chars.Count; PreviewVisitor(chars[visitorIndex]); }
            visitorIndex = EditorGUILayout.Popup(visitorIndex, chars.Select(c => c.displayName).ToArray());
            if (GUILayout.Button("▶", GUILayout.Width(30))) { visitorIndex = (visitorIndex + 1) % chars.Count; PreviewVisitor(chars[visitorIndex]); }
            if (GUILayout.Button("Show in Scene", GUILayout.Width(110))) PreviewVisitor(chars[visitorIndex]);
        }

        var c = chars[visitorIndex];
        using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            var tex = c.portrait ? AssetPreview.GetAssetPreview(c.portrait) : null;
            GUILayout.Label(tex, GUILayout.Width(72), GUILayout.Height(72));
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUI.BeginChangeCheck();
                var p = (Sprite)EditorGUILayout.ObjectField("Portrait", c.portrait, typeof(Sprite), false);
                if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(c, "portrait"); c.portrait = p; EditorUtility.SetDirty(c); PreviewVisitor(c); }
                EditorGUILayout.LabelField("Order", Order(c));
                EditorGUILayout.LabelField("Napkin", c.napkinText, EditorStyles.wordWrappedMiniLabel);
            }
        }
    }

    static string Order(CharacterSO c) =>
        (c.main ? c.main.displayName : "?") + " + " + (c.side ? c.side.displayName : "?") + " + " + (c.sauce ? c.sauce.displayName : "?");

    void DrawIngredients()
    {
        Header("Kitchen — ingredient art & jar placement");
        nudge = EditorGUILayout.FloatField("Nudge step (px)", nudge);

        foreach (var ing in Ingredients())
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(ing.displayName + "  [" + ing.slot + "]", EditorStyles.boldLabel, GUILayout.Width(190));
                    var jar = Jars().FirstOrDefault(j => j.Ingredient == ing);
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
