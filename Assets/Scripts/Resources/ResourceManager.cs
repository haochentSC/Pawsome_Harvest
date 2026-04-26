using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieBunker
{
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }

        [Header("Starting Values")]
        [SerializeField] private float startingBullets = 10f;
        [SerializeField] private float startingRockets = 0f;
        [SerializeField] private float startingCash = 5000f;

        private Dictionary<ResourceType, float> resourceCounts = new Dictionary<ResourceType, float>();
        private Dictionary<ResourceType, float> resourceRates = new Dictionary<ResourceType, float>();
        private Dictionary<ResourceType, float> rateMultipliers = new Dictionary<ResourceType, float>();
        private Dictionary<ResourceType, float> consumptionRates = new Dictionary<ResourceType, float>();
        private Dictionary<ResourceType, float> totalProduced = new Dictionary<ResourceType, float>();

        private const float MaxResource = 9999f;

        private bool rocketsUnlocked = false;

        public event Action<ResourceType, float> OnResourceChanged;
        public event Action<ResourceType, float> OnRateChanged;
        public event Action<ResourceType, float> OnTotalProducedChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeResources();
        }

        private void InitializeResources()
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                resourceCounts[type] = 0f;
                resourceRates[type] = 0f;
                rateMultipliers[type] = 1f;
                consumptionRates[type] = 0f;
                totalProduced[type] = 0f;
            }

            resourceCounts[ResourceType.Bullets] = startingBullets;
            resourceCounts[ResourceType.Rockets] = startingRockets;
            resourceCounts[ResourceType.Cash] = startingCash;
        }

        private void Update()
        {
            EulerStep(Time.deltaTime);
        }

        public void EulerStep(float dt)
        {
            IntegrateResource(ResourceType.Bullets, dt);
            if (rocketsUnlocked)
            {
                IntegrateResource(ResourceType.Rockets, dt);
            }
            // Cash is integrated via turret conversion, not directly
        }

        private void IntegrateResource(ResourceType type, float dt)
        {
            float effectiveRate = GetEffectiveRate(type);
            if (effectiveRate <= 0f) return;

            float delta = effectiveRate * dt;
            float capped = Mathf.Min(resourceCounts[type] + delta, MaxResource);
            float actualDelta = capped - resourceCounts[type];
            resourceCounts[type] = capped;
            totalProduced[type] += actualDelta;
            OnResourceChanged?.Invoke(type, resourceCounts[type]);
            OnTotalProducedChanged?.Invoke(type, totalProduced[type]);
        }

        public float GetEffectiveRate(ResourceType type)
        {
            return resourceRates[type] * rateMultipliers[type];
        }

        public float GetResourceCount(ResourceType type)
        {
            return resourceCounts[type];
        }

        public float GetTotalProduced(ResourceType type)
        {
            return totalProduced[type];
        }

        public float GetBaseRate(ResourceType type)
        {
            return resourceRates[type];
        }

        public float GetRateMultiplier(ResourceType type)
        {
            return rateMultipliers[type];
        }

        public void SetBaseRate(ResourceType type, float rate)
        {
            resourceRates[type] = rate;
            OnRateChanged?.Invoke(type, GetEffectiveRate(type));
        }

        public void SetConsumptionRate(ResourceType type, float rate)
        {
            consumptionRates[type] = rate;
        }

        public float GetConsumptionRate(ResourceType type)
        {
            return consumptionRates[type];
        }

        public float GetNetRate(ResourceType type)
        {
            return GetEffectiveRate(type) - consumptionRates[type];
        }

        public void AddToBaseRate(ResourceType type, float amount)
        {
            resourceRates[type] += amount;
            OnRateChanged?.Invoke(type, GetEffectiveRate(type));
        }

        public void SetRateMultiplier(ResourceType type, float multiplier)
        {
            rateMultipliers[type] = multiplier;
            OnRateChanged?.Invoke(type, GetEffectiveRate(type));
        }

        public void MultiplyRateMultiplier(ResourceType type, float factor)
        {
            rateMultipliers[type] *= factor;
            OnRateChanged?.Invoke(type, GetEffectiveRate(type));
        }

        public void AddResource(ResourceType type, float amount)
        {
            resourceCounts[type] = Mathf.Min(resourceCounts[type] + amount, MaxResource);
            if (amount > 0f)
            {
                totalProduced[type] += amount;
                OnTotalProducedChanged?.Invoke(type, totalProduced[type]);
            }
            OnResourceChanged?.Invoke(type, resourceCounts[type]);
        }

        public bool TrySpend(ResourceType type, float amount)
        {
            if (resourceCounts[type] >= amount)
            {
                resourceCounts[type] -= amount;
                OnResourceChanged?.Invoke(type, resourceCounts[type]);
                return true;
            }
            return false;
        }

        public bool CanAfford(ResourceType type, float amount)
        {
            return resourceCounts[type] >= amount;
        }

        public void SetRocketsUnlocked(bool unlocked)
        {
            rocketsUnlocked = unlocked;
        }

        public bool AreRocketsUnlocked()
        {
            return rocketsUnlocked;
        }

        // Save/Load support
        public void SetResourceCount(ResourceType type, float value)
        {
            resourceCounts[type] = value;
            OnResourceChanged?.Invoke(type, resourceCounts[type]);
        }

        public void SetTotalProduced(ResourceType type, float value)
        {
            totalProduced[type] = value;
            OnTotalProducedChanged?.Invoke(type, totalProduced[type]);
        }

        public void ResetForPrestige()
        {
            resourceCounts[ResourceType.Bullets] = startingBullets;
            resourceCounts[ResourceType.Rockets] = 0f;
            resourceCounts[ResourceType.Cash] = 0f;
            resourceRates[ResourceType.Bullets] = 0f;
            resourceRates[ResourceType.Rockets] = 0f;
            resourceRates[ResourceType.Cash] = 0f;
            rateMultipliers[ResourceType.Bullets] = 1f;
            rateMultipliers[ResourceType.Rockets] = 1f;
            rateMultipliers[ResourceType.Cash] = 1f;
            consumptionRates[ResourceType.Bullets] = 0f;
            consumptionRates[ResourceType.Rockets] = 0f;
            consumptionRates[ResourceType.Cash] = 0f;
            totalProduced[ResourceType.Bullets] = 0f;
            totalProduced[ResourceType.Rockets] = 0f;
            totalProduced[ResourceType.Cash] = 0f;
            rocketsUnlocked = false;

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                OnResourceChanged?.Invoke(type, resourceCounts[type]);
                OnRateChanged?.Invoke(type, 0f);
            }
        }
    }
}
