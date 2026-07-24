# Tasks: Tavern Stew — Last Orders (M1 grey-box build)

Source plan: `Assets/generatedPlan1/tavern-stew/BUILD_PLAN.md` (copied to project root as `BUILD_PLAN.md` in Task 1).
Progress state: `PROGRESS.md` (updated after every change and after each of Yan's reviews).

**Workflow:** one task at a time → Yan verifies → mark done in PROGRESS.md → next task.

**Execution method:** the Unity MCP bridge from the plan's kickoff prompt is NOT installed in this
project. Instead, Unity-side work (SO assets, scene, prefab, wiring) is done with disposable Editor
scripts under `Assets/Editor/TavernStewBuild/`, run headlessly via Unity 6000.4.7f1 batch-mode CLI
(when the editor is closed) or via menu items (when Yan has the editor open). Same result, reviewable
as code first.

**Rule:** the 22 gameplay scripts are FINAL. Any change to them is a deviation that needs Yan's
explicit approval first (see Decisions D1–D2 in PROGRESS.md).

---

## Phase 1 — M0 project setup

1. [x] **Relocate scripts into the real project layout.**
   Move `Assets/generatedPlan1/tavern-stew/Assets/Scripts/{Data,Core,Tavern,Kitchen}` →
   `Assets/Scripts/{Data,Core,Tavern,Kitchen}` (move, not copy — a copy would leave duplicate
   classes that break compilation). Copy `BUILD_PLAN.md` and `.gitignore` to the project root.
   No `.meta` files exist yet in the plan folder, so the move is clean.
   **Verify:** folder tree matches the plan's tree; `generatedPlan1` keeps only the doc + gitignore.

2. [x] **Project settings: Active Input Handling → "Both".** (depends on: 1)
   `ProjectSettings.asset` currently has `activeInputHandler: 1` (Input System only). DebugOverlay
   uses legacy `Input.GetKeyDown` and would throw at runtime. Plan M0 requires "Both" (= 2).
   **Verify:** grep shows `activeInputHandler: 2`; Unity opens without input errors.

3. [x] **Headless compile + import check.** (depends on: 1, 2)
   Run Unity batch-mode (`-batchmode -quit`) to import the moved scripts and compile.
   **Verify:** log shows 0 compile errors; `.meta` files generated under `Assets/Scripts`.

4. [x] **Import TMP Essentials.** (depends on: 3)
   Batch-import `TMP Essential Resources.unitypackage` from the uGUI package (plan M0: first TMP use).
   **Verify:** `Assets/TextMesh Pro/` exists with default font asset.

5. [x] **git init + initial commit.** (Yan's call — plan M0 says "git init, commit every milestone";
   no repo exists today.)
   **Verify:** `git log` shows the initial commit; Library/Temp ignored.

## Phase 2 — Data layer (SO assets)

6. [x] **Create the 15 SO assets with plan values.** (depends on: 3)
   Editor script `CreateDataAssets` creates `Assets/Data/{GameConfig,TavernFeel,KitchenFeel,StoryData,DebugConfig}.asset`
   and `Assets/Data/Ingredients/ING_*.asset` ×10 (displayName / slot / startsLocked / placeholderColor
   from the plan table). StoryData: toast lines from plan, unlockBeats index-aligned with {60,105,135},
   idealStew references wired, beat[2] hasNapkin=false. Intro lines, 4 rank endings, and the 2 napkin
   bodies get bracketed `[WRITER TEXT — paste verbatim]` placeholders — the writer's text is not in
   this repo and the plan forbids inventing it (Decision D4). DebugConfig: masterEnabled ON for dev.
   **Verify:** Yan opens the assets in the Inspector and checks values against the plan tables.

## Phase 3 — Scene, prefab, wiring

7. [x] **Build `Assets/Scenes/Main.unity` hierarchy (grey-box).** (depends on: 6)
   Editor script `BuildMainScene`: Camera, EventSystem (Input System UI module), `Systems` GO with
   GameManager/ScreenManager/NightClock/CustomerGenerator/PatienceMeter **+ HintSystem** (plan's
   wiring checklist includes HintSystem but its hierarchy omits it — Systems is the always-active
   home, Decision D3), TavernScreen / CookingScreen / OverlayCanvas exactly per the plan's tree.
   Canvas Scaler on all 3 canvases: Scale With Screen Size 1920×1080, Match 0.5. Grey rectangles +
   TMP labels everywhere; candle Image = vertical fill; patience fill = horizontal.
   ⚠ Decision D2 (needs Yan's OK): save the scene with CookingScreen ACTIVE so every Awake runs at
   load (`GameManager.Start` hides it before the first frame anyway). Saving it inactive per the plan
   leaves a latent bug: `StewBuilder.Awake` runs on first kitchen visit and re-hides any jar that was
   unlocked before then.
   **Verify:** Yan opens Main.unity and walks the hierarchy against the plan's tree.

8. [x] **Jar prefab + 10 shelf instances.** (depends on: 7)
   `Assets/Prefabs/Jar.prefab` = Button + IngredientJar + icon Image + TMP name label. 10 instances
   under Shelf (HorizontalLayoutGroup), each assigned its IngredientSO + StewBuilder + KitchenFeel.
   **Verify:** prefab exists; 10 labeled jars under Shelf, ingredients assigned in order.

9. [x] **Wire every serialized slot + the 5 Button.onClick rows.** (depends on: 8)
   Editor script `WireScene` fills the full checklist from the plan (GameManager 17 slots,
   ScreenManager, NightClock, CustomerGenerator allIngredients ×10, CustomerView, both BustDressers,
   PatienceMeter, ReactionFX, ToastBanner, NapkinPile, NotePanel, HintSystem, EndScreen, StewBuilder,
   DebugOverlay) + persistent onClick: StartButton→StartNight, MenuBook→OnMenuBookClicked,
   Customer→OnCustomerClicked, NotePanel Prev/Next/Close, Retry→EndScreen.Retry.
   **Verify:** Task 10's audit + Yan spot-checks GameManager and StewBuilder in the Inspector.

10. [x] **Automated empty-slot audit.** (depends on: 9)
    Editor script `AuditSlots` reflection-scans every `[SerializeField]` on all scene components and
    reports empty slots ("an empty inspector slot = a visible bug"), whitelisting the plan's
    optional ones (debug slots, timeLabel/candleFill in grey-box, kitchenFill, face sprites).
    **Verify:** audit output = zero missing required slots.

11. [x] **Build Settings: Main.unity as scene 0.** (depends on: 7)
    Currently only SampleScene is listed. `EndScreen.Retry` reloads by buildIndex — a scene not in
    Build Settings has index −1 and Retry would throw. Keep or delete SampleScene (Yan's call).
    **Verify:** EditorBuildSettings lists Main.unity first.

## Phase 4 — Verification & ship prep

12. [ ] **M1 Play-Mode checklist run (Yan, in-editor).** (depends on: 10, 11)
    Full night: intro → customers → cook → serve → react → toasts/unlocks → napkins → LAST CALL →
    end screen → Retry. Use the debug rig (TAB, 4× speed) per the plan's M1 checklist. I stay on
    standby to fix whatever it turns up.
    **Verify:** every box in the plan's "M1 verification checklist" ticks.

13. [ ] **Apply approved code deviations (only if Yan approves).**
    D1: `GameManager.ConsumeUnlockBeats` iterates `pendingUnlocks` with `foreach` while the clock can
    still `Add()` to it mid-toast → `InvalidOperationException` risk. Fix = index-based `for` loop
    (2 lines). Held until approved — scripts are declared final.
    **Verify:** re-run Task 12's affected checks at 4× speed.

14. [ ] **WebGL build (Gzip + decompression fallback), test in Chrome, milestone commit.** (depends on: 12)
    **Verify:** build loads in Chrome, full night playable, then commit.
