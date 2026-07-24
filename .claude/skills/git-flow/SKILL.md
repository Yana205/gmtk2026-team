---
name: git-flow
description: >
  Team git workflow for this Unity project — branch naming (feat/fix/bug/art/audio/chore),
  clean commits, pushes, and pull requests. Use whenever starting new work, committing,
  pushing, opening a PR, or merging. Enforces "never commit to main/dev directly",
  Unity .meta safety, and a simple main + dev + short-lived-branch model.
---

# git-flow — team workflow for gmtk2026-team

A small, strict workflow so a mixed macOS + Windows team can move fast without
stepping on each other. Read the rules, then use the copy-paste commands.

Remote: `origin` → https://github.com/Yana205/gmtk2026-team.git

---

## 1. Branch model

Two long-lived branches (never work directly on them):

| Branch | Purpose | Who merges here |
| --- | --- | --- |
| `main` | Stable / playable / release builds. Always green. | Only via PR from `dev` at a milestone. |
| `dev`  | Integration branch. Everyone's work lands here first. | Via PR from short-lived work branches. |

Everything else is a **short-lived work branch**: branch off `dev`, do one focused
thing, open a PR back into `dev`, then delete it.

```
main  ●───────────────────────●  (releases / jam submission)
       \                      /
dev     ●──●──●──●──●──●──●──●    (integration)
            \        /
feat/...     ●──●──●             (one branch = one task)
```

---

## 2. Branch naming

Format: `<type>/<short-kebab-description>` — lowercase, hyphens, no spaces.

| Prefix    | Use for | Example |
| --------- | ------- | ------- |
| `feat/`   | New gameplay / feature | `feat/dash-mechanic` |
| `fix/`    | Fixing a bug | `fix/player-clips-through-wall` |
| `bug/`    | Reproducing / investigating a bug (may become a `fix/`) | `bug/audio-cuts-out` |
| `art/`    | Sprites, models, textures, animations, VFX | `art/main-character-sprites` |
| `audio/`  | Music & sound effects | `audio/level1-bgm` |
| `chore/`  | Tooling, config, CI, deps, docs, refactors (a.k.a. "dev" work) | `chore/update-gitignore` |

If unsure, use `feat/`. Keep the description to 2–4 words.

---

## 3. Start new work (ALWAYS do this before editing)

Never start editing while on `main` or `dev`. Branch first, off the latest `dev`.

```bash
git switch dev
git pull --ff-only origin dev        # get everyone's latest
git switch -c feat/your-thing        # create + switch to your work branch
```

---

## 4. Commit

- Commit small and often. One logical change per commit.
- **Unity rule:** always stage an asset AND its `.meta` file together. `git add -A`
  handles this for you — prefer it over adding single files.
- Use Conventional Commit style messages:

```
<type>: <imperative summary>

# types: feat, fix, art, audio, chore, docs, refactor, perf
```

```bash
git add -A
git commit -m "feat: add dash with 0.2s i-frames"
```

Good: `fix: stop player clipping through moving platforms`
Avoid: `stuff`, `wip`, `asdf`.

---

## 5. Push

First push of a new branch sets the upstream:

```bash
git push -u origin HEAD        # HEAD = current branch, no need to type its name
```

After that, just:

```bash
git push
```

If push is rejected because the remote moved, sync then push (see §7).

---

## 6. Open a Pull Request (into `dev`)

Work branches always PR into `dev`, never straight into `main`.

```bash
git push -u origin HEAD
gh pr create --base dev --fill --web
```

- `--fill` pre-fills title/body from your commits; `--web` opens the browser to review.
- Add a teammate as reviewer if you want eyes on it: `--reviewer <github-username>`.
- Non-interactive (title + body from CLI):

```bash
gh pr create --base dev --title "feat: dash mechanic" \
  --body "Adds a dash with i-frames. Testing: play scene 1, press Shift."
```

After it's approved and merged on GitHub, clean up locally:

```bash
git switch dev
git pull --ff-only origin dev
git branch -d feat/your-thing        # delete the merged local branch
```

---

## 7. Keep your branch fresh / resolve "push rejected"

If `dev` moved on while you worked, bring your branch up to date:

```bash
git switch feat/your-thing
git fetch origin
git merge origin/dev                 # or: git rebase origin/dev (if you know rebase)
# resolve any conflicts, then:
git push
```

### Unity scene / prefab merge conflicts
Unity `.unity`, `.prefab`, and `.asset` files are YAML and merge badly by hand.
Set up Unity's Smart Merge (UnityYAMLMerge) once per machine — see §9. Coordinate
so two people rarely edit the same scene at once; prefer prefabs over big shared scenes.

---

## 8. Release: promote `dev` → `main`

At a milestone (playable build, jam submission):

```bash
git switch dev
git pull --ff-only origin dev
gh pr create --base main --head dev --title "release: <milestone>" --fill
# review, merge on GitHub, then optionally tag:
git switch main && git pull --ff-only origin main
git tag -a v0.1 -m "Playable build" && git push origin v0.1
```

---

## 9. One-time machine setup (each teammate)

```bash
# Identity (use your own name/email)
git config --global user.name  "Your Name"
git config --global user.email "you@example.com"

# Unity Smart Merge for scenes/prefabs (adjust path to your Unity version).
# macOS:
git config --global merge.unityyamlmerge.name "Unity SmartMerge"
git config --global merge.unityyamlmerge.driver '"/Applications/Unity/Hub/Editor/6000.4.7f1/Unity.app/Contents/Tools/UnityYAMLMerge" merge -p %O %B %A %A'
# Windows (PowerShell/CMD path):
# git config --global merge.unityyamlmerge.driver '"C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Data\Tools\UnityYAMLMerge.exe" merge -p %O %B %A %A'
```

---

## 10. Guardrails (do NOT break these)

- ❌ Never `git commit` or `git push` directly on `main` or `dev`. Branch first (§3).
- ❌ Never `git push --force` to `main` or `dev`. On your own branch, use
  `--force-with-lease` only if you understand it.
- ❌ Never commit `Library/`, `Temp/`, `Logs/`, `Build/` — `.gitignore` handles this.
- ✅ Always commit an asset and its `.meta` together (use `git add -A`).
- ✅ Pull `dev` with `--ff-only` before branching so you start from latest.
- ✅ One task = one branch = one PR. Keep PRs small and reviewable.

---

## Quick reference

```bash
# new work
git switch dev && git pull --ff-only origin dev && git switch -c feat/thing
# save + share
git add -A && git commit -m "feat: thing" && git push -u origin HEAD
# open PR into dev
gh pr create --base dev --fill --web
# after merge
git switch dev && git pull --ff-only origin dev && git branch -d feat/thing
```
