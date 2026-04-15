# Implementation Plan

This is the complete step-by-step build guide for all three systems.

**Track 1 (Planting, Prompts 1-12):** Work through in order — each step builds on the previous.
**Track 2 (Pet Care, Prompts 13-16):** Can start after Prompt 4 is done (needs EconomyManager + FeedbackManager).
**Track 3 (Lottery, Prompts 17-20):** Can start after Prompt 4 is done (needs EconomyManager + FeedbackManager).
**Integration (Prompts 21-22):** After all three tracks are functional.

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
PetSlot (bunny)       (2.0, 0.5, -1.0)      Home area — on a cushion/bed
PetFoodStation        (2.5, 1.0, -1.0)      Home area — food shop shelf
PetVitalsDisplay      (2.0, 1.5, -1.0)      World-space canvas above bunny
LotteryMachine        (-2.0, 0.8, 0.5)      Lottery area — the machine
CollectibleDisplay    (-3.0, 1.2, 0.5)      Lottery area — shelf for collected items
```

---

## Formulas Quick Reference

```
Money rate:      0.1 × activePots × soilMultiplier × lightBonus  (coins/sec)
Fert rate:       0.05 × activePots                               (fert/sec, after unlock)
Grow time:       cropBaseTime / irrigationSpeedMultiplier
Harvest value:   cropBaseValue × soilMultiplier × lightBonus × petHarvestBonus × lotteryHarvestBoost
Seed cost:       round(10 × 1.15^seedsBought)                    (exponential)
Pot costs:       25 / 50 / 100 / 200                             (fixed tiers, pots 3-6)
Unlock cost:     500 money (one-time)
Hunger decay:    2.0 per second (Euler integrated)
Stress gain:     1.0 per second (only when hunger < 30)
Feed restore:    +25 hunger (costs 1 food)
Pet action:      -15 stress, +5 bonding (3s cooldown)
Harvest bonus:   1.0 + (bonding × 0.01), max 2.0
Ticket cost:     25 + 5 × floor(totalSpins / 10), cap 100
```

---

## Track 2: Pet Care System (Prompts 13-16)

> **Owner: Pet Care Teammate**
> Depends on: EconomyManager + FeedbackManager (Prompts 2-3 done)

### 🔲 Prompt 13 — PetData + PetCareManager

**What this builds:** The pet vital signs system. Hunger and stress decay over time via Euler integration. Bonding grows passively when the bunny is well-cared-for.

**Scripts to write:**
- `Assets/Scripts/Data/PetData.cs` — ScriptableObject defining bunny stats
- `Assets/Scripts/Managers/PetCareManager.cs` — singleton manager for pet state

**PetData fields:**
```
string petName
float hungerDecayRate        (default: 2.0/s)
float stressGainRate         (default: 1.0/s, only when hunger < 30)
float bondingPassiveRate     (default: 0.5/s, only when hunger > 50 AND stress < 30)
float feedHungerRestore      (default: 25)
float petStressReduce        (default: 15)
float petBondingGain         (default: 5)
float petCooldown            (default: 3s)
```

**PetCareManager design:**
- Singleton on Managers GameObject
- Tick every 1s (same coroutine pattern as EconomyManager)
- State: hunger (0-100, starts at 80), stress (0-100, starts at 10), bonding (0-100, starts at 0), foodInventory (int, starts at 5)
- Events: `OnHungerChanged(float)`, `OnStressChanged(float)`, `OnBondingChanged(float)`, `OnMoodChanged(PetMood)`
- API: `Feed()`, `Pet()`, `GetMood()`, `GetHarvestBonus()`, `AddFood(int)`, `RestoreState()`, `GetSaveState()`
- `GetHarvestBonus()` returns `1.0 + (bonding * 0.01)`, max 2.0

**Add to Enums.cs:**
```csharp
public enum PetMood { Happy, Content, Hungry, Stressed, Critical }
public enum PetAction { Feed, Pet, CheckVitals }
```

**After writing scripts — in Unity editor:**
1. Create `Assets/ScriptableObjects/Pet/DefaultBunny.asset`
2. Add PetCareManager to Managers GameObject
3. Test: enter play mode, watch hunger decay, verify events fire

---

### 🔲 Prompt 14 — PetSlot + PetFoodStation

**What this builds:** The bunny interaction point and the food shop.

**Scripts to write:**
- `Assets/Scripts/Interactables/PetSlot.cs` — bunny XR interactions
- `Assets/Scripts/Interactables/PetFoodStation.cs` — buy food with money

**PetSlot design:**
- Attaches to the bunny root GameObject
- Has 3 XRSimpleButton children: ButtonFeed, ButtonPet, ButtonCheckVitals
- `OnFeedPressed()`: checks foodInventory > 0 and hunger < 100, calls `PetCareManager.Feed()`, triggers heart particles + haptic + munch sound via FeedbackManager
- `OnPetPressed()`: calls `PetCareManager.Pet()`, triggers heart particles + purr sound + gentle haptic
- `OnCheckVitalsPressed()`: toggles PetVitalsDisplay visibility
- `RefreshButtons()`: show/hide buttons based on state (Feed only when food > 0)

**PetSlot prefab hierarchy:**
```
PetSlot (root — has PetSlot script, BoxCollider)
  BunnyModel           (3D bunny mesh)
  BunnyCushion         (bed/cushion mesh)
  ParticleAnchor       (empty Transform — heart particles spawn here)
  ButtonFeed           (XRButton prefab instance, "Feed")
  ButtonPet            (XRButton prefab instance, "Pet")
  ButtonCheckVitals    (XRButton prefab instance, "Check Vitals")
  SpatialAudioSource   (AudioSource, spatialBlend = 1.0)
