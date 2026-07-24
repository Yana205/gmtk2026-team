using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TavernStewBuild
{
    // Task 8 (layout v2) — Jar.prefab + 10 shelf instances in labeled MAIN/SIDE/SAUCE sections.
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

        // Layout v3 — 3-column matrix under the right-side Shelf: column = slot (MAIN | SIDE | SAUCE),
        // row = order within the slot. Jars hang from the shelf top (anchor 0.5,1). Manual cells so a
        // locked jar (Kraken, Dragon, Pixie Dust) leaves a visible gap in its column until unlocked.
        //             Ham    Ven    Krak   Drag | Onion  Carrot Eggpl | Cumin  Pepper Pixie
        private static readonly float[] X = { -200f, -200f, -200f, -200f, 0f, 0f, 0f, 200f, 200f, 200f };
        private static readonly float[] Y = { -150f, -325f, -500f, -675f, -150f, -325f, -500f, -150f, -325f, -500f };

        [MenuItem("Tavern Stew/Build/3 Jar Prefab + Instances")]
        public static void Run()
        {
            TSBUtil.EnsureFolder("Assets/Prefabs");
            const string prefabPath = "Assets/Prefabs/Jar.prefab";
            var feel = TSBUtil.Asset<KitchenFeelSO>("Assets/Data/KitchenFeel.asset");

            // template
            var jar = new GameObject("Jar", typeof(RectTransform));
            var rt = (RectTransform)jar.transform;
            rt.sizeDelta = new Vector2(130f, 170f);
            var bg = jar.AddComponent<Image>();
            bg.color = new Color(.26f, .23f, .20f);
            bg.raycastTarget = true;
            var btn = jar.AddComponent<Button>();
            btn.targetGraphic = bg;
            var jarComp = jar.AddComponent<IngredientJar>();
            var icon = TSBUtil.Box("Icon", jar.transform, new Vector2(0f, 28f), new Vector2(78f, 78f), Color.white)
                .GetComponent<Image>();
            var nameLabel = TSBUtil.Label("NameLabel", jar.transform, new Vector2(0f, -56f), new Vector2(124f, 48f),
                "Name", 20f, new Color(.92f, .88f, .80f));
            TSBUtil.Wire(jarComp, ("feel", feel), ("icon", icon), ("nameLabel", nameLabel));

            var prefab = PrefabUtility.SaveAsPrefabAsset(jar, prefabPath);
            Object.DestroyImmediate(jar);

            // 10 instances under Shelf at fixed section positions (wipe previous jars, keep headers)
            var shelf = TSBUtil.FindT("CookingScreen/Shelf");
            for (int i = shelf.childCount - 1; i >= 0; i--)
                if (shelf.GetChild(i).GetComponent<IngredientJar>())
                    Object.DestroyImmediate(shelf.GetChild(i).gameObject);

            var stew = TSBUtil.Find<StewBuilder>("CookingScreen/StewBuilder");
            for (int i = 0; i < Keys.Length; i++)
            {
                var ing = TSBUtil.Asset<IngredientSO>("Assets/Data/Ingredients/ING_" + Keys[i] + ".asset");
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, shelf);
                inst.name = "Jar_" + Keys[i];
                var irt = (RectTransform)inst.transform;
                irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 1f);   // hang from the shelf top
                irt.anchoredPosition = new Vector2(X[i], Y[i]);
                TSBUtil.Wire(inst.GetComponent<IngredientJar>(), ("ingredient", ing), ("stewBuilder", stew));
                // edit-time preview only — IngredientJar.Awake re-applies both at runtime
                inst.transform.Find("Icon").GetComponent<Image>().color = ing.placeholderColor;
                inst.transform.Find("NameLabel").GetComponent<TextMeshProUGUI>().text = ing.displayName;
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TSB] Jar.prefab + 10 sectioned shelf instances created.");
        }
    }
}
