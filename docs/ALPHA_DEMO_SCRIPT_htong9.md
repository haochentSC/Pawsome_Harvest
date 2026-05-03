# Alpha Demo Recording Script — htong9 (Planting Track Only)

**Read top-to-bottom while recording.** Each section: what to do on camera, what to say out loud, the rubric story it satisfies, and the points it earns. Total run-time ≈ 5 minutes.

**Pre-record checklist:**
- Save file deleted (`%USERPROFILE%\AppData\LocalLow\DefaultCompany\MP3_Juicy_Sim\save.json`).
- `EconomyManager.startingMoney = 50`.
- `SaveManager.welcomeBackPopupPrefab` assigned, exec order = -25.
- Headset on, controllers paired.

---

## Opening (~10 s, 0 pts)

> "Hi, I'm htong9. This is my alpha demo for the planting track of *Pawsome Harvest*. I'll be showing eight features for a total of 18 story points. The other tracks — pet care, pests, lottery — are owned by my teammates and don't appear in this video."

Press **Play**. Camera looks at the bench.

---

## Row 1 — Resource Simulation (2 resources)  →  **2 pts**

**Action:** Point camera at the ResourceHUD. Money ticks visibly. (After Row 4, fertilizer also ticks.)

**Narration:**
> "Money and fertilizer are both Euler-integrated each tick: `money += rate × dt`. Same loop runs for fertilizer once it's unlocked. That's two simulated resources."

**Rubric:** *Resource Simulation (2 resources)* — Progression section.
**Code path:** `EconomyManager.Tick()`.

---

## Row 2 — Particle Emission Rate Tracking  →  **1 pt**

**Action:** Plant pot #1. Coin particles burst above the bench. Plant pot #2 — bursts get denser. Plant pot #3 — denser still.

**Narration:**
> "Each tick, a coin particle burst fires from the active pots, and the burst count is multiplied by the current money rate. So as I plant more pots, the particles visibly thicken — the visual rate matches the simulation rate."

**Rubric:** *Particle Emission Rate Tracking* (Juice section).
**Code path:** `EconomyManager.Tick() → FeedbackManager.TriggerCoinParticles(burstPos, CurrentMoneyRate)` — **rubric criterion 1**.

---

## Row 3 — Leveling-up Stats (3 upgrade tracks)  →  **3 pts**

**Action:** Walk to the right wall. Buy **Soil L1 ($50)** — point at RateText jumping. Buy **Lights L1 ($75)**. Buy **Irrigation L1 ($100)** — next plant grows visibly faster.

**Narration:**
> "Three independent upgrade tracks: Soil and Lights multiply the money rate, Irrigation divides the grow time. Each purchase exchanges a resource — coins — for a stat increase that feeds back into the resource simulation."

**Rubric:** *Leveling-up Stats* (Progression section).
**Code path:** `UpgradeStation.OnPurchasePressed → UpgradeManager.PurchaseUpgrade(type, controller) → EconomyManager.SetSoilMultiplier / SetLightBonus / SetIrrigationMultiplier`.

**Bonus to call out (no extra points, just mention):** the controller buzzes on each purchase — **rubric criterion 3**: `UpgradeManager.PurchaseUpgrade → FeedbackManager.TriggerHaptic(controller, 0.7f, 0.15f)`.

---

## Row 4 — Leveling to New Interactions (Fertilizer Station unlock)  →  **2 pts**

**Action:** Wait until money ≥ $500 (or fast-forward by buying nothing for ~30 s with all 3 pots planted + Soil L1). Turn around. Press the **Unlock ($500)** button on the Fertilizer Station.

**Narration:**
> "Spending 500 coins unlocks a brand-new interactable — the Fertilizer Station — which I couldn't access before. The locked visual swaps for the active station, and the fertilizer panel on the HUD becomes visible."

Listen for the spatial chime behind you.

> "And that chime is spatially positioned on the station — you can hear it coming from behind me."

**Rubric:** *Leveling to New Interactions* (Progression section).
**Code path:** `FertilizerStation.OnUnlockPressed → UnlockManager.ConfirmUnlock(spatialAudioSource, chimeClip) → FeedbackManager.PlaySpatialSound(...)` — **rubric criterion 4**.

---

## Row 5 — Ramping Difficulty  →  **2 pts**

**Action:** Before each Soil purchase in Row 3, briefly point camera at the cost label as it flips: $50 → $150 → $400. Same for Lights ($75 → $200 → $500) and Irrigation ($100 → $250 → $600).

**Narration:**
> "After each level-up, the cost for the next level goes up. There's a visible cost label on each station that updates the moment I purchase, so the player can see the increased distance to the next upgrade."

**Rubric:** *Ramping Difficulty* (Progression section, requires Leveling-up Stats).
**Code path:** `UpgradeStation.RefreshCostLabel ← UpgradeManager.GetCostForNextLevel(type)`.

---

## Row 6 — Juicy Feedback (2 motion eases)  →  **2 pts**

**Action (a):** Plant a seed on a fresh pot. Lean in — the seedling pops in with a slight overshoot then settles.

**Narration:**
> "First ease: the seedling spawns from scale zero with an ease-out-back curve, so it pops in and overshoots before settling. Hand-coded coroutine, no DOTween."

