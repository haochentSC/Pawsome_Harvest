using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using TMPro;

namespace ZombieBunker
{
    public class TurretUpgradeStation : MonoBehaviour
    {
        [SerializeField] private float baseCost = 100f;
        [SerializeField] private float costMultiplier = 1.5f;
        [SerializeField] private ResourceType costResource = ResourceType.Cash;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private CooldownTimer cooldownTimer;
        [SerializeField] private float cooldownDuration = 1f;
        [SerializeField] private Button upgradeButton;

        public float CurrentCost
        {
            get
            {
                int level = TurretManager.Instance != null ? TurretManager.Instance.TurretLevel : 0;
                return baseCost * Mathf.Pow(costMultiplier, level);
            }
        }

        public ResourceType CostResource => costResource;

        private void OnEnable()
        {
            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        private void OnDisable()
        {
            if (upgradeButton != null)
                upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
        }

        private void OnUpgradeClicked()
        {
            if (cooldownTimer != null && cooldownTimer.IsOnCooldown) return;
            TryUpgrade();
        }

        public bool TryUpgrade()
        {
            float cost = CurrentCost;
            if (ResourceManager.Instance.TrySpend(costResource, cost))
            {
                TurretManager.Instance.UpgradeTurret();
                if (cooldownTimer != null)
                    cooldownTimer.StartCooldown(cooldownDuration);
                SendPurchaseHaptic();
                UpdateUI();
                return true;
            }
            return false;
        }

        private void Update()
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (costText != null) costText.text = $"Cost: {CurrentCost:F0} {costResource}";
            if (levelText != null)
            {
                int level = TurretManager.Instance != null ? TurretManager.Instance.TurretLevel : 0;
                levelText.text = $"Turret Level: {level}";
            }
        }

        private void SendPurchaseHaptic()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
            foreach (var device in devices)
                device.SendHapticImpulse(0, 0.6f, 1.0f);
        }
    }
}
