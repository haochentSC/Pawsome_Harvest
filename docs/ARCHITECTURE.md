# Architecture Guide

This document explains how the codebase is structured and why. Read this before writing any new scripts.

---

## The Big Picture

The game uses a **Manager + Slot** architecture. A small set of singleton manager scripts own all the game state and logic. Per-object scripts (like PotSlot, PetSlot, LotteryMachine) are lightweight and mostly just report back to managers.

The project has **three interconnected systems** sharing a central economy:
1. **Planting** — plant crops, grow, harvest, earn money
2. **Pet Care** — feed and bond with a bunny, vital signs decay over time
3. **Lottery** — spend money for random rewards that cross into all systems

```
                         ┌──────────────────┐
                         │   GameManager    │  (init order, app lifecycle)
                         └────────┬─────────┘
                                  │ owns refs to all managers
       ┌──────────────┬───────────┼───────────┬──────────────┐
       │              │           │           │              │
┌──────▼──────┐ ┌─────▼─────┐ ┌──▼───────┐ ┌─▼──────────┐ ┌▼─────────────┐
│  Economy    │ │   Pot     │ │ Upgrade  │ │  PetCare   │ │   Lottery    │
│  Manager    │ │  Manager  │ │ Manager  │ │  Manager   │ │   Manager    │
└──────┬──────┘ └─────┬─────┘ └──────────┘ └─────┬──────┘ └──────┬───────┘
       │              │                           │               │
       │        ┌─────▼─────┐              ┌──────▼──────┐ ┌──────▼───────┐
       │        │  PotSlot  │ (6 pots)     │  PetSlot   │ │LotteryMachine│
       │        └───────────┘              │  (bunny)   │ │(scene object)│
       │                                   └────────────┘ └──────────────┘
       │
┌──────▼──────┐
│  Feedback   │  (particles, haptics, audio, eases — all 3 systems call in)
│  Manager    │
└─────────────┘
```

### Cross-System Economy Flow

Money is the universal currency. Each subsystem owns its own domain resources internally but transacts through `EconomyManager.SpendMoney()` / `AddMoney()`:

```
Planting ──(harvest earnings)──▶ EconomyManager ◀──(ticket cost)── Lottery
    ▲                                │                                │
    │                          SpendMoney()                    reward seeds,
    │                          AddMoney()                      pet food, boosts
    │                                │                                │
    └───(buy food)─── PetCareManager ◀──────(food drops)──────────────┘
                           │
                    GetHarvestBonus() ──▶ PotSlot.Harvest() multiplier
```

---

## Manager Scripts

All managers live on a single `Managers` GameObject in the scene. They are all singletons.

| Script | Owns | Key Responsibility | System |
|---|---|---|---|
| `GameManager` | References to all other managers | Initialization order, save/load on app lifecycle | Shared |
| `EconomyManager` | Money, fertilizer, rates | 1-second Euler tick, events for UI and particles | Shared |
| `PotManager` | Array of 6 PotSlots | Tracks how many pots are planted, feeds count to EconomyManager | Planting |
| `UpgradeManager` | Upgrade levels and multipliers | Cost tables, feeds multipliers to EconomyManager | Planting |
| `UnlockManager` | Fertilizer station unlock gate | Watches money, reveals station at 500 | Planting |
| `PetCareManager` | Hunger, stress, bonding, food inventory | 1-second Euler tick for vital decay, bonding gain, harvest bonus | Pet Care |
| `LotteryManager` | Spin state, collectibles, temporary boosts | Weighted random rewards, ticket cost escalation, cross-system reward dispatch | Lottery |
| `FeedbackManager` | Zero game state | Pure output only: particles, haptics, audio, easing | Shared |
| `SaveManager` | JSON save file | Read/write to Application.persistentDataPath (all 3 systems) | Shared |
| `TutorialManager` | Tutorial queue | Shows/hides popup canvas on first-time events | Shared |
| `TrophyManager` | Trophy unlock state | Watches money milestones, reveals trophies | Shared |

---

## Per-Object Scripts

These attach to specific GameObjects in the scene (often on prefabs):

