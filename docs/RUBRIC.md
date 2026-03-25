# Assignment Rubric Mapping

This document explains exactly how each graded requirement maps to working code.
Keep this in mind whenever making design decisions — don't drift from these mappings.

---

## Required Features

### 1. Particles for Ramping Resources

**What the rubric says:** Two resource counters that are Euler integrated over timesteps to increase. Particles emitted to correspond with resource growth.

**How we satisfy it:**
- `EconomyManager` ticks every 1 second using Euler integration: `money += rate * deltaTime`
- Money AND Fertilizer are both Euler-integrated (fertilizer activates after station unlock)
- On each tick, `FeedbackManager.TriggerCoinParticles(position, rate)` is called
- The particle burst count scales with `rate` — more active pots = higher rate = more particles emitted per tick
- This directly satisfies "particles correspond with resource growth" and "ramping" (more generators = more visual density)

**Exact method call:** `EconomyManager.Tick()` → `FeedbackManager.TriggerCoinParticles()`

**Status:** Coins working (placeholder position). Fertilizer particles pending Prompt 9.

---

### 2. Eases for Planting Generators

**What the rubric says:** Player deploys generators through XR interaction. Generators add to resource rate and cost resources. XR interaction triggers an animation using motion eases on a visual element that tracks the number of generators planted.

**How we satisfy it:**
- Each planted pot IS the generator (active pots drive the money rate)
- Planting costs money (buying a seed first, then planting)
- Planting is done via XR button press on an empty PotSlot
- On plant: `PotSlot.PlantSeed()` calls `FeedbackManager.EaseScale()` on the seed model — it scales from 0 to its natural size with an ease-out-back curve (overshoot and settle)
- The generator counter UI label on the HUD eases its number upward using `FeedbackManager.EaseCounterUI()`
- The counter label ("Generators: N") is the "visual element that tracks the number of generators planted"

**Exact method call:** `PotSlot.PlantSeed()` → `FeedbackManager.EaseScale()` + `ResourceDisplay.UpdateGeneratorCount()` → `FeedbackManager.EaseCounterUI()`

**Status:** Pending Prompt 5 (PotSlot).

---

### 3. Haptics for Purchasing Power-ups

**What the rubric says:** Player exchanges resources for items that multiply generator rate contribution through XR interaction. This triggers haptic feedback.

**How we satisfy it:**
- SoilQuality, GrowLights, and Irrigation upgrades each multiply the money rate
- Each upgrade is purchased via XR button press on the Upgrade Panel
- On purchase: `UpgradeManager.PurchaseUpgrade()` calls `FeedbackManager.TriggerHaptic(controller, 0.7f, 0.15f)`
- The controller is identified from the XR interaction (which hand pressed the button)
- Haptics fire on the pressing hand's controller

**Exact method call:** `UpgradeManager.PurchaseUpgrade()` → `FeedbackManager.TriggerHaptic()`

**Status:** Pending Prompt 7 (UpgradeManager).

---

### 4. Sound for Unlockable UI

**What the rubric says:** Player exchanges a large amount of primary resource to unlock the space for the second resource's display and interaction. This triggers a sound effect spatialized to the location of the unlocked UI.

**How we satisfy it:**
- Spending 500 money unlocks the Fertilizer Station
- The Fertilizer Station IS the second resource's display and interaction area
- On unlock: `UnlockManager.ConfirmUnlock()` calls `FeedbackManager.PlaySpatialSound(fertStation.audioSource, chimeClip)`
- The AudioSource is physically on the FertilizerStation GameObject in the scene, with `spatialBlend = 1.0`
- The player hears the sound coming from the station's location in 3D space

**Exact method call:** `UnlockManager.ConfirmUnlock()` → `FeedbackManager.PlaySpatialSound()`

**Status:** Pending Prompt 8 (UnlockManager).

---

## Optional Features (In Progress)

| Feature | Plan | Priority |
|---|---|---|
| Clicker | Tap a growing pot for a small instant coin bonus | Medium |
| Inter-session Saves | JSON save to device storage, offline progress calculation | High |
| Tutorial Popups | 5 messages triggered by first-time events | Medium |
| Exponential Costs | Seed cost = 10 × 1.15^seedsBought | Done (in formula, pending PotSlot) |
| Cooldown Timers | Visual ring fill on each pot showing grow progress | In PotSlot spec |
| Level-up Reveal | Scale pop + particles on upgrade purchase | Medium |
| Achievement Trophies | TrophySlot reveals model on milestone events | Low |
| Anthropomorphizing Eyes | Scarecrow with random blink + slow look-at-player | Low |

---

## What to Avoid (Will Hurt the Grade)

- Making the fertilizer interaction something other than "unlock via spending primary resource"
- Putting the ease animation somewhere other than the planting interaction
- Using a non-XR interaction for the haptic trigger
- Placing the spatialized audio source anywhere other than the Fertilizer Station object
