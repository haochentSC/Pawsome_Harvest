# Three-System Integration Plan

This document describes how the Planting, Pet Care, and Lottery systems share a central economy and interact with each other. Read this alongside ARCHITECTURE.md and IMPLEMENTATION_PLAN.md.

---

## Architecture Overview

```
                         ┌──────────────────┐
                         │  EconomyManager  │  (money, fertilizer — universal currency)
                         └────────┬─────────┘
                                  │ SpendMoney() / AddMoney()
              ┌───────────────────┼───────────────────┐
              │                   │                   │
       ┌──────▼──────┐    ┌──────▼──────┐    ┌───────▼───────┐
       │ PotManager  │    │ PetCare     │    │ Lottery       │
       │ + PotSlots  │    │ Manager     │    │ Manager       │
       └──────┬──────┘    └──────┬──────┘    └───────┬───────┘
              │                  │                    │
              │           ┌──────▼──────┐    ┌───────▼───────┐
              │           │  PetSlot    │    │LotteryMachine │
              │           │  (bunny)    │    │(scene object) │
              │           └─────────────┘    └───────────────┘
              │
       ┌──────▼──────┐
       │ FeedbackMgr │  (all 3 systems call into this — zero game logic)
       └─────────────┘
```

---

## Cross-System Data Flow

### Money Flow

```
  Planting                    Shared                    Pet Care / Lottery
  ────────                    ──────                    ──────────────────

  PotSlot.Harvest()     ──▶  AddMoney(harvestValue)
  UpgradeStation        ──▶  SpendMoney(upgradeCost)
  PotSlot.PlantSeed()   ──▶  SpendMoney(seedCost)
                              SpendMoney(foodCost)    ◀── PetFoodStation
                              SpendMoney(ticketCost)  ◀── LotteryMachine.Spin()
                              AddMoney(reward)        ◀── LotteryManager (money reward)
```

### Cross-System Bonuses

| Source | Target | Mechanism |
|---|---|---|
| Pet bonding → Planting | PotSlot.Harvest() multiplies by `PetCareManager.GetHarvestBonus()` | Returns 1.0 + (bonding * 0.01), max 2.0 |
| Lottery → Planting (harvest boost) | PotSlot.Harvest() multiplies by `LotteryManager.GetHarvestBoostMultiplier()` | Temporary 2x for 60s after rare lottery win |
| Lottery → Planting (grow boost) | PotSlot.GrowRoutine() divides time by `LotteryManager.GetGrowBoostMultiplier()` | Temporary 2x for 60s after rare lottery win |
| Lottery → Planting (seeds) | Lottery seed reward calls `PotSlot.PlantSeed()` on random empty pot | Auto-plants without spending money |
| Lottery → Pet Care (food) | Lottery food reward calls `PetCareManager.AddFood(amount)` | Adds to food inventory |
| Pet Care → Lottery (token) | At bonding 50+, PetCareManager fires event, LotteryManager grants 1 free spin | One-time bonus per bonding threshold |
| Planting → Pet Care (money) | Player earns money from farming, spends at PetFoodStation | Indirect: money is the bridge |

### Null-Safe Cross-References

All cross-system reads use null-coalescing so each system works independently:

```csharp
// In PotSlot.Harvest():
float petBonus = PetCareManager.Instance?.GetHarvestBonus() ?? 1f;
float lotteryBoost = LotteryManager.Instance?.GetHarvestBoostMultiplier() ?? 1f;
float finalValue = cropData.harvestBonus * petBonus * lotteryBoost;
```

This means planting works even if pet care and lottery aren't in the scene yet.

---

## Per-Teammate Feature Ownership & Story Points

### Planting Owner (You) — Required + Alpha: ~17pts

| Feature | Story Item | Points |
|---|---|---|
| Money + Fertilizer ticking | Resource Simulation | 2 |
| 3 upgrade tracks | Leveling-up Stats | 3 |
| Fertilizer Station unlock | Leveling to New Interactions | 2 |
| Escalating upgrade costs | Ramping Difficulty | 2 |
| 10+ XR buttons | Poke Interactor Buttons | 2 |
| 10 feedback triggers | Juicy Feedback | 5 |
| JSON save/load | Inter-session Saves | 3 |
| Offline earnings | Idle Progress | 3 |
| Restart + Quit | Pausing | 2 |
| **Total** | | **~18** |

### Pet Care Owner — Required + Alpha: ~17pts

