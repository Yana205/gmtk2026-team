using UnityEditor;
using UnityEngine;

namespace TavernStewBuild
{
    // Task 6 — the 15 SO assets with plan values.
    // Writer text is NOT in the repo and the plan forbids inventing it (Decision D4):
    // bracketed placeholders mark every spot Yan must paste verbatim text into.
    // Idempotent: existing assets are kept untouched so pasted writer text never gets clobbered.
    public static class CreateDataAssets
    {
        private const string W = "[WRITER TEXT — paste verbatim]";

        [MenuItem("Tavern Stew/Build/1 Create Data Assets")]
        public static void Run()
        {
            TSBUtil.EnsureFolder("Assets/Data");
            TSBUtil.EnsureFolder("Assets/Data/Ingredients");

            // 10 ingredient cards — displayName / slot / startsLocked / placeholderColor from the plan table
            var ham      = Ing("Ham",              SlotType.Main,  false, new Color(1f,   .6f,  .7f));
            var venison  = Ing("Venison",          SlotType.Main,  false, new Color(.55f, .35f, .2f));
            var kraken   = Ing("Kraken",           SlotType.Main,  true,  new Color(.2f,  .7f,  .7f));
            var dragon   = Ing("Dragon",           SlotType.Main,  true,  new Color(.85f, .2f,  .15f));
            var onion    = Ing("ForestOnion",      SlotType.Side,  false, new Color(.35f, .7f,  .3f),  "Forest Onion");
            var carrot   = Ing("MountainCarrot",   SlotType.Side,  false, new Color(.95f, .55f, .15f), "Mountain Carrot");
            Ing("Eggplant",                        SlotType.Side,  false, new Color(.55f, .3f,  .65f));
            var cumin    = Ing("ParadiseCumin",    SlotType.Sauce, false, new Color(.95f, .85f, .3f),  "Paradise Cumin");
            var pepper   = Ing("SalamanderPepper", SlotType.Sauce, false, new Color(.6f,  .1f,  .1f),  "Salamander Pepper");
            var pixie    = Ing("PixieDust",        SlotType.Sauce, true,  new Color(.7f,  .85f, 1f),   "Pixie Dust");

            // Plan values for these four == the code defaults, so a fresh instance is already correct
            Create<GameConfigSO>("Assets/Data/GameConfig.asset", out _);
            Create<TavernFeelSO>("Assets/Data/TavernFeel.asset", out _);
            Create<KitchenFeelSO>("Assets/Data/KitchenFeel.asset", out _);

            var dbg = Create<DebugConfigSO>("Assets/Data/DebugConfig.asset", out bool dbgFresh);
            if (dbgFresh)
            {
                // Deviation note: plan wants the TAB hotkey on, but it reads legacy Input which
                // throws until the "Active Input Handling = Both" editor restart happens.
                // Flip this back on after the first restart.
                dbg.screenSwitchHotkey = false;
                EditorUtility.SetDirty(dbg);
            }

            var story = Create<StoryDataSO>("Assets/Data/StoryData.asset", out bool storyFresh);
            if (storyFresh)
            {
                story.introLine1 = W + " (intro line 1)";
                story.introLine2 = W + " (intro line 2)";
                story.endingLinesByRank = new[]
                {
                    W + " (ending — rank C)",
                    W + " (ending — rank B)",
                    W + " (ending — rank A)",
                    W + " (ending — rank S)",
                };
                story.unlockBeats = new[]
                {
                    new StoryDataSO.UnlockBeat
                    {
                        ingredientToUnlock = kraken,
                        toastLine = "Letitia dropped off today's kill: KRAKEN!",
                        hasNapkin = true,
                        guestName = "Letitia",
                        napkinText = W + " (Letitia's seafood note)",
                        idealStew = new[] { kraken, carrot, pepper },
                    },
                    new StoryDataSO.UnlockBeat
                    {
                        ingredientToUnlock = dragon,
                        toastLine = "Milog dropped off today's kill: DRAGON!",
                        hasNapkin = true,
                        guestName = "Milog",
                        napkinText = W + " (Milog's quest note)",
                        idealStew = new[] { venison, onion, cumin },
                    },
                    new StoryDataSO.UnlockBeat
                    {
                        ingredientToUnlock = pixie,
                        toastLine = "New on the shelf: PIXIE DUST",
                        hasNapkin = false,
                        guestName = "",
                        napkinText = "",
                        idealStew = new IngredientSO[0],
                    },
                };
                EditorUtility.SetDirty(story);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[TSB] Data assets ready: 5 config SOs + 10 ingredients.");
        }

        private static T Create<T>(string path, out bool fresh) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing) { fresh = false; return existing; }
            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            fresh = true;
            return so;
        }

        private static IngredientSO Ing(string key, SlotType slot, bool locked, Color c, string display = null)
        {
            string path = "Assets/Data/Ingredients/ING_" + key + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<IngredientSO>(path);
            if (existing) return existing;
            var so = ScriptableObject.CreateInstance<IngredientSO>();
            so.displayName = display ?? key;
            so.slot = slot;
            so.startsLocked = locked;
            so.placeholderColor = c;
            AssetDatabase.CreateAsset(so, path);
            return so;
        }
    }
}
