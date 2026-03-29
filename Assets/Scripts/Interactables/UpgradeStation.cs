using UnityEngine;
using TMPro;

/// <summary>
/// One upgrade station in the scene — drives one UpgradeType track.
///
/// Place three of these (SoilQuality, GrowLights, Irrigation) on the right wall panel.
///
/// Expected child hierarchy:
///   UpgradeStation (this script)
///   ├── Visual            (mesh — station body)
///   ├── ButtonBuy         (XRSimpleButton, onPressed → OnBuyPressed)
///   ├── LevelText         (TMP_Text — e.g. "Soil Lv 1 / 3")
///   └── CostText          (TMP_Text — e.g. "Cost: $150" or "MAX")
/// </summary>
public class UpgradeStation : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Upgrade Track")]
    [Tooltip("Which upgrade type this station controls.")]
    [SerializeField] private UpgradeType upgradeType;

    [Header("References")]
    [SerializeField] private XRSimpleButton buyButton;
    [SerializeField] private TMP_Text       levelText;
    [SerializeField] private TMP_Text       costText;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Refresh display whenever money or upgrades change
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged += OnMoneyChanged;

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradePurchased += OnUpgradePurchased;

        RefreshDisplay();
    }

    private void OnDestroy()
    {
        if (EconomyManager.Instance != null)
            EconomyManager.Instance.OnMoneyChanged -= OnMoneyChanged;

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradePurchased -= OnUpgradePurchased;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Button callback — wire ButtonBuy.onPressed → this in inspector
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Called by ButtonBuy.onPressed via inspector UnityEvent.</summary>
    public void OnBuyPressed()
    {
        if (UpgradeManager.Instance == null) return;

        // Pass the pressing controller so UpgradeManager can send haptics to the right hand
        var controller = buyButton != null ? buyButton.LastPressingController : null;
        UpgradeManager.Instance.PurchaseUpgrade(upgradeType, controller);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Event handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void OnMoneyChanged(float _)        => RefreshDisplay();
    private void OnUpgradePurchased(UpgradeType type, int level)
    {
        if (type == upgradeType) RefreshDisplay();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Display
    // ─────────────────────────────────────────────────────────────────────────

    private void RefreshDisplay()
    {
        if (UpgradeManager.Instance == null) return;

        int   level    = UpgradeManager.Instance.GetLevel(upgradeType);
        bool  isMax    = UpgradeManager.Instance.IsMaxLevel(upgradeType);
        float cost     = UpgradeManager.Instance.GetCostForNextLevel(upgradeType);
        float money    = EconomyManager.Instance != null ? EconomyManager.Instance.GetMoney() : 0f;

        string label = upgradeType switch
        {
            UpgradeType.SoilQuality => "Soil",
            UpgradeType.GrowLights  => "Lights",
            UpgradeType.Irrigation  => "Water",
            _                       => upgradeType.ToString()
        };

        if (levelText != null)
            levelText.text = $"{label} Lv {level}/3";

        if (costText != null)
        {
            if (isMax)
                costText.text = "MAX";
            else if (money < cost)
                costText.text = $"${cost:F0} (need ${cost - money:F0} more)";
            else
                costText.text = $"Buy: ${cost:F0}";
        }

        // Disable button when maxed or can't afford
        if (buyButton != null)
            buyButton.SetEnabled(!isMax && money >= cost);
    }
}