**Action (b):** Point at the Generators counter on the HUD as it ticks up after planting another pot.

> "Second ease: the generator count on the HUD smoothly counts up to its new value instead of snapping. Two distinct script-driven motion eases."

**Rubric:** *Juicy Feedback (2 feedbacks)* (Juice section).
**Code paths:**
- Plant: `PotSlot.PlantSeed() → FeedbackManager.EaseScale(_seedlingInstance.transform, 0.4f)` — **rubric criterion 2**.
- Counter: `PotManager.NotifyPotStateChanged → ResourceDisplay.UpdateGeneratorCount → EaseCounterUI`.

---

## Row 7 — Inter-session Saves  →  **3 pts**

**Action:** Note the current state on screen ("money 480, soil L1, lights L1, 2 pots growing").
Stop play (in the editor — for the headset cut, just take the headset off and quit).
Open File Explorer to `%USERPROFILE%\AppData\LocalLow\DefaultCompany\MP3_Juicy_Sim\` and double-click `save.json` to show its contents on screen.
Re-enter Play. Show that money / fert / upgrades / pot states are restored exactly.

**Narration:**
> "Quitting writes a JSON save file to persistent storage. Here's the file — money, fertilizer, fertilizer-unlocked flag, upgrade levels, every pot's state and grow timer, and a save timestamp. When I reopen the game, every manager's `RestoreState` is called from `SaveManager.Load`, and the scene comes back exactly as I left it."

**Rubric:** *Inter-session Saves* (Pausing section).
**Code path:** `SaveManager.Save()` on `OnApplicationQuit`/`OnApplicationPause(true)`/auto every 60 s; `SaveManager.Load()` on `Start`.

---

## Row 8 — Idle Progress  →  **3 pts**

**Action:** Quit again with at least 3 active pots. Wait one real-world minute (or — if recording in editor — open `save.json` and rewind `lastSaveIsoUtc` by 30 minutes; show the edit on camera so the math is provable). Re-enter Play. The money counter jumps, and a "Welcome back!" popup floats in front of you for a few seconds.

**Narration:**
> "On load, SaveManager computes elapsed time since last save, clamps it to eight hours, and credits 50% of the rate × elapsed as offline earnings. The popup confirms it: plus X coins, away for Y minutes."

**Rubric:** *Idle Progress* (Pausing section, requires Inter-session Saves).
**Code path:** `SaveManager.Load → ApplyOfflineEarnings → EconomyManager.ApplyOfflineProgress(coins)` + `WelcomeBackPopup` instance.

---

## Closing (~10 s, 0 pts)

> "That covers the planting track for alpha. Eight features, 18 story points before the team multiplier. Save file and source are on GitHub. Thanks for watching."

Stop play.

---

# Points Total

| # | Feature shown on screen | Rubric story | Points |
|---|---|---|---|
| 1 | Money + fertilizer Euler ticks | Resource Simulation (2 resources) | **2** |
| 2 | Coin burst scales with rate | Particle Emission Rate Tracking | **1** |
| 3 | Soil / Lights / Irrigation upgrades | Leveling-up Stats | **3** |
| 4 | Fertilizer Station unlock @ $500 | Leveling to New Interactions | **2** |
| 5 | Escalating upgrade costs | Ramping Difficulty | **2** |
| 6 | Plant ease + counter ease | Juicy Feedback (2 eases) | **2** |
| 7 | JSON save/load round trip | Inter-session Saves | **3** |
| 8 | Offline earnings + welcome popup | Idle Progress | **3** |
| **Subtotal (pre-multiplier)** | | | **18** |

## Team multiplier

| Code contributors on GitHub | Multiplier | Final score |
|---|---|---|
| 1 (just me) | × 0.75 | 13.5 |
| 2–3 contributors | × 1.5 | **27** |
| 4–6 contributors | × 2.0 | **36** |
| 7 + contributors | × 2.5 | 45 |

Realistic target with current team size (3–4 active contributors on GitHub): **27–36 final points** for the planting track alone.

## Rubric criterion call-sites also being demonstrated (no extra points — these are the 4 graded must-haves baked into Rows 2, 6, 3, 4)

| # | Criterion | Where it fires in this script |
|---|---|---|
| 1 | Particles for Ramping Resources | Row 2 (every tick) |
| 2 | Eases for Planting Generators | Row 6 (a) (seedling ease on plant) |
| 3 | Haptics for Purchasing Power-ups | Row 3 (each upgrade buy) |
| 4 | Sound for Unlockable UI | Row 4 (chime on Fertilizer Station unlock) |

## What I am NOT claiming in this video

- **Restart / Quit (1 pt + 1 pt)** — owned by hc101.
- **Pest grab-and-burn loop** — teammate's work. I'll note in the video that it exists in the scene but isn't mine.
- **Pet care, lottery** — other teammates / not built yet.

## Recording notes

- Don't move the camera too fast; rubric graders need to read the labels.
- For Row 7, having the file open in Notepad for ~3 s on camera is enough — they don't need to read every field.
- For Row 8, doing the JSON-edit trick beats waiting for real elapsed time, and showing the edit on camera also proves the math.
- Total budget: ~5 min. Don't go over 7 min.
