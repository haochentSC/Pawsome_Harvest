# Build Progress

**Last updated:** 2026-03-24

This document tracks what has been built, what is currently broken or stubbed, and what needs to be done next. Update this file whenever you finish a chunk of work.

---

## Overall Status

| Phase | Status |
|---|---|
| XR Setup (packages, XR Origin) | Done |
| Economy system (money ticking) | Done |
| Resource HUD | Done |
| Feedback system (particles, audio, ease) | Done |
| XR Button (reusable interactable) | Done |
| Pot loop (plant, grow, harvest) | **Next up** |
| Shop + Upgrades | Not started |
| Fertilizer Station unlock | Not started |
| Save / Load | Not started |
| Tutorial popups | Not started |
| Trophy shelf | Not started |
| Scene geometry + polish | Not started |

---

## What Is Done

### XR Setup
- XR Interaction Toolkit 3.x installed with OpenXR
- XR Origin in scene with Camera Offset, left/right ActionBasedControllers, Ray Interactors
- XR Device Simulator set up for editor testing (disable before device builds)
- Input Action Manager wired with XRI Default Input Actions

### EconomyManager (`Assets/Scripts/Managers/EconomyManager.cs`)
- Ticks every 1 second using a coroutine
- Euler integration: `money += rate * dt`
- Rate = `baseRate * activePotCount * soilMultiplier * lightBonus`
- Events: `OnMoneyChanged`, `OnFertilizerChanged`, `OnMoneyTick`
- Public API: `SpendMoney()`, `AddMoney()`, `SetActivePotCount()`, `SetSoilMultiplier()` etc.
- Fertilizer system is coded but disabled until `SetFertilizerUnlocked(true)` is called

**⚠️ Known stub:** `_activePotCount = 2` is hardcoded in `Start()` for testing. This line must be removed when PotManager is wired up (Build Step 6).

### ResourceDisplay (`Assets/Scripts/UI/ResourceDisplay.cs`)
- World-space canvas showing money and generator count
- Subscribes to EconomyManager events
- Generator count label animates with a smooth count-up ease
- Fertilizer panel is hidden until `SetFertilizerVisible(true)` is called

**⚠️ Known stub:** Fertilizer panel stays hidden until Prompt 9 (Fertilizer unlock).

### FeedbackManager (`Assets/Scripts/Managers/FeedbackManager.cs`)
Centralizes all juicy feedback. Other scripts call this — no game logic lives here.
- `TriggerCoinParticles(pos, rate)` — bursts coin particles at a world position
- `TriggerFertParticles(pos, rate)` — bursts fertilizer particles
- `TriggerHaptic(controller, amplitude, duration)` — vibrates controller (logs in editor, fires on device)
- `PlaySpatialSound(audioSource, clip)` — plays 3D audio at a scene location
- `PlayUISound(clip)` — plays 2D UI audio
- `EaseScale(transform, curve, duration)` — spawns object from scale 0 to its natural size
- `ScalePop(transform, peakMultiplier, duration)` — pulses an object briefly larger then back

**⚠️ Important distinction:** `EaseScale` is for spawning objects (seed planting). `ScalePop` is for button presses. They are NOT interchangeable — see ARCHITECTURE.md.

**⚠️ Known stub:** Coin particles fire at placeholder position `(0, 1.2, 1.5)`. Prompt 6 replaces this with each pot's real world position.

### XRSimpleButton (`Assets/Scripts/Interactables/XRSimpleButton.cs`)
Reusable XR button component. Put this on any GameObject with a Collider + XRSimpleInteractable.
- Fires `onPressed` UnityEvent when a controller ray selects it
- 0.3s cooldown to prevent double-fires
- Plays click sound (optional)
- Plays scale pop on the visual mesh (optional)
- `SetEnabled(bool)` — show/hide the button programmatically
- `LastPressingController` — exposes which hand pressed it (used for haptics)

**⚠️ Prefab needed:** Build the `XRButton.prefab` as described in IMPLEMENTATION_PLAN.md Step 4.

