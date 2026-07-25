using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// Kitchen ingredient name labels are hover tooltips: IngredientJar.Awake() blanks + disables them,
// and only enables one on pointer hover. So in the editor they're invisible (disabled, empty text)
// and you can't position them by eye. These menu items reveal them (with each jar's real ingredient
// name) so you can drag each Jar's NameLabel child, then hide them again. Nothing here is saved for
// you and none of it affects the build — at runtime Awake re-hides every label regardless.
public static class KitchenLabelPreview
{
    const string kMenu = "Tavern Stew/Kitchen/";

    [MenuItem(kMenu + "Show Ingredient Labels", priority = 0)]
    static void Show()
    {
        SetScreen("CookingScreen", true);
        SetScreen("TavernScreen", false);

        int n = 0;
        foreach (var jar in Resources.FindObjectsOfTypeAll<IngredientJar>())
        {
            if (!jar.gameObject.scene.IsValid()) continue;   // skip prefab assets
            var tmp = FindLabel(jar);
            if (!tmp) continue;
            var ing = jar.Ingredient;
            tmp.text = ing ? ing.displayName : jar.name;
            tmp.enabled = true;
            EditorUtility.SetDirty(tmp);
            n++;
        }
        Repaint();
        Debug.Log($"[KitchenLabelPreview] Showing {n} ingredient labels for editing. " +
                  "Select a Jar's NameLabel child to reposition it. Run 'Hide Ingredient Labels' when done.");
    }

    [MenuItem(kMenu + "Hide Ingredient Labels", priority = 1)]
    static void Hide()
    {
        foreach (var jar in Resources.FindObjectsOfTypeAll<IngredientJar>())
        {
            if (!jar.gameObject.scene.IsValid()) continue;
            var tmp = FindLabel(jar);
            if (!tmp) continue;
            tmp.enabled = false;
            tmp.text = "";
            EditorUtility.SetDirty(tmp);
        }
        SetScreen("TavernScreen", true);
        SetScreen("CookingScreen", false);
        Repaint();
        Debug.Log("[KitchenLabelPreview] Labels hidden, back to Tavern screen.");
    }

    // Nudge every label the same amount — handy for a uniform "closer to the art" pass.
    [MenuItem(kMenu + "Nudge Labels Up 4px", priority = 20)]
    static void NudgeUp() => Nudge(+4f);

    [MenuItem(kMenu + "Nudge Labels Down 4px", priority = 21)]
    static void NudgeDown() => Nudge(-4f);

    static void Nudge(float dy)
    {
        int n = 0;
        foreach (var jar in Resources.FindObjectsOfTypeAll<IngredientJar>())
        {
            if (!jar.gameObject.scene.IsValid()) continue;
            var tmp = FindLabel(jar);
            if (!tmp) continue;
            var rt = (RectTransform)tmp.transform;
            Undo.RecordObject(rt, "Nudge Ingredient Labels");
            rt.anchoredPosition += new Vector2(0f, dy);
            EditorUtility.SetDirty(rt);
            n++;
        }
        Repaint();
        Debug.Log($"[KitchenLabelPreview] Nudged {n} labels {dy:+0;-0}px (Y now includes the offset). Save the scene to keep it.");
    }

    static TMP_Text FindLabel(IngredientJar jar)
    {
        var lbl = jar.transform.Find("NameLabel");
        return lbl ? lbl.GetComponent<TMP_Text>() : null;
    }

    static void SetScreen(string name, bool active)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name) root.SetActive(active);
    }

    static void Repaint()
    {
        Canvas.ForceUpdateCanvases();
        SceneView.RepaintAll();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
