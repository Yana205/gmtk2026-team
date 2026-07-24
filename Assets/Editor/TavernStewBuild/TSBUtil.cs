using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TavernStewBuild
{
    // Shared helpers for the disposable M1 grey-box build scripts (tasks 6-10).
    // Delete this whole folder after M1 ships.
    public static class TSBUtil
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";

        // ---------- hierarchy ----------
        public static GameObject Child(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }

        public static RectTransform Place(GameObject go, Vector2 pos, Vector2 size)
        {
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static GameObject Box(string name, Transform parent, Vector2 pos, Vector2 size, Color color, bool raycast = false)
        {
            var go = Child(name, parent);
            Place(go, pos, size);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return go;
        }

        public static GameObject Stretch(string name, Transform parent, Color color, bool raycast = false)
        {
            var go = Child(name, parent);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return go;
        }

        public static TextMeshProUGUI Label(string name, Transform parent, Vector2 pos, Vector2 size,
            string text, float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = Child(name, parent);
            Place(go, pos, size);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        public static Button MakeButton(GameObject go)
        {
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            return btn;
        }

        public static GameObject ButtonBox(string name, Transform parent, Vector2 pos, Vector2 size,
            Color color, string caption, float fontSize, Color textColor)
        {
            var go = Box(name, parent, pos, size, color, raycast: true);
            MakeButton(go);
            Label("Label", go.transform, Vector2.zero, size, caption, fontSize, textColor);
            return go;
        }

        // ---------- lookup (works on inactive objects, unlike GameObject.Find) ----------
        public static Transform FindT(string path)
        {
            int slash = path.IndexOf('/');
            string rootName = slash < 0 ? path : path.Substring(0, slash);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name != rootName) continue;
                if (slash < 0) return root.transform;
                var t = root.transform.Find(path.Substring(slash + 1));
                if (t) return t;
            }
            throw new System.Exception("[TSB] Missing GameObject: " + path);
        }

        public static GameObject FindGO(string path) => FindT(path).gameObject;

        public static T Find<T>(string path) where T : Component
        {
            var c = FindT(path).GetComponent<T>();
            if (!c) throw new System.Exception("[TSB] Missing component " + typeof(T).Name + " on " + path);
            return c;
        }

        public static T Asset<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!a) throw new System.Exception("[TSB] Missing asset: " + path);
            return a;
        }

        // ---------- wiring (private [SerializeField] slots via SerializedObject) ----------
        public static void Wire(Object target, params (string prop, Object val)[] slots)
        {
            var so = new SerializedObject(target);
            foreach (var (prop, val) in slots)
            {
                var p = so.FindProperty(prop);
                if (p == null) throw new System.Exception("[TSB] " + target.GetType().Name + " has no serialized property '" + prop + "'");
                p.objectReferenceValue = val;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void WireArray(Object target, string prop, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p == null) throw new System.Exception("[TSB] " + target.GetType().Name + " has no serialized property '" + prop + "'");
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- folders ----------
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
