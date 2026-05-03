# Planting-Track Alpha — Progress Tracker

**Owner:** htong9
**Source plan:** `C:\Users\tongh\.claude\plans\robust-swinging-bear.md`
**Demo script:** `docs/game_alpha_demo_htong9.md` (8 rows = 18 pts pre-multiplier)
**Last updated:** 2026-05-03 (Steps 1–5 done; ready for headset recording)

---

## Overview

The planting track is feature-complete in code (Prompts 1–9). The remaining alpha work is **Prompt 10 — Save/Load + Idle Progress**, plus a verification sweep of the 8-row demo.

Both new scripts already exist and compile:
- `Assets/Scripts/Data/SaveData.cs`
- `Assets/Scripts/Managers/SaveManager.cs`

What's missing is **scene wiring + a popup prefab + verification**. Five small steps.

**Out of scope (other people):** Pet Care, Lottery, Zombie Shooting, Pause/Restart/Quit (hc101), Cross-system wiring (Prompt 21).

---

## Step Status

| # | Step | Status | Notes |
|---|---|---|---|
| 1 | Add `SaveManager` to `Managers` GameObject in `SampleScene.unity` + Script Execution Order = `-25` | ✅ done | Component added via MCP, exec order set 0 → -25, scene saved |
| 2 | Build `Assets/Prefabs/WelcomeBackPopup.prefab` (duplicate `TutorialPopup.prefab`, ensure a `TMP_Text` is in children) and assign to `SaveManager.welcomeBackPopupPrefab` | ✅ done | Duplicated from TutorialPopup; TMP_Text confirmed; wired via SerializedObject; scene saved |
| 3 | Save/Load round-trip smoke test via Unity MCP (Play → mutate state → wait 60s autosave → Stop → Play → verify restored) | ✅ done | Saved money=12337 + upgrades 2/1/1 + 2 Growing pots → file persisted (572 B) → restored exactly. Offline earnings (~9 coins) also auto-applied. |
| 4 | Offline-progress test (rewind `lastSaveIsoUtc` in `save.json` by 30 min and 12 h; verify earnings + cap) | ✅ done | 30 min @ rate 0.54 → +490 coins (expected 486). 12 h rewind → +7775 coins (capped at 8 h = 7776, vs uncapped 11664). Both within rounding tolerance. |
| 5 | Pre-recording sweep — wipe save, walk all 8 rows, update `BUILD_PROGRESS.md` | ✅ done (auto parts) | save.json deleted; startingMoney=50, popup wired, exec order=-25, autoSave=60s, efficiency=0.5, cap=28800 all verified; BUILD_PROGRESS flipped. **Manual VR walkthrough still needs to happen on headset before recording.** |

Legend: ✅ done · ⏳ in progress · ⬜ not started · ❌ blocked

---

## Verified Pre-Conditions (don't re-verify)

Already confirmed against the live source tree:

- All 4 graded rubric call sites are wired (coin particles on tick, EaseScale on plant, haptic on upgrade, spatial chime on unlock).
- `EconomyManager` has `GetMoney/GetFertilizer/IsFertilizerUnlocked/CurrentMoneyRate/RestoreState/ApplyOfflineProgress`.
- `_activePotCount = 2` debug stub is gone; `PotManager.GetCenterOfActivePots()` drives coin particle position.
- `UpgradeManager.GetSaveState()` / `RestoreState(soil,lights,irrigation)` exist.
- `UnlockManager.RestoreState(bool)` exists.
- `PotSlot.GetSaveData()` / `RestoreFromSave()` exist; `[Serializable] PotSaveData { state, growTimer }`.
- `Managers` GameObject in `SampleScene.unity` already hosts: EconomyManager, FeedbackManager, PotManager, UpgradeManager, UnlockManager, TutorialManager.
- `SaveData.cs` schema includes reserved `petJsonBlob` / `lotteryJsonBlob` strings so teammates can extend without breaking the schema.

---

## Critical Files (don't modify the scripts; they're done)

**Scripts — already complete:**
- `Assets/Scripts/Managers/SaveManager.cs` — orchestrator (Save/Load/AutoSave 60 s/Offline Earnings 50 % efficiency, 8 h cap/Welcome popup)
- `Assets/Scripts/Data/SaveData.cs` — JsonUtility-friendly schema
- `Assets/Scripts/Managers/EconomyManager.cs` — already exposes `RestoreState`, `ApplyOfflineProgress`, `CurrentMoneyRate`
- `Assets/Scripts/Managers/UpgradeManager.cs` — `GetSaveState/RestoreState`
- `Assets/Scripts/Managers/UnlockManager.cs` — `RestoreState`
- `Assets/Scripts/Interactables/PotSlot.cs` — `GetSaveData/RestoreFromSave`

**Editor work (no code):**
- `Assets/Scenes/SampleScene.unity` — add `SaveManager` to Managers GO
- `ProjectSettings/ScriptExecutionOrder` — `SaveManager = -25`
- `Assets/Prefabs/WelcomeBackPopup.prefab` — new (duplicate of `TutorialPopup.prefab`)

---

## How to Resume in a New Session

1. Read this file (`docs/ALPHA_PROGRESS.md`) and `docs/game_alpha_demo_htong9.md`.
2. Find the first ⏳ or ⬜ row in the Step Status table — that's the next action.
3. After completing a step: flip its row to ✅ and bump "Last updated".
4. After Step 5: also update `docs/BUILD_PROGRESS.md` planting rows for Save/Load + Idle Progress to "Done".

## Verification Checklist (Step 5 final gate)

- [ ] Console clean (`Unity_ReadConsole`, zero errors)
- [ ] `[SaveManager] Saved → ...` log appears every 60 s in Play
- [ ] Money / fertilizer / upgrade levels / pot growTimers restored on Play → Stop → Play
- [ ] Rewinding `lastSaveIsoUtc` by 30 min adds `≈ rate × 1800 × 0.5` coins + popup
- [ ] Rewinding 12 h clamps to 8 h
- [ ] All 4 graded rubric call-sites still fire
- [ ] `docs/BUILD_PROGRESS.md` updated
