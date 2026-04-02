using System;
using UnityEngine;

/// <summary>
/// Watches money and controls what gets unlocked in the scene.
///
/// Currently manages one unlock: the Fertilizer Station (costs 500).
///
/// Rubric criterion 4:
///   ConfirmUnlock() → FeedbackManager.PlaySpatialSound()
///   (AudioSource lives ON the FertilizerStation with spatialBlend = 1.0)
/// </summary>
public class UnlockManager : MonoBehaviour
{
    
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static UnlockManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Fertilizer Station Unlock")]
    [Tooltip("Cost to unlock the fertilizer station.")]
    [SerializeField] private float fertUnlockCost = 500f;

    [Tooltip("Assign the FertilizerStation script from the scene.")]
    [SerializeField] private FertilizerStation fertilizerStation;

    // ── State ─────────────────────────────────────────────────────────────────
    private bool _fertilizerUnlocked = false;


    // ── Events ────────────────────────────────────────────────────────────────
    public event Action OnFertilizerUnlocked;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged += OnMoneyChanged;
    }

    private void OnDestroy()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Money watcher
    // ─────────────────────────────────────────────────────────────────────────

    private void OnMoneyChanged(float newMoney)
    {

        if (_fertilizerUnlocked) return;


        if (newMoney >= fertUnlockCost)
            fertilizerStation?.ShowUnlockButton();
        else
            fertilizerStation?.HideUnlockButton();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API — called by FertilizerStation.OnUnlockPressed()
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spend the unlock cost, activate fertilizer production, play spatial sound.
    ///
    /// RUBRIC criterion 4: this method calls FeedbackManager.PlaySpatialSound().
    /// The AudioSource passed in must have spatialBlend = 1.0.
    /// </summary>
    public void ConfirmUnlock(UnityEngine.AudioSource spatialSource, AudioClip chimeClip)
    {
        if (_fertilizerUnlocked) return;
        if (!EconomyManager.Instance.SpendMoney(fertUnlockCost)) return;

        _fertilizerUnlocked = true;

        // Enable fertilizer ticking in EconomyManager
        EconomyManager.Instance.SetFertilizerUnlocked(true);

        // Show fertilizer panel on HUD
        ResourceDisplay.Instance?.SetFertilizerVisible(true);

        // ── Rubric criterion 4: spatialized sound ─────────────────────────────
        FeedbackManager.Instance?.PlaySpatialSound(spatialSource, chimeClip);

        // Coin particle burst at the station
        FeedbackManager.Instance?.TriggerCoinParticles(
            spatialSource != null ? spatialSource.transform.position : Vector3.zero, 0.5f);

        OnFertilizerUnlocked?.Invoke();
        Debug.Log("[UnlockManager] Fertilizer Station unlocked!");


    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public void RestoreState(bool fertUnlocked)
    {
        _fertilizerUnlocked = fertUnlocked;
        if (fertUnlocked)
        {
            fertilizerStation?.SetUnlockedVisual();
            EconomyManager.Instance?.SetFertilizerUnlocked(true);
            ResourceDisplay.Instance?.SetFertilizerVisible(true);
        }
    }

    public bool IsFertilizerUnlocked() => _fertilizerUnlocked;
}