---

## Bugs Fixed (History)

These are documented so future teammates don't re-introduce the same issues.

| Bug | Root Cause | Fix Applied |
|---|---|---|
| Coin particles not visible | Placeholder position `(0, 1.1, 0)` was above player's feet, off-camera | Moved to `(0, 1.2, 1.5)` — bench height, in front of player |
| Button resize to giant cube on press | `EaseScale` forced scale to `(1,1,1)`, ignoring button's actual scale | Added separate `ScalePop()` method. XRSimpleButton now calls `ScalePop`, not `EaseScale` |
| Button press didn't fire UnityEvent | `DebugAddMoney()` was `private` — Unity can't wire private methods in inspector | Changed to `public` |
| FeedbackManager calls missing from Tick() | User edited EconomyManager.Start() at same time as a code change, accidentally reverted Tick() | Re-applied manually |
| Particles invisible (simulation space) | ParticleSystem Simulation Space was Local instead of World | Set Simulation Space = World on CoinParticles |

---

## What Comes Next

### Immediate Next Task: Prompt 5 — CropData + PotSlot

This builds the core game loop: plant a seed, watch it grow, harvest it.

**Scripts to write:**
- `Assets/Scripts/Data/Enums.cs` — shared enums (PotState, UpgradeType)
- `Assets/Scripts/Data/CropData.cs` — ScriptableObject defining each crop
- `Assets/Scripts/Interactables/PotSlot.cs` — the main per-pot state machine

**After writing scripts, do this manually in Unity:**
1. Create 3 CropData assets: `Assets/ScriptableObjects/Crops/Lettuce.asset`, `Tomato.asset`, `Strawberry.asset`
2. Build a `PotSlot` prefab (see IMPLEMENTATION_PLAN.md for hierarchy)
3. Place 1 test pot in the scene to verify the loop works

**Crop values to configure:**

| Crop | Grow Time | Harvest Value |
|---|---|---|
| Lettuce | 30 seconds | 15 coins |
| Tomato | 60 seconds | 35 coins |
| Strawberry | 45 seconds | 25 coins |

### After That: Prompt 6 — PotManager

- Connects the pots to EconomyManager's rate calculation
- Removes the `_activePotCount = 2` debug stub
- Routes coin particle positions to real pot anchor locations

### Full Remaining Order

See IMPLEMENTATION_PLAN.md for the full 12-step plan with details.

```
Prompt 5  — CropData + PotSlot          ← NEXT
Prompt 6  — PotManager + rate wiring
Prompt 7  — UpgradeManager + haptics
Prompt 8  — UnlockManager + Fertilizer Station + spatialized sound
Prompt 9  — Fertilizer second resource
Prompt 10 — SaveManager + offline progress
Prompt 11 — TutorialManager + popups
Prompt 12 — Trophy shelf + Eye animator
```

---

## Inspector Wiring Reference

Things that must be set in the Unity inspector (not in code):

| Object | Component | Field | Value |
|---|---|---|---|
| Managers | EconomyManager | Starting Money | 50 |
| Managers | EconomyManager | Tick Interval | 1 |
| Managers | EconomyManager | Base Money Rate Per Pot | 0.1 |
| Managers | FeedbackManager | Coin Particle System | CoinParticles (scene object) |
| Managers | FeedbackManager | Fert Particle System | FertParticles (scene object) |
| Managers | FeedbackManager | UI Audio Source | UIAudio child of Managers |
| CoinParticles | Particle System | Simulation Space | World |
| CoinParticles | Particle System | Start Size | 0.15 |
| CoinParticles | Particle System | Start Color | Gold (255, 200, 0) |
| CoinParticles | Particle System | Play On Awake | OFF |
| ResourceHUD | Canvas | Render Mode | World Space |
| ResourceHUD | Canvas | Scale | (0.005, 0.005, 0.005) |

**Script Execution Order (Edit > Project Settings > Script Execution Order):**
- FeedbackManager: -50
- EconomyManager: 0 (default)
- ResourceDisplay: +100