| Script | Attaches To | Responsibility | System |
|---|---|---|---|
| `PotSlot` | Each of the 6 pots | Growth state machine, visual stage swap, calls managers | Planting |
| `XRSimpleButton` | Any interactable button | Fires UnityEvent on XR press, cooldown, scale pop | Shared |
| `SeedShop` | Shop panel | Handles seed selection and purchase | Planting |
| `UpgradeStation` | Upgrade panel | Wires upgrade buttons to UpgradeManager | Planting |
| `FertilizerStation` | Fertilizer station object | Locked/unlocked visual state | Planting |
| `PetSlot` | Bunny GameObject | XR buttons for Feed, Pet, Check Vitals; drives visual state | Pet Care |
| `PetFoodStation` | Food shop shelf | Buy food with money (XRSimpleButton → SpendMoney) | Pet Care |
| `LotteryMachine` | Lottery machine object | Spin button, reward animation, result display | Lottery |
| `CollectibleSlot` | Each collectible shelf position | Reveals collectible on lottery win | Lottery |
| `ResourceDisplay` | HUD canvas | Updates money/fert/generator text labels | Shared |
| `PetVitalsDisplay` | World-space canvas near bunny | Shows hunger/stress/bonding bars | Pet Care |
| `CollectibleDisplay` | World-space shelf | Shows collected items from lottery | Lottery |
| `TrophySlot` | Each trophy shelf position | Reveals trophy on milestone | Shared |
| `EyeAnimator` | Scarecrow | Random blink, slow look-at-player | Planting |
| `PauseMenu` | Pause canvas | Restart and Quit buttons | Shared |

---

## Data Classes

| Script | Type | Notes | System |
|---|---|---|---|
| `CropData` | ScriptableObject | Crop stats — one asset per crop type | Planting |
| `PetData` | ScriptableObject | Bunny stats — hungerDecayRate, stressGainRate, bondingGainPerPet, feedRestore, feedCost | Pet Care |
| `LotteryRewardData` | ScriptableObject | Reward definition — name, type, value, rarity weight, icon | Lottery |
| `SaveData` | Plain `[Serializable]` class | JSON-friendly container with sections for all 3 systems | Shared |
| `Enums.cs` | Static file | `PotState`, `UpgradeType`, `PetMood`, `PetAction`, `LotteryRewardType`, `Rarity` | Shared |

---

## The Resource Model

```
Money Rate = baseRate(0.1) × activePlantedPots × soilMultiplier × lightBonus

where:
  soilMultiplier  = 1.0 / 1.5 / 2.25 / 3.375  (SoilQuality upgrade levels 0-3)
  lightBonus      = 1.0 / 1.2 / 1.4 / 1.6      (GrowLights upgrade levels 0-3)
  activePlantedPots = pots currently in Growing or Mature state

money += moneyRate × 1.0  (every 1 second, dt = 1s)
```

Fertilizer uses the same pattern but only runs after the Fertilizer Station is unlocked.

---

## The Pet Vital Signs Model

PetCareManager ticks every 1 second using the same Euler integration pattern:

```
hunger  -= hungerDecayRate(2.0) × dt    (clamp 0-100, 100 = full)
stress  += stressGainRate(1.0) × dt     (clamp 0-100, only when hunger < 30)
bonding += bondingPassiveRate × dt       (only when hunger > 50 AND stress < 30)

Feed():  hunger += 25 (costs 1 food item)
Pet():   stress -= 15, bonding += 5 (3s cooldown)
```

**Cross-system bonus:** `GetHarvestBonus()` returns `1.0 + (bonding * 0.01)`, max 2.0. This multiplies `PotSlot.Harvest()` value — a well-bonded bunny doubles farming income.

**Mood states:** Happy (hunger>70, stress<20), Content (hunger>40, stress<50), Hungry (hunger<30), Stressed (stress>60), Critical (hunger<10 OR stress>80)

---

## The Lottery Reward Model

```
Ticket cost = 25 + 5 × floor(totalSpins / 10)    (cap at 100)

Reward table (weighted random):
  40% Common:     20-50 money, OR 1 seed packet, OR 2 pet food
  30% Uncommon:   75-150 money, OR 3 seed packets, OR 5 pet food
  20% Rare:       200-400 money, OR 2× harvest boost (60s), OR 2× grow speed (60s)
  10% Legendary:  collectible item + 500 money
```

