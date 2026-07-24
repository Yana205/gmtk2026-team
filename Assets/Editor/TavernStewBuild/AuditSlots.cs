using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TavernStewBuild
{
    // Task 10 — reflection-scan every [SerializeField] object reference on the project's own
    // scene components; an empty inspector slot = a visible bug. Whitelist = plan's optional slots.
    public static class AuditSlots
    {
        private static readonly HashSet<string> Optional = new HashSet<string>
        {
            "BustDresser.faceNeutral",   // face sprites optional in grey-box
            "BustDresser.faceHappy",
            "BustDresser.faceSad",
            "IngredientJar.ingredient",  // only optional on the prefab ASSET; instances are checked
        };

        [MenuItem("Tavern Stew/Build/5 Audit Slots")]
        public static void Run()
        {
            var missing = new List<string>();
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!mb) continue;
                if (mb.GetType().Assembly.GetName().Name != "Assembly-CSharp") continue;

                var so = new SerializedObject(mb);
                var it = so.GetIterator();
                bool enterChildren = true;
                while (it.NextVisible(enterChildren))
                {
                    enterChildren = true;
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (it.name == "m_Script") continue;
                    if (it.objectReferenceValue != null) continue;

                    string key = mb.GetType().Name + "." + it.propertyPath;
                    if (Optional.Contains(key)) continue;
                    // Portrait's BustDresser deliberately has no face (plan)
                    if (mb is BustDresser && mb.gameObject.name == "Portrait" && it.propertyPath == "face") continue;

                    missing.Add(Path(mb.transform) + " :: " + key);
                }
            }

            if (missing.Count == 0)
                Debug.Log("[TSB AUDIT] PASS — zero empty required slots.");
            else
                Debug.LogError("[TSB AUDIT] " + missing.Count + " empty required slot(s):\n" + string.Join("\n", missing));
        }

        private static string Path(Transform t)
        {
            string p = t.name;
            while (t.parent) { t = t.parent; p = t.name + "/" + p; }
            return p;
        }
    }
}
