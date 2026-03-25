# AI Onboarding — Tiny Greenhouse XR

> This file is for AI assistants (Cursor, ChatGPT, Copilot, etc.).
> Load this file into your context window before asking for help on this project.
> For human-readable documentation, see the `docs/` folder.

---

## What This Project Is

A Unity 6 VR idle/clicker sim for Oculus Quest. One greenhouse room. Player plants seeds
into pots, pots passively generate money, player spends money on upgrades and eventually
unlocks a second resource (Fertilizer). All interaction is XR controller button presses.

**Unity:** 6000.3.9f1 LTS | **Render:** URP | **XR:** XRI 3.x + OpenXR | **Platform:** Android / Quest

---

## Current Build State

Prompts 1–4 complete. Currently starting **Prompt 5 (CropData + PotSlot)**.

| Script | Path | Status |
|---|---|---|
| EconomyManager | Assets/Scripts/Managers/EconomyManager.cs | Done |
| FeedbackManager | Assets/Scripts/Managers/FeedbackManager.cs | Done |
| ResourceDisplay | Assets/Scripts/UI/ResourceDisplay.cs | Done |
| XRSimpleButton | Assets/Scripts/Interactables/XRSimpleButton.cs | Done |
| PotSlot | Assets/Scripts/Interactables/PotSlot.cs | **Next** |
| CropData | Assets/Scripts/Data/CropData.cs | **Next** |
| Enums | Assets/Scripts/Data/Enums.cs | **Next** |
| PotManager | Assets/Scripts/Managers/PotManager.cs | Prompt 6 |
| UpgradeManager | Assets/Scripts/Managers/UpgradeManager.cs | Prompt 7 |
| UnlockManager | Assets/Scripts/Managers/UnlockManager.cs | Prompt 8 |
| FertilizerStation | Assets/Scripts/Interactables/FertilizerStation.cs | Prompt 8 |
| SaveManager | Assets/Scripts/Managers/SaveManager.cs | Prompt 10 |
| TutorialManager | Assets/Scripts/Managers/TutorialManager.cs | Prompt 11 |
| TrophySlot | Assets/Scripts/Interactables/TrophySlot.cs | Prompt 12 |
| EyeAnimator | Assets/Scripts/Interactables/EyeAnimator.cs | Prompt 12 |

**Active debug stubs (remove later):**
- `EconomyManager.Start()` has `_activePotCount = 2` — hardcoded for testing, remove when PotManager is wired
- `EconomyManager.Tick()` fires coin particles at `(0, 1.2, 1.5)` placeholder — replace with real pot anchor positions in Prompt 6
- `FeedbackManager.TriggerCoinParticles()` has a `Debug.Log` line — remove after particles confirmed

---

## Architecture — Read Before Writing Code

### Manager + Slot Pattern
All game logic lives in singleton manager scripts on a single `Managers` GameObject.
Per-object scripts (like PotSlot) are lightweight — they report state changes to managers.

```
GameManager
  ├── EconomyManager   owns: money, fertilizer, Euler tick, rate calculation
  ├── PotManager       owns: PotSlot[6] array, active count
  ├── UpgradeManager   owns: upgrade levels, multipliers, cost tables
  ├── UnlockManager    owns: fertilizer station unlock gate
  ├── FeedbackManager  owns: NOTHING — pure output (particles, haptics, audio, easing)
  ├── SaveManager      owns: JSON persistence
  └── TutorialManager  owns: popup message queue
```

### Strict Rules
1. `FeedbackManager` has **zero game logic**. Call into it; never move logic into it.
2. `EaseScale(t, curve, dur)` = spawn object from scale 0 → its natural size. For PotSlot seed planting.
3. `ScalePop(t, peak, dur)` = pulse existing scale briefly larger. For button press feedback.
4. These two are **not interchangeable**. EaseScale on a button resets it to (1,1,1). ScalePop on a freshly spawned object just twitches it.
5. All XR interaction = `XRSimpleInteractable` press. No physics grab, no throw.
6. One scene only. Do not add scenes.
7. `CropData` = ScriptableObject. `SaveData` = plain `[Serializable]` class (no MonoBehaviour).

### XRI Namespace Convention
The project linter expands XRI short names to fully qualified. Write them fully:
```csharp
UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable
UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor
```

### UnityEvent Wiring
Methods wired to UnityEvent in the inspector **must be public**.
Private methods appear to wire but fire nothing at runtime.

---

## Grading Rubric — Do Not Change These Mappings

