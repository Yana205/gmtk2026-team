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
    // Task 7 (layout v2 after Yan's playtest) — Main.unity grey-box hierarchy.
    // Readability rules this layout enforces:
    //  - customer = ONE simple body rect + 3 clearly separated worn slots, each with a name label
    //  - static MAIN/SIDE/SAUCE captions teach the slot mapping on both screens
    //  - shelf is grouped into labeled MAIN/SIDE/SAUCE sections (manual positions, no layout groups)
    // Decision D2: scene SAVED with both screen canvases active so every Awake runs at load.
    // WARNING: rerunning wipes everything except Main Camera — jars + wiring must be redone after.
    public static class BuildMainScene
    {
        private static readonly Color CaptionColor = new Color(1f, 1f, 1f, .45f);
        private static readonly Color TextWarm = new Color(.95f, .92f, .80f);

        [MenuItem("Tavern Stew/Build/2 Build Main Scene")]
        public static void Run()
        {
            // GUARD (2026-07-24): the layout is now hand-grouped & edited manually. This button
            // DELETES everything in Main.unity and regenerates the old greybox, wiping that work.
            // Kept for reference only — must be confirmed twice to run.
            if (!EditorUtility.DisplayDialog(
                    "Wipe & rebuild Main.unity?",
                    "This DELETES the entire scene (including your manual grouping and any art) and " +
                    "regenerates the greybox from code.\n\nYou are editing the layout manually now — " +
                    "you almost certainly do NOT want this.",
                    "Wipe everything", "Cancel"))
                return;
            if (!EditorUtility.DisplayDialog(
                    "Are you absolutely sure?",
                    "Last chance. All manual layout work in Main.unity will be lost.",
                    "Yes, wipe it", "Cancel"))
                return;

            var scene = EditorSceneManager.OpenScene(TSBUtil.ScenePath, OpenSceneMode.Single);

            foreach (var root in scene.GetRootGameObjects())
                if (root.name != "Main Camera") Object.DestroyImmediate(root);

            SetupCamera();
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            BuildSystems();
            BuildTavernScreen();
            BuildCookingScreen();
            BuildOverlayCanvas();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TSB] Main.unity hierarchy built and saved (layout v2).");
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

        // Worn slot = colored rect + centered name label (BustDresser fills both at runtime)
        private static GameObject WornSlot(string name, Transform parent, Vector2 pos, Vector2 size, float fontSize)
        {
            var slot = TSBUtil.Box(name, parent, pos, size, Color.white);
            TSBUtil.Label("Label", slot.transform, Vector2.zero, size, "-", fontSize, new Color(.12f, .10f, .08f));
            return slot;
        }

        // ------------------------------------------------ Tavern
        // Layout v3 (tavern_layout.svg): customer CENTER · menu book MID-LEFT · a DESK strip along
        // the bottom holding the NIGHT BAR (bottom-left, was the top-left candle), the DISH
        // (bottom-center, hidden until serve) and the NAPKINS pile (bottom-right).
        private static void BuildTavernScreen()
        {
            var t = MakeCanvas("TavernScreen", 0).transform;
            TSBUtil.Stretch("Background", t, new Color(.16f, .12f, .10f));

            // DESK — decorative counter strip along the bottom, drawn under the desk items
            var desk = TSBUtil.Box("Desk", t, Vector2.zero, new Vector2(1860f, 250f), new Color(.13f, .09f, .07f));
            var deskRt = (RectTransform)desk.transform;
            deskRt.anchorMin = deskRt.anchorMax = new Vector2(0.5f, 0f);
            deskRt.pivot = new Vector2(0.5f, 0f);
            deskRt.anchoredPosition = new Vector2(0f, 20f);

            // ClockUI ("NIGHT BAR") — vertical bar, bottom-left, sitting on the desk; drains ↓.
            // Keeps GO names ClockUI/Candle + ClockUI/TimeLabel so NightClock wiring is unchanged.
            var clockUi = TSBUtil.Child("ClockUI", t);
            var crt = TSBUtil.Place(clockUi, new Vector2(150f, 220f), new Vector2(90f, 320f));
            crt.anchorMin = crt.anchorMax = new Vector2(0f, 0f);
            TSBUtil.Label("NightCaption", clockUi.transform, new Vector2(0f, 185f), new Vector2(140f, 30f),
                "NIGHT", 22f, CaptionColor);
            var candle = TSBUtil.Box("Candle", clockUi.transform, new Vector2(0f, 15f), new Vector2(60f, 250f),
                new Color(.93f, .85f, .55f)).GetComponent<Image>();
            candle.type = Image.Type.Filled;
            candle.fillMethod = Image.FillMethod.Vertical;
            candle.fillAmount = 1f;
            TSBUtil.Label("TimeLabel", clockUi.transform, new Vector2(0f, -185f), new Vector2(140f, 40f),
                "3:00", 34f, TextWarm);

            // Customer — CENTER. One simple body rect + 3 labeled worn slots + slot captions.
            var customer = TSBUtil.Child("Customer", t);
            TSBUtil.Place(customer, new Vector2(0f, 90f), new Vector2(360f, 520f));
            customer.AddComponent<CanvasGroup>();
            var body = TSBUtil.Box("Body", customer.transform, new Vector2(0f, -70f), new Vector2(280f, 360f),
                new Color(.50f, .46f, .42f), raycast: true);   // the Button's click surface
            WornSlot("HatLayer", customer.transform, new Vector2(0f, 170f), new Vector2(200f, 70f), 24f);
            WornSlot("SideLayer", customer.transform, new Vector2(0f, -40f), new Vector2(240f, 70f), 24f);
            WornSlot("SauceLayer", customer.transform, new Vector2(0f, -150f), new Vector2(160f, 56f), 22f);
            Caption(customer.transform, "MAIN >", new Vector2(-235f, 170f));
            Caption(customer.transform, "SIDE >", new Vector2(-235f, -40f));
            Caption(customer.transform, "SAUCE >", new Vector2(-235f, -150f));
            var bubble = TSBUtil.Box("ThoughtBubble", customer.transform, new Vector2(230f, 190f), new Vector2(100f, 80f),
                new Color(.95f, .95f, .90f));
            TSBUtil.Label("Label", bubble.transform, Vector2.zero, new Vector2(100f, 80f),
                "food?", 24f, new Color(.30f, .25f, .20f));
            bubble.SetActive(false);
            MakeHeart(customer.transform, "Heart0", new Vector2(-80f, 270f));
            MakeHeart(customer.transform, "Heart1", new Vector2(0f, 292f));
            MakeHeart(customer.transform, "Heart2", new Vector2(80f, 270f));
            var coin = TSBUtil.Label("CoinPopup", customer.transform, new Vector2(0f, 345f), new Vector2(200f, 50f),
                "+0", 40f, new Color(.98f, .85f, .30f));
            coin.gameObject.SetActive(false);
            var custBtn = customer.AddComponent<Button>();
            custBtn.targetGraphic = body.GetComponent<Image>();
            customer.AddComponent<CustomerView>();
            customer.AddComponent<BustDresser>();

            TSBUtil.Child("ReactionFX", t).AddComponent<ReactionFX>();

            // DISH — BOTTOM-CENTER, on the desk, hidden until Serve
            var dish = TSBUtil.Child("DishOnCounter", t);
            var dishRt = TSBUtil.Place(dish, new Vector2(0f, 140f), new Vector2(420f, 170f));
            dishRt.anchorMin = dishRt.anchorMax = new Vector2(0.5f, 0f);
            TSBUtil.Box("Dish", dish.transform, new Vector2(-60f, -10f), new Vector2(220f, 90f), new Color(.70f, .65f, .55f));
            TSBUtil.Box("MeadMug", dish.transform, new Vector2(150f, 5f), new Vector2(90f, 130f), new Color(.85f, .60f, .25f));
            dish.SetActive(false);

            // MenuBook — MID-LEFT, the primary call to action, big and obvious
            var menu = TSBUtil.ButtonBox("MenuBook", t, Vector2.zero, new Vector2(380f, 220f),
                new Color(.50f, .32f, .18f), "MENU BOOK", 40f, TextWarm);
            var menuRt = (RectTransform)menu.transform;
            menuRt.anchorMin = menuRt.anchorMax = new Vector2(0f, 0.5f);
            menuRt.anchoredPosition = new Vector2(320f, 20f);
            TSBUtil.Label("SubLabel", menu.transform, new Vector2(0f, -70f), new Vector2(360f, 40f),
                "click to start cooking", 22f, new Color(1f, 1f, 1f, .55f));

            // PatienceBar — hidden root, horizontal fill, above the (centered) customer
            var bar = TSBUtil.Child("PatienceBar", t);
            TSBUtil.Place(bar, new Vector2(0f, 400f), new Vector2(340f, 30f));
            TSBUtil.Stretch("BG", bar.transform, new Color(.10f, .09f, .08f));
            var fill = TSBUtil.Box("Fill", bar.transform, Vector2.zero, new Vector2(330f, 22f),
                new Color(.45f, .75f, .35f)).GetComponent<Image>();
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            bar.SetActive(false);

            // NapkinPile — BOTTOM-RIGHT, on the desk. Button surface = the (hidden) PileVisual.
            var pileGo = TSBUtil.Child("NapkinPile", t);
            var pileRt = TSBUtil.Place(pileGo, new Vector2(-200f, 140f), new Vector2(170f, 130f));
            pileRt.anchorMin = pileRt.anchorMax = new Vector2(1f, 0f);
            var pileVisual = TSBUtil.Box("PileVisual", pileGo.transform, Vector2.zero, new Vector2(150f, 110f),
                new Color(.92f, .90f, .82f), raycast: true);
            TSBUtil.Label("Label", pileVisual.transform, Vector2.zero, new Vector2(150f, 40f),
                "napkins", 22f, new Color(.35f, .28f, .20f));
            var pileBtn = pileGo.AddComponent<Button>();
            pileBtn.targetGraphic = pileVisual.GetComponent<Image>();
            pileGo.AddComponent<NapkinPile>();
            pileVisual.SetActive(false);

            // Hints (texts come from the HintSystem comments in the repo — not invented)
            var tavernHint = TSBUtil.Label("TavernHint", t, new Vector2(0f, 470f), new Vector2(1100f, 60f),
                "Read their outfit... then click the menu book!", 30f, TextWarm);
            tavernHint.gameObject.SetActive(false);
            var serveHint = TSBUtil.Label("ServeHint", t, new Vector2(0f, 470f), new Vector2(1100f, 60f),
                "Click the customer to hand it over!", 30f, TextWarm);
            serveHint.gameObject.SetActive(false);
        }

        private static void Caption(Transform parent, string text, Vector2 pos)
        {
            TSBUtil.Label("Caption" + text.Replace(" ", "").Replace(">", ""), parent, pos, new Vector2(150f, 36f),
                text, 22f, CaptionColor, TextAlignmentOptions.Right);
        }

        private static void MakeHeart(Transform parent, string name, Vector2 pos)
        {
            var h = TSBUtil.Box(name, parent, pos, new Vector2(52f, 52f), new Color(.90f, .25f, .35f));
            h.SetActive(false);
        }

        // ------------------------------------------------ Kitchen
        // Layout v3 (kitchen_layout.svg): portrait TOP-LEFT · pot MID-LEFT · shelf RIGHT as a
        // 3-column MAIN | SIDE | SAUCE matrix of jar buttons · serve button BOTTOM-CENTER.
        private static void BuildCookingScreen()
        {
            var k = MakeCanvas("CookingScreen", 1).transform;
            TSBUtil.Stretch("Background", k, new Color(.11f, .13f, .15f));

            // Portrait — TOP-LEFT. Mirrors the customer so the order follows you into the kitchen.
            var portrait = TSBUtil.Child("Portrait", k);
            var prt = TSBUtil.Place(portrait, new Vector2(200f, -220f), new Vector2(260f, 360f));
            prt.anchorMin = prt.anchorMax = new Vector2(0f, 1f);
            TSBUtil.Label("Caption", portrait.transform, new Vector2(0f, 165f), new Vector2(240f, 36f),
                "THE ORDER", 26f, CaptionColor);
            TSBUtil.Box("PortraitBase", portrait.transform, new Vector2(0f, -50f), new Vector2(170f, 190f),
                new Color(.45f, .42f, .40f));
            WornSlot("HatLayer", portrait.transform, new Vector2(0f, 80f), new Vector2(150f, 46f), 20f);
            WornSlot("SideLayer", portrait.transform, new Vector2(0f, -35f), new Vector2(160f, 46f), 20f);
            WornSlot("SauceLayer", portrait.transform, new Vector2(0f, -115f), new Vector2(130f, 40f), 18f);
            portrait.AddComponent<BustDresser>();

            var kFill = TSBUtil.Box("KitchenPatienceFill", k, new Vector2(200f, -430f), new Vector2(220f, 16f),
                new Color(.45f, .75f, .35f)).GetComponent<Image>();
            var kFillRt = (RectTransform)kFill.transform;
            kFillRt.anchorMin = kFillRt.anchorMax = new Vector2(0f, 1f);
            kFill.type = Image.Type.Filled;
            kFill.fillMethod = Image.FillMethod.Horizontal;
            kFill.fillAmount = 1f;

            // Shelf — RIGHT side, 3-column MAIN | SIDE | SAUCE matrix. Jars land here in task 8 at
            // fixed cell positions (manual, so locked jars leave a visible gap in their column).
            var shelf = TSBUtil.Child("Shelf", k);
            var srt = TSBUtil.Place(shelf, new Vector2(-360f, -20f), new Vector2(680f, 780f));
            srt.anchorMin = srt.anchorMax = new Vector2(1f, 0.5f);
            ShelfHeader(shelf.transform, "MAIN", -200f);
            ShelfHeader(shelf.transform, "SIDE", 0f);
            ShelfHeader(shelf.transform, "SAUCE", 200f);

            // Pot — MID-LEFT. Base + one overlay Image per slot; potRect = the Pot parent.
            var pot = TSBUtil.Child("Pot", k);
            var potRt = TSBUtil.Place(pot, new Vector2(430f, -60f), new Vector2(520f, 380f));
            potRt.anchorMin = potRt.anchorMax = new Vector2(0f, 0.5f);
            TSBUtil.Label("Caption", pot.transform, new Vector2(0f, 145f), new Vector2(500f, 36f),
                "THE POT — click jars to fill it", 26f, CaptionColor);
            TSBUtil.Box("PotBase", pot.transform, new Vector2(0f, -40f), new Vector2(520f, 300f), new Color(.28f, .28f, .30f));
            TSBUtil.Box("MainLayer", pot.transform, new Vector2(0f, 30f), new Vector2(260f, 110f), Color.white);
            TSBUtil.Box("SideLayer", pot.transform, new Vector2(-115f, -50f), new Vector2(190f, 90f), Color.white);
            TSBUtil.Box("SauceLayer", pot.transform, new Vector2(115f, -55f), new Vector2(170f, 80f), Color.white);

            var flyer = TSBUtil.Box("Flyer", k, Vector2.zero, new Vector2(84f, 84f), Color.white);
            flyer.SetActive(false);

            // ServeButton — BOTTOM-CENTER
            var serve = TSBUtil.ButtonBox("ServeButton", k, Vector2.zero, new Vector2(340f, 120f),
                new Color(.30f, .55f, .30f), "SERVE", 44f, new Color(.95f, .98f, .90f));
            var serveRt = (RectTransform)serve.transform;
            serveRt.anchorMin = serveRt.anchorMax = new Vector2(0.5f, 0f);
            serveRt.anchoredPosition = new Vector2(0f, 150f);

            TSBUtil.Child("StewBuilder", k).AddComponent<StewBuilder>();

            var kitchenHint = TSBUtil.Label("KitchenHint", k, new Vector2(0f, -500f), new Vector2(1100f, 50f),
                "Match the stew to what they're wearing.", 30f, new Color(.85f, .90f, .95f));
            kitchenHint.gameObject.SetActive(false);
        }

        // Header hangs from the shelf top, centered over its column (x is column offset)
        private static void ShelfHeader(Transform shelf, string text, float x)
        {
            var tmp = TSBUtil.Label("Header" + text, shelf, new Vector2(x, -30f), new Vector2(180f, 36f),
                text, 28f, CaptionColor);
            var rt = (RectTransform)tmp.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);   // hang from the shelf top
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
