# Pest Control System — Implementation Guide

This document is a step-by-step guide for implementing the **Pest Guard System** described in [plan.md](plan.md) into the existing Tiny Greenhouse XR codebase. It assumes the reader is familiar with the codebase as documented in [AI_CONTEXT.md](AI_CONTEXT.md) and [CLAUDE.md](CLAUDE.md).

The pest system is fully self-contained — it adds new files only and does not modify any existing scripts. Its only cross-system call is `EconomyManager.Instance.AddMoney(...)`, which is already public ([EconomyManager.cs:162](Assets/Scripts/Managers/EconomyManager.cs#L162)).

---

## 0. Pre-flight: Reconcile with project conventions

Before writing code, resolve these conflicts between [plan.md](plan.md) and [CLAUDE.md](CLAUDE.md):

| Conflict | plan.md says | CLAUDE.md says | Resolution |
|---|---|---|---|
| Grab style | `XRGrabInteractable` + Rigidbody | "All XR interactions = `XRSimpleInteractable` button presses. No physics grab." | Pest system needs grab to claim "Grab Interactables" rubric points. Add `XRGrabInteractable` **only on pest prefabs**. Do not retrofit it onto existing pots/buttons. Flag this exception in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). |
| Manager singleton | New `PestManager` on Managers GameObject | "All managers are singletons on one `Managers` GameObject." | Comply — attach `PestManager` to the existing Managers GameObject. |
| Money rate mutation | "EconomyManager.Instance._moneyRate -= penalty" (in plan diagram) | `_moneyRate` is private; only `AddMoney`/`SpendMoney` are public | Implement bug penalty as **periodic `AddMoney(-penalty)` calls** from `Pest.cs`, not by touching internal rate fields. This keeps `EconomyManager` untouched. |
| Sparkles section | Mentioned in plan.md text | Point total table omits sparkles (totals to 19 without them) | Treat sparkles as **optional / future work**. Implementation in this doc focuses on the 19-point pest core. A short Sparkle section appears at the end. |

---

## 1. File layout

All new files live under `Assets/Scripts/`. Folder structure:

```
Assets/Scripts/
├── Data/
│   └── PestData.cs              (NEW — ScriptableObject, optional but recommended)
├── Managers/
│   └── PestManager.cs           (NEW)
├── Interactables/
│   ├── Pest.cs                  (NEW)
│   └── FireplaceZone.cs         (NEW)
└── UI/
    └── PestDisplay.cs           (NEW)
```

`PestData.cs` is recommended because the project already uses ScriptableObjects for tunable per-type data (see [CropData.cs](Assets/Scripts/Data/CropData.cs)). It avoids duplicating tuning across three prefab inspectors.

---

## 2. Data layer — `PestData.cs`

ScriptableObject mirroring [CropData.cs](Assets/Scripts/Data/CropData.cs):

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Pawsome/PestData", fileName = "NewPestData")]
public class PestData : ScriptableObject
{
    public PestType type;            // Weed, Bug, Snail
    public string   displayName;
    public int      rewardOnKill = 5;        // coins via EconomyManager.AddMoney
    public float    drainPerSecond = 0f;     // bug/snail only; 0 for weeds
    public float    drainRadius = 0.6f;      // distance from pot at which drain applies
    public float    drainTickInterval = 1f;  // seconds between drain calls
    public AudioClip spawnSound;
    public AudioClip deathSound;
}
```

Add `Weed, Bug, Snail` to the existing [Enums.cs](Assets/Scripts/Data/Enums.cs):

```csharp
public enum PestType { Weed, Bug, Snail }
```

Create three asset instances in `Assets/Resources/PestData/` (right-click → Create → Pawsome → PestData). Tune values:
- **Weed:** reward 3, drain 0
- **Bug:** reward 5, drain 0.05/s, radius 0.6
- **Snail:** reward 8, drain 0.02/s, radius 0.8

---

## 3. `Pest.cs` — per-prefab behavior

Attach to each pest prefab. Drives lifecycle and the bug-near-pot drain. **Pests are static** (no movement, no AI) — the threat comes from where they spawn, not from chasing.

Key responsibilities:
1. On `Start()`: play spawn sound at this position, trigger small spawn-puff particle, run `FeedbackManager.EaseScale(transform, 0.4f)` (rubric feedback hooks #1, #2).
2. **Optional cosmetic bob** in `Update()` — keeps pests visually alive without any real movement:
   ```csharp
   [SerializeField] private float bobAmplitude = 0.03f;
   [SerializeField] private float bobFrequency = 1.5f;
   private Vector3 _bobOrigin;
   private void Start() { _bobOrigin = transform.localPosition; /* ...rest of Start... */ }
   private void Update() {
       if (_isHeld) return;  // freeze bob while player is holding it
       float y = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
       transform.localPosition = _bobOrigin + new Vector3(0f, y, 0f);
   }
   ```
   Skip this entirely for v1 if you want — pure cosmetic.
3. **Grab hook** — wire `XRGrabInteractable.selectEntered` (UnityEvent in inspector) to a public `OnGrabbed(SelectEnterEventArgs args)` method:
   - Set `_isHeld = true` (freezes bob).
   - Extract the controller from `args.interactorObject` and call `FeedbackManager.TriggerHaptic(controller, 0.5f, 0.1f)`.
   - Play grab sound + small sparkle particle at this position.
4. If `data.drainPerSecond > 0`, run a coroutine that every `data.drainTickInterval`:
   - If `_isHeld`, skip the tick (held pests can't drain).
   - Iterate `PotManager.Instance.ActivePots` (new accessor; see §6).
   - If any active pot is within `data.drainRadius`, call `EconomyManager.Instance.AddMoney(-data.drainPerSecond * data.drainTickInterval)` and trigger a damage-puff particle at the pot (rubric feedback #5).
5. On `OnTriggerEnter(Collider other)`:
   - If `other.CompareTag("Fireplace")` → call `Die()`.
6. `Die()`:
   - Play death sound at fireplace position (rubric feedback #3).
   - Trigger fire/smoke particle burst at fireplace (rubric feedback #4) — via `FeedbackManager.TriggerFireBurst()` (see §7).
   - Call `EconomyManager.Instance.AddMoney(data.rewardOnKill)`.
   - Notify `PestManager.Instance.PestCleared(this)`.
   - `Destroy(gameObject)`.

Required components on the prefab root: `Rigidbody` (use gravity, freeze rotation X/Z so it doesn't tip while bobbing), `Collider` (non-trigger for grab physics), `XRGrabInteractable`, `Pest` (this script). Add a child trigger collider sized slightly larger than the visual mesh for the fireplace overlap detection — easier than tuning the physics collider for both grab and trigger duties.

Minor implementation note: when picked up by `XRGrabInteractable`, the Rigidbody becomes kinematic, and `_isHeld` short-circuits both the bob and the drain. When dropped, `selectExited` fires — wire that to `OnReleased()` to set `_isHeld = false` and refresh `_bobOrigin = transform.localPosition` so the bob continues from wherever the pest landed.

---

## 4. `FireplaceZone.cs` — trigger marker

Tiny script on the fireplace GameObject. The fireplace needs a Box Collider with `isTrigger = true` and tag `"Fireplace"` (add this tag in Project Settings → Tags & Layers).

```csharp
using UnityEngine;
public class FireplaceZone : MonoBehaviour
{
    // Singleton purely so Pest.Die() can find this position for spatial sound/particles.
    public static FireplaceZone Instance { get; private set; }
    private void Awake() { Instance = this; }
}
```

The actual trigger logic lives in `Pest.OnTriggerEnter` (CompareTag check). Two-script split keeps `FireplaceZone` cheap.

---

## 5. `PestManager.cs` — singleton on Managers GameObject

Mirror the structure of [PotManager.cs](Assets/Scripts/Managers/PotManager.cs).

State:
```csharp
public static PestManager Instance { get; private set; }

[SerializeField] private GameObject weedPrefab;
[SerializeField] private GameObject bugPrefab;
[SerializeField] private GameObject snailPrefab;

[SerializeField] private float       spawnInterval     = 8f;
[SerializeField] private float       spawnRadiusAroundPot = 0.5f;  // pests appear within this radius of a random active pot
[SerializeField] private float       spawnHeightOffset = 0.05f;    // sit slightly above pot surface
[SerializeField] private int         maxAlive          = 8;
[SerializeField] private int         infestationThreshold = 5;

[SerializeField] private Light       infestationLight;   // green/red point light
[SerializeField] private Color       safeColor    = Color.green;
[SerializeField] private Color       dangerColor  = Color.red;

private readonly List<Pest> _alive = new();
private int _totalCleared;
private bool _isInfested;
public  int  TotalCleared => _totalCleared;
public  event System.Action<int> OnTotalClearedChanged;
```

API:
```csharp
public void PestCleared(Pest p) {
    _alive.Remove(p);
    _totalCleared++;
    OnTotalClearedChanged?.Invoke(_totalCleared);
    UpdateInfestationState();
}

private IEnumerator SpawnLoop() {
    var wait = new WaitForSeconds(spawnInterval);
    while (true) {
        yield return wait;
        if (_alive.Count >= maxAlive) continue;
        TrySpawnNearActivePot();
    }
}

// Spawns a random pest at a random offset from a randomly chosen ACTIVE pot.
// If no pots are planted yet, no pest spawns this tick — the threat only exists
// once the player has something worth attacking.
private void TrySpawnNearActivePot() {
    if (PotManager.Instance == null) return;
    var pots = PotManager.Instance.ActivePots;
    if (pots == null || pots.Count == 0) return;

    PotSlot pot = pots[Random.Range(0, pots.Count)];
    Vector2 offset2D = Random.insideUnitCircle * spawnRadiusAroundPot;
    Vector3 spawnPos = pot.ParticleAnchorPosition
                     + new Vector3(offset2D.x, spawnHeightOffset, offset2D.y);

    GameObject prefab = Random.value < 0.5f ? weedPrefab
                       : Random.value < 0.5f ? bugPrefab : snailPrefab;
    var go   = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
    var pest = go.GetComponent<Pest>();
    _alive.Add(pest);
    UpdateInfestationState();
}

private void UpdateInfestationState() {
    bool nowInfested = _alive.Count >= infestationThreshold;
    if (nowInfested == _isInfested) return;
    _isInfested = nowInfested;
    if (infestationLight != null)
        infestationLight.color = _isInfested ? dangerColor : safeColor;
}
```

Lifecycle: standard singleton `Awake` (copy pattern from [PotManager.cs:26-30](Assets/Scripts/Managers/PotManager.cs#L26-L30)), then `Start` kicks off `StartCoroutine(SpawnLoop())`.

---

## 6. Cross-system access — `PotManager` accessor

`Pest.cs` needs to know the world positions of active pots to detect proximity. Two clean options:

**Option A (preferred): add a read-only accessor to PotManager.**
```csharp
public IReadOnlyList<PotSlot> ActivePots {
    get {
        var list = new List<PotSlot>();
        if (potSlots != null)
            foreach (var p in potSlots)
                if (p != null && p.IsActive) list.Add(p);
        return list;
    }
}
```
Add to [PotManager.cs](Assets/Scripts/Managers/PotManager.cs). This is a tiny, additive change; no behavior shift. Use [PotSlot.ParticleAnchorPosition](Assets/Scripts/Interactables/PotSlot.cs#L61) as the proximity anchor.

**Option B:** find pots via `FindObjectsOfType<PotSlot>()` cached on Pest spawn. Simpler but slower; acceptable for a small scene.

Use **Option A** — it matches the project's manager-owns-collection pattern.

---

## 7. FeedbackManager additions (optional but cleaner)

To keep [FeedbackManager.cs](Assets/Scripts/Managers/FeedbackManager.cs) the single effects dispatcher (per CLAUDE.md hard rule), add two thin methods rather than spawning particles from `Pest.cs` directly:

```csharp
[SerializeField] private ParticleSystem fireBurstParticleSystem;
[SerializeField] private ParticleSystem damagePuffParticleSystem;

public void TriggerFireBurst(Vector3 worldPos)   => BurstParticlesAt(fireBurstParticleSystem, worldPos, 12);
public void TriggerDamagePuff(Vector3 worldPos)  => BurstParticlesAt(damagePuffParticleSystem, worldPos, 4);
```

Reuse the existing `BurstParticlesAt` helper at [FeedbackManager.cs:91](Assets/Scripts/Managers/FeedbackManager.cs#L91). For spatial death/spawn sound, reuse `PlaySpatialSound(AudioSource, AudioClip)` already at [FeedbackManager.cs:138](Assets/Scripts/Managers/FeedbackManager.cs#L138) — give the fireplace an `AudioSource` with `spatialBlend = 1.0` and pass it in.

These additions keep all game logic out of FeedbackManager (it only dispatches), satisfying the hard rule.

---

## 8. `PestDisplay.cs` — world-space counter

Mirror [ResourceDisplay.cs](Assets/Scripts/UI/ResourceDisplay.cs) pattern:

```csharp
using UnityEngine;
using TMPro;

public class PestDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private void OnEnable() {
        if (PestManager.Instance != null) {
            PestManager.Instance.OnTotalClearedChanged += UpdateLabel;
            UpdateLabel(PestManager.Instance.TotalCleared);
        }
    }
    private void OnDisable() {
        if (PestManager.Instance != null)
            PestManager.Instance.OnTotalClearedChanged -= UpdateLabel;
    }
    private void UpdateLabel(int total) {
        label.text = $"Pests Cleared: {total}";
    }
}
```

Place on a world-space Canvas near the garden bench.

---

## 9. Scene wiring (minimal editor work)

Order of operations in the Unity editor:

1. **Tag setup** — Project Settings → Tags & Layers → add tag `Fireplace`.
2. **Fireplace GameObject** — create a flat cube near the garden corner (e.g. `(2.0, 0.05, 1.5)`); scale `(0.6, 0.05, 0.6)`; add Box Collider with `Is Trigger = true`; assign `Fireplace` tag; attach `FireplaceZone.cs`; add a child `AudioSource` with `Spatial Blend = 1.0`; add a Particle System for embers (assign as `fireBurstParticleSystem` on FeedbackManager).
3. **Pest prefabs** — create three under `Assets/Prefabs/Pests/`:
   - Each: primitive mesh (Capsule for weed, Sphere for bug, Cylinder for snail), Rigidbody (use gravity, freeze rotation X/Z), MeshCollider/SphereCollider non-trigger for grab physics, child trigger collider for fireplace detection, `XRGrabInteractable` (XRI 3.x: `UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable`), `Pest.cs` with the matching `PestData` asset assigned.
   - On the prefab's `XRGrabInteractable`, wire `Select Entered` → `Pest.OnGrabbed` and `Select Exited` → `Pest.OnReleased` in the inspector. Both methods must be `public` (UnityEvent rule from CLAUDE.md).
4. **No spawn points needed** — `PestManager` derives spawn positions from `PotManager.ActivePots` at runtime (offset by `spawnRadiusAroundPot`). Skip placing empty transforms; just tune the radius in the PestManager inspector.
5. **Infestation Light** — Point Light near the garden entrance; drag into `PestManager.infestationLight`.
6. **PestManager** — add component to the existing `Managers` GameObject; assign all three pest prefabs and the infestation light. No spawn-point array.
7. **Particle systems** — create two (fire burst, damage puff) as scene objects under FeedbackManager; assign their references on FeedbackManager. Settings: Simulation Space = World, Start Size ≈ 0.15 (matches the fix noted in [AI_CONTEXT.md:184](AI_CONTEXT.md#L184)).
8. **Counter Canvas** — World-Space Canvas near the bench with a TMP label; attach `PestDisplay.cs`; drag the label in.
9. **XR Origin grab interactor** — verify each hand has a Direct or Ray Interactor capable of grabbing (XRI sample rigs include this; if not, add `XRDirectInteractor` per controller).

---

## 10. Hard-rule checklist — what NOT to do

Cross-checked against [CLAUDE.md](CLAUDE.md):

- ❌ Do not add new singletons outside the `Managers` GameObject — `PestManager` lives there.
- ❌ Do not put pest spawn / drain / scoring logic inside `FeedbackManager` — it must remain pure dispatch.
- ❌ Do not modify `EconomyManager` internals; only call `AddMoney(±x)`.
- ❌ Do not use DOTween or Animator controllers; reuse `FeedbackManager.EaseScale` for the spawn pop.
- ❌ Do not create additional scenes; everything goes into `SampleScene.unity`.
- ✅ Use fully qualified XRI namespaces: `UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable`.
- ✅ Methods wired to UnityEvent (none expected here, since the system is event-free) must be `public` if added.
- ✅ Pest prefabs are the **only** place `XRGrabInteractable` appears in this project — document the exception.

---

## 11. Suggested build order

**Two phases: get every mechanic working first with zero juice, then sweep through and add all sound/particle/haptic calls in one pass.**

### Phase 1 — mechanics only (no sound, no particles, no haptics)

| Step | Task | Verifies |
|---|---|---|
| 1 | Add `PestType` to Enums.cs, create `PestData.cs`, create 3 SO assets | Compiles |
| 2 | Add `ActivePots` accessor to PotManager | Compiles, no behavior change |
| 3 | Write `Pest.cs` (skeleton: grab + fireplace despawn + AddMoney; no drain, no bob, no effects), `FireplaceZone.cs`, `PestManager.cs` with a `[ContextMenu]` "Spawn Now" button | Manually spawn a pest, grab it, drop in fireplace → despawns + money goes up |
| 4 | Implement `TrySpawnNearActivePot` + spawn loop in PestManager | Pests appear automatically near planted pots; nothing spawns when no pots are planted |
| 5 | Add bug drain coroutine in `Pest.cs` | Money counter visibly drops while a bug sits next to a planted pot; stops when grabbed or killed |
| 6 | Add infestation Light color toggle + `_totalCleared` counter event | Light flips green↔red at threshold; counter increments on each kill |
| 7 | Write `PestDisplay.cs`, add world-space Canvas | "Pests Cleared: N" updates live |

End of Phase 1: the system is fully playable. Ugly, but correct.

### Phase 2 — juice pass (one sweep)

| Step | Task |
|---|---|
| 8 | Add `TriggerFireBurst`, `TriggerDamagePuff`, `TriggerSpawnPuff`, `TriggerGrabSparkle` to FeedbackManager (all thin `BurstParticlesAt` wrappers) |
| 9 | In `Pest.Start`: spawn sound (spatial) + spawn puff + `EaseScale(transform, 0.4f)` |
| 10 | In `Pest.OnGrabbed`: grab sound + grab sparkle + `TriggerHaptic(controller, 0.5f, 0.1f)` |
| 11 | In `Pest.Die`: fireplace crackle (spatial) + fire burst at fireplace |
| 12 | In drain coroutine: damage puff at the pot being drained |
| 13 | (Optional) Add cosmetic bob in `Pest.Update` |

Each Phase 2 step is one-to-three lines into `FeedbackManager`. Test once at the end of Phase 2 — no need to play-test between hooks.

---

## 12. Future / optional

- **Garden Sparkles** (mentioned in plan.md): a sibling `SparkleManager` + `Sparkle.cs` + a second TMP label on the same Canvas. Implement only if the team needs the extra Collectibles point and the rubric allows double-claiming.
- **Attacking Health (+2 pts)**: per plan.md §"Optional: Push to 21pts". One extra `int hp` field on `Pest`, one `XRSimpleInteractable` "whack" button child, one material color coroutine.
- **Save support**: when `SaveManager` lands (Prompt 10 in [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md)), add `_totalCleared` and the alive-pest snapshot to `SaveData`. Out of scope until then.

---

## 13. Rubric trace — line-of-sight from code to points

| Rubric Item | Where it lives in code |
|---|---|
| NPC Spawner (2) | `PestManager.SpawnLoop()` |
| Conditional Despawning (2) | `Pest.OnTriggerEnter` → `Pest.Die()` |
| Loot Drop (2) | `Pest.Die()` → `EconomyManager.AddMoney(rewardOnKill)` |
| Hit Boxes (2) | (a) Fireplace trigger — `Pest.OnTriggerEnter`; (b) Pot proximity — `Pest` drain coroutine |
| Enemies (2) | `Pest` drain coroutine calls `AddMoney(-drain)` |
| Grab Interactables (1) | `XRGrabInteractable` on weed + bug prefabs |
| Collectibles (2) | `PestManager._totalCleared` + `PestDisplay` label |
| Visible Flag (2) | `PestManager.UpdateInfestationState` swapping Light color |
| Juicy Feedback (4) | (1) spawn sound, (2) `EaseScale`, (3) fireplace sound, (4) fire burst, (5) damage puff |
| **Total** | **19 pts** |

---

# Next Steps — What Was Implemented and What You Must Do in the Unity Editor

The C# side of the pest system is fully written. Nothing in this section requires writing more code; everything below is asset creation and inspector wiring inside Unity. Read it top-to-bottom — the order matters because later steps depend on earlier ones existing.

## A. Code Already Implemented (reference)

### A.1 New files

| File | Purpose |
|---|---|
| [Assets/Scripts/Data/PestData.cs](Assets/Scripts/Data/PestData.cs) | ScriptableObject. One asset per pest type (Weed / Bug / Snail). Holds reward, drain, radius, audio clips. |
| [Assets/Scripts/Interactables/FireplaceZone.cs](Assets/Scripts/Interactables/FireplaceZone.cs) | Singleton on the fireplace GameObject. Exposes its position + a spatial AudioSource so `Pest.Die()` can play the death crackle from the right place. |
| [Assets/Scripts/Interactables/Pest.cs](Assets/Scripts/Interactables/Pest.cs) | Per-pest behavior: spawn ease + puff, optional cosmetic bob, grab sparkle + haptic + sound, drain coroutine, fireplace-trigger death routine. |
| [Assets/Scripts/Managers/PestManager.cs](Assets/Scripts/Managers/PestManager.cs) | Singleton on `Managers`. Spawn loop (picks a random active pot from `PotManager.ActivePots`, offsets by `spawnRadiusAroundPot`), alive-list, infestation light toggle, total-cleared event. |
| [Assets/Scripts/UI/PestDisplay.cs](Assets/Scripts/UI/PestDisplay.cs) | World-space TMP label that subscribes to `PestManager.OnTotalClearedChanged`. |

### A.2 Modified files

| File | Change |
|---|---|
| [Assets/Scripts/Data/Enums.cs](Assets/Scripts/Data/Enums.cs) | Added `PestType { Weed, Bug, Snail }`. |
| [Assets/Scripts/Managers/PotManager.cs](Assets/Scripts/Managers/PotManager.cs) | Added `IReadOnlyList<PotSlot> ActivePots` property. |
| [Assets/Scripts/Managers/FeedbackManager.cs](Assets/Scripts/Managers/FeedbackManager.cs) | Added 4 ParticleSystem inspector slots (`fireBurstParticleSystem`, `damagePuffParticleSystem`, `spawnPuffParticleSystem`, `grabSparkleParticleSystem`) and 4 thin `Trigger*` methods (`TriggerFireBurst`, `TriggerDamagePuff`, `TriggerSpawnPuff`, `TriggerGrabSparkle`). |

### A.3 Runtime call graph

```
PestManager.SpawnLoop()  ── every spawnInterval ──▶ TrySpawnNearActivePot()
        │
        ├── reads PotManager.Instance.ActivePots         (skips spawn if empty)
        └── Instantiate(prefab) ──▶ Pest.Start()
                                       │
                                       ├── FeedbackManager.EaseScale()        ← rubric feedback #2
                                       ├── FeedbackManager.TriggerSpawnPuff() ← rubric feedback (extra)
                                       ├── PlayClipAtPoint(spawnSound)        ← rubric feedback #1
                                       └── (if drain > 0) StartCoroutine(DrainLoop)
                                                              │
                                                              └── EconomyManager.AddMoney(-drain)
                                                                  FeedbackManager.TriggerDamagePuff()  ← rubric feedback #5

