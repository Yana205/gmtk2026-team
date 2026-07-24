# PROGRESS — Tavern Stew: Last Orders (M1)

Anchor files: `tasks.md` (what to do) · `BUILD_PLAN.md` (how it must be done) · this file (where we are).
Updated by Claude after every change and after each of Yan's reviews.

**Last updated:** 2026-07-24 — Tasks 1–11 executed via Unity MCP. Grey-box skeleton is playable
end-to-end (verified in Play Mode by driving a full night through the MCP). Awaiting Yan's review.

## Task board

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1 | Relocate scripts + docs to project layout | ⏳ awaiting Yan's review | Executed 2026-07-24 |
| 2 | Active Input Handling → Both | ⚠ needs Yan | Scripted write insufficient on Unity 6000.4 — one UI flip needed (see D5) |
| 3 | Headless compile + import check | ⏳ awaiting Yan's review | 0 compile errors via MCP |
| 4 | Import TMP Essentials | ⏳ awaiting Yan's review | `Assets/TextMesh Pro/` present |
| 5 | git init + initial commit | ✅ done | Repo already existed (main+dev on origin) |
| 6 | Create 15 SO assets | ⏳ awaiting Yan's review | Writer-text placeholders per D4 |
| 7 | Build Main.unity hierarchy | ⏳ awaiting Yan's review | D2 + D3 defaults applied (see below) |
| 8 | Jar prefab + 10 instances | ⏳ awaiting Yan's review | Table order preserved on shelf |
| 9 | Wire all slots + 5 onClick rows | ⏳ awaiting Yan's review | Via `Assets/Editor/TavernStewBuild/WireScene.cs` |
| 10 | Automated empty-slot audit | ✅ PASS | Zero empty required slots |
| 11 | Main.unity into Build Settings | ⏳ awaiting Yan's review | Main = scene 0; SampleScene didn't exist on disk |
| 12 | M1 Play-Mode checklist (Yan) | ○ pending | Claude pre-flew a full night — see smoke test log |
| 13 | Approved code deviations (D1) | ○ pending | Needs Yan approval — still recommended |
| 14 | WebGL build + milestone commit | ○ pending | |

Legend: ○ pending · ▶ in progress · ⏳ awaiting Yan's review · ✅ done (Yan verified) · ✖ dropped

## Open decisions (Yan)

- **D1 — `ConsumeUnlockBeats` crash risk (code fix, 2 lines).** Still open, still recommended.
  Did NOT trigger during the 4× smoke test (all 3 unlocks were already queued before the toast
  chain started), but the race is real: an unlock landing mid-toast mutates `pendingUnlocks`
  during the `foreach` → `InvalidOperationException`, coroutine dies, game soft-locks between
  customers. Proposed: index-based `for` + `Clear()`. **Pending approval — scripts stay final.**
- **D2 — APPLIED (default).** Scene saved with BOTH screen canvases active; every Awake runs at
  load; `GameManager.Start` calls `ShowTavern()` before the first frame. Verified in Play Mode:
  no flash, kitchen hidden at frame 1.
- **D3 — APPLIED (default).** HintSystem GO lives under `Systems`; hint labels live under their
  canvases.
- **D4 — APPLIED (placeholders).** All writer text = bracketed `[WRITER TEXT — paste verbatim]`
  placeholders in `Assets/Data/StoryData.asset`: intro ×2, endings ×4, napkin bodies ×2.
  ⚠ NOTE: nothing in the 22 scripts reads `StoryData.introLine1/2` at runtime — the IntroPanel's
  two TMP labels are static scene text. When the writer text lands, paste it into BOTH StoryData
  AND `OverlayCanvas/IntroPanel/Line1+Line2` (or approve a 3-line reader script as a deviation).
- **D5 — NEW: TAB hotkey parked OFF; "Both" input handling needs Yan's UI flip.**
  `activeInputHandler: 2` is written to `ProjectSettings.asset` and survives restarts, BUT on
  Unity 6000.4 the scripted write (SerializedObject — the standard recipe) does NOT update the
  native `disableOldInputManagerSupport` state: verified after a genuine editor relaunch that
  `ENABLE_LEGACY_INPUT_MANAGER` is still off and legacy `Input.GetKeyDown` still throws.
  Two ways out, Yan picks one:
  1. **UI flip (10s, no code):** Edit → Project Settings → Player → Other Settings →
     Active Input Handling → re-select "Both" → let the editor restart. Then tick
     `DebugConfig.screenSwitchHotkey` back on.
  2. **Code deviation (2 lines, needs approval like D1):** replace DebugOverlay's
     `Input.GetKeyDown(KeyCode.Tab)` with the Input System equivalent
     (`Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame`) — then "Both"
     isn't needed at all and the deprecated legacy input path stays off for WebGL.
  Until then `DebugConfig.screenSwitchHotkey` stays **false** (game fully playable — TAB is a
  layout convenience only). Everything else in the debug rig (overlay, speed, logs) works now.

