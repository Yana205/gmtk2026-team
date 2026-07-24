using UnityEditor;
using UnityEngine;

namespace TavernStewBuild
{
    // Edit-mode layout preview helper (disposable, delete with the rest of TavernStewBuild after M1).
    // The scene is saved with BOTH screen canvases + the IntroPanel overlay active (Decision D2), so
    // in the editor they overlap. These menu items just toggle canvas active-state in edit mode so you
    // can eyeball one screen at a time in the Scene/Game view WITHOUT entering Play Mode.
    // Nothing here is saved automatically — pick "Show All" before saving to keep the play-ready state.
    public static class ViewLayout
    {
        private const string Tavern = "TavernScreen";
        private const string Cooking = "CookingScreen";
        private const string Overlay = "OverlayCanvas";

        [MenuItem("Tavern Stew/View/Show Tavern Only")]
        public static void ShowTavern() => Apply(tavern: true, cooking: false, overlay: false);

        [MenuItem("Tavern Stew/View/Show Kitchen Only")]
        public static void ShowKitchen() => Apply(tavern: false, cooking: true, overlay: false);

        [MenuItem("Tavern Stew/View/Show All (play-ready)")]
        public static void ShowAll() => Apply(tavern: true, cooking: true, overlay: true);

        private static void Apply(bool tavern, bool cooking, bool overlay)
        {
            SetActive(Tavern, tavern);
            SetActive(Cooking, cooking);
            SetActive(Overlay, overlay);
            SceneView.RepaintAll();
        }

        private static void SetActive(string rootName, bool active)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == rootName) { root.SetActive(active); return; }
        }
    }
}