XRGrabInteractable.selectEntered ──▶ Pest.OnGrabbed(args)
        ├── FeedbackManager.TriggerGrabSparkle()
        ├── PlayClipAtPoint(grabSound)
        └── FeedbackManager.TriggerHaptic(controller, 0.5, 0.1)

Pest.OnTriggerEnter("Fireplace") ──▶ Die()
        ├── EconomyManager.AddMoney(rewardOnKill)
        ├── FeedbackManager.TriggerFireBurst()                 ← rubric feedback #4
        ├── FeedbackManager.PlaySpatialSound(deathSound)       ← rubric feedback #3
        ├── PestManager.PestCleared(this)
        │       └── OnTotalClearedChanged(_totalCleared) ──▶ PestDisplay updates label
        │       └── UpdateInfestationState() ──▶ light color flip if threshold crossed
        └── Destroy(gameObject)
```

---

## B. Unity Editor Setup — Step by Step

Do these in order. Save the scene after each major section.

### B.1 Add the Fireplace tag (1 minute)

1. **Edit ▸ Project Settings ▸ Tags and Layers ▸ Tags**.
2. Click **+**, type `Fireplace`, press Enter.
3. Save.

If this tag isn't created, `Pest.OnTriggerEnter` will silently never fire and pests will never die in the fireplace.

---

### B.2 Create the three PestData ScriptableObject assets (3 minutes)

1. In the Project window, create folder `Assets/Data/PestData/`.
2. Right-click in that folder ▸ **Create ▸ Greenhouse ▸ PestData**. Name it `Pest_Weed`.
3. Repeat twice more: `Pest_Bug`, `Pest_Snail`.
4. Select each in turn and fill in the inspector:

| Asset | type | rewardOnKill | drainPerSecond | drainRadius | drainTickInterval |
|---|---|---|---|---|---|
| Pest_Weed | Weed | 3 | 0 | 0.6 | 1 |
| Pest_Bug | Bug | 5 | 0.05 | 0.6 | 1 |
| Pest_Snail | Snail | 8 | 0.02 | 0.8 | 1 |

5. **Audio clips** — leave empty for Phase 1 (mechanics-only test). Fill in `spawnSound`, `grabSound`, `deathSound` in Phase 2.

---

### B.3 Build the three Pest prefabs (8 minutes)

Do this for each of the three pests. The recipe is identical except for the visual mesh and which `PestData` you assign.

#### B.3.1 Weed prefab

1. **GameObject ▸ 3D Object ▸ Capsule**. Rename to `Pest_Weed`.
2. Scale: `(0.15, 0.2, 0.15)`. Position: `(0, 0, 0)`.
3. Material: create a new material `Mat_Weed` with green color, drag onto the capsule.
4. Add components on the root:
   - **Rigidbody** — Use Gravity ✅. Constraints ▸ Freeze Rotation **X** and **Z** (Y free so the bob/grab don't tip it).
   - **XR Grab Interactable** (full path: `UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable`). Movement Type = **Velocity Tracking** (best for VR feel). Throw On Detach ✅.
   - **Pest** (the script you wrote). Drag `Pest_Weed` (the PestData asset) into the **Data** slot. Leave bob amplitude at 0.03.
5. The Capsule's existing **Capsule Collider** is the grab/physics collider — leave Is Trigger **OFF**.
6. Add a child trigger collider for fireplace detection:
   - Right-click the prefab root ▸ **Create Empty Child**, name it `FireplaceTrigger`.
   - Add **Sphere Collider**. Radius `0.15`. **Is Trigger ✅**.
   - This child inherits the parent's transform — the trigger moves with the pest when the player carries it.
7. **Wire the grab events on the XRGrabInteractable component:**
   - Find **Select Entered (SelectEnterEventArgs)** in the inspector. Click **+**.
   - Drag the prefab root into the object slot. Function dropdown ▸ **Pest ▸ OnGrabbed (Dynamic SelectEnterEventArgs)**. Use the **dynamic** version so the args (controller info) flow through.
   - Find **Select Exited (SelectExitEventArgs)**. Click **+**. Same prefab root, function ▸ **Pest ▸ OnReleased (Dynamic SelectExitEventArgs)**.
8. Drag this GameObject from the Hierarchy into `Assets/Prefabs/Pests/` to save it as a prefab.
9. Delete the instance from the scene.

#### B.3.2 Bug prefab

Same as Weed except:
- Use a **Sphere** mesh, scale `(0.12, 0.12, 0.12)`.
- Material: dark brown `Mat_Bug`.
- Pest.Data slot: `Pest_Bug` asset.
- Sphere Collider exists already; child `FireplaceTrigger` Sphere Collider radius `0.12`.

#### B.3.3 Snail prefab

Same as Weed except:
- Use a **Cylinder**, scale `(0.13, 0.07, 0.13)` (squat shape).
- Material: gray `Mat_Snail`.
- Pest.Data slot: `Pest_Snail` asset.
- The default Capsule Collider Unity adds to a Cylinder is fine; child `FireplaceTrigger` Sphere Collider radius `0.16`.

**Important about the trigger collider on a held pest:** the parent Capsule/Sphere collider is **not** a trigger, so it collides physically with the player and world. The child `FireplaceTrigger` IS a trigger, and Unity's physics requires at least one of the two colliding objects to have a Rigidbody for `OnTriggerEnter` to fire. The pest has a Rigidbody on its root, so the trigger child fires events to `Pest.OnTriggerEnter` correctly because Unity bubbles trigger callbacks from children to the script that owns the Rigidbody only if you put the script on the Rigidbody object. Since `Pest.cs` IS on the root with the Rigidbody, this works. **Do not** put `Pest.cs` on the trigger child.

---

### B.4 Build the Fireplace GameObject (3 minutes)

1. In the scene, **GameObject ▸ 3D Object ▸ Cube**. Rename to `Fireplace`.
2. Position: `(2.0, 0.05, 1.5)` (garden corner, near the bench at z=1.5). Adjust to taste.
3. Scale: `(0.6, 0.05, 0.6)` — a flat square pit.
4. Material: create `Mat_Fireplace` with a dark/orange color.
5. **Tag**: top of the inspector, dropdown ▸ **Fireplace** (the tag you created in B.1).
6. **Box Collider** (already present): set **Is Trigger ✅**. Resize so it sticks up ~0.3m above the visual cube — pests dropped from above need vertical room to register the trigger before they hit the floor. Set Center Y to `~3` and Size Y to `~6` (relative to the scaled cube), or just enlarge the collider in world units.
7. Add **FireplaceZone** component (your script).
8. Create a child GameObject named `SpatialAudio`:
   - Add **AudioSource**. Spatial Blend = **1.0**. Play On Awake ❌. Loop ❌.
9. Drag `SpatialAudio` into the FireplaceZone's **Spatial Audio Source** slot.
10. (Optional) Add a child Point Light, orange tint, intensity ~2, range ~1.5, for ambient glow.
11. (Optional) Add a child Particle System for a constant fire effect — purely cosmetic, separate from the death-burst system.

---

### B.5 Set up the four FeedbackManager particle systems (5 minutes)

The `FeedbackManager` GameObject lives on `Managers`. Add four particle systems as children so they share the manager's transform.

For **each** of the four (FireBurst, DamagePuff, SpawnPuff, GrabSparkle):

1. Right-click the `FeedbackManager` GameObject ▸ **Effects ▸ Particle System**.
2. Rename to `PS_FireBurst` / `PS_DamagePuff` / `PS_SpawnPuff` / `PS_GrabSparkle`.
3. In the Particle System inspector, set:
   - **Looping**: ❌ (we Emit on demand)
   - **Play On Awake**: ❌
   - **Simulation Space**: **World** (critical — local mode visually anchors particles to the FeedbackManager transform, which is wrong)
   - **Start Size**: `0.15` (matches the existing coin particle fix in [AI_CONTEXT.md:184](AI_CONTEXT.md#L184))
   - **Start Lifetime**: 0.5–1.0
   - **Start Speed**: 0.5–1.5
4. Specifics per system:

| System | Color | Start Size | Notes |
|---|---|---|---|
| PS_FireBurst | Orange/yellow | 0.2 | Add **Color over Lifetime** fading to dark red. Start speed 1.5. |
| PS_DamagePuff | Dark gray/purple | 0.12 | Lower alpha, gravity modifier 0.5 so it droops. |
| PS_SpawnPuff | Light green / dust | 0.1 | Quick burst, lifetime 0.4. |
| PS_GrabSparkle | White / gold | 0.08 | Bright, lifetime 0.3. |

5. Select the `FeedbackManager` GameObject and drag each particle system into its matching slot in the inspector:
   - **Fire Burst Particle System** ← PS_FireBurst
   - **Damage Puff Particle System** ← PS_DamagePuff
   - **Spawn Puff Particle System** ← PS_SpawnPuff
   - **Grab Sparkle Particle System** ← PS_GrabSparkle

---

### B.6 Set up the Infestation Point Light (1 minute)

1. **GameObject ▸ Light ▸ Point Light**. Rename `InfestationLight`.
2. Position: somewhere visible from the play area, e.g. `(2.0, 1.8, 1.5)` above the fireplace.
3. Range ~3, Intensity ~2.
4. Color: green to start (PestManager will overwrite this on Awake anyway).
5. Hold this reference for B.7.

---

### B.7 Add PestManager to the Managers GameObject (2 minutes)

1. Select the existing `Managers` GameObject in the scene.
2. **Add Component ▸ PestManager**.
3. Fill in the inspector:
   - **Weed Prefab** ← `Assets/Prefabs/Pests/Pest_Weed.prefab`
   - **Bug Prefab** ← `Pest_Bug.prefab`
   - **Snail Prefab** ← `Pest_Snail.prefab`
   - **Spawn Interval** = 8
   - **Spawn Radius Around Pot** = 0.5
   - **Spawn Height Offset** = 0.05
   - **Max Alive** = 8
   - **Startup Delay** = 5 (gives the player time to plant before pests appear)
   - Weights: leave all at 1
   - **Infestation Threshold** = 5
   - **Infestation Light** ← drag `InfestationLight` from the Hierarchy
   - Safe / Danger colors: green / red defaults are fine

---

### B.8 Build the Pest counter Canvas (3 minutes)

1. **GameObject ▸ XR ▸ UI Canvas** (or Canvas ▸ then set Render Mode = World Space).
2. Rename to `PestCounterCanvas`. Position near the garden, e.g. `(1.5, 1.9, 1.5)`. Scale `(0.005, 0.005, 0.005)` (standard world-space UI scale for VR).
3. Right-click canvas ▸ **UI ▸ Text - TextMeshPro** (import TMP essentials if prompted).
4. Set the text rect to a sensible size, font size ~36, anchor center.
5. Default text: `Pests Cleared: 0`.
6. Add **PestDisplay** component to the Canvas root (or any child).
7. Drag the TMP text component into PestDisplay's **Label** slot.
8. The **Prefix** field already defaults to `"Pests Cleared: "`.

---

### B.9 Verify XR Origin grab interactors (1 minute)

If the player can't already grab existing objects in the scene, add a grab-capable interactor to each hand:

1. Find the XR Origin's Left/Right Controller objects.
2. Each must have either a **XR Direct Interactor** (touch grab) or **XR Ray Interactor** (point grab) capable of `Select` events.
3. The XRI Sample rig includes both — if you used it, you're done.
4. Without an interactor, `XRGrabInteractable.selectEntered` never fires, so pests can be touched but never picked up.

---

## C. Phase 1 Test Plan (mechanics only, no audio yet)

Press Play and verify in this order. If something fails, the failing item below tells you which earlier setup step is wrong.

1. **No pests appear immediately** — startup delay is 5s. ✅
2. **Plant a seed in any pot.** Wait 5s after planting. A pest spawns near the planted pot (within ~0.5m). ✅
   - **Fails?** PestManager has no prefab assigned, OR no pot is `IsActive` (PotSlot is `Empty`/`Seeded`, not `Growing`/`Mature`).
3. **Pest scales up from zero on spawn** (EaseScale). ✅
   - **Fails?** FeedbackManager reference is null on the pest, or the pest prefab's initial scale is `(0,0,0)` (set it to its intended scale; EaseScale animates *to* the inspector scale).
4. **Walk up and grab the pest with your VR controller.** It snaps to your hand. ✅
   - **Fails?** XRGrabInteractable is missing on the prefab, or the controller has no interactor (B.9).
5. **Carry the pest into the fireplace and release.** It disappears. Money increments by `rewardOnKill`. ✅
   - **Fails?** Fireplace tag isn't `Fireplace`, OR fireplace's Box Collider isn't Is Trigger, OR the trigger collider is too small to overlap the dropped pest.
6. **Spawn a Bug pest, place it next to a planted pot, leave it alone.** Watch the money counter — it should drop by ~0.05/s. ✅
   - **Fails?** Bug's PestData has `drainPerSecond = 0`, OR the bug is outside `drainRadius` of any active pot.
7. **Spawn 5+ pests** (use the PestManager `Debug: Spawn One Pest Now` context menu in the inspector, right-click the component). The InfestationLight flips from green to red. Kill some — it flips back when alive count < 5. ✅
8. **Each kill** increments the "Pests Cleared: N" label on the canvas. ✅

---

## D. Phase 2 Test Plan (juice pass)

Once Phase 1 passes, do this in one sweep:

1. Drop 4 audio clips into the project (`Assets/Audio/Pests/`):
   - `pest_spawn_pop.wav` — short pop
   - `pest_grab.wav` — quick rustle/squish
   - `fireplace_crackle.wav` — fire whoosh
2. Assign per PestData asset:
   - All 3 pests get the same `spawnSound` and `grabSound`.
   - All 3 pests get `pest_death_crackle.wav` as `deathSound`.
3. Tune the four particle systems' colors and start sizes (B.5) until they read clearly in VR.
4. Re-run the Phase 1 test plan — every event should now have a visible+audible+haptic response.

---

## E. Hard Rules Re-checked

- ✅ `FeedbackManager` still has zero game logic — only added inspector slots and dispatch methods.
- ✅ All managers (PestManager) live on the existing `Managers` GameObject.
- ✅ `EconomyManager` was not modified — pests use `AddMoney(±x)` only.
- ✅ One scene only.
- ✅ `PestData` is a ScriptableObject; matches `CropData` pattern.
- ⚠️ `XRGrabInteractable` is used **only on pest prefabs** — this is the documented exception to the "all interaction is XRSimpleInteractable" rule. Flag this in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) when you next touch it.
- ✅ Fully qualified XRI namespaces used in `Pest.cs`.
- ✅ All UnityEvent target methods (`OnGrabbed`, `OnReleased`) are `public`.

---

## F. Cleanup To-Do (once verified)

- Remove `[ContextMenu]` debug helpers in `PestManager` (`DebugSpawnOne`, `DebugPrintState`).
- Lower `startupDelay` to 0 once you're confident pests don't appear before the first pot is planted (the spawn loop already guards on `ActivePots.Count == 0`, so this is just a polish).
- If save/load lands later (Prompt 10), serialize `_totalCleared` and the alive-pest snapshot.
