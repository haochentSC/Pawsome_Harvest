# Architecture Guide

This document explains how the codebase is structured and why. Read this before writing any new scripts.

---

## The Big Picture

The game uses a **Manager + Slot** architecture. A small set of singleton manager scripts own all the game state and logic. Per-object scripts (like PotSlot) are lightweight and mostly just report back to managers.

```
                    ┌─────────────┐
                    │ GameManager │  (init order, app lifecycle)
                    └──────┬──────┘
                           │ owns refs to all managers
           ┌───────────────┼───────────────┐
           │               │               │
    ┌──────▼──────┐  ┌──────▼──────┐  ┌──────▼──────┐
    │ Economy     │  │  Pot        │  │  Upgrade    │
    │ Manager     │  │  Manager    │  │  Manager    │
    └──────┬──────┘  └──────┬──────┘  └─────────────┘
           │                │
           │         ┌──────▼──────┐
           │         │  PotSlot    │  (6 instances)
           │         │  PotSlot    │
           │         │  PotSlot    │
           │         └─────────────┘
           │
    ┌──────▼──────┐
    │  Feedback   │  (particles, haptics, audio, eases)
    │  Manager    │
    └─────────────┘
```

---

## Manager Scripts

All managers live on a single `Managers` GameObject in the scene. They are all singletons.

| Script | Owns | Key Responsibility |
|---|---|---|
| `GameManager` | References to all other managers | Initialization order, save/load on app lifecycle |
| `EconomyManager` | Money, fertilizer, rates | 1-second Euler tick, events for UI and particles |
| `PotManager` | Array of 6 PotSlots | Tracks how many pots are planted, feeds count to EconomyManager |
| `UpgradeManager` | Upgrade levels and multipliers | Cost tables, feeds multipliers to EconomyManager |
| `UnlockManager` | Fertilizer station unlock gate | Watches money, reveals station at 500 |
| `FeedbackManager` | Zero game state | Pure output only: particles, haptics, audio, easing |
| `SaveManager` | JSON save file | Read/write to Application.persistentDataPath |
| `TutorialManager` | Tutorial queue | Shows/hides popup canvas on first-time events |

---

## Per-Object Scripts

These attach to specific GameObjects in the scene (often on prefabs):

| Script | Attaches To | Responsibility |
|---|---|---|
| `PotSlot` | Each of the 6 pots | Growth state machine, visual stage swap, calls managers |
| `XRSimpleButton` | Any interactable button | Fires UnityEvent on XR press, cooldown, scale pop |
| `SeedShop` | Shop panel | Handles seed selection and purchase |
| `UpgradeStation` | Upgrade panel | Wires upgrade buttons to UpgradeManager |
| `FertilizerStation` | Fertilizer station object | Locked/unlocked visual state |
| `ResourceDisplay` | HUD canvas | Updates money/fert/generator text labels |
| `TrophySlot` | Each trophy shelf position | Reveals trophy on milestone |
| `EyeAnimator` | Scarecrow | Random blink, slow look-at-player |

---

## Data Classes

| Script | Type | Notes |
|---|---|---|
| `CropData` | ScriptableObject | Crop stats — create one asset per crop type in the editor |
| `SaveData` | Plain `[Serializable]` class | No MonoBehaviour, no ScriptableObject — just a JSON-friendly data container |
| `Enums.cs` | Static file | Shared enums: `PotState`, `UpgradeType` |

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
    Managers/         ← All singleton manager scripts
    Interactables/    ← Per-object scripts (PotSlot, XRSimpleButton, etc.)
    UI/               ← HUD and popup scripts
    Data/             ← CropData.cs, SaveData.cs, Enums.cs
  Prefabs/
    Interactables/    ← PotSlot.prefab, XRButton.prefab, panels
    Plants/           ← Stage models per crop (Stage0-3 per crop type)
    UI/               ← ResourceHUD.prefab, TutorialPopup.prefab
    Effects/          ← CoinBurst.prefab, FertBurst.prefab
  Materials/
  Sounds/
  ScriptableObjects/
    Crops/            ← Lettuce.asset, Tomato.asset, Strawberry.asset
  Scenes/
    SampleScene.unity ← The only scene
  Settings/           ← URP render pipeline assets (don't touch)

docs/                 ← This folder (project documentation)
```
