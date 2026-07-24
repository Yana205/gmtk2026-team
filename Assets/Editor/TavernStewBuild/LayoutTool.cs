using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TavernStewBuild
{
    // Manual layout helper (2026-07-24). The scene is edited by hand now, so this window gives the
    // basic moves you need without hunting through the Inspector:
    //   - isolate one screen at a time (Tavern / Kitchen / All)
    //   - click a design group to select + frame it (parent = whole group, expand it for children)
    //   - nudge the selected element's position with arrows or type an exact X/Y
    // Screens are ScreenSpace-Overlay, so DRAG in the Game view (or use the nudges here); the 3D
    // Scene view won't show them nicely. Positions here are RectTransform.anchoredPosition (pixels).
    public class LayoutTool : EditorWindow
    {
        private const string Tavern = "TavernScreen";
        private const string Cooking = "CookingScreen";
        private const string Overlay = "OverlayCanvas";

        private float step = 10f;
        private Vector2 scroll;

        [MenuItem("Tavern Stew/Layout %#l")]   // Ctrl/Cmd+Shift+L
        public static void Open()
        {
            var w = GetWindow<LayoutTool>("Layout");
            w.minSize = new Vector2(280f, 360f);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Show screen", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Tavern")) Apply(true, false, false);
                if (GUILayout.Button("Kitchen")) Apply(false, true, false);
                if (GUILayout.Button("All")) Apply(true, true, true);
            }
            EditorGUILayout.HelpBox("Screens are auto-restored to All when you save, so you can never " +
                "save a broken play state.", MessageType.None);

            EditorGUILayout.Space(6);
            DrawGroups();

            EditorGUILayout.Space(6);
            DrawNudge();
        }

        // ---- design groups: the direct children of whichever screen root is currently visible ----
        private void DrawGroups()
        {
            EditorGUILayout.LabelField("Groups (click to select + frame)", EditorStyles.boldLabel);
            var screen = ActiveVisibleScreen();
            if (screen == null)
            {
                EditorGUILayout.HelpBox("Show a screen above to list its groups.", MessageType.Info);
                return;
            }
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(180f));
            var t = screen.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                var child = t.GetChild(i).gameObject;
                bool selected = Selection.activeGameObject == child;
                var style = selected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                if (GUILayout.Button((selected ? "▸ " : "    ") + child.name, style))
                    SelectAndFrame(child);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawNudge()
        {
            EditorGUILayout.LabelField("Move selected", EditorStyles.boldLabel);
            var go = Selection.activeGameObject;
            var rt = go ? go.transform as RectTransform : null;
            if (rt == null)
            {
                EditorGUILayout.HelpBox("Select a UI element (a group or a child) to move it.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Selected", go.name);
            step = EditorGUILayout.FloatField("Step (px)", step);

            EditorGUI.BeginChangeCheck();
            Vector2 pos = EditorGUILayout.Vector2Field("Position (x, y)", rt.anchoredPosition);
            if (EditorGUI.EndChangeCheck()) SetPos(rt, pos);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("▲", GUILayout.Width(48))) Move(rt, new Vector2(0, step));
                GUILayout.FlexibleSpace();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("◀", GUILayout.Width(48))) Move(rt, new Vector2(-step, 0));
                if (GUILayout.Button("▼", GUILayout.Width(48))) Move(rt, new Vector2(0, -step));
                if (GUILayout.Button("▶", GUILayout.Width(48))) Move(rt, new Vector2(step, 0));
                GUILayout.FlexibleSpace();
            }
        }

        // ------------------------------------------------ helpers
        private static void Move(RectTransform rt, Vector2 delta)
        {
            Undo.RecordObject(rt, "Layout Nudge");
            rt.anchoredPosition += delta;
            Dirty(rt);
        }

        private static void SetPos(RectTransform rt, Vector2 pos)
        {
            Undo.RecordObject(rt, "Layout Set Position");
            rt.anchoredPosition = pos;
            Dirty(rt);
        }

        private static void Dirty(Object o)
        {
            EditorUtility.SetDirty(o);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private static void SelectAndFrame(GameObject go)
        {
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.FrameSelected();
        }

        private void Apply(bool tavern, bool cooking, bool overlay)
        {
            SetActive(Tavern, tavern);
            SetActive(Cooking, cooking);
            SetActive(Overlay, overlay);
            SceneView.RepaintAll();
            Repaint();
        }

        private static void SetActive(string rootName, bool active)
        {
            var r = FindRoot(rootName);
            if (r) r.SetActive(active);
        }

        // The screen whose root is active (prefer whichever single screen is shown; else Tavern)
        private static GameObject ActiveVisibleScreen()
        {
            var t = FindRoot(Tavern);
            var c = FindRoot(Cooking);
            if (c && c.activeSelf && (t == null || !t.activeSelf)) return c;
            return t;
        }

        private static GameObject FindRoot(string name)
        {
            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }
    }

    // Save-guard: whatever you had isolated, force all screens active right before the scene is
    // written so Main.unity is always saved play-ready (Decision D2: every Awake runs at load).
    [InitializeOnLoad]
    public static class LayoutSaveGuard
    {
        static LayoutSaveGuard()
        {
            EditorSceneManager.sceneSaving += (scene, path) =>
            {
                foreach (var root in scene.GetRootGameObjects())
                    if (root.name == "TavernScreen" || root.name == "CookingScreen" || root.name == "OverlayCanvas")
                        root.SetActive(true);
            };
        }
    }
}