**Cross-system rewards:**
- Seed packets → auto-plant a random empty pot via `PotSlot.PlantSeed()`
- Pet food → adds to `PetCareManager` food inventory
- Harvest boost → temporary multiplier read by `PotSlot.Harvest()`
- Grow speed boost → temporary multiplier read by `PotSlot.GrowRoutine()`

---

## The Pot State Machine

Each PotSlot cycles through these states:

```
Empty ──(PlantSeed)──▶ Seeded ──(1 frame)──▶ Growing ──(growTime)──▶ Mature ──(Harvest)──▶ Empty
  ▲                                                                                              │
  └──────────────────────────────────────────────────────────────────────────────────────────────┘
```

- **Empty:** shows "Plant Here" button, no contribution to rate
- **Seeded:** transitional for 1 frame (triggers ease animation)
- **Growing:** contributes to rate, shows growth progress ring, shows "Tap" clicker button
- **Mature:** contributes to rate, shows "Harvest" button
- Back to **Empty** after harvest

---

## Important Distinctions

### EaseScale vs ScalePop
These are two different methods in FeedbackManager and they are NOT interchangeable:

- **`EaseScale(transform, curve, duration)`** — for spawning objects. Reads the object's current scale as the target, sets it to zero, then animates it back up. Use this when a new object appears (seed planting, trophy reveal).
- **`ScalePop(transform, peakMultiplier, duration)`** — for button press feedback. Pulses the object briefly larger then returns it to its original scale. Use this on button visuals.

Calling EaseScale on a button will reset it to (1,1,1) scale. Calling ScalePop on a freshly spawned object will just twitch it. Use the right tool.

### XRSimpleButton vs XRSimpleInteractable
`XRSimpleInteractable` is the Unity XRI component that detects controller interaction. `XRSimpleButton` is our custom script that wraps it with cooldown, sound, and scale pop behavior. Always put both on the same GameObject (XRSimpleButton requires XRSimpleInteractable via RequireComponent).

---

## What Is Out of Scope

These have been explicitly cut to keep the project manageable:

- Physics-based grab or throw mechanics
- More than one room or scene
- Crop death, disease, or watering simulation
- Multiplayer
- Prestige / reset systems
- Complex crafting
- Custom shaders
- DOTween or other animation libraries (all easing is hand-coded coroutines)
- Animator controllers for plant growth (uses direct GameObject.SetActive instead)

---

## File Structure

```
Assets/
  Scripts/
    Managers/         ← All singleton managers (EconomyManager, PotManager, PetCareManager, LotteryManager, etc.)
    Interactables/    ← Per-object scripts (PotSlot, PetSlot, LotteryMachine, XRSimpleButton, etc.)
    UI/               ← HUD, popup, and display scripts (ResourceDisplay, PetVitalsDisplay, CollectibleDisplay)
    Data/             ← CropData.cs, PetData.cs, LotteryRewardData.cs, SaveData.cs, Enums.cs
  Prefabs/
    Interactables/    ← PotSlot.prefab, XRButton.prefab, panels
    Plants/           ← Stage models per crop (Stage0-3 per crop type)
    Pet/              ← Bunny model, food item visuals, bed/cushion
    Lottery/          ← LotteryMachine model, collectible item models
    UI/               ← ResourceHUD.prefab, TutorialPopup.prefab, PetVitalsHUD.prefab
    Effects/          ← CoinBurst.prefab, FertBurst.prefab, HeartBurst.prefab, StarBurst.prefab
  Materials/
  Sounds/
  ScriptableObjects/
    Crops/            ← Tomato.asset, Herb.asset, Flower.asset
    Pet/              ← DefaultBunny.asset
    Lottery/          ← Reward_SmallMoney.asset, Reward_SeedPacket.asset, Reward_Collectible_01..10.asset, etc.
  Scenes/
    SampleScene.unity ← The only scene
  Settings/           ← URP render pipeline assets (don't touch)

docs/                 ← Project documentation
```
