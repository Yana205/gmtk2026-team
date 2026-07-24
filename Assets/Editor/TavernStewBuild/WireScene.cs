using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TavernStewBuild
{
    // Task 9 — every serialized slot from BUILD_PLAN's wiring checklist + the 5 Button.onClick rows.
    public static class WireScene
    {
        [MenuItem("Tavern Stew/Build/4 Wire Scene")]
        public static void Run()
        {
            // data assets
            var config      = TSBUtil.Asset<GameConfigSO>("Assets/Data/GameConfig.asset");
            var tavernFeel  = TSBUtil.Asset<TavernFeelSO>("Assets/Data/TavernFeel.asset");
            var kitchenFeel = TSBUtil.Asset<KitchenFeelSO>("Assets/Data/KitchenFeel.asset");
            var story       = TSBUtil.Asset<StoryDataSO>("Assets/Data/StoryData.asset");
            var dbg         = TSBUtil.Asset<DebugConfigSO>("Assets/Data/DebugConfig.asset");
            var ingredients = new List<Object>();
            foreach (var key in CreateJarPrefab.Keys)
                ingredients.Add(TSBUtil.Asset<IngredientSO>("Assets/Data/Ingredients/ING_" + key + ".asset"));

            // systems
            var gm       = TSBUtil.Find<GameManager>("Systems/GameManager");
            var screens  = TSBUtil.Find<ScreenManager>("Systems/ScreenManager");
            var clock    = TSBUtil.Find<NightClock>("Systems/NightClock");
            var gen      = TSBUtil.Find<CustomerGenerator>("Systems/CustomerGenerator");
            var patience = TSBUtil.Find<PatienceMeter>("Systems/PatienceMeter");
            var hints    = TSBUtil.Find<HintSystem>("Systems/HintSystem");

            // tavern
            var customerT    = TSBUtil.FindT("TavernScreen/Customer");
            var customerBtn  = customerT.GetComponent<Button>();
            var customerView = customerT.GetComponent<CustomerView>();
            var customerBust = customerT.GetComponent<BustDresser>();
            var reactionFX   = TSBUtil.Find<ReactionFX>("TavernScreen/ReactionFX");
            var menuBookBtn  = TSBUtil.Find<Button>("TavernScreen/MenuBook");
            var napkinPile   = TSBUtil.Find<NapkinPile>("TavernScreen/NapkinPile");

            // kitchen
            var stew         = TSBUtil.Find<StewBuilder>("CookingScreen/StewBuilder");
            var portraitBust = TSBUtil.Find<BustDresser>("CookingScreen/Portrait");
            var serveBtn     = TSBUtil.Find<Button>("CookingScreen/ServeButton");

            // overlay
            var toast     = TSBUtil.Find<ToastBanner>("OverlayCanvas/ToastBanner");
            var notePanel = TSBUtil.Find<NotePanel>("OverlayCanvas/NotePanel");
            var endScreen = TSBUtil.Find<EndScreen>("OverlayCanvas/EndScreen");
            var overlayDbg = TSBUtil.Find<DebugOverlay>("OverlayCanvas/DebugOverlay");

            // ---------- serialized slots ----------
            TSBUtil.Wire(gm,
                ("config", config), ("tavernFeel", tavernFeel), ("story", story), ("debug", dbg),
                ("screens", screens), ("nightClock", clock),
                ("generator", gen), ("customerView", customerView),
                ("dishOnCounter", TSBUtil.FindGO("TavernScreen/DishOnCounter")),
                ("reactionFX", reactionFX), ("patience", patience), ("toast", toast),
                ("napkins", napkinPile), ("endScreen", endScreen),
                ("introPanel", TSBUtil.FindGO("OverlayCanvas/IntroPanel")),
                ("stewBuilder", stew), ("menuBookButton", menuBookBtn), ("customerButton", customerBtn));

            TSBUtil.Wire(screens,
                ("tavernScreen", TSBUtil.FindGO("TavernScreen")),
                ("cookingScreen", TSBUtil.FindGO("CookingScreen")));

            TSBUtil.Wire(clock,
                ("config", config), ("debug", dbg), ("gameManager", gm),
                ("timeLabel", TSBUtil.Find<TMP_Text>("TavernScreen/ClockUI/TimeLabel")),
                ("candleFill", TSBUtil.Find<Image>("TavernScreen/ClockUI/Candle")));

            TSBUtil.WireArray(gen, "allIngredients", ingredients.ToArray());
            TSBUtil.Wire(gen, ("debug", dbg));

            TSBUtil.Wire(customerView,
                ("feel", tavernFeel), ("bust", customerBust),
                ("group", customerT.GetComponent<CanvasGroup>()),
                ("rect", (RectTransform)customerT),
                ("thoughtBubble", TSBUtil.FindGO("TavernScreen/Customer/ThoughtBubble")));

            TSBUtil.Wire(customerBust,
                ("hatLayer", TSBUtil.Find<Image>("TavernScreen/Customer/HatLayer")),
                ("sideLayer", TSBUtil.Find<Image>("TavernScreen/Customer/SideLayer")),
                ("sauceLayer", TSBUtil.Find<Image>("TavernScreen/Customer/SauceLayer")),
                ("face", TSBUtil.Find<Image>("TavernScreen/Customer/Face")));
            // face sprites stay empty in grey-box (optional per plan)

            TSBUtil.Wire(portraitBust,
                ("hatLayer", TSBUtil.Find<Image>("CookingScreen/Portrait/HatLayer")),
                ("sideLayer", TSBUtil.Find<Image>("CookingScreen/Portrait/SideLayer")),
                ("sauceLayer", TSBUtil.Find<Image>("CookingScreen/Portrait/SauceLayer")));
            // portrait face stays empty per plan

            TSBUtil.Wire(patience,
                ("config", config), ("feel", tavernFeel), ("gameManager", gm),
                ("tavernBarRoot", TSBUtil.FindGO("TavernScreen/PatienceBar")),
                ("tavernFill", TSBUtil.Find<Image>("TavernScreen/PatienceBar/Fill")),
                ("kitchenFill", TSBUtil.Find<Image>("CookingScreen/KitchenPatienceFill")));

            TSBUtil.Wire(reactionFX,
                ("feel", tavernFeel),
                ("coinPopupLabel", TSBUtil.Find<TMP_Text>("TavernScreen/Customer/CoinPopup")),
                ("coinPopupRect", (RectTransform)TSBUtil.FindT("TavernScreen/Customer/CoinPopup")));
            TSBUtil.WireArray(reactionFX, "hearts", new Object[]
            {
                TSBUtil.FindGO("TavernScreen/Customer/Heart0"),
                TSBUtil.FindGO("TavernScreen/Customer/Heart1"),
                TSBUtil.FindGO("TavernScreen/Customer/Heart2"),
            });

            TSBUtil.Wire(toast,
                ("feel", tavernFeel),
                ("banner", (RectTransform)TSBUtil.FindT("OverlayCanvas/ToastBanner/Banner")),
                ("label", TSBUtil.Find<TMP_Text>("OverlayCanvas/ToastBanner/Banner/Label")));

            TSBUtil.Wire(napkinPile,
                ("notePanel", notePanel),
                ("pileVisual", TSBUtil.FindGO("TavernScreen/NapkinPile/PileVisual")));

            TSBUtil.Wire(notePanel,
                ("gameManager", gm), ("story", story),
                ("panelRoot", TSBUtil.FindGO("OverlayCanvas/NotePanel/PanelRoot")),
                ("headerLabel", TSBUtil.Find<TMP_Text>("OverlayCanvas/NotePanel/PanelRoot/Napkin/Header")),
                ("guestLabel", TSBUtil.Find<TMP_Text>("OverlayCanvas/NotePanel/PanelRoot/Napkin/Guest")),
                ("bodyLabel", TSBUtil.Find<TMP_Text>("OverlayCanvas/NotePanel/PanelRoot/Napkin/Body")),
                ("prevArrow", TSBUtil.FindGO("OverlayCanvas/NotePanel/PanelRoot/Napkin/PrevArrow")),
                ("nextArrow", TSBUtil.FindGO("OverlayCanvas/NotePanel/PanelRoot/Napkin/NextArrow")));

            TSBUtil.Wire(hints,
                ("gameManager", gm),
                ("tavernHint", TSBUtil.FindGO("TavernScreen/TavernHint")),
                ("kitchenHint", TSBUtil.FindGO("CookingScreen/KitchenHint")),
                ("serveHint", TSBUtil.FindGO("TavernScreen/ServeHint")));

            TSBUtil.Wire(endScreen,
                ("config", config),
                ("panelRoot", TSBUtil.FindGO("OverlayCanvas/EndScreen/PanelRoot")),
                ("servedLabel", TSBUtil.Find<TMP_Text>("OverlayCanvas/EndScreen/PanelRoot/Panel/ServedLabel")),
                ("heartsLabel", TSBUtil.Find<TMP_Text>("OverlayCanvas/EndScreen/PanelRoot/Panel/HeartsLabel")),
                ("coinsLabel", TSBUtil.Find<TMP_Text>("OverlayCanvas/EndScreen/PanelRoot/Panel/CoinsLabel")),
                ("rankLabel", TSBUtil.Find<TMP_Text>("OverlayCanvas/EndScreen/PanelRoot/Panel/RankLabel")),
                ("storyLabel", TSBUtil.Find<TMP_Text>("OverlayCanvas/EndScreen/PanelRoot/Panel/StoryLabel")));

            TSBUtil.Wire(stew,
                ("config", config), ("feel", kitchenFeel), ("debug", dbg), ("gameManager", gm),
                ("mainLayer", TSBUtil.Find<Image>("CookingScreen/Pot/MainLayer")),
                ("sideLayer", TSBUtil.Find<Image>("CookingScreen/Pot/SideLayer")),
                ("sauceLayer", TSBUtil.Find<Image>("CookingScreen/Pot/SauceLayer")),
                ("potRect", (RectTransform)TSBUtil.FindT("CookingScreen/Pot")),
                ("flyer", TSBUtil.Find<Image>("CookingScreen/Flyer")),
                ("rootCanvas", TSBUtil.Find<Canvas>("CookingScreen")),
                ("serveButton", serveBtn),
                ("portrait", portraitBust));
            var shelf = TSBUtil.FindT("CookingScreen/Shelf");
            var jarComps = new List<Object>();
            foreach (Transform c in shelf)
            {
                var jc = c.GetComponent<IngredientJar>();
                if (jc) jarComps.Add(jc);
            }
            if (jarComps.Count != 10)
                throw new System.Exception("[TSB] Expected 10 jars under Shelf, found " + jarComps.Count);
            TSBUtil.WireArray(stew, "jars", jarComps.ToArray());

            TSBUtil.Wire(overlayDbg,
                ("debug", dbg), ("gm", gm), ("clock", clock), ("screens", screens),
                ("label", TSBUtil.Find<TMP_Text>("OverlayCanvas/DebugOverlay/Label")));

            // ---------- the 5 Button.onClick rows (persistent listeners) ----------
            Click(TSBUtil.Find<Button>("OverlayCanvas/IntroPanel/StartButton"), gm.StartNight);
            Click(menuBookBtn, gm.OnMenuBookClicked);
            Click(customerBtn, gm.OnCustomerClicked);
            Click(TSBUtil.Find<Button>("OverlayCanvas/NotePanel/PanelRoot/Napkin/PrevArrow"), notePanel.Prev);
            Click(TSBUtil.Find<Button>("OverlayCanvas/NotePanel/PanelRoot/Napkin/NextArrow"), notePanel.Next);
            Click(TSBUtil.Find<Button>("OverlayCanvas/NotePanel/PanelRoot/Napkin/CloseButton"), notePanel.Close);
            Click(TSBUtil.Find<Button>("OverlayCanvas/EndScreen/PanelRoot/Panel/RetryButton"), endScreen.Retry);

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TSB] All serialized slots + onClick rows wired.");
        }

        private static void Click(Button b, UnityEngine.Events.UnityAction action)
        {
            for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(b.onClick, i);
            UnityEventTools.AddVoidPersistentListener(b.onClick, action);
            EditorUtility.SetDirty(b);
        }
    }
}
