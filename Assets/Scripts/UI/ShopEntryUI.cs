using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ZombieBunker
{
    public enum ShopEntryType
    {
        GeneratorProgression,
        PowerUp,
        RocketRoomProgression,
        TurretUpgrade,
        Craft
    }

    public class ShopEntryUI : MonoBehaviour
    {
        [Header("Entry Configuration")]
        [SerializeField] private ShopEntryType entryType;
        [SerializeField] private List<GeneratorConfig> generatorConfigs = new List<GeneratorConfig>();
        [SerializeField] private PowerUpConfig powerUpConfig;

        [Header("Turret Upgrade Settings")]
        [SerializeField] private float turretUpgradeBaseCost = 100f;
        [SerializeField] private float turretUpgradeCostMultiplier = 1.5f;
        [SerializeField] private ResourceType turretUpgradeCostResource = ResourceType.Cash;

        [Header("Craft Settings")]
        [SerializeField] private ResourceType craftInputResource = ResourceType.Bullets;
        [SerializeField] private ResourceType craftOutputResource = ResourceType.Cash;
        [SerializeField] private float craftInputAmount = 10f;
        [SerializeField] private float craftOutputAmount = 5f;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private Button buyButton;
        [SerializeField] private GameObject affordableIndicator;
        [SerializeField] private GameObject unaffordableIndicator;

        private ShopManager shopManager;

        public void Initialize(ShopManager manager)
        {
            shopManager = manager;

            if (buyButton != null)
                buyButton.onClick.AddListener(OnBuyClicked);
        }

        private void Update()
        {
            RefreshDynamicInfo();
        }

        private void RefreshDynamicInfo()
        {
            bool canAfford = false;
            bool alreadyDone = false;
            bool noSpotsLeft = false;

            switch (entryType)
            {
                case ShopEntryType.GeneratorProgression:
                    RefreshGeneratorProgression(ref canAfford, ref noSpotsLeft);
                    break;

                case ShopEntryType.PowerUp:
                    RefreshPowerUp(ref canAfford, ref alreadyDone);
                    break;

                case ShopEntryType.RocketRoomProgression:
                    RefreshRocketRoomProgression(ref canAfford, ref alreadyDone, ref noSpotsLeft);
                    break;

                case ShopEntryType.TurretUpgrade:
                    RefreshTurretUpgrade(ref canAfford);
                    break;

                case ShopEntryType.Craft:
                    RefreshCraft(ref canAfford);
                    break;
            }

            bool interactable = canAfford && !alreadyDone && !noSpotsLeft;
            if (buyButton != null) buyButton.interactable = interactable;
            if (affordableIndicator != null) affordableIndicator.SetActive(interactable);
            if (unaffordableIndicator != null) unaffordableIndicator.SetActive(!interactable);
        }

        private void RefreshGeneratorProgression(ref bool canAfford, ref bool noSpotsLeft)
        {
            if (generatorConfigs.Count == 0 || GeneratorManager.Instance == null || shopManager == null) return;

            GeneratorConfig current = shopManager.GetCurrentConfig(generatorConfigs);
            if (current == null)
            {
                if (nameText != null) nameText.text = "All Placed";
                if (costText != null) costText.text = "";
                if (descriptionText != null) descriptionText.text = "No more placement spots";
                if (countText != null) countText.text = "";
                noSpotsLeft = true;
                return;
            }

            float cost = GeneratorManager.Instance.GetCurrentCost(current);
            if (nameText != null) nameText.text = current.displayName;
            if (costText != null) costText.text = $"${cost:F0}";
            if (descriptionText != null) descriptionText.text = $"+{current.baseRate:F1}/s {current.resourceType}";
            int count = GeneratorManager.Instance.GetGeneratorCount(current);
            if (countText != null) countText.text = $"Owned: {count}";
            canAfford = GeneratorManager.Instance.CanAffordGenerator(current);
        }

        private void RefreshPowerUp(ref bool canAfford, ref bool alreadyDone)
        {
            if (powerUpConfig == null || PowerUpManager.Instance == null) return;

            int count = PowerUpManager.Instance.GetPurchaseCount(powerUpConfig);
            if (count >= 1)
            {
                if (nameText != null) nameText.text = "PURCHASED";
                if (costText != null) costText.text = "";
                if (descriptionText != null) descriptionText.text = powerUpConfig.description;
                if (countText != null) countText.text = "";
                alreadyDone = true;
                return;
            }

            if (nameText != null) nameText.text = powerUpConfig.displayName;
            if (costText != null) costText.text = $"${powerUpConfig.cost:F0}";
            if (descriptionText != null) descriptionText.text = powerUpConfig.description;
            if (countText != null) countText.text = "";
            canAfford = PowerUpManager.Instance.CanAfford(powerUpConfig);
        }

        private void RefreshRocketRoomProgression(ref bool canAfford, ref bool alreadyDone, ref bool noSpotsLeft)
        {
            if (UnlockManager.Instance == null) return;

            if (!UnlockManager.Instance.IsRocketRoomUnlocked)
            {
                float cost = UnlockManager.Instance.RocketRoomUnlockCost;
                if (nameText != null) nameText.text = "Rocket Room";
                if (costText != null) costText.text = $"${cost:F0}";
                if (descriptionText != null) descriptionText.text = "Unlock the rocket production room";
                if (countText != null) countText.text = "";
                canAfford = ResourceManager.Instance != null
                    && ResourceManager.Instance.CanAfford(ResourceType.Cash, cost);
                return;
            }

            if (generatorConfigs.Count == 0 || GeneratorManager.Instance == null || shopManager == null)
            {
                alreadyDone = true;
                if (nameText != null) nameText.text = "Rocket Room";
                if (costText != null) costText.text = "UNLOCKED";
                if (descriptionText != null) descriptionText.text = "";
                if (countText != null) countText.text = "";
                return;
            }

            RefreshGeneratorProgression(ref canAfford, ref noSpotsLeft);
        }

        private void RefreshTurretUpgrade(ref bool canAfford)
        {
            if (TurretManager.Instance == null || ResourceManager.Instance == null) return;

            int level = TurretManager.Instance.TurretLevel;
            float cost = turretUpgradeBaseCost * Mathf.Pow(turretUpgradeCostMultiplier, level);

            if (nameText != null) nameText.text = "Turret Upgrade";
            if (costText != null) costText.text = $"${cost:F0}";
            if (descriptionText != null) descriptionText.text = "Increase turret firepower";
            if (countText != null) countText.text = $"Level: {level}";
            canAfford = ResourceManager.Instance.CanAfford(turretUpgradeCostResource, cost);
        }

        private void RefreshCraft(ref bool canAfford)
        {
            if (ResourceManager.Instance == null) return;

            string inputLabel = craftInputResource == ResourceType.Cash ? $"${craftInputAmount:F0}" : $"{craftInputAmount:F0} {craftInputResource}";
            string outputLabel = craftOutputResource == ResourceType.Cash ? $"${craftOutputAmount:F0}" : $"{craftOutputAmount:F0} {craftOutputResource}";
            if (nameText != null) nameText.text = $"{inputLabel} → {outputLabel}";
            if (costText != null) costText.text = inputLabel;
            if (descriptionText != null) descriptionText.text = $"Convert to {outputLabel}";
            if (countText != null) countText.text = "";
            canAfford = ResourceManager.Instance.CanAfford(craftInputResource, craftInputAmount);
        }

        private void OnBuyClicked()
        {
            if (shopManager == null) return;

            switch (entryType)
            {
                case ShopEntryType.GeneratorProgression:
                    GeneratorConfig currentGen = shopManager.GetCurrentConfig(generatorConfigs);
                    if (currentGen != null)
                        shopManager.PurchaseGenerator(currentGen);
                    break;

                case ShopEntryType.PowerUp:
                    shopManager.PurchasePowerUp(powerUpConfig);
                    break;

                case ShopEntryType.RocketRoomProgression:
                    if (UnlockManager.Instance != null && !UnlockManager.Instance.IsRocketRoomUnlocked)
                    {
                        shopManager.PurchaseRocketRoomUnlock();
                    }
                    else
                    {
                        GeneratorConfig currentRocket = shopManager.GetCurrentConfig(generatorConfigs);
                        if (currentRocket != null)
                            shopManager.PurchaseGenerator(currentRocket);
                    }
                    break;

                case ShopEntryType.TurretUpgrade:
                    int level = TurretManager.Instance != null ? TurretManager.Instance.TurretLevel : 0;
                    float cost = turretUpgradeBaseCost * Mathf.Pow(turretUpgradeCostMultiplier, level);
                    shopManager.PurchaseTurretUpgrade(turretUpgradeCostResource, cost);
                    break;

                case ShopEntryType.Craft:
                    shopManager.PurchaseCraft(craftInputResource, craftOutputResource, craftInputAmount, craftOutputAmount);
                    break;
            }
        }
    }
}