| Criterion | Method | Notes |
|---|---|---|
| Particles for Ramping Resources | `EconomyManager.Tick()` → `FeedbackManager.TriggerCoinParticles()` | Rate scales burst count |
| Eases for Planting Generators | `PotSlot.PlantSeed()` → `FeedbackManager.EaseScale()` | Seed spawns from 0, counter UI eases |
| Haptics for Purchasing Power-ups | `UpgradeManager.PurchaseUpgrade()` → `FeedbackManager.TriggerHaptic()` | amplitude=0.7, dur=0.15s |
| Sound for Unlockable UI | `UnlockManager.ConfirmUnlock()` → `FeedbackManager.PlaySpatialSound()` | AudioSource ON the station, spatialBlend=1.0 |

---

## Gameplay Formulas

```
Money rate   = 0.1 × activePlantedPots × soilMultiplier × lightBonus   (coins/sec)
Fert rate    = 0.05 × activePlantedPots                                 (fert/sec, post-unlock)
Grow time    = cropData.baseGrowTime / irrigationSpeedMultiplier
Harvest val  = cropData.baseHarvestValue × soilMultiplier × lightBonus
Seed cost    = Round(10 × 1.15^seedsBought)                             (exponential)
Pot costs    = 25 / 50 / 100 / 200                                      (slots 3–6, fixed)
Unlock cost  = 500 money (one-time)

Soil multipliers:       1.0 / 1.5 / 2.25 / 3.375   (level 0–3)
GrowLights multipliers: 1.0 / 1.2 / 1.4  / 1.6     (level 0–3)
Irrigation speed:       1.0 / 1.3 / 1.7  / 2.2     (level 0–3, divides grow time)
```

---

## Crop Values

| Crop | Grow Time | Harvest Value |
|---|---|---|
| Lettuce | 30s | 15 coins |
| Tomato | 60s | 35 coins |
| Strawberry | 45s | 25 coins |

---

## Pot State Machine

```
Empty ──PlantSeed()──▶ Seeded ──1 frame──▶ Growing ──(growTime elapsed)──▶ Mature ──Harvest()──▶ Empty
```

- **Empty:** "Plant Here" button visible, no rate contribution
- **Seeded:** 1 frame, triggers EaseScale on seed model + generator counter ease
- **Growing:** contributes to rate, shows cooldown ring fill, "Tap" clicker button visible
- **Mature:** contributes to rate, "Harvest" button visible

---

## PotSlot Prefab Hierarchy

```
PotSlot (root — PotSlot.cs, BoxCollider)
  PotMesh
  ParticleAnchor      (empty Transform, coin particles fire here)
  Stage_Seed          (child mesh, disabled by default)
  Stage_Sprout        (child mesh, disabled by default)
  Stage_Plant         (child mesh, disabled by default)
  Stage_Fruit         (child mesh, disabled by default)
  ButtonPlant         (XRButton prefab, "Plant Here")
  ButtonHarvest       (XRButton prefab, hidden default)
  ButtonTap           (XRButton prefab, hidden default)
  CooldownCanvas      (World Space Canvas)
    CooldownRing      (Image, Fill Method=Radial360, Fill Amount driven by grow progress)
```

---

## Scene Layout

```
XR Origin         (0, 0, 0)       Player start, facing +Z
Bench             (0, 0.9, 1.5)   6 pot slots on top
PotSlot 0         (-0.75, 1.05, 1.5)   Unlocked at start
PotSlot 1         (-0.45, 1.05, 1.5)   Unlocked at start
PotSlot 2–5       (continuing right)    Buy to unlock
Shop Panel        (-1.8, 1.4, 0)  Left wall
Upgrade Panel     (1.8, 1.4, 0)   Right wall
Fertilizer Stn    (0, 1.0, -1.5)  Behind player, locked until 500 coins
Resource HUD      (0, 1.9, 1.0)   World-space canvas, money + fert display
Trophy Shelf      (-3.5, 1.5, 2.5)
Scarecrow         (3.0, 1.2, 2.5)
```

---

## Known Bugs Already Fixed

| Bug | Was | Fix |
|---|---|---|
| Button scaled to giant cube on press | `EaseScale` hardcoded target to (1,1,1) | Added `ScalePop` for buttons, `EaseScale` now respects original scale |
| Button UnityEvent fired nothing | Method was `private` | Must be `public` for UnityEvent |
| Particles fired at wrong position | Placeholder was above player's feet | Moved to (0, 1.2, 1.5), Prompt 6 replaces with real positions |
| Particles invisible | Simulation Space = Local, size too small | Set Simulation Space = World, Start Size = 0.15 |

---

## What Is Out of Scope — Do Not Add

- Physics grab/throw
- Multiple scenes or rooms
- Multiplayer
- DOTween or animation packages
- Animator controllers for growth (use SetActive instead)
- Prestige or reset systems
- Crop death or disease simulation

---

## Script Execution Order

| Script | Order |
|---|---|
| FeedbackManager | -50 |
| EconomyManager | 0 |
| ResourceDisplay | +100 |

Set in **Edit > Project Settings > Script Execution Order**.
