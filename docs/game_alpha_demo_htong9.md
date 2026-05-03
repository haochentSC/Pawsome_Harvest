# Alpha Demo Cheat-Sheet — htong9 (Planting Track)

**Use this file while recording the alpha demo video.** Read top-to-bottom, hit each row, narrate the rubric story name, point at the visible scene element, and move on.

- **Total points (pre-multiplier): 18** — 10 from required mechanics + 8 from alpha features.
- With 2–3 collaborators (×1.5) → **27**. With 4–6 (×2) → **36**. Target ≈ 30 lands inside that range.
- Restart Option (1pt) and Quit Option (1pt) are **hc101's**, not htong9's. Don't claim them.

> **Important:** all 4 graded rubric call-sites are listed in the "Required Rubric Mappings" block at the bottom. Hit those first in the video so the grader can tick them off in the first ~60 seconds.

---

## The Demo Order — 9 rows, ~5 minutes

| # | Pts | Rubric Story | Demo Action (1 sentence) | Scene Object | Script Call | Visible Confirmation |
|---|-----|---|---|---|---|---|
| 1 | **2** | Resource Simulation (2 resources) — money + fertilizer Euler-integrated each tick | Start scene; point at MoneyText incrementing every second | `ResourceHUD/MoneyText`, `ResourceHUD/FertText` | `EconomyManager.Tick()` (`money += rate * dt`; same for fert after unlock) | Money number rises smoothly each second; Fert number rises after row 5 unlock |
| 2 | **1** | Particle Emission Rate Tracking — coin burst scales with rate | Plant 1 pot, watch sparse coin burst above bench. Then plant a 2nd & 3rd; show denser burst | `CoinParticles` (above bench) | `EconomyManager.Tick → FeedbackManager.TriggerCoinParticles(burstPos, CurrentMoneyRate)` | Coin particle count per tick visibly grows with `_activePotCount` |
| 3 | **3** | Leveling-up Stats — 3 upgrade tracks each multiply rate | Buy Soil L1 ($50). Cut to RateText jumping. Repeat for Lights L1 and Irrigation L1 | `UpgradeStation_Soil`, `UpgradeStation_Lights`, `UpgradeStation_Irrigation` | `UpgradeStation.OnPurchasePressed → UpgradeManager.PurchaseUpgrade(type, controller) → EconomyManager.SetSoilMultiplier / SetLightBonus` | RateText goes 0.10 → 0.15 → 0.18 → 0.234; Irrigation visibly speeds up the next plant's grow bar |
| 4 | **2** | Leveling to New Interactions — Fertilizer Station unlock @ $500 | Get to ≥ $500, point at Unlock button appearing on station, press it | `FertilizerStation` (behind player) | `FertilizerStation.OnUnlockPressed → UnlockManager.ConfirmUnlock(spatialAudioSource, chimeClip)` | LockedVisual swaps to ActiveVisual; FertPanel becomes visible; FertText starts ticking |
| 5 | **2** | Ramping Difficulty — escalating upgrade costs are visible | Before each Soil purchase, point camera at the cost label changing $50 → $150 → $400 | `UpgradeStation_Soil/CostLabel` | `UpgradeStation.RefreshCostLabel ← UpgradeManager.GetCostForNextLevel(type)` | Cost label flips on every purchase; matches `SoilCosts = {50, 150, 400}` |
| 6 | **2** | Juicy Feedback — 2 motion eases | (a) Plant a seed, point at seedling popping in with overshoot; (b) point at Generators counter easing up | (a) `PotSlot/PlantAnchor/_seedlingInstance`, (b) `ResourceHUD/GeneratorText` | (a) `PotSlot.PlantSeed → FeedbackManager.EaseScale(seedling, 0.4f)`; (b) `PotManager.NotifyPotStateChanged → ResourceDisplay.UpdateGeneratorCount → EaseCounterUI` | Seedling visibly scales 0 → overshoot → settle; counter text smoothly counts up |
| 7 | **3** | Inter-session Saves — JSON persistence | Stop play with money=X, fert=Y, soil L1, 2 pots growing. Re-enter Play | `Managers/SaveManager` (singleton) | `SaveManager.Save()` on `OnApplicationQuit/OnApplicationPause(true)`; `SaveManager.Load()` on Awake → forwards to each manager's `RestoreState` | Money, fert, upgrade levels, pot states all restored exactly. Open `%appdata%/.../save.json` to show the file |
| 8 | **3** | Idle Progress — offline earnings + welcome popup | Quit with 3 active pots → wait 1 min → reopen | `Managers/SaveManager` + `WelcomeBackPopup` | `SaveManager.Load → ApplyOfflineEarnings(elapsed) → EconomyManager.ApplyOfflineProgress(coins)`; `WelcomeBackPopup.Show("+X coins")` | Money jumps by `rate × elapsed × 0.5`; popup floats in front of player for ~5s |

