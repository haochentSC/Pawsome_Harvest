using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieBunker
{
    public class PowerUpManager : MonoBehaviour
    {
        public static PowerUpManager Instance { get; private set; }

        [SerializeField] private List<PowerUpConfig> availablePowerUps = new List<PowerUpConfig>();

        private Dictionary<PowerUpConfig, int> purchasedCounts = new Dictionary<PowerUpConfig, int>();

        public event Action<PowerUpConfig> OnPowerUpPurchased;

        public IReadOnlyList<PowerUpConfig> AvailablePowerUps => availablePowerUps;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public bool TryPurchasePowerUp(PowerUpConfig config)
        {
            if (disabledPowerUps.Contains(config)) return false;
            if (!ResourceManager.Instance.TrySpend(ResourceType.Cash, config.cost))
                return false;

            if (!purchasedCounts.ContainsKey(config))
                purchasedCounts[config] = 0;
            purchasedCounts[config]++;

            ApplyPowerUp(config);
            OnPowerUpPurchased?.Invoke(config);

            TutorialManager tutorialMgr = FindFirstObjectByType<TutorialManager>();
            if (tutorialMgr != null)
                tutorialMgr.OnPowerUpPurchased(config);

            return true;
        }

        private void ApplyPowerUp(PowerUpConfig config)
        {
            switch (config.target)
            {
                case PowerUpTarget.AllGeneratorsOfType:
                    GeneratorManager.Instance.ApplyMultiplierToAll(config.affectedResourceType, config.multiplier);
                    break;
                case PowerUpTarget.AllTurrets:
                    TurretManager turretMgr = FindFirstObjectByType<TurretManager>();
                    if (turretMgr != null)
                        turretMgr.ApplyEfficiencyMultiplier(config.multiplier);
                    break;
                case PowerUpTarget.SpecificGeneratorTier:
                    GeneratorManager.Instance.ApplyMultiplierToAll(config.affectedResourceType, config.multiplier);
                    break;
            }
        }

        public int GetPurchaseCount(PowerUpConfig config)
        {
            return purchasedCounts.TryGetValue(config, out int count) ? count : 0;
        }

        public bool CanAfford(PowerUpConfig config)
        {
            return ResourceManager.Instance.CanAfford(ResourceType.Cash, config.cost);
        }

        public void ClearAllPowerUps()
        {
            purchasedCounts.Clear();
            disabledPowerUps.Clear();
        }

        private HashSet<PowerUpConfig> disabledPowerUps = new HashSet<PowerUpConfig>();

        /// <summary>
        /// Prevents the given power-up from being purchased (used by challenge mode).
        /// </summary>
        public void DisablePowerUp(PowerUpConfig config)
        {
            if (config != null) disabledPowerUps.Add(config);
        }

        /// <summary>Re-enables all challenge-disabled power-ups.</summary>
        public void EnableAllPowerUps()
        {
            disabledPowerUps.Clear();
        }

        public bool IsPowerUpDisabled(PowerUpConfig config)
        {
            return config != null && disabledPowerUps.Contains(config);
        }

        public Dictionary<PowerUpConfig, int> GetAllPurchasedPowerUps()
        {
            return new Dictionary<PowerUpConfig, int>(purchasedCounts);
        }

        /// <summary>
        /// Restores a purchase count without spending resources or re-applying effects.
        /// Used by SaveManager when loading saved state.
        /// </summary>
        public void RestorePurchaseCount(PowerUpConfig config, int count)
        {
            purchasedCounts[config] = count;
        }
    }
}
