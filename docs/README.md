# Tiny Greenhouse XR — Project Documentation

**Last updated:** 2026-03-24
**Unity version:** 6000.3.9f1 LTS
**Platform:** Oculus Quest (via OpenXR)
**Course:** XR class project — MP3 Juicy Sim

---

## What Is This?

A small VR idle/clicker sim set inside a single greenhouse room. The player:
1. Buys seeds and plants them into pots
2. Pots passively generate money over time
3. Harvests crops, spends money on upgrades
4. Eventually unlocks a second resource system (Fertilizer)

It's designed to feel like a cozy, satisfying little sim — not a big game. The scope is intentionally small so the "juicy" feedback (particles, haptics, sound, easing animations) can shine.

---

## Quick Links

| Document | What It Covers |
|---|---|
| [ARCHITECTURE.md](ARCHITECTURE.md) | How the code is organized, key rules, script list |
| [BUILD_PROGRESS.md](BUILD_PROGRESS.md) | What's done, what's broken, what comes next |
| [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) | Full step-by-step build guide (12 prompts) |
| [RUBRIC.md](RUBRIC.md) | How the assignment criteria map to specific code |

---

## Getting Started (New Teammate)

### 1. Clone and open the project
Open in Unity Hub with **Unity 6000.3.9f1**. URP and XR packages are already configured.

### 2. Install missing packages (if first time)
Go to **Window > Package Manager** and confirm these are installed:
- `com.unity.xr.interaction.toolkit` (3.x)
- `com.unity.xr.openxr`
- `com.unity.textmeshpro`

### 3. Read BUILD_PROGRESS.md first
It tells you exactly what works, what has known issues, and what the immediate next task is. Don't start coding without reading it.

### 4. Open the scene
`Assets/Scenes/SampleScene.unity` — this is the only scene. Everything lives here.

### 5. Check the Managers GameObject
There is a single `Managers` GameObject in the hierarchy. It holds all the manager scripts. If you add a new manager script, it goes here.

---

## The Game at a Glance

```
Player has money
   ↓
Buy seed (costs money, exponential price)
   ↓
Plant seed in empty pot (XR button press)
   ↓
Pot grows through 3 stages over time
   ↓
Harvest mature plant (XR button press)
   ↓
Earn money automatically (passive rate while growing)
   ↓
Buy upgrades → multiplies money rate (haptic feedback on purchase)
   ↓
Spend 500 money → unlock Fertilizer Station (spatialized sound plays)
   ↓
Fertilizer becomes second passive resource
```

---

## Assignment Rubric — The Four Required Features

The assignment specifically requires these four things. Every design decision flows from making these work cleanly:

| # | Feature | What It Means In This Game |
|---|---|---|
| 1 | **Particles for Ramping Resources** | Coin particles burst from pots every second. More pots = more particles. |
| 2 | **Eases for Planting Generators** | Planting a seed plays an ease-out-back scale animation. A counter UI eases up. |
| 3 | **Haptics for Purchasing Power-ups** | Buying an upgrade vibrates the controller. |
| 4 | **Sound for Unlockable UI** | Unlocking the Fertilizer Station plays a 3D spatialized chime at its location. |

For the detailed code-level mapping, see [RUBRIC.md](RUBRIC.md).

---

## Team Notes

- All interactions are simple XR button presses — no physics grab or throw mechanics
- The scope is intentionally locked. See ARCHITECTURE.md for what is out of scope.
- If you're unsure whether a feature fits, check RUBRIC.md first
- The game is one scene only. Do not add additional scenes.