**Sum: 2 + 1 + 3 + 2 + 2 + 2 + 3 + 3 = 18 pts.**

---

## Required Rubric Mappings (the 4 graded call-sites)

These four are the hardcoded rubric criteria for our project. Demonstrate them clearly in the first minute of the video — the grader is specifically checking each one.

| # | Rubric Criterion | Exact Code Path | Where to Point |
|---|---|---|---|
| 1 | **Particles for Ramping Resources** | `EconomyManager.Tick() → FeedbackManager.TriggerCoinParticles(burstPos, CurrentMoneyRate)` | Coin burst above the bench while pots are growing |
| 2 | **Eases for Planting Generators** | `PotSlot.PlantSeed() → FeedbackManager.EaseScale(_seedlingInstance.transform, 0.4f)` | Seedling overshoot/settle on plant |
| 3 | **Haptics for Purchasing Power-ups** | `UpgradeManager.PurchaseUpgrade(type, controller) → FeedbackManager.TriggerHaptic(controller, 0.7f, 0.15f)` | Controller buzz on Soil/Lights/Irrigation purchase |
| 4 | **Sound for Unlockable UI** | `UnlockManager.ConfirmUnlock(spatialAudioSource, chimeClip) → FeedbackManager.PlaySpatialSound(spatialAudioSource, chimeClip)` | Spatial chime from the FertilizerStation behind the player |

---

## How to Record (one take)

1. **Wipe save first.** Stop Play. Delete `%userprofile%/AppData/LocalLow/<Company>/Pawsome_Harvest/save.json` so the demo starts from a clean state.
2. **Pre-set starting money** in `EconomyManager` inspector → `Starting Money = 50`.
3. **Order in the video matches the table above.** Don't skip; the row index is your script.
4. **For row 7 (Saves):** stop Play after row 6, re-enter Play. Show the in-game state matches what you left.
5. **For row 8 (Idle):** before stopping Play, note the timestamp. Stop Play, wait a real-world minute (or fast-forward by editing `lastSaveIsoUtc` in `save.json` — show that file edit on camera if you go that route, since it proves the offline-earning math).
6. **End on the welcome popup** — clearest single-frame proof of Idle Progress.

## Total scene-time budget

| Row | Budget |
|---|---|
| 1 (Resources) | 20s |
| 2 (Particle rate) | 30s |
| 3 (Three upgrades) | 60s |
| 4 (Fert unlock) | 30s |
| 5 (Ramping costs) | 20s |
| 6 (2 eases) | 30s |
| 7 (Saves) | 45s |
| 8 (Idle) | 45s |
| **Total** | **~5 min** |

## Source-of-truth references

- `docs/Design_Technical_Document.md` — canonical scope.
- `docs/Design_Vision_Documents.md` — high-level pitch.
- `docs/RUBRIC.md` — the 4 graded rubric call-sites.
- `Assets/Scripts/Managers/EconomyManager.cs` — Tick + RestoreState + ApplyOfflineProgress.
- `Assets/Scripts/Managers/SaveManager.cs` — orchestrator (alpha-new file).
- `Assets/Scripts/Data/SaveData.cs` — JSON container (alpha-new file).