```

**PetFoodStation design:**
- `OnBuyCarrotPressed()`: `EconomyManager.SpendMoney(10)`, `PetCareManager.AddFood(1)`
- `OnBuyMedicinePressed()`: `EconomyManager.SpendMoney(25)`, `PetCareManager.AddFood(3)`
- Shows cost text, disables buttons when can't afford

**Scene placement:**
- PetSlot at `(2.0, 0.5, -1.0)`
- PetFoodStation at `(2.5, 1.0, -1.0)`

---

### 🔲 Prompt 15 — PetVitalsDisplay + Bunny Feedback

**What this builds:** Visual feedback for the bunny's state and all pet-related juicy feedback.

**Scripts to write:**
- `Assets/Scripts/UI/PetVitalsDisplay.cs` — world-space canvas with bars

**PetVitalsDisplay design:**
- World-space canvas at `(2.0, 1.5, -1.0)`, scale `(0.005, 0.005, 0.005)`
- Three fill bars: Hunger (green), Stress (red), Bonding (pink)
- Subscribes to PetCareManager events, updates bar fill amounts
- Mood icon changes based on `GetMood()` return value
- Starts hidden, toggled by "Check Vitals" button

**FeedbackManager additions (add to existing script):**
- New SerializeField: `heartParticleSystem`, `warningParticleSystem`
- New SerializeField clips: `munchClip`, `purrClip`, `petSadClip`
- New methods: `TriggerHeartParticles(Vector3 pos, int count)`, `TriggerWarningParticles(Vector3 pos)`
- All methods = zero game logic, pure dispatch

**Bunny visual feedback wiring:**
| Trigger | FeedbackManager Call |
|---|---|
| Feed bunny | `TriggerHeartParticles(bunnyPos, 5)` + `TriggerHaptic(controller, 0.5, 0.1)` + `PlaySpatialSound(bunnyAudio, munchClip)` |
| Pet bunny | `TriggerHeartParticles(bunnyPos, 3)` + `TriggerHaptic(controller, 0.3, 0.08)` + `PlaySpatialSound(bunnyAudio, purrClip)` |
| Hunger critical (<10) | `TriggerWarningParticles(bunnyPos)` + `PlaySpatialSound(bunnyAudio, petSadClip)` |
| Bonding milestone (25/50/75/100) | `EaseScale(rewardObject)` + `TriggerCoinParticles(bunnyPos)` |

---

### 🔲 Prompt 16 — Combo Streak + Path Following

**What this builds:** Bonus mechanics for the pet system — streak rewards and bunny idle movement.

**Combo streak (add to PetCareManager):**
- Track consecutive successful feeds without hunger hitting zero
- Streak counter: 0, 1, 2, 3... resets to 0 if hunger reaches 0
- At streak 3+: bonding gain per feed doubles (10 instead of 5)
- At streak 5+: star particles trigger on each feed
- Visual: streak counter text near PetVitalsDisplay

**Path following (add to PetSlot):**
- Define 4-6 waypoints as empty Transforms in the scene around the home area
- Bunny moves between waypoints at walking speed (0.3 m/s) when idle (not being interacted with)
- Stops moving when player presses any button on PetSlot
- Resumes movement 5 seconds after last interaction
- Uses simple lerp between waypoints, no NavMesh needed

---

## Track 3: Lottery System (Prompts 17-20)

> **Owner: Lottery Teammate**
> Depends on: EconomyManager + FeedbackManager (Prompts 2-3 done)

### 🔲 Prompt 17 — LotteryRewardData + LotteryManager

**What this builds:** The lottery reward system core — reward definitions, weighted random selection, collectible tracking.

**Scripts to write:**
- `Assets/Scripts/Data/LotteryRewardData.cs` — ScriptableObject per reward
- `Assets/Scripts/Managers/LotteryManager.cs` — singleton manager for lottery state

**LotteryRewardData fields:**
```
string rewardName
LotteryRewardType rewardType    (Money, Seeds, PetFood, Fertilizer, HarvestBoost, GrowSpeedBoost, Collectible)
Rarity rarity                    (Common, Uncommon, Rare, Legendary)
float weight                     (probability weight within rarity tier)
int value                        (amount: money amount, food count, etc.)
float boostDuration              (seconds, for temporary boosts)
float boostMultiplier            (multiplier value for boosts)
Sprite icon
GameObject rewardPrefab          (visual object that flies out of machine)
int collectibleId                (unique ID, only for Collectible type, -1 otherwise)
```

**Add to Enums.cs:**
```csharp
public enum LotteryRewardType { Money, Seeds, PetFood, Fertilizer, HarvestBoost, GrowSpeedBoost, Collectible }
public enum Rarity { Common, Uncommon, Rare, Legendary }
```

**LotteryManager design:**
- Singleton on Managers GameObject
- State: totalSpins, collectiblesOwned (bool[10]), activeHarvestBoost (timer + multiplier), activeGrowBoost (timer + multiplier)
- `LotteryRewardData[] rewardTable` assigned in inspector
- `Spin()`: check money >= ticket cost, spend money, roll weighted random, apply reward, fire events
- `GetTicketCost()`: `25 + 5 * (totalSpins / 10)`, capped at 100
- `GetHarvestBoostMultiplier()`: returns active boost multiplier or 1.0
- `GetGrowBoostMultiplier()`: returns active boost multiplier or 1.0
- Events: `OnSpinStarted`, `OnSpinResult(LotteryRewardData)`, `OnCollectibleUnlocked(int id)`
- `RestoreState()`, `GetSaveState()`

**Reward application by type:**
- Money: `EconomyManager.AddMoney(value)`
- Seeds: find random empty PotSlot, call `PlantSeed()` with random CropData
- PetFood: `PetCareManager.Instance?.AddFood(value)`
- Fertilizer: `EconomyManager.AddFertilizer(value)`
- HarvestBoost: set activeHarvestBoost timer = boostDuration, multiplier = boostMultiplier
- GrowSpeedBoost: set activeGrowBoost timer = boostDuration, multiplier = boostMultiplier
- Collectible: set collectiblesOwned[collectibleId] = true, fire OnCollectibleUnlocked

**After writing scripts — in Unity editor:**
1. Create reward assets in `Assets/ScriptableObjects/Lottery/`
2. Add LotteryManager to Managers GameObject
3. Assign reward table array
4. Test: verify weighted random produces expected distribution

---

### 🔲 Prompt 18 — LotteryMachine + Spin Interaction

**What this builds:** The physical lottery machine in the scene — button press, spin animation, reward display.

**Scripts to write:**
- `Assets/Scripts/Interactables/LotteryMachine.cs` — machine interaction script

**LotteryMachine prefab hierarchy:**
```
LotteryMachine (root — has LotteryMachine script)
  MachineModel         (3D lottery machine mesh)
  RewardOutput         (empty Transform — reward objects spawn here)
  ParticleAnchor       (empty Transform — star particles spawn here)
  ButtonSpin           (XRButton prefab, "Spin! ($X)")
  CostDisplay          (World-space canvas showing current ticket cost)
  ResultDisplay        (World-space canvas showing last result, hidden by default)
  SpatialAudioSource   (AudioSource, spatialBlend = 1.0)
