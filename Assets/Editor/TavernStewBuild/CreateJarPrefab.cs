using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TavernStewBuild
{
    // Task 8 — Jar.prefab + 10 shelf instances in ingredient-table order.
    // Prefab carries the asset-safe slots (feel, icon, nameLabel); ingredient + stewBuilder are
    // per-instance because a prefab asset cannot reference scene objects.
    public static class CreateJarPrefab
    {
        // Table order — must match CustomerGenerator.allIngredients and StewBuilder.jars
        public static readonly string[] Keys =
        {
            "Ham", "Venison", "Kraken", "Dragon", "ForestOnion",
            "MountainCarrot", "Eggplant", "ParadiseCumin", "SalamanderPepper", "PixieDust",
        };

        [MenuItem("Tavern Stew/Build/3 Jar Prefab + Instances")]
        public static void Run()
        {
            TSBUtil.EnsureFolder("Assets/Prefabs");
            const string prefabPath = "Assets/Prefabs/Jar.prefab";
            var feel = TSBUtil.Asset<KitchenFeelSO>("Assets/Data/KitchenFeel.asset");

            // template
            var jar = new GameObject("Jar", typeof(RectTransform));
            var rt = (RectTransform)jar.transform;
            rt.sizeDelta = new Vector2(150f, 200f);
            var bg = jar.AddComponent<Image>();
            bg.color = new Color(.26f, .23f, .20f);
            bg.raycastTarget = true;
            var le = jar.AddComponent<LayoutElement>();
            le.preferredWidth = 150f;
            le.preferredHeight = 200f;
            var btn = jar.AddComponent<Button>();
            btn.targetGraphic = bg;
            var jarComp = jar.AddComponent<IngredientJar>();
            var icon = TSBUtil.Box("Icon", jar.transform, new Vector2(0f, 30f), new Vector2(92f, 92f), Color.white)
                .GetComponent<Image>();
            var nameLabel = TSBUtil.Label("NameLabel", jar.transform, new Vector2(0f, -65f), new Vector2(146f, 54f),
                "Name", 22f, new Color(.92f, .88f, .80f));
            TSBUtil.Wire(jarComp, ("feel", feel), ("icon", icon), ("nameLabel", nameLabel));

            var prefab = PrefabUtility.SaveAsPrefabAsset(jar, prefabPath);
            Object.DestroyImmediate(jar);

            // 10 instances under Shelf (wipe previous ones so reruns stay clean)
            var shelf = TSBUtil.FindT("CookingScreen/Shelf");
            for (int i = shelf.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(shelf.GetChild(i).gameObject);

            var stew = TSBUtil.Find<StewBuilder>("CookingScreen/StewBuilder");
            foreach (var key in Keys)
            {
                var ing = TSBUtil.Asset<IngredientSO>("Assets/Data/Ingredients/ING_" + key + ".asset");
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, shelf);
                inst.name = "Jar_" + key;
                TSBUtil.Wire(inst.GetComponent<IngredientJar>(), ("ingredient", ing), ("stewBuilder", stew));
                // edit-time preview only — IngredientJar.Awake re-applies both at runtime
                inst.transform.Find("Icon").GetComponent<Image>().color = ing.placeholderColor;
                inst.transform.Find("NameLabel").GetComponent<TextMeshProUGUI>().text = ing.displayName;
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TSB] Jar.prefab + 10 shelf instances created.");
        }
    }
}
