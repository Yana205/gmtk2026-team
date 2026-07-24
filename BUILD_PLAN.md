# Tavern Stew: Last Orders — Unity Build Plan for Claude Code + Unity MCP
**Status:** All architecture steps approved by Yan in chat, July 23. All 22 scripts final and included in `Assets/Scripts/`. This document is the single source of truth for the build.

## Architecture principles (enforce these in every change)
1. **One brain.** Only `GameManager` changes state. Everything else reports up via public methods or C# events.
2. **All numbers live in SO assets.** Balance → `GameConfig`. Front-desk feel → `TavernFeel`. Kitchen feel → `KitchenFeel`. Strings → `StoryData` (writer's words VERBATIM). Debug → `DebugConfig`. Zero hardcoded gameplay numbers in scripts.
3. **SO edits during Play Mode persist** — that is the tuning workflow. Scene component fields reset on stop; SO assets don't.
4. **Wiring is `[SerializeField]` slots + C# events.** An empty inspector slot = a visible bug. No `Find()`, no singletons, no UnityEvents wired in inspector (except Button.onClick for pure UI buttons listed below).
5. **Update() exists in exactly 3 scripts:** `NightClock`, `PatienceMeter`, `DebugOverlay`. Everything else is event/click/coroutine-driven.
6. **Logic scripts live under `Systems` (always active).** Scripts under a disabled canvas stop running — that's why the timers are never children of the two screen canvases.

## Folder tree
```
Assets/
  Scripts/
    Data/     SlotType, IngredientSO, GameConfigSO, TavernFeelSO, KitchenFeelSO, StoryDataSO, DebugConfigSO
    Core/     GameManager, ScreenManager, NightClock, ScoringService, Tween, DebugOverlay
    Tavern/   BustDresser, CustomerGenerator, CustomerView, PatienceMeter, ReactionFX,
              ToastBanner, NapkinPile, NotePanel, HintSystem, EndScreen
    Kitchen/  StewBuilder, IngredientJar
  Data/       (SO asset instances)
    GameConfig, TavernFeel, KitchenFeel, StoryData, DebugConfig
    Ingredients/ ING_Ham ... ING_PixieDust (x10)
  Prefabs/    Jar.prefab
  Sprites/    (artist drops land here)
  Scenes/     Main.unity
```

## Project settings checklist (M0)
- Unity 6, 2D template. **WebGL build target from day one.**
- Canvas Scaler on every canvas: Scale With Screen Size, **1920×1080**, Match 0.5.
- Player Settings → **Active Input Handling: "Both"** (DebugOverlay uses legacy `Input.GetKeyDown`).
- Import **TMP Essentials** on first TMP use.
- WebGL publishing: Gzip + decompression fallback. Build every milestone.
- git init at M0, commit every milestone. `.gitignore` included in this package.

## SO assets to create (values)
| Asset | Menu | Notes |
|---|---|---|
| `GameConfig` | Tavern Stew/Game Config | defaults already in code: night 180, lastCall 30, patience 20, delayBetweenCustomers 0.5, **minSlotsToServe 3**, coins 5/2/×2, ranks {0,8,14,20}/{C,B,A,S}, unlocks {60,105,135} |
| `TavernFeel` | Tavern Stew/Feel - Tavern | defaults in code |
| `KitchenFeel` | Tavern Stew/Feel - Kitchen | defaults in code |
| `StoryData` | Tavern Stew/Story Data | fill: intro placeholders, 4 rank endings, lastCallBanner, 3 unlockBeats (below), napkinHeader |
| `DebugConfig` | Tavern Stew/Debug Config | masterEnabled ON during dev; **OFF for the shipped build** |

**Ingredients (10 assets in Assets/Data/Ingredients/).** displayName / slot / startsLocked / suggested placeholderColor:
| Asset | Slot | Locked | Placeholder color |
|---|---|---|---|
| ING_Ham | Main | no | pink (1, .6, .7) |
| ING_Venison | Main | no | brown (.55, .35, .2) |
| ING_Kraken | Main | **yes** | teal (.2, .7, .7) |
| ING_Dragon | Main | **yes** | red (.85, .2, .15) |
| ING_ForestOnion | Side | no | green (.35, .7, .3) |
| ING_MountainCarrot | Side | no | orange (.95, .55, .15) |
| ING_Eggplant | Side | no | purple (.55, .3, .65) |
| ING_ParadiseCumin | Sauce | no | yellow (.95, .85, .3) |
| ING_SalamanderPepper | Sauce | no | dark red (.6, .1, .1) |
| ING_PixieDust | Sauce | **yes** | light blue (.7, .85, 1) |

**StoryData.unlockBeats (index-aligned with unlockTimes {60,105,135}):**
- [0] Kraken · toast "Letitia dropped off today's kill: KRAKEN!" · hasNapkin ✓ · guest "Letitia" · napkinText = writer's seafood text VERBATIM · idealStew: Kraken+MountainCarrot+SalamanderPepper (dormant)
- [1] Dragon · toast "Milog dropped off today's kill: DRAGON!" · hasNapkin ✓ · guest "Milog" · napkinText = writer's quest text VERBATIM · idealStew: Venison+ForestOnion+ParadiseCumin (dormant)
- [2] PixieDust · toast "New on the shelf: PIXIE DUST" · hasNapkin ✗ (**do not invent text**)

## Scene hierarchy (Main.unity — ONE scene, never reloaded except Retry)
```
Main Camera
EventSystem
Systems                          (empty GO — always active; ALL logic lives here)
  GameManager                    (GameManager)
  ScreenManager                  (ScreenManager)
  NightClock                     (NightClock)
  CustomerGenerator              (CustomerGenerator)
  PatienceMeter                  (PatienceMeter)
TavernScreen                     (Canvas — active at start)
  Background                     (Image)
  ClockUI                        (candle Image w/ Fill Method Vertical + TMP time label)
  Customer                       (Button → GameManager.OnCustomerClicked; CanvasGroup; CustomerView; BustDresser)
    BustBase / HatLayer / SideLayer / SauceLayer / Face   (Images)
    ThoughtBubble                (Image, hidden)
    Hearts                       (3 Images, hidden — ReactionFX)
    CoinPopup                    (TMP, hidden — ReactionFX)
  ReactionFX                     (ReactionFX component, e.g. on Customer or own GO)
  DishOnCounter                  (parent GO, hidden: Dish Image + MeadMug Image)
  MenuBook                       (Button → GameManager.OnMenuBookClicked)
  PatienceBar                    (root GO hidden: bg + Fill Image w/ Fill Method Horizontal)
  NapkinPile                     (Button + NapkinPile; PileVisual child hidden)
  TavernHint                     (TMP, hidden)
  ServeHint                      (TMP, hidden)
CookingScreen                    (Canvas — inactive at start)
  Background                     (Image)
  Portrait                       (BustDresser — small bust: base + 3 layers, no face needed)
  KitchenPatienceFill            (optional Image — PatienceMeter.kitchenFill)
  Pot                            (PotBase Image + Main/Side/Sauce overlay Images; potRect = Pot)
  Shelf                          (HorizontalLayoutGroup — 10 × Jar prefab, one per IngredientSO)
  Flyer                          (Image, disabled — StewBuilder.flyer)
  ServeButton                    (Button — StewBuilder wires onClick in code)
  StewBuilder                    (component on CookingScreen root or own GO under it)
  KitchenHint                    (TMP, hidden)
OverlayCanvas                    (Canvas — ALWAYS active, sort order above both screens)
  ToastBanner                    (ToastBanner; Banner child at hiddenPos + TMP label)
  NotePanel                      (NotePanel; PanelRoot hidden: napkin bg, header/guest/body TMPs,
                                  Prev/Next arrow Buttons → NotePanel.Prev/Next, Close Button → NotePanel.Close)
  EndScreen                      (EndScreen; PanelRoot hidden: 5 TMPs + Retry Button → EndScreen.Retry)
  IntroPanel                     (active at start: darkened bg, 2 TMP lines from StoryData,
                                  Start Button → GameManager.StartNight — doubles as WebGL audio unlock)
  DebugOverlay                   (DebugOverlay + tiny TMP label, corner)
```
**Jar prefab:** Button + IngredientJar + icon Image + TMP name label. 10 instances under Shelf, each with its IngredientSO + StewBuilder + KitchenFeel wired. Locked ones are auto-hidden by StewBuilder.Awake.

## Button.onClick wiring (the ONLY inspector-wired events — all pure UI)
| Button | → |
|---|---|
| IntroPanel/StartButton | GameManager.StartNight |
| MenuBook | GameManager.OnMenuBookClicked |
| Customer | GameManager.OnCustomerClicked |
| NotePanel Prev / Next / Close | NotePanel.Prev / Next / Close |
| EndScreen/RetryButton | EndScreen.Retry |

(Jar and Serve buttons are wired in code. NapkinPile wires itself.)

## Serialized-slot wiring checklist (every slot must be non-empty unless marked optional)
- **GameManager:** config, tavernFeel, story, debug(opt), screens, nightClock, generator, customerView, dishOnCounter, reactionFX, patience, toast, napkins, endScreen, introPanel, stewBuilder, menuBookButton, customerButton
- **ScreenManager:** tavernScreen, cookingScreen
- **NightClock:** config, debug(opt), gameManager, timeLabel(opt in greybox), candleFill(opt)
- **CustomerGenerator:** allIngredients (all 10!), debug(opt)
- **CustomerView:** feel(TavernFeel), bust, group, rect, thoughtBubble
- **BustDresser (Customer):** 3 layers + face + 3 face sprites (sprites opt in greybox)
- **BustDresser (Portrait):** 3 layers, face empty
- **PatienceMeter:** config, feel, gameManager, tavernBarRoot, tavernFill, kitchenFill(opt)
- **ReactionFX:** feel, hearts[3], coinPopupLabel, coinPopupRect
- **ToastBanner:** feel, banner, label (+ shownPos/hiddenPos to taste)
- **NapkinPile:** notePanel, pileVisual · **NotePanel:** gameManager, story, panelRoot, 3 labels, 2 arrows
- **HintSystem:** gameManager, 3 hint GOs · **EndScreen:** config, panelRoot, 5 labels
- **StewBuilder:** config, feel(KitchenFeel), debug(opt), gameManager, 3 pot layers, potRect, jars[10], flyer, rootCanvas(CookingScreen canvas), serveButton, portrait
- **IngredientJar (per jar):** ingredient, stewBuilder, feel(KitchenFeel), icon, nameLabel
- **DebugOverlay:** debug, gm, clock, screens, label

## Build order for Claude Code + MCP (M1)
1. Copy `Assets/Scripts/` in → let Unity compile (all files at once; cross-references resolve).
2. Create the 15 SO assets with the values above.
3. Build the scene hierarchy exactly as listed (grey rectangles + TMP labels everywhere; no art needed — placeholderColor tints make everything readable).
4. Wire all serialized slots (checklist above), then the 5 Button.onClick rows.
5. Make the Jar prefab, place 10, assign ingredients.
6. Enter Play Mode: full night must be playable start→end screen.
7. WebGL build, test in Chrome. Commit.

## Debug rig (DebugConfig asset)
- TAB = flip screens (layout tool, ignores game locks — view only, state guards still apply).
- nightSpeedMultiplier 4 = whole night in 45s (test LAST CALL, unlocks, endings fast). The unlock check is a while-loop so fast-forward can't skip beats.
- unlockAllJarsAtStart / skipIntro for art & flow testing.
- **Ship:** untick masterEnabled (or set every `debug` slot to None — all access is null-safe).

## M1 verification checklist
- [ ] Full night playable: intro → customers → cook → serve → react → ... → end screen → Retry works
- [ ] State field on GameManager matches expectations at every step; no state ever changes outside GameManager
- [ ] Menu book dim outside Ordering; customer dim outside Delivering (Disabled Color on Buttons)
- [ ] Serve locked until minSlotsToServe picks; same-category click replaces pot layer
- [ ] Jar click → flyer arcs into pot; pot layer updates instantly (state before juice)
- [ ] At 4× speed: 3 toasts fire between customers, new jars clickable, napkins open, clock+patience freeze while note open
- [ ] Patience empty → angry leave from tavern AND from inside the kitchen (kicks back, clears stew)
- [ ] Clock zero mid-cooking → beat finishes, then end screen (mid-dish grace)
- [ ] Rank line matches rank; all writer strings verbatim, no truncation

## Approved amendments vs plan v2.3 (chat, July 23)
1. **GameFeelSO split** into TavernFeelSO + KitchenFeelSO (per-screen tuning; each has its own easeCurve).
2. **`minSlotsToServe` slider (0–3)** in GameConfig amends the locked "Serve at 3/3": default 3 = original behavior; partial serves are now a Saturday playtest decision, not a code change. Scoring unchanged (missing slot = no match).
3. **DebugConfigSO rig** added (master switch, overlay, TAB hotkey, speed/unlock/intro cheats). Null-safe, strippable.
4. **`delayBetweenCustomers`** pacing knob added to GameConfig.
5. **Mid-dish grace** on night end (beat in progress finishes; waiting customers cut immediately).
6. **"Only PatienceMeter has Update()"** refined to: NightClock, PatienceMeter, DebugOverlay tick; all logic is event-driven.
7. **placeholderColor** on IngredientSO: playable, readable grey-box with zero art.
8. Kitchen may show an optional patience fill + the portrait (pressure follows you in).

## Kickoff prompt for Claude Code (paste this)
> Open the Unity project via the Unity MCP bridge. Read BUILD_PLAN.md at the repo root and follow it exactly. The scripts in Assets/Scripts are final — do not rewrite them; your job is: (1) create the SO assets with the listed values, (2) build the Main scene hierarchy as specified, (3) wire every serialized slot per the checklist, (4) create the Jar prefab and 10 instances, (5) enter Play Mode and verify the M1 checklist. Use grey placeholder rectangles and TMP labels for all visuals. Ask before deviating from the plan in any way.
