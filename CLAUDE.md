# Tiny Greenhouse XR — Claude Code Project Instructions

## Start Here

Read these files before doing anything:
- `docs/BUILD_PROGRESS.md` — current state, what's done, what's broken, what's next
- `docs/ARCHITECTURE.md` — code rules, manager diagram, what's out of scope
- `docs/IMPLEMENTATION_PLAN.md` — the full 12-step build guide

Current position: **Prompt 5 (CropData + PotSlot)** is the immediate next task.

---

## Hard Rules — Never Break These

- Planted pots = generators. Active pot count drives money rate. Do not change this mapping.
- All XR interactions = `XRSimpleInteractable` button presses. No physics grab.
- `FeedbackManager` contains zero game logic. It only dispatches effects.
- `EaseScale` = spawn from zero. `ScalePop` = button press pulse. Never swap them.
- All managers are singletons on one `Managers` GameObject. No new GameManager-like objects.
- `CropData` = ScriptableObject. `SaveData` = plain `[Serializable]` class. Do not mix these up.
- One scene only (`SampleScene.unity`). Do not create additional scenes.

---

## Coding Conventions

- Use fully qualified XRI namespaces — the project linter enforces them:
  - `UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable`
  - `UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor`
- Methods wired to UnityEvent in inspector must be `public` (private = silently fires nothing)
- All easing is hand-coded coroutines. No DOTween, no animation packages.
- Plant growth stage transitions = `GameObject.SetActive()`. No Animator controllers.
- Singleton pattern: `public static T Instance { get; private set; }` set in `Awake()`

---

## Known Stubs to Remove Later

- `_activePotCount = 2` in `EconomyManager.Start()` — debug line, remove when PotManager is wired (Prompt 6)
- Coin particle position `(0, 1.2, 1.5)` in `EconomyManager.Tick()` — replace with real pot anchor positions (Prompt 6)
- Debug log in `FeedbackManager.TriggerCoinParticles()` — remove after particles confirmed working

---

## Rubric — The Four Things That Get Graded

| # | Feature | Exact Call |
|---|---|---|
| 1 | Particles for Ramping Resources | `EconomyManager.Tick()` → `FeedbackManager.TriggerCoinParticles()` |
| 2 | Eases for Planting Generators | `PotSlot.PlantSeed()` → `FeedbackManager.EaseScale()` |
| 3 | Haptics for Purchasing Power-ups | `UpgradeManager.PurchaseUpgrade()` → `FeedbackManager.TriggerHaptic()` |
| 4 | Sound for Unlockable UI | `UnlockManager.ConfirmUnlock()` → `FeedbackManager.PlaySpatialSound()` |

Do not move these calls to different methods or different trigger points.

---

## Script Execution Order

FeedbackManager: -50 → EconomyManager: 0 → ResourceDisplay: +100
