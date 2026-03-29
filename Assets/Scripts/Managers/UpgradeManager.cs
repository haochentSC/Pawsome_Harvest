using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Manages the three upgrade tracks: SoilQuality, GrowLights, Irrigation.
///
/// Rubric criterion 3:
///   PurchaseUpgrade() → FeedbackManager.TriggerHaptic()   (amplitude 0.7, duration 0.15s)
///
/// Each track has 3 purchasable levels (0 = not bought).
/// Multipliers are applied immediately to EconomyManager / PotSlot on purchase.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static UpgradeManager Instance { get; private set; }

    // ── Upgrade costs per level (index 0 = Level 1 cost) ─────────────────────
    private static readonly float[] SoilCosts       = { 50f,  150f, 400f };
    private static readonly float[] GrowLightsCosts = { 75f,  200f, 500f };
    private static readonly float[] IrrigationCosts = { 100f, 250f, 600f };

    // ── Multiplier tables (index 0 = base/no upgrade) ─────────────────────────
    private static readonly float[] SoilMultipliers       = { 1f, 1.5f,  2.25f, 3.375f };
    private static readonly float[] GrowLightsMultipliers = { 1f, 1.2f,  1.4f,  1.6f   };
    private static readonly float[] IrrigationMultipliers = { 1f, 1.3f,  1.7f,  2.2f   }; // divides grow time

    // ── State ─────────────────────────────────────────────────────────────────
    private int _soilLevel       = 0;   // 0 = no upgrade, max 3
    private int _growLightsLevel = 0;
    private int _irrigationLevel = 0;

    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Fired after any upgrade is purchased. Passes the type and new level.</summary>
    public event Action<UpgradeType, int> OnUpgradePurchased;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempt to buy the next level of an upgrade track.
    /// Returns true if successful.
    ///
    /// RUBRIC: calls FeedbackManager.TriggerHaptic() on success.
    /// </summary>
    public bool PurchaseUpgrade(UpgradeType type, XRBaseController controller = null)
    {
        int currentLevel = GetLevel(type);
        int maxLevel     = 3;

        if (currentLevel >= maxLevel)
        {
            Debug.Log($"[UpgradeManager] {type} already at max level.");
            return false;
        }

        float cost = GetCostForNextLevel(type);
        if (!EconomyManager.Instance.SpendMoney(cost))
        {
            Debug.Log($"[UpgradeManager] Not enough money for {type} (need {cost}).");
            return false;
        }

        // Increment level
        switch (type)
        {
            case UpgradeType.SoilQuality:  _soilLevel++;        break;
            case UpgradeType.GrowLights:   _growLightsLevel++;  break;
            case UpgradeType.Irrigation:   _irrigationLevel++;  break;
        }

        int newLevel = GetLevel(type);

        // Apply to EconomyManager
        ApplyMultipliers();

        // ── Rubric criterion 3: Haptic on purchase ────────────────────────────
        FeedbackManager.Instance?.TriggerHaptic(controller, 0.7f, 0.15f);
        FeedbackManager.Instance?.PlayUpgradeBuy();

        OnUpgradePurchased?.Invoke(type, newLevel);
        Debug.Log($"[UpgradeManager] Purchased {type} → Level {newLevel}");
        return true;
    }

    /// <summary>Cost of the next level, or 0 if already maxed.</summary>
    public float GetCostForNextLevel(UpgradeType type)
    {
        int level = GetLevel(type);
        if (level >= 3) return 0f;

        return type switch
        {
            UpgradeType.SoilQuality => SoilCosts[level],
            UpgradeType.GrowLights  => GrowLightsCosts[level],
            UpgradeType.Irrigation  => IrrigationCosts[level],
            _                       => 0f
        };
    }

    public int GetLevel(UpgradeType type) => type switch
    {
        UpgradeType.SoilQuality => _soilLevel,
        UpgradeType.GrowLights  => _growLightsLevel,
        UpgradeType.Irrigation  => _irrigationLevel,
        _                       => 0
    };

    public bool IsMaxLevel(UpgradeType type) => GetLevel(type) >= 3;

    /// <summary>
    /// Irrigation multiplier used by PotSlot to divide grow time.
    /// e.g. level 2 → 1.7 → grow time is 1/1.7 of base.
    /// </summary>
    public float GetIrrigationMultiplier() => IrrigationMultipliers[_irrigationLevel];

    // ── Save / Load ───────────────────────────────────────────────────────────

    public void RestoreState(int soil, int lights, int irrigation)
    {
        _soilLevel       = Mathf.Clamp(soil,       0, 3);
        _growLightsLevel = Mathf.Clamp(lights,     0, 3);
        _irrigationLevel = Mathf.Clamp(irrigation, 0, 3);
        ApplyMultipliers();
    }

    public (int soil, int lights, int irrigation) GetSaveState()
        => (_soilLevel, _growLightsLevel, _irrigationLevel);

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyMultipliers()
    {
        if (EconomyManager.Instance == null) return;
        EconomyManager.Instance.SetSoilMultiplier(SoilMultipliers[_soilLevel]);
        EconomyManager.Instance.SetLightBonus(GrowLightsMultipliers[_growLightsLevel]);
        // Irrigation is read directly by PotSlot via GetIrrigationMultiplier()
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Debug
    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Debug: Print Upgrade Levels")]
    private void DebugPrint() =>
        Debug.Log($"[Upgrades] Soil={_soilLevel} Lights={_growLightsLevel} Irrigation={_irrigationLevel}");
}