```

**LotteryMachine design:**
- `OnSpinPressed()`:
  1. Disable spin button (prevent double-spin)
  2. `FeedbackManager.TriggerHaptic(controller, 0.6, 0.2)` — lever pull feel
  3. `FeedbackManager.PlaySpatialSound(machineAudio, spinClip)` — spinning sound
  4. Start spin animation coroutine (shake machine model for 2s)
  5. Call `LotteryManager.Spin()` to get result
  6. Spawn reward prefab at RewardOutput with Rigidbody (Copier requirement)
  7. Play result feedback based on rarity (see feedback table below)
  8. Show result on ResultDisplay for 3s
  9. Re-enable spin button
- Update CostDisplay text from `LotteryManager.GetTicketCost()` on money change
- Disable button when can't afford

**Feedback by rarity:**
| Rarity | Particles | Sound | Haptic |
|---|---|---|---|
| Common | Small star burst (3 particles) | cha-ching | (0.3, 0.1) |
| Uncommon | Medium star burst (8 particles) | cha-ching | (0.5, 0.15) |
| Rare | Large star burst (15 particles) + coin particles | trumpet fanfare | (0.7, 0.2) |
| Legendary | Massive star burst (25 particles) + coin particles | extended fanfare | (1.0, 0.3) |

**Scene placement:** LotteryMachine at `(-2.0, 0.8, 0.5)`

---

### 🔲 Prompt 19 — CollectibleDisplay + Collectible Shelf

**What this builds:** The shelf that shows collected items from lottery wins.

**Scripts to write:**
- `Assets/Scripts/UI/CollectibleDisplay.cs` — shelf tracker
- `Assets/Scripts/Interactables/CollectibleSlot.cs` — individual slot on shelf

**CollectibleDisplay design:**
- Holds array of CollectibleSlot references (10 slots)
- Subscribes to `LotteryManager.OnCollectibleUnlocked(int id)`
- On new collectible: `FeedbackManager.EaseScale(slot.transform, 0.5)` — item scales from 0 to natural size
- Shows "X/10 Collected" text
- Each slot has a silhouette mesh (greyed out) that becomes full-color when owned

**CollectibleSlot design:**
- `int collectibleId`
- `GameObject silhouetteMesh` (greyed, shown when locked)
- `GameObject revealedMesh` (full color, shown when unlocked)
- `XRSimpleButton inspectButton` — shows info popup with collectible name/description

**Scene placement:** CollectibleDisplay at `(-3.0, 1.2, 0.5)`

---

### 🔲 Prompt 20 — Secrets + Ramping Ticket Cost

**What this builds:** Hidden Easter egg and the escalating lottery cost.

**Secret interaction:**
- Hidden button on the back/underside of the lottery machine (not visible from normal play angle)
- Pressing it 3 times in sequence within 5 seconds triggers the secret
- Unlocks a special 11th collectible not in the normal reward table
- Triggers: massive star particles + unique chime sound + EaseScale on new collectible slot
- `LotteryManager` tracks `secretFound` boolean (saved in SaveData)

**Ramping cost display:**
- LotteryMachine.CostDisplay updates whenever totalSpins changes
- Shows: "Spin: $X" with the escalating cost
- Color changes: green (< $50), yellow ($50-$75), red ($75+)
- Satisfies Ramping Difficulty requirement — visible element marks increased distance to next spin

---

## Integration Phase (Prompts 21-22)

> **Owner: All teammates together**
> Depends on: All three tracks functional

### 🔲 Prompt 21 — Cross-System Wiring + Shared Save

**What this builds:** The connections between all three systems and persistence.

**Cross-system wiring:**
1. **PotSlot.Harvest()** — multiply harvest value by:
   - `PetCareManager.Instance?.GetHarvestBonus() ?? 1f` (pet bonding bonus)
   - `LotteryManager.Instance?.GetHarvestBoostMultiplier() ?? 1f` (lottery temporary boost)
2. **PotSlot.GrowRoutine()** — divide grow time by:
   - `LotteryManager.Instance?.GetGrowBoostMultiplier() ?? 1f` (lottery temporary boost)
3. **Lottery seed reward** — find random empty PotSlot, call PlantSeed with random CropData
4. **Lottery food reward** — call `PetCareManager.Instance?.AddFood(amount)`
5. **Bonding token bonus** — at bonding 50+, PetCareManager fires event, LotteryManager gives 1 free spin

**SaveData class** (`Assets/Scripts/Data/SaveData.cs`):
```csharp
[System.Serializable]
public class SaveData
{
    // Economy
    public float money;
    public float fertilizer;
    public bool fertilizerUnlocked;

