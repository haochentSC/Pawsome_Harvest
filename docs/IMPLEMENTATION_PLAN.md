# Implementation Plan

This is the complete step-by-step build guide. Work through these in order.
Each step builds on the previous one — skipping ahead usually causes debugging pain.

---

## How to Use This Document

Each "Prompt" is a self-contained chunk of work. When using AI to help code, give it the prompt description under each step and it will have enough context to write the right script. The full project context lives in ARCHITECTURE.md and BUILD_PROGRESS.md.

---

## Current Position

> **You are here: Prompt 5 — CropData + PotSlot**
> Prompts 1–4 are complete. See BUILD_PROGRESS.md for details.

---

## Completed Steps

### ✅ Prompt 1 — XR Setup
Installed XR Interaction Toolkit 3.x + OpenXR. Set up XR Origin with Camera Offset, left/right ActionBasedControllers, Ray Interactors, and XR Device Simulator for editor testing.

Key settings:
- XR Plug-in Management → Android tab → OpenXR enabled
- OpenXR settings → Oculus Touch Controller Profile added
- Input Action Manager in scene with XRI Default Input Actions asset
- Script execution order: FeedbackManager(-50), EconomyManager(0), ResourceDisplay(+100)

### ✅ Prompt 2 — EconomyManager + ResourceDisplay
Money ticks every second. HUD shows money and generator count. Events fire when values change.
- `Assets/Scripts/Managers/EconomyManager.cs`
- `Assets/Scripts/UI/ResourceDisplay.cs`

### ✅ Prompt 3 — FeedbackManager
Central hub for all juicy feedback. No game logic — just dispatches effects.
- `Assets/Scripts/Managers/FeedbackManager.cs`
- Two scale methods: `EaseScale` (spawn from zero) and `ScalePop` (button press)

### ✅ Prompt 4 — XRSimpleButton
Reusable XR button wrapper. Every interactive button in the game uses this.
- `Assets/Scripts/Interactables/XRSimpleButton.cs`
- Build `XRButton.prefab`: root with XRSimpleInteractable + BoxCollider + XRSimpleButton, child Visual mesh

---

## Upcoming Steps

### 🔲 Prompt 5 — CropData + PotSlot

**What this builds:** The core game loop. Plant a seed, watch it grow, harvest it.

**Scripts to write:**
- `Assets/Scripts/Data/Enums.cs` — shared enums
- `Assets/Scripts/Data/CropData.cs` — ScriptableObject
- `Assets/Scripts/Interactables/PotSlot.cs` — state machine per pot

**CropData fields:**
```
string cropName
float baseGrowTime       (Lettuce=30s, Tomato=60s, Strawberry=45s)
float baseHarvestValue   (Lettuce=15, Tomato=35, Strawberry=25)
GameObject[] stageModels (4 elements: seed, sprout, plant, fruit)
Sprite uiIcon
```

**PotSlot state machine:**
```
Empty → Seeded → Growing → Mature → Empty (repeats)
```

**PotSlot key methods:**
- `PlantSeed(CropData crop)` — starts growth, triggers EaseScale on seed model
- `Harvest()` — returns harvest value, resets to Empty
- `TapClicker()` — gives small instant bonus while Growing
- `SetUnlocked(bool)` — shows/hides the entire slot
- `Update()` — advances growth timer, updates cooldown ring fill

**PotSlot child hierarchy to build as prefab:**
```
PotSlot (root — has PotSlot script, BoxCollider)
  PotMesh             (the physical pot visual)
  ParticleAnchor      (empty Transform — coin particles spawn here)
  Stage_Seed          (GameObject, disabled by default)
  Stage_Sprout        (GameObject, disabled by default)
  Stage_Plant         (GameObject, disabled by default)
  Stage_Fruit         (GameObject, disabled by default)
  ButtonPlant         (XRButton prefab instance, "Plant Here")
  ButtonHarvest       (XRButton prefab instance, "Harvest", hidden by default)
  ButtonTap           (XRButton prefab instance, "Tap!", hidden by default)
  CooldownCanvas      (World Space canvas, child of pot)
    CooldownRing      (Image with Fill Method = Radial 360, Fill Amount = 0..1)
```

**After writing scripts — do in Unity editor:**
1. Create assets: Right-click `Assets/ScriptableObjects/Crops/` → Create → Greenhouse → CropData
2. Create `Lettuce.asset`, `Tomato.asset`, `Strawberry.asset` and fill in values
3. Build the PotSlot prefab hierarchy above
4. Wire all SerializeField references in the inspector
5. Place 1 pot in the scene at `(-0.75, 1.05, 1.5)` and test the loop

---

### 🔲 Prompt 6 — PotManager + Rate Integration

**What this builds:** Connects the pots to the economy. Rate actually changes based on planted pots.

**Scripts to write:**
- `Assets/Scripts/Managers/PotManager.cs`

**Key tasks:**
- PotManager holds `PotSlot[6]` array (assign in inspector)
- `GetActivePlantedCount()` counts pots in Growing or Mature state
- Calls `EconomyManager.SetActivePotCount()` when any pot state changes
- Passes real ParticleAnchor world positions to EconomyManager for coin particles
- Remove the debug line `_activePotCount = 2` from EconomyManager.Start()

---

### 🔲 Prompt 7 — UpgradeManager + UpgradeStation + Haptics

**What this builds:** The three upgrade tracks. Buying one vibrates the controller.

**Scripts to write:**
- `Assets/Scripts/Managers/UpgradeManager.cs`
- `Assets/Scripts/Interactables/UpgradeStation.cs`

**Upgrade tracks and costs:**
```
SoilQuality:  L1=50,  L2=150, L3=400  → multiplies money rate
GrowLights:   L1=75,  L2=200, L3=500  → multiplies money rate
Irrigation:   L1=100, L2=250, L3=600  → speeds up grow time
```