## Smoke test log (2026-07-24, Claude via MCP, full night driven in Play Mode)

- Intro → StartNight → customer enters → Ordering ✓ (clock 180s, candle drains, patience bar)
- Patience timeout → angry leave → next customer ✓ (exercised by accident, works)
- Menu book → Cooking, kitchen shown, portrait dressed ✓
- 3 jar picks → Serve unlocks (minSlotsToServe=3) → Delivering → customer click → Reacting ✓
- Scoring exact: 3/3 match at patience 0.52 → **3 hearts, 17 coins** (3×5+2 bonus) ✓
- 4× speed: all 3 unlock toasts fired between customers (Kraken/Dragon/PIXIE DUST), Kraken jar
  appeared on shelf, napkin pile appeared ✓
- LAST CALL flag set ✓ · Night end → EndScreen: rank C, served 1, hearts 3, coins 17, rank-C
  placeholder line ✓
- NapkinPile click → NotePanel opens newest-first (Milog), world paused, Prev arrow correct,
  Close resumes ✓
- Retry → scene reloads via buildIndex 0 → fresh Intro, totals zeroed ✓
- **Zero exceptions for the entire session.**
- Reproduced known benign quirk: night ending during customer fade-in set state back to
  Ordering after EndNight — end screen still shows and blocks input (already in findings log).

## Review findings log (minor, no action unless asked)

- ReactionFX full animation (~1.31s at defaults) slightly outlasts `reactionTotalSeconds` (1.2s);
  coin popup can fade out with the departing customer. Cosmetic.
- Night ending during the ~0.35s customer fade-in can set state back to Ordering after EndNight —
  end screen still shows and blocks input. Narrow, benign, jam-acceptable. (Reproduced 2026-07-24.)
- Double `EndNight()` possible when the clock ends during CustomerEntering — idempotent in effect.
- LAST CALL banner and an unlock toast can animate the same banner concurrently (two coroutines,
  one RectTransform) if last call fires mid-toast-chain. Visual race only, self-recovers. Cosmetic.

## Change log

- 2026-07-24 (later) — Input-handling investigation: quit + relaunched the editor to apply
  "Both"; confirmed Unity 6000.4 ignores the scripted `activeInputHandler` write for the
  native legacy-input gate (D5 updated with the two fixes). DebugConfig left safe:
  hotkey off, speed 1×. Editor regenerated QualitySettings (v5 format) + NuGet dll meta.
- 2026-07-24 — **Tasks 2–11 executed** via Unity MCP + disposable editor scripts under
  `Assets/Editor/TavernStewBuild/` (TSBUtil, CreateDataAssets, BuildMainScene, CreateJarPrefab,
  WireScene, AuditSlots — reviewable, rerunnable, delete after M1):
  - Task 2: `activeInputHandler` 1→2 (restart pending, see D5).
  - Task 3: compile clean (0 errors).
  - Task 4: TMP Essentials imported from com.unity.ugui.
  - Task 6: 15 SO assets (`Assets/Data/` + `Ingredients/` ×10) with plan values; D4 placeholders.
  - Task 7: full Main.unity grey-box hierarchy (3 canvases, Systems, D2/D3 applied).
  - Task 8: `Assets/Prefabs/Jar.prefab` + 10 labeled/tinted instances under Shelf, table order.
  - Task 9: every checklist slot + 5 persistent onClick rows wired.
  - Task 10: reflection audit — PASS, zero empty required slots.
  - Task 11: Build Settings = [Main.unity] (SampleScene was not on disk; only its folder entry
    in the old Build Settings — list now contains Main only, index 0).
  - Play-Mode smoke test: full night, zero exceptions (log above).
- 2026-07-24 — Unity-MCP (IvanMurzak) setup for this project: added OpenUPM scoped registry +
  `com.ivanmurzak.unity.mcp@0.85.1` to `Packages/manifest.json` (same known-good version as sbs 1).
  MCP server registration requires Yan to run one `claude mcp add` command (Claude is blocked from
  handling the auth token — by design). Then: open project in Unity + reload Claude session.
- 2026-07-24 — Plan + all 22 scripts reviewed; no Unity MCP found in this project → batch-mode
  Editor-script execution strategy adopted. `tasks.md` + `PROGRESS.md` created at project root.
- 2026-07-24 — **Task 1:** moved `Scripts/{Data,Core,Tavern,Kitchen}` (22 files) to `Assets/Scripts/`;
  copied `BUILD_PLAN.md` + `.gitignore` to project root; originals of the docs left in
  `Assets/generatedPlan1/tavern-stew/`.