| Feature | Story Item | Points |
|---|---|---|
| Hunger, stress, bonding | Resource Simulation (+3) | 3 |
| Bonding level tracking | Points Scoring | 2 |
| Consecutive feed bonus | Combo Streak | 2 |
| Bunny idle walk | Path Following | 2 |
| Feed/pet/buy buttons | Poke Interactor Buttons (shared) | — |
| Food consumed on feed | Conditional Despawning | 2 |
| 11 feedback triggers | Juicy Feedback | 4 |
| Bunny + food + bed models | 3D Mesh Integrations | 2 |
| **Total** | | **~17** |

### Lottery Owner — Required + Alpha: ~19pts

| Feature | Story Item | Points |
|---|---|---|
| 10 collectible items | Collectibles | 2 |
| Reward object spawning | Copier | 3 |
| Dynamic reward spawns | NPC Spawner | 2 |
| Rewards grant resources | Loot Drop | 2 |
| Escalating ticket cost | Ramping Difficulty | 2 |
| Hidden Easter egg | Secrets | 2 |
| 10 feedback triggers | Juicy Feedback | 4 |
| Machine + collectible models | 3D Mesh Integrations | 2 |
| **Total** | | **~19** |

---

## Integration Timeline

### Week 1: Foundation
- **Planting owner:** Complete Prompts 5-8 (pot loop, upgrades, fertilizer unlock)
- **Pet Care owner:** Design PetData, begin PetCareManager (Prompt 13)
- **Lottery owner:** Design LotteryRewardData, begin LotteryManager (Prompt 17)

### Week 2: Core Systems
- **Planting owner:** Complete Prompts 9-10 (fertilizer resource, save/load)
- **Pet Care owner:** Complete Prompts 14-15 (PetSlot, food station, vitals display)
- **Lottery owner:** Complete Prompts 18-19 (machine interaction, collectible shelf)

### Week 3: Polish + Integration
- **Planting owner:** Complete Prompts 11-12 (tutorials, trophies)
- **Pet Care owner:** Complete Prompt 16 (combo streak, path following)
- **Lottery owner:** Complete Prompt 20 (secrets, ramping cost)
- **All together:** Prompt 21 (cross-system wiring, shared save)

### Week 4: Final
- **All together:** Prompt 22 (pause menu, final testing)
- Integration testing checklist (see IMPLEMENTATION_PLAN.md Prompt 22)

---

## Shared Contracts

Each manager exposes these methods for cross-system use:

### EconomyManager (existing)
```
SpendMoney(float amount) → bool
AddMoney(float amount)
AddFertilizer(float amount)
float Money { get; }
```

### PetCareManager (new)
```
float GetHarvestBonus()        // 1.0 to 2.0 based on bonding
void AddFood(int amount)       // add food from lottery rewards
PetMood GetMood()              // for UI display
event OnBondingChanged(float)  // for cross-system triggers
```

### LotteryManager (new)
```
float GetHarvestBoostMultiplier()   // 1.0 or boost value if active
float GetGrowBoostMultiplier()      // 1.0 or boost value if active
int GetCollectibleCount()           // for UI display
void GrantFreeSpin()                // called by PetCareManager on bonding milestone
event OnSpinResult(LotteryRewardData)
```

### FeedbackManager (extended)
```
// Existing (unchanged):
TriggerCoinParticles(Vector3 pos, float rate)
TriggerFertParticles(Vector3 pos, float rate)
TriggerHaptic(XRBaseController controller, float amplitude, float duration)
PlaySpatialSound(AudioSource source, AudioClip clip)
PlayUISound(AudioClip clip)
EaseScale(Transform target, float duration)
ScalePop(Transform target, float peakMultiplier, float duration)

// New (pet care):
TriggerHeartParticles(Vector3 pos, int count)
TriggerWarningParticles(Vector3 pos)

// New (lottery):
TriggerStarParticles(Vector3 pos, float intensity)
```

---

## Rubric Call Sites (UNCHANGED)

These four graded call sites must NOT move:

| # | Feature | Exact Call |
|---|---|---|
| 1 | Particles for Ramping Resources | `EconomyManager.Tick()` → `FeedbackManager.TriggerCoinParticles()` |
| 2 | Eases for Planting Generators | `PotSlot.PlantSeed()` → `FeedbackManager.EaseScale()` |
| 3 | Haptics for Purchasing Power-ups | `UpgradeManager.PurchaseUpgrade()` → `FeedbackManager.TriggerHaptic()` |
| 4 | Sound for Unlockable UI | `UnlockManager.ConfirmUnlock()` → `FeedbackManager.PlaySpatialSound()` |