**Multiplier tables:**
```
Soil:       1.0 / 1.5 / 2.25 / 3.375
GrowLights: 1.0 / 1.2 / 1.4  / 1.6
Irrigation: 1.0 / 1.3 / 1.7  / 2.2  (divides grow time, not multiplies rate)
```

**Haptic on purchase:** amplitude=0.7, duration=0.15s

---

### 🔲 Prompt 8 — UnlockManager + FertilizerStation + Spatialized Sound

**What this builds:** The fertilizer station unlock — the key rubric item for sound.

**Scripts to write:**
- `Assets/Scripts/Managers/UnlockManager.cs`
- `Assets/Scripts/Interactables/FertilizerStation.cs`

**Key behavior:**
- FertilizerStation starts locked (greyed out, no button)
- UnlockManager watches `EconomyManager.OnMoneyChanged`
- When money ≥ 500: "Unlock (500 coins)" button appears on the station
- Player presses it: spend 500, reveal station, play 3D chime sound
- AudioSource must be ON the FertilizerStation object, `spatialBlend = 1.0`

**Station position in scene:** `(0, 1.0, -1.5)` — behind the player's starting position

---

### 🔲 Prompt 9 — Fertilizer Second Resource

**What this builds:** Fertilizer starts ticking after the station is unlocked.

**Changes needed:**
- Call `EconomyManager.SetFertilizerUnlocked(true)` from UnlockManager
- Fertilizer ticks at: `0.05 × activePots` per second
- FertPanel on ResourceDisplay becomes visible
- FertParticles burst from the station every tick

---

### 🔲 Prompt 10 — SaveManager + Offline Progress

**What this builds:** The game remembers state between sessions.

**Scripts to write:**
- `Assets/Scripts/Data/SaveData.cs` — serializable data container
- `Assets/Scripts/Managers/SaveManager.cs` — read/write JSON

**What gets saved:** money, fertilizer, fertilizer station unlocked, pot states (which crop, how far grown), upgrade levels, seeds bought count, trophies unlocked, timestamp.

**Offline progress:** On load, calculate `elapsed = now - lastSaveTime`. Apply `offlineEarnings = rate × elapsed × 0.5` (50% efficiency cap at 8 hours). Show a "Welcome back! +X coins" popup.

**Save triggers:** `OnApplicationPause(true)` (Quest home button), `OnApplicationQuit()`, auto-save every 60 seconds.

---

### 🔲 Prompt 11 — TutorialManager + Popups

**What this builds:** 5 popup messages triggered on first-time events.

**Scripts to write:**
- `Assets/Scripts/Managers/TutorialManager.cs`
- `Assets/Scripts/UI/TutorialPopup.cs`

**Messages:**
1. "Welcome! Buy seeds from the left panel."
2. "Press 'Plant' on an empty pot to grow a crop."
3. "Pots earn money over time. Harvest when ready!"
4. "Buy upgrades on the right to multiply your earnings."
5. "Spend 500 coins to unlock the Fertilizer Station!"

**Popup prefab:** World-space canvas, dark semi-transparent panel, TMP text (36pt+), "Got it" dismiss button. Position at `(0, 1.7, 1.0)` — above the bench, readable at arm's length in VR.

---

### 🔲 Prompt 12 — Trophy Shelf + Eye Animator

**What this builds:** Milestone achievement trophies and the scarecrow's animated eyes.

**Scripts to write:**
- `Assets/Scripts/Interactables/TrophySlot.cs`
- `Assets/Scripts/Interactables/EyeAnimator.cs`

**Trophy milestones:**
1. Plant your first pot
2. Harvest 5 crops total
3. Unlock the Fertilizer Station
4. Max out one upgrade track

**Trophy reveal:** Scale from 0 to 1 using `FeedbackManager.EaseScale()` + coin particle burst + short fanfare sound.

**Eye animator:** Random blink every 3–8 seconds (swap sprite frame). Slow, lerped look-toward player's XR Origin (update every frame, lerp factor ~0.02 per frame so it's subtle).

---

## Scene Layout Reference

```
Greenhouse Room: 8m wide × 6m deep × 3m tall
Player starts at (0, 0, 0) facing +Z

Object                Position              Notes
XR Origin             (0, 0, 0)             Player start
Main Bench            (0, 0.9, 1.5)         Table surface, 6 pots on top
  PotSlot 0           (-0.75, 1.05, 1.5)    Unlocked from start
  PotSlot 1           (-0.45, 1.05, 1.5)    Unlocked from start
  PotSlot 2-5         (0.15 to 0.75 ...)    Buy to unlock
Shop Panel            (-1.8, 1.4, 0)        Left wall — buy seeds + pots
Upgrade Panel         (1.8, 1.4, 0)         Right wall — buy upgrades
Fertilizer Station    (0, 1.0, -1.5)        Behind player — locked until 500
Resource HUD          (0, 1.9, 1.0)         World-space canvas above bench
Trophy Shelf          (-3.5, 1.5, 2.5)      North wall corner
Scarecrow             (3.0, 1.2, 2.5)       East corner, animated eyes
```

---

## Formulas Quick Reference

```
Money rate:     0.1 × activePots × soilMultiplier × lightBonus  (coins/sec)
Fert rate:      0.05 × activePots                               (fert/sec, after unlock)
Grow time:      cropBaseTime / irrigationSpeedMultiplier
Harvest value:  cropBaseValue × soilMultiplier × lightBonus
Seed cost:      round(10 × 1.15^seedsBought)                    (exponential)
Pot costs:      25 / 50 / 100 / 200                             (fixed tiers, pots 3-6)
Unlock cost:    500 money (one-time)
```
