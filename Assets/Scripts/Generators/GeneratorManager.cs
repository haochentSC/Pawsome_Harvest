using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZombieBunker
{
    public class GeneratorManager : MonoBehaviour
    {
        public static GeneratorManager Instance { get; private set; }

        [Header("Generator Configs")]
        [SerializeField] private List<GeneratorConfig> bulletGeneratorConfigs = new List<GeneratorConfig>();
        [SerializeField] private List<GeneratorConfig> rocketGeneratorConfigs = new List<GeneratorConfig>();

        private List<Generator> activeGenerators = new List<Generator>();
        private Dictionary<GeneratorConfig, int> generatorCounts = new Dictionary<GeneratorConfig, int>();
        private Dictionary<ResourceType, float> accumulatedMultipliers = new Dictionary<ResourceType, float>();

        public event Action<Generator> OnGeneratorPlaced;
        public event Action<int> OnGeneratorCountChanged;

        public IReadOnlyList<Generator> ActiveGenerators => activeGenerators;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public float GetCurrentCost(GeneratorConfig config)
        {
            int count = GetGeneratorCount(config);
            return config.baseCost * Mathf.Pow(config.costMultiplier, count);
        }

        public int GetGeneratorCount(GeneratorConfig config)
        {
            return generatorCounts.TryGetValue(config, out int count) ? count : 0;
        }

        public int GetTotalGeneratorCount()
        {
            return activeGenerators.Count;
        }

        public bool CanAffordGenerator(GeneratorConfig config)
        {
            float cost = GetCurrentCost(config);
            return ResourceManager.Instance.CanAfford(ResourceType.Cash, cost);
        }

        public Generator ActivateGenerator(GeneratorConfig config, GeneratorSlot slot)
        {
            if (!CanAffordGenerator(config)) return null;

            Generator generator = slot.Activate();
            if (generator == null) return null;

            float cost = GetCurrentCost(config);
            ResourceManager.Instance.TrySpend(ResourceType.Cash, cost);

            // Apply any accumulated power-up multiplier to the new generator
            if (accumulatedMultipliers.TryGetValue(config.resourceType, out float accMult) && accMult != 1f)
                generator.SetPowerUpMultiplier(accMult);

            activeGenerators.Add(generator);
            if (!generatorCounts.ContainsKey(config))
                generatorCounts[config] = 0;
            generatorCounts[config]++;

            RecalculateRates();

            OnGeneratorPlaced?.Invoke(generator);
            OnGeneratorCountChanged?.Invoke(activeGenerators.Count);

            slot.NotifyEaseAnimator(activeGenerators.Count);

            return generator;
        }

        /// <summary>
        /// Activates a generator slot without deducting cost. Used for loading saved state.
        /// </summary>
        public Generator ActivateGeneratorFree(GeneratorConfig config, GeneratorSlot slot)
        {
            Generator generator = slot.Activate();
            if (generator == null) return null;

            // Apply any accumulated power-up multiplier to the new generator
            if (accumulatedMultipliers.TryGetValue(config.resourceType, out float accMult) && accMult != 1f)
                generator.SetPowerUpMultiplier(accMult);

            activeGenerators.Add(generator);
            if (!generatorCounts.ContainsKey(config))
                generatorCounts[config] = 0;
            generatorCounts[config]++;

            RecalculateRates();

            OnGeneratorPlaced?.Invoke(generator);
            OnGeneratorCountChanged?.Invoke(activeGenerators.Count);

            return generator;
        }

        public void RecalculateRates()
        {
            float bulletRate = 0f;
            float rocketRate = 0f;

            foreach (var gen in activeGenerators)
            {
                if (gen.ResourceType == ResourceType.Bullets)
                    bulletRate += gen.EffectiveRate;
                else if (gen.ResourceType == ResourceType.Rockets)
                    rocketRate += gen.EffectiveRate;
            }

            ResourceManager.Instance.SetBaseRate(ResourceType.Bullets, bulletRate);
            ResourceManager.Instance.SetBaseRate(ResourceType.Rockets, rocketRate);
        }

        public void ApplyMultiplierToAll(ResourceType resourceType, float multiplier)
        {
            if (!accumulatedMultipliers.ContainsKey(resourceType))
                accumulatedMultipliers[resourceType] = 1f;
            accumulatedMultipliers[resourceType] *= multiplier;

            foreach (var gen in activeGenerators)
            {
                if (gen.ResourceType == resourceType)
                {
                    gen.ApplyPowerUpMultiplier(multiplier);
                }
            }
            RecalculateRates();
        }

        public void ApplyMultiplierToTier(GeneratorTier tier, float multiplier)
        {
            foreach (var gen in activeGenerators)
            {
                if (gen.Tier == tier)
                {
                    gen.ApplyPowerUpMultiplier(multiplier);
                }
            }
            RecalculateRates();
        }

        public void ApplyTemporaryMultiplier(float multiplier)
        {
            foreach (var gen in activeGenerators)
            {
                gen.ApplyPowerUpMultiplier(multiplier);
            }
            RecalculateRates();
        }

        public void RemoveTemporaryMultiplier(float multiplier)
        {
            float inverse = 1f / multiplier;
            foreach (var gen in activeGenerators)
            {
                gen.ApplyPowerUpMultiplier(inverse);
            }
            RecalculateRates();
        }

        public List<GeneratorConfig> GetBulletGeneratorConfigs() => bulletGeneratorConfigs;
        public List<GeneratorConfig> GetRocketGeneratorConfigs() => rocketGeneratorConfigs;

        public void ClearAllGenerators()
        {
            activeGenerators.Clear();
            generatorCounts.Clear();
            accumulatedMultipliers.Clear();
            RecalculateRates();
            OnGeneratorCountChanged?.Invoke(0);
        }

        public int GetCountForConfig(GeneratorConfig config)
        {
            return GetGeneratorCount(config);
        }

        public float GetAccumulatedMultiplier(ResourceType type)
        {
            return accumulatedMultipliers.TryGetValue(type, out float mult) ? mult : 1f;
        }

        public void SetAccumulatedMultiplier(ResourceType type, float multiplier)
        {
            accumulatedMultipliers[type] = multiplier;
        }
    }
}
