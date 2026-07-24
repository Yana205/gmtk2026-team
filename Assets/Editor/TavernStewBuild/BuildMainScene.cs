using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TavernStewBuild
{
    // Task 7 — Main.unity grey-box hierarchy, exactly per BUILD_PLAN's tree.
    // Decision D2: the scene is SAVED with both screen canvases active so every Awake runs at
    // load (GameManager.Start calls ShowTavern before the first frame, so nothing flashes).
    // Decision D3: HintSystem lives under Systems (always active).
    // WARNING: rerunning wipes everything except Main Camera — jars + wiring must be redone after.
    public static class BuildMainScene
    {
        [MenuItem("Tavern Stew/Build/2 Build Main Scene")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(TSBUtil.ScenePath, OpenSceneMode.Single);

            foreach (var root in scene.GetRootGameObjects())
                if (root.name != "Main Camera") Object.DestroyImmediate(root);

            SetupCamera();
            BuildEventSystem();
            BuildSystems();
            BuildTavernScreen();
            BuildCookingScreen();
            BuildOverlayCanvas();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TSB] Main.unity hierarchy built and saved.");
        }

        private static void SetupCamera()
        {
            var cam = Camera.main;
            if (!cam)
            {
                var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                go.tag = "MainCamera";
                cam = go.GetComponent<Camera>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.08f, .07f, .06f);
        }

        private static void BuildEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void BuildSystems()
        {
            var systems = new GameObject("Systems");
            Sys(systems, "GameManager").AddComponent<GameManager>();
            Sys(systems, "ScreenManager").AddComponent<ScreenManager>();
            Sys(systems, "NightClock").AddComponent<NightClock>();
            Sys(systems, "CustomerGenerator").AddComponent<CustomerGenerator>();
            Sys(systems, "PatienceMeter").AddComponent<PatienceMeter>();
            Sys(systems, "HintSystem").AddComponent<HintSystem>();   // D3
        }

        private static GameObject Sys(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static GameObject MakeCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        // ------------------------------------------------ Tavern
        private static void BuildTavernScreen()
        {
            var t = MakeCanvas("TavernScreen", 0).transform;
            TSBUtil.Stretch("Background", t, new Color(.16f, .12f, .10f));

            // ClockUI — candle drains vertically, time label under it
            var clockUi = TSBUtil.Child("ClockUI", t);
            var crt = TSBUtil.Place(clockUi, new Vector2(110f, -160f), new Vector2(130f, 280f));
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 1f);
            var candle = TSBUtil.Box("Candle", clockUi.transform, new Vector2(0f, 30f), new Vector2(44f, 170f),
                new Color(.93f, .85f, .55f)).GetComponent<Image>();
            candle.type = Image.Type.Filled;
            candle.fillMethod = Image.FillMethod.Vertical;
            candle.fillAmount = 1f;
            TSBUtil.Label("TimeLabel", clockUi.transform, new Vector2(0f, -95f), new Vector2(130f, 40f),
                "3:00", 34f, new Color(.95f, .9f, .8f));

            // Customer — Button + CanvasGroup + CustomerView + BustDresser
            var customer = TSBUtil.Child("Customer", t);
            TSBUtil.Place(customer, new Vector2(0f, -60f), new Vector2(420f, 560f));
            customer.AddComponent<CanvasGroup>();
            // BustBase is the click surface; overlay layers don't intercept
            var bustBase = TSBUtil.Box("BustBase", customer.transform, new Vector2(0f, -60f), new Vector2(300f, 380f),
                new Color(.45f, .42f, .40f), raycast: true);
            TSBUtil.Box("HatLayer", customer.transform, new Vector2(0f, 165f), new Vector2(240f, 100f), Color.white);
            TSBUtil.Box("SideLayer", customer.transform, new Vector2(0f, -170f), new Vector2(340f, 110f), Color.white);
            TSBUtil.Box("SauceLayer", customer.transform, new Vector2(0f, -35f), new Vector2(170f, 50f), Color.white);
            TSBUtil.Box("Face", customer.transform, new Vector2(0f, 75f), new Vector2(150f, 150f), new Color(.85f, .75f, .62f));
            var bubble = TSBUtil.Box("ThoughtBubble", customer.transform, new Vector2(205f, 185f), new Vector2(110f, 90f),
                new Color(.95f, .95f, .90f));
            bubble.SetActive(false);
            MakeHeart(customer.transform, "Heart0", new Vector2(-80f, 250f));
            MakeHeart(customer.transform, "Heart1", new Vector2(0f, 272f));
            MakeHeart(customer.transform, "Heart2", new Vector2(80f, 250f));
            var coin = TSBUtil.Label("CoinPopup", customer.transform, new Vector2(0f, 320f), new Vector2(200f, 50f),
                "+0", 40f, new Color(.98f, .85f, .30f));
            coin.gameObject.SetActive(false);
            var custBtn = customer.AddComponent<Button>();
            custBtn.targetGraphic = bustBase.GetComponent<Image>();
            customer.AddComponent<CustomerView>();
            customer.AddComponent<BustDresser>();

            TSBUtil.Child("ReactionFX", t).AddComponent<ReactionFX>();

            var dish = TSBUtil.Child("DishOnCounter", t);
            TSBUtil.Place(dish, new Vector2(0f, -400f), new Vector2(420f, 170f));
            TSBUtil.Box("Dish", dish.transform, new Vector2(-60f, -15f), new Vector2(220f, 90f), new Color(.70f, .65f, .55f));
            TSBUtil.Box("MeadMug", dish.transform, new Vector2(150f, 5f), new Vector2(90f, 130f), new Color(.85f, .60f, .25f));
            dish.SetActive(false);

            TSBUtil.ButtonBox("MenuBook", t, new Vector2(-700f, -380f), new Vector2(240f, 130f),
                new Color(.50f, .32f, .18f), "MENU BOOK", 30f, new Color(.95f, .90f, .80f));

            // PatienceBar — hidden root, horizontal fill
            var bar = TSBUtil.Child("PatienceBar", t);
            TSBUtil.Place(bar, new Vector2(0f, 260f), new Vector2(340f, 34f));
            TSBUtil.Stretch("BG", bar.transform, new Color(.10f, .09f, .08f));
            var fill = TSBUtil.Box("Fill", bar.transform, Vector2.zero, new Vector2(328f, 24f),
                new Color(.45f, .75f, .35f)).GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            bar.SetActive(false);

            // NapkinPile — Button whose click surface is the (hidden until first napkin) PileVisual
            var pileGo = TSBUtil.Child("NapkinPile", t);
            TSBUtil.Place(pileGo, new Vector2(640f, -380f), new Vector2(170f, 130f));
            var pileVisual = TSBUtil.Box("PileVisual", pileGo.transform, Vector2.zero, new Vector2(150f, 110f),
                new Color(.92f, .90f, .82f), raycast: true);
            TSBUtil.Label("Label", pileVisual.transform, new Vector2(0f, 0f), new Vector2(150f, 40f),
                "napkins", 22f, new Color(.35f, .28f, .20f));
            var pileBtn = pileGo.AddComponent<Button>();
            pileBtn.targetGraphic = pileVisual.GetComponent<Image>();
            pileGo.AddComponent<NapkinPile>();
            pileVisual.SetActive(false);

            // Hints (texts come from the HintSystem comments in the repo — not invented)
            var tavernHint = TSBUtil.Label("TavernHint", t, new Vector2(0f, 350f), new Vector2(1000f, 60f),
                "Read their outfit... then click the menu book!", 30f, new Color(.95f, .92f, .80f));
            tavernHint.gameObject.SetActive(false);
            var serveHint = TSBUtil.Label("ServeHint", t, new Vector2(0f, 350f), new Vector2(1000f, 60f),
                "Click the customer to hand it over!", 30f, new Color(.95f, .92f, .80f));
            serveHint.gameObject.SetActive(false);
        }

        private static void MakeHeart(Transform parent, string name, Vector2 pos)
        {
            var h = TSBUtil.Box(name, parent, pos, new Vector2(52f, 52f), new Color(.90f, .25f, .35f));
            h.SetActive(false);
        }

        // ------------------------------------------------ Kitchen
        private static void BuildCookingScreen()
        {
            var k = MakeCanvas("CookingScreen", 1).transform;
            TSBUtil.Stretch("Background", k, new Color(.11f, .13f, .15f));

            // Portrait — small BustDresser, top-left corner (face stays empty per plan)
            var portrait = TSBUtil.Child("Portrait", k);
            TSBUtil.Place(portrait, new Vector2(-800f, 360f), new Vector2(160f, 220f));
            TSBUtil.Box("PortraitBase", portrait.transform, new Vector2(0f, -20f), new Vector2(110f, 140f), new Color(.45f, .42f, .40f));
            TSBUtil.Box("HatLayer", portrait.transform, new Vector2(0f, 65f), new Vector2(90f, 36f), Color.white);
            TSBUtil.Box("SideLayer", portrait.transform, new Vector2(0f, -62f), new Vector2(120f, 40f), Color.white);
            TSBUtil.Box("SauceLayer", portrait.transform, new Vector2(0f, -14f), new Vector2(60f, 22f), Color.white);
            portrait.AddComponent<BustDresser>();

            var kFill = TSBUtil.Box("KitchenPatienceFill", k, new Vector2(-800f, 230f), new Vector2(170f, 18f),
                new Color(.45f, .75f, .35f)).GetComponent<Image>();
            kFill.type = Image.Type.Filled;
            kFill.fillMethod = Image.FillMethod.Horizontal;
            kFill.fillAmount = 1f;

            // Pot — base + one overlay Image per slot; potRect = the Pot parent
            var pot = TSBUtil.Child("Pot", k);
            TSBUtil.Place(pot, new Vector2(0f, -180f), new Vector2(520f, 380f));
            TSBUtil.Box("PotBase", pot.transform, new Vector2(0f, -40f), new Vector2(520f, 300f), new Color(.28f, .28f, .30f));
            TSBUtil.Box("MainLayer", pot.transform, new Vector2(0f, 30f), new Vector2(260f, 110f), Color.white);
            TSBUtil.Box("SideLayer", pot.transform, new Vector2(-115f, -50f), new Vector2(180f, 85f), Color.white);
            TSBUtil.Box("SauceLayer", pot.transform, new Vector2(115f, -55f), new Vector2(160f, 75f), Color.white);

            // Shelf — 10 Jar prefab instances land here (task 8)
            var shelf = TSBUtil.Child("Shelf", k);
            var srt = TSBUtil.Place(shelf, new Vector2(0f, -140f), new Vector2(1700f, 230f));
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
            var hlg = shelf.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var flyer = TSBUtil.Box("Flyer", k, Vector2.zero, new Vector2(84f, 84f), Color.white);
            flyer.SetActive(false);

            TSBUtil.ButtonBox("ServeButton", k, new Vector2(640f, -390f), new Vector2(300f, 110f),
                new Color(.30f, .55f, .30f), "SERVE", 40f, new Color(.95f, .98f, .90f));

            TSBUtil.Child("StewBuilder", k).AddComponent<StewBuilder>();

            var kitchenHint = TSBUtil.Label("KitchenHint", k, new Vector2(0f, -480f), new Vector2(1000f, 50f),
                "Match the stew to what they're wearing.", 30f, new Color(.85f, .90f, .95f));
            kitchenHint.gameObject.SetActive(false);
        }

        // ------------------------------------------------ Overlay
        private static void BuildOverlayCanvas()
        {
            var o = MakeCanvas("OverlayCanvas", 20).transform;

            // ToastBanner — holder centered on the top screen edge; Banner starts at hiddenPos (0,120)
            var toastGo = TSBUtil.Child("ToastBanner", o);
            var trt = TSBUtil.Place(toastGo, Vector2.zero, new Vector2(900f, 80f));
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
            var banner = TSBUtil.Box("Banner", toastGo.transform, new Vector2(0f, 120f), new Vector2(900f, 80f),
                new Color(.15f, .12f, .20f, .95f));
            TSBUtil.Label("Label", banner.transform, Vector2.zero, new Vector2(880f, 80f),
                "toast", 36f, new Color(.97f, .93f, .80f));
            toastGo.AddComponent<ToastBanner>();

            // NotePanel — modal napkin reader
            var noteGo = TSBUtil.Child("NotePanel", o);
            var noteRoot = TSBUtil.Stretch("PanelRoot", noteGo.transform, new Color(0f, 0f, 0f, .6f), raycast: true);
            var napkin = TSBUtil.Box("Napkin", noteRoot.transform, Vector2.zero, new Vector2(720f, 540f),
                new Color(.93f, .90f, .80f));
            TSBUtil.Label("Header", napkin.transform, new Vector2(0f, 200f), new Vector2(620f, 60f),
                "Tell us what you thought of the food!", 30f, new Color(.25f, .18f, .12f));
            TSBUtil.Label("Guest", napkin.transform, new Vector2(0f, 140f), new Vector2(620f, 40f),
                "— guest —", 26f, new Color(.35f, .25f, .15f));
            TSBUtil.Label("Body", napkin.transform, new Vector2(0f, -30f), new Vector2(620f, 280f),
                "napkin text", 26f, new Color(.25f, .18f, .12f));
            TSBUtil.ButtonBox("PrevArrow", napkin.transform, new Vector2(-300f, -220f), new Vector2(64f, 64f),
                new Color(.75f, .70f, .58f), "<", 34f, new Color(.25f, .18f, .12f));
            TSBUtil.ButtonBox("NextArrow", napkin.transform, new Vector2(300f, -220f), new Vector2(64f, 64f),
                new Color(.75f, .70f, .58f), ">", 34f, new Color(.25f, .18f, .12f));
            TSBUtil.ButtonBox("CloseButton", napkin.transform, new Vector2(330f, 240f), new Vector2(56f, 56f),
                new Color(.70f, .30f, .25f), "X", 30f, Color.white);
            noteGo.AddComponent<NotePanel>();
            noteRoot.SetActive(false);

            // EndScreen
            var endGo = TSBUtil.Child("EndScreen", o);
            var endRoot = TSBUtil.Stretch("PanelRoot", endGo.transform, new Color(0f, 0f, 0f, .75f), raycast: true);
            var panel = TSBUtil.Box("Panel", endRoot.transform, Vector2.zero, new Vector2(820f, 640f),
                new Color(.20f, .16f, .13f));
            TSBUtil.Label("RankLabel", panel.transform, new Vector2(0f, 210f), new Vector2(300f, 120f),
                "?", 90f, new Color(.95f, .80f, .30f));
            TSBUtil.Label("ServedLabel", panel.transform, new Vector2(0f, 100f), new Vector2(700f, 50f),
                "Customers served: 0", 32f, new Color(.92f, .88f, .80f));
            TSBUtil.Label("HeartsLabel", panel.transform, new Vector2(0f, 45f), new Vector2(700f, 50f),
                "Hearts: 0", 32f, new Color(.92f, .88f, .80f));
            TSBUtil.Label("CoinsLabel", panel.transform, new Vector2(0f, -10f), new Vector2(700f, 50f),
                "Coins: 0", 32f, new Color(.92f, .88f, .80f));
            TSBUtil.Label("StoryLabel", panel.transform, new Vector2(0f, -120f), new Vector2(700f, 170f),
                "ending line", 26f, new Color(.85f, .80f, .72f));
            TSBUtil.ButtonBox("RetryButton", panel.transform, new Vector2(0f, -250f), new Vector2(260f, 85f),
                new Color(.30f, .50f, .30f), "RETRY", 36f, Color.white);
            endGo.AddComponent<EndScreen>();
            endRoot.SetActive(false);

            // IntroPanel — active at start; Start button doubles as the WebGL audio unlock
            var intro = TSBUtil.Stretch("IntroPanel", o, new Color(0f, 0f, 0f, .85f), raycast: true);
            TSBUtil.Label("Line1", intro.transform, new Vector2(0f, 140f), new Vector2(1000f, 90f),
                "[WRITER TEXT — paste verbatim] (intro line 1)", 34f, new Color(.95f, .92f, .82f));
            TSBUtil.Label("Line2", intro.transform, new Vector2(0f, 30f), new Vector2(1000f, 90f),
                "[WRITER TEXT — paste verbatim] (intro line 2)", 34f, new Color(.95f, .92f, .82f));
            TSBUtil.ButtonBox("StartButton", intro.transform, new Vector2(0f, -160f), new Vector2(300f, 95f),
                new Color(.50f, .35f, .18f), "START NIGHT", 36f, new Color(.97f, .93f, .82f));

            // DebugOverlay — tiny corner label
            var dbgGo = TSBUtil.Child("DebugOverlay", o);
            var drt = (RectTransform)dbgGo.transform;
            drt.anchorMin = drt.anchorMax = new Vector2(0f, 0f);
            drt.pivot = new Vector2(0f, 0f);
            drt.anchoredPosition = new Vector2(12f, 8f);
            drt.sizeDelta = new Vector2(760f, 32f);
            var dbgLabel = TSBUtil.Label("Label", dbgGo.transform, Vector2.zero, new Vector2(760f, 32f),
                "debug", 18f, new Color(1f, 1f, 1f, .6f), TextAlignmentOptions.Left);
            var lrt = (RectTransform)dbgLabel.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            dbgGo.AddComponent<DebugOverlay>();
        }
    }
}
