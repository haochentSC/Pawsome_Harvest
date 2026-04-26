using UnityEngine;

namespace ZombieBunker
{
    public class Generator : MonoBehaviour
    {
        [SerializeField] private GeneratorConfig config;

        private float currentRate;
        private float powerUpMultiplier = 1f;

        public GeneratorConfig Config => config;
        public float CurrentRate => currentRate;
        public float EffectiveRate => currentRate * powerUpMultiplier;
        public ResourceType ResourceType => config.resourceType;
        public GeneratorTier Tier => config.tier;

        public void Initialize(GeneratorConfig generatorConfig)
        {
            config = generatorConfig;
            currentRate = config.baseRate;
        }

        public void ApplyPowerUpMultiplier(float multiplier)
        {
            powerUpMultiplier *= multiplier;
        }

        public void SetPowerUpMultiplier(float multiplier)
        {
            powerUpMultiplier = multiplier;
        }

        public float GetPowerUpMultiplier()
        {
            return powerUpMultiplier;
        }

        public void ResetMultiplier()
        {
            powerUpMultiplier = 1f;
        }
    }
}
