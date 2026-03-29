using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Owns all resource state (money, fertilizer).
/// Runs a 1-second Euler integration tick.
/// All other systems call SpendMoney / AddMoney and subscribe to events.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static EconomyManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Starting Values")]
    [SerializeField] private float startingMoney = 50f;

    [Header("Tick")]
    [SerializeField] private float tickInterval = 1f;

    [Header("Base Rates")]
    [SerializeField] private float baseMoneyRatePerPot   = 0.1f;
    [SerializeField] private float baseFertRatePerPot    = 0.05f;

    // ── State ─────────────────────────────────────────────────────────────────
    private float _money;
    private float _fertilizer;
    private bool  _fertilizerUnlocked;

    // Calculated each tick — updated by PotManager + UpgradeManager (wired in Prompt 6 & 7)
    private int   _activePotCount;      // set by PotManager.NotifyPotStateChanged()
    private float _soilMultiplier  = 1f;
    private float _lightBonus      = 1f;
    private float _fertMultiplier  = 1f;

    // Cached rate (useful for SaveManager offline progress calc)
    public float CurrentMoneyRate { get; private set; }
    public float CurrentFertRate  { get; private set; }

    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Fired every tick and on any direct money change. Passes new total.</summary>
    public event Action<float> OnMoneyChanged;

    /// <summary>Fired every tick when fertilizer is unlocked. Passes new total.</summary>
    public event Action<float> OnFertilizerChanged;

    /// <summary>Fired every tick. Passes (delta, effectiveRatePerPot) for particle scaling.</summary>
    public event Action<float, float> OnMoneyTick;

    /// <summary>Fired whenever the active pot count or a multiplier changes. Passes new rate.</summary>
    public event Action<float> OnRateChanged;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _money = startingMoney;
        OnMoneyChanged?.Invoke(_money);
        StartCoroutine(TickLoop());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Tick loop
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator TickLoop()
    {
        var wait = new WaitForSeconds(tickInterval);
        while (true)
        {
            yield return wait;
            Tick();
        }
    }

    private void Tick()
    {
        RecalculateRates();

        // ── Money ────────────────────────────────────────────────────────────
        if (CurrentMoneyRate > 0f)
        {
            float delta      = CurrentMoneyRate * tickInterval;  // Euler: resource += rate * dt
            float ratePerPot = CurrentMoneyRate / Mathf.Max(1, _activePotCount);
            _money += delta;
            OnMoneyChanged?.Invoke(_money);
            OnMoneyTick?.Invoke(delta, ratePerPot);

            // Fire one coin particle burst above the pot area.
            if (FeedbackManager.Instance != null)
            {
                Vector3 burstPos = PotManager.Instance != null
                    ? PotManager.Instance.GetCenterOfActivePots()
                    : new Vector3(0f, 1.5f, 1.5f);
                FeedbackManager.Instance.TriggerCoinParticles(burstPos, CurrentMoneyRate);
            }
        }

        // ── Fertilizer ───────────────────────────────────────────────────────
        if (_fertilizerUnlocked && CurrentFertRate > 0f)
        {
            float fertDelta  = CurrentFertRate * tickInterval;
            float fertPerPot = CurrentFertRate / Mathf.Max(1, _activePotCount);
            _fertilizer += fertDelta;
            OnFertilizerChanged?.Invoke(_fertilizer);

            if (FeedbackManager.Instance != null)
            {
                Vector3 fertPos = FertilizerStation.Instance != null
                    ? FertilizerStation.Instance.transform.position + Vector3.up * 0.3f
                    : new Vector3(0f, 1.3f, -1.5f);
                FeedbackManager.Instance.TriggerFertParticles(fertPos, fertPerPot);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Rate calculation
    // Called by Tick(), and also by PotManager / UpgradeManager when state changes.
    // ─────────────────────────────────────────────────────────────────────────

    public void RecalculateRates()
    {
        // Prompt 6 will wire PotManager here.
        // Prompt 7 will wire UpgradeManager here.
        // For now, _activePotCount / multipliers are set externally via the setters below.

        CurrentMoneyRate = baseMoneyRatePerPot * _activePotCount * _soilMultiplier * _lightBonus;
        CurrentFertRate  = _fertilizerUnlocked
            ? baseFertRatePerPot * _activePotCount * _fertMultiplier
            : 0f;
        OnRateChanged?.Invoke(CurrentMoneyRate);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API — called by other systems
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Attempt to spend money. Returns false (and does nothing) if insufficient.</summary>
    public bool SpendMoney(float amount)
    {
        if (_money < amount) return false;
        _money -= amount;
        OnMoneyChanged?.Invoke(_money);
        return true;
    }

    /// <summary>Add money directly (harvest, clicker, offline progress).</summary>
    public void AddMoney(float amount)
    {
        _money += amount;
        OnMoneyChanged?.Invoke(_money);
    }

    /// <summary>Add fertilizer directly.</summary>
    public void AddFertilizer(float amount)
    {
        _fertilizer += amount;
        OnFertilizerChanged?.Invoke(_fertilizer);
    }

    /// <summary>Attempt to spend fertilizer. Returns false if insufficient.</summary>
    public bool SpendFertilizer(float amount)
    {
        if (_fertilizer < amount) return false;
        _fertilizer -= amount;
        OnFertilizerChanged?.Invoke(_fertilizer);
        return true;
    }

    public float GetMoney()      => _money;
    public float GetFertilizer() => _fertilizer;

    // ── Rate/multiplier setters (called by PotManager and UpgradeManager) ────

    /// <summary>Called by PotManager whenever a pot is planted or harvested.</summary>
    public void SetActivePotCount(int count)
    {
        _activePotCount = count;
        RecalculateRates();
    }

    /// <summary>Called by UpgradeManager when SoilQuality level changes.</summary>
    public void SetSoilMultiplier(float multiplier)
    {
        _soilMultiplier = multiplier;
        RecalculateRates();
    }

    /// <summary>Called by UpgradeManager when GrowLights level changes.</summary>
    public void SetLightBonus(float bonus)
    {
        _lightBonus = bonus;
        RecalculateRates();
    }

    /// <summary>Called by UpgradeManager when fertilizer upgrade level changes.</summary>
    public void SetFertMultiplier(float multiplier)
    {
        _fertMultiplier = multiplier;
        RecalculateRates();
    }

    /// <summary>Called by UnlockManager when the fertilizer station is unlocked.</summary>
    public void SetFertilizerUnlocked(bool unlocked)
    {
        _fertilizerUnlocked = unlocked;
        RecalculateRates();
        if (unlocked) OnFertilizerChanged?.Invoke(_fertilizer);
    }

    public bool IsFertilizerUnlocked() => _fertilizerUnlocked;

    // ── Save / Load support ──────────────────────────────────────────────────

    /// <summary>Restore state directly from a save file (called by GameManager on load).</summary>
    public void RestoreState(float money, float fertilizer, bool fertUnlocked)
    {
        _money               = money;
        _fertilizer          = fertilizer;
        _fertilizerUnlocked  = fertUnlocked;
        RecalculateRates();
        OnMoneyChanged?.Invoke(_money);
        if (_fertilizerUnlocked) OnFertilizerChanged?.Invoke(_fertilizer);
    }

    /// <summary>Apply offline earnings (capped, 50 % efficiency). Called by SaveManager.</summary>
    public void ApplyOfflineProgress(float earnings)
    {
        if (earnings <= 0f) return;
        _money += earnings;
        OnMoneyChanged?.Invoke(_money);
    }

    // ── Debug helper ─────────────────────────────────────────────────────────

    [ContextMenu("Debug: Add 100 Money")]
    public void DebugAddMoney() => AddMoney(100f);

    [ContextMenu("Debug: Print State")]
    private void DebugPrintState() =>
        Debug.Log($"[Economy] Money={_money:F1} | Fert={_fertilizer:F1} | " +
                  $"Rate={CurrentMoneyRate:F2}/s | ActivePots={_activePotCount}");
}