    // Planting
    public PotSaveData[] potStates;
    public int soilLevel, lightsLevel, irrigationLevel;
    public int seedsBought;

    // Pet Care
    public float petHunger, petStress, petBonding;
    public int petFoodInventory;
    public int feedStreak;

    // Lottery
    public int totalSpins;
    public bool[] collectiblesOwned;
    public bool secretFound;

    // Trophies
    public bool[] trophiesEarned;

    // Meta
    public string lastSaveTimestamp;
}
```

**SaveManager** (`Assets/Scripts/Managers/SaveManager.cs`):
- Singleton on Managers GameObject
- `Save()`: collect state from all managers, serialize to JSON, write to `Application.persistentDataPath/save.json`
- `Load()`: read JSON, deserialize, call `RestoreState()` on all managers
- Save triggers: `OnApplicationPause(true)`, `OnApplicationQuit()`, auto every 60 seconds
- Offline progress: `elapsed = now - lastSaveTimestamp`, earnings = rate x elapsed x 0.5 (cap 8 hours)

---

### 🔲 Prompt 22 — PauseMenu + Final Polish

**What this builds:** Restart/quit buttons and final integration testing.

**Scripts to write:**
- `Assets/Scripts/UI/PauseMenu.cs` — restart and quit buttons

**PauseMenu design:**
- World-space canvas, positioned at player's left or on a wall
- Two XRSimpleButtons: "Restart" and "Quit"
- Restart: `SaveManager.DeleteSave()`, reload scene
- Quit: `SaveManager.Save()`, `Application.Quit()`

**Final integration checklist:**
1. Plant a seed → watch it grow → harvest → money increases (planting works)
2. Buy food → feed bunny → hunger restores → bonding grows (pet care works)
3. Spend money → spin lottery → receive reward (lottery works)
4. Verify pet bonding bonus increases harvest value (cross-system)
5. Verify lottery seed reward auto-plants a pot (cross-system)
6. Verify lottery food reward adds to pet food inventory (cross-system)
7. Save → quit → reopen → state restored (persistence works)
8. All 4 rubric call sites still fire correctly
9. All 31 juicy feedback triggers verified
