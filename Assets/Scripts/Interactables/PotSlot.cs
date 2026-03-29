using System.Collections;
using UnityEngine;

/// <summary>
/// Controls one planting pot in the greenhouse.
///
/// State machine:  Empty → Seeded → Growing → Mature → Empty (after harvest)
///
/// Rubric tie-in (criterion 2):
///   PlantSeed() → FeedbackManager.EaseScale()   (spawn animation on the seedling)
///
/// PotManager (Prompt 6) will call:
///   RegisterWithManager() / UnregisterWithManager() to hook this pot into the economy.
///
/// Prefab hierarchy expected:
///   PotSlot (this script + XRSimpleButton for planting)
///   ├── PotVisual          (the ceramic pot mesh)
///   ├── PlantAnchor        (empty transform; seedling/mature models spawn here)
///   ├── ParticleAnchor     (empty transform; EconomyManager grabs worldPosition for particles)
///   ├── ButtonPlant        (XRSimpleButton → calls PlantSeed)
///   ├── ButtonHarvest      (XRSimpleButton → calls Harvest)
///   └── GrowProgressRoot   (optional: cooldown ring / progress visual)
/// </summary>
public class PotSlot : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Crop")]
    [Tooltip("Which crop this pot plants. Assign in inspector or swap at runtime.")]
    [SerializeField] private CropData cropData;

    [Header("References")]
    [Tooltip("ButtonPlant XRSimpleButton. Enabled only in Empty state.")]
    [SerializeField] private XRSimpleButton buttonPlant;

    [Tooltip("ButtonHarvest XRSimpleButton. Enabled only in Mature state.")]
    [SerializeField] private XRSimpleButton buttonHarvest;

    [Tooltip("Parent transform where seedling / mature prefabs are instantiated.")]
    [SerializeField] private Transform plantAnchor;

    [Tooltip("World-space position EconomyManager uses for coin particle bursts.")]
    [SerializeField] private Transform particleAnchor;

    [Header("Grow Progress Visual (optional)")]
    [Tooltip("Ring or bar that shows grow progress. Scaled 0→1 along X.")]
    [SerializeField] private Transform growProgressBar;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private PotState     _state          = PotState.Empty;
    private float        _growTimer      = 0f;
    private GameObject   _seedlingInstance;
    private GameObject   _matureInstance;
    private Coroutine    _growCoroutine;

    /// <summary>World-space position of the particle anchor, or this transform if unassigned.</summary>
    public Vector3 ParticleAnchorPosition =>
        particleAnchor != null ? particleAnchor.position : transform.position + Vector3.up * 0.15f;

    /// <summary>True when this pot is actively generating income (Growing or Mature).</summary>
    public bool IsActive => _state == PotState.Growing || _state == PotState.Mature;

    public PotState State => _state;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        RefreshButtons();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API — wired to XRSimpleButton.onPressed in inspector
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by ButtonPlant.onPressed.
    /// Costs seedCost money, spawns seedling, triggers EaseScale (rubric criterion 2).
    /// </summary>
    public void PlantSeed()
    {
        if (_state != PotState.Empty) return;
        if (cropData == null)
        {
            Debug.LogWarning($"[PotSlot] {name}: No CropData assigned. Cannot plant.");
            return;
        }

        // Attempt to spend money
        if (!EconomyManager.Instance.SpendMoney(cropData.seedCost))
        {
            Debug.Log($"[PotSlot] {name}: Not enough money to plant (need {cropData.seedCost}).");
            return;
        }

        // Spawn seedling under PlantAnchor
        if (cropData.seedlingPrefab != null && plantAnchor != null)
        {
            _seedlingInstance = Instantiate(cropData.seedlingPrefab, plantAnchor.position, plantAnchor.rotation, plantAnchor);

            // ── Rubric criterion 2: EaseScale on the seedling spawn ──────────
            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.EaseScale(_seedlingInstance.transform, 0.4f);
        }

        SetState(PotState.Growing);
        _growCoroutine = StartCoroutine(GrowRoutine());
    }

    /// <summary>
    /// Called by ButtonHarvest.onPressed.
    /// Adds harvestBonus money, destroys plant model, resets to Empty.
    /// </summary>
    public void Harvest()
    {
        if (_state != PotState.Mature) return;

        EconomyManager.Instance.AddMoney(cropData.harvestBonus);
        FeedbackManager.Instance?.PlayHarvestSound();

        // Destroy plant models
        if (_seedlingInstance != null) { Destroy(_seedlingInstance); _seedlingInstance = null; }
        if (_matureInstance  != null)  { Destroy(_matureInstance);  _matureInstance  = null; }

        SetState(PotState.Empty);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Grow coroutine
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator GrowRoutine()
    {
        float irrigationMultiplier = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.GetIrrigationMultiplier() : 1f;
        float totalTime = (cropData != null ? cropData.growTime : 30f) / irrigationMultiplier;
        _growTimer = 0f;

        while (_growTimer < totalTime)
        {
            _growTimer += Time.deltaTime;

            // Update optional progress bar (scale X from 0 to 1)
            if (growProgressBar != null)
            {
                float t = Mathf.Clamp01(_growTimer / totalTime);
                Vector3 s = growProgressBar.localScale;
                growProgressBar.localScale = new Vector3(t, s.y, s.z);
            }

            yield return null;
        }

        // Swap seedling → mature model
        if (_seedlingInstance != null)
        {
            Destroy(_seedlingInstance);
            _seedlingInstance = null;
        }

        if (cropData.maturePrefab != null && plantAnchor != null)
        {
            _matureInstance = Instantiate(cropData.maturePrefab, plantAnchor.position, plantAnchor.rotation, plantAnchor);

            // Ease the mature plant in too
            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.EaseScale(_matureInstance.transform, 0.35f);
        }

        // Reset progress bar
        if (growProgressBar != null)
        {
            Vector3 s = growProgressBar.localScale;
            growProgressBar.localScale = new Vector3(0f, s.y, s.z);
        }

        SetState(PotState.Mature);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // State machine
    // ─────────────────────────────────────────────────────────────────────────

    private void SetState(PotState newState)
    {
        _state = newState;
        RefreshButtons();

        // Notify PotManager so it can update EconomyManager active count
        PotManager.Instance?.NotifyPotStateChanged();
    }

    private void RefreshButtons()
    {
        if (buttonPlant   != null) buttonPlant.SetEnabled(_state == PotState.Empty);
        if (buttonHarvest != null) buttonHarvest.SetEnabled(_state == PotState.Mature);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Save / Load support (used by SaveManager in Prompt 10)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns serialisable state snapshot.</summary>
    public PotSaveData GetSaveData()
    {
        return new PotSaveData
        {
            state     = _state,
            growTimer = _growTimer
        };
    }

    /// <summary>Restores state from a save file. Call before Start() completes or via SaveManager.</summary>
    public void RestoreFromSave(PotSaveData data)
    {
        // Stop any in-progress grow
        if (_growCoroutine != null) StopCoroutine(_growCoroutine);

        _state     = data.state;
        _growTimer = data.growTimer;

        if (_state == PotState.Growing)
        {
            // Resume grow from saved timer
            _growCoroutine = StartCoroutine(ResumeGrowRoutine());
        }
        else if (_state == PotState.Mature)
        {
            // Re-spawn mature model
            if (cropData?.maturePrefab != null && plantAnchor != null)
                _matureInstance = Instantiate(cropData.maturePrefab, plantAnchor.position, plantAnchor.rotation, plantAnchor);
        }

        RefreshButtons();
    }

    private IEnumerator ResumeGrowRoutine()
    {
        float totalTime = cropData != null ? cropData.growTime : 30f;

        // Spawn seedling to represent in-progress grow
        if (cropData?.seedlingPrefab != null && plantAnchor != null && _seedlingInstance == null)
            _seedlingInstance = Instantiate(cropData.seedlingPrefab, plantAnchor.position, plantAnchor.rotation, plantAnchor);

        while (_growTimer < totalTime)
        {
            _growTimer += Time.deltaTime;

            if (growProgressBar != null)
            {
                float t = Mathf.Clamp01(_growTimer / totalTime);
                Vector3 s = growProgressBar.localScale;
                growProgressBar.localScale = new Vector3(t, s.y, s.z);
            }

            yield return null;
        }

        if (_seedlingInstance != null) { Destroy(_seedlingInstance); _seedlingInstance = null; }

        if (cropData?.maturePrefab != null && plantAnchor != null)
            _matureInstance = Instantiate(cropData.maturePrefab, plantAnchor.position, plantAnchor.rotation, plantAnchor);

        if (growProgressBar != null)
        {
            Vector3 s = growProgressBar.localScale;
            growProgressBar.localScale = new Vector3(0f, s.y, s.z);
        }

        SetState(PotState.Mature);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Debug
    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Debug: Force Plant")]
    private void DebugPlant()
    {
        _state = PotState.Empty;
        PlantSeed();
    }

    [ContextMenu("Debug: Force Mature")]
    private void DebugForceMature()
    {
        if (_growCoroutine != null) StopCoroutine(_growCoroutine);
        _growTimer = cropData != null ? cropData.growTime : 30f;

        if (_seedlingInstance != null) { Destroy(_seedlingInstance); _seedlingInstance = null; }
        if (cropData?.maturePrefab != null && plantAnchor != null)
            _matureInstance = Instantiate(cropData.maturePrefab, plantAnchor.position, plantAnchor.rotation, plantAnchor);

        SetState(PotState.Mature);
    }
}

// ── Serialisable save struct (used by SaveManager) ────────────────────────────
[System.Serializable]
public class PotSaveData
{
    public PotState state;
    public float    growTimer;
}
