using UnityEngine;

namespace ZombieBunker
{
    [CreateAssetMenu(fileName = "GeneratorConfig", menuName = "ZombieBunker/Generator Config")]
    public class GeneratorConfig : ScriptableObject
    {
        public string displayName;
        public GeneratorTier tier;
        public ResourceType resourceType;
        public float baseRate = 1f;
        public float baseCost = 10f;
        public float costMultiplier = 1.15f;
        [Tooltip("Bullet count threshold to reveal this generator tier")]
        public float revealThreshold = 0f;
        public GameObject meshPrefab;
    }
}
