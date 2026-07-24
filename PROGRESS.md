# PROGRESS — Tavern Stew: Last Orders (M1)

Anchor files: `tasks.md` (what to do) · `BUILD_PLAN.md` (how it must be done) · this file (where we are).
Updated by Claude after every change and after each of Yan's reviews.

**Last updated:** 2026-07-24 — Task 1 executed, awaiting Yan's review.

## Task board

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1 | Relocate scripts + docs to project layout | ⏳ awaiting Yan's review | Executed 2026-07-24 |
| 2 | Active Input Handling → Both | ○ pending | |
| 3 | Headless compile + import check | ○ pending | |
| 4 | Import TMP Essentials | ○ pending | |
| 5 | git init + initial commit | ○ pending | Yan's call |
| 6 | Create 15 SO assets | ○ pending | Writer-text placeholders (D4) |
| 7 | Build Main.unity hierarchy | ○ pending | Needs D2 + D3 decisions |
| 8 | Jar prefab + 10 instances | ○ pending | |
| 9 | Wire all slots + 5 onClick rows | ○ pending | |
| 10 | Automated empty-slot audit | ○ pending | |
| 11 | Main.unity into Build Settings | ○ pending | SampleScene keep/delete = Yan's call |
| 12 | M1 Play-Mode checklist (Yan) | ○ pending | |
| 13 | Approved code deviations (D1) | ○ pending | Needs Yan approval |
| 14 | WebGL build + milestone commit | ○ pending | |

Legend: ○ pending · ▶ in progress · ⏳ awaiting Yan's review · ✅ done (Yan verified) · ✖ dropped

## Open decisions (Yan)

- **D1 — `ConsumeUnlockBeats` crash risk (code fix, 2 lines).** `foreach` over `pendingUnlocks`
  while NightClock keeps ticking during the toast yield; an unlock landing mid-toast mutates the
  list mid-iteration → `InvalidOperationException`. Unlikely at default values, easy to hit once
  configs are retuned (they're meant to be). Proposed: index-based `for` + `Clear()`. **Pending.**
- **D2 — Save scene with CookingScreen active.** Plan says "inactive at start", but then
  `StewBuilder.Awake` only runs on the first kitchen visit and re-hides any jar unlocked before
  that (unwinnable orders possible: generator already serves Kraken, jar hidden). Saving the canvas
  active runs all Awakes at load; `GameManager.Start` calls `ShowTavern()` before the first frame,
  so nothing flashes. Zero code change. **Pending.**
- **D3 — HintSystem GO under `Systems`.** Plan's wiring checklist includes HintSystem but the scene
  hierarchy never places it. Low risk default unless Yan objects. **Pending (soft).**
- **D4 — Writer text placeholders.** Napkin bodies (Letitia, Milog), intro lines, 4 rank endings are
  not in the repo and the plan forbids inventing them → bracketed `[WRITER TEXT — paste verbatim]`
  placeholders in StoryData until Yan supplies the real strings. **Pending (needs the text).**

## Review findings log (minor, no action unless asked)

- ReactionFX full animation (~1.31s at defaults) slightly outlasts `reactionTotalSeconds` (1.2s);
  coin popup can fade out with the departing customer. Cosmetic.
- Night ending during the ~0.35s customer fade-in can set state back to Ordering after EndNight —
  end screen still shows and blocks input. Narrow, benign, jam-acceptable.
- Double `EndNight()` possible when the clock ends during CustomerEntering — idempotent in effect.

## Change log

- 2026-07-24 — Unity-MCP (IvanMurzak) setup for this project: added OpenUPM scoped registry +
  `com.ivanmurzak.unity.mcp@0.85.1` to `Packages/manifest.json` (same known-good version as sbs 1).
  MCP server registration requires Yan to run one `claude mcp add` command (Claude is blocked from
  handling the auth token — by design). Then: open project in Unity + reload Claude session.
  Bulk build tasks (6–9) still go through Editor scripts; MCP is for inspection/verification.
- 2026-07-24 — Plan + all 22 scripts reviewed; no Unity MCP found in this project → batch-mode
  Editor-script execution strategy adopted. `tasks.md` + `PROGRESS.md` created at project root.
- 2026-07-24 — **Task 1:** moved `Scripts/{Data,Core,Tavern,Kitchen}` (22 files) to `Assets/Scripts/`;
  copied `BUILD_PLAN.md` + `.gitignore` to project root; originals of the docs left in
  `Assets/generatedPlan1/tavern-stew/`.
