using UnityEngine;

namespace ZombieBunker
{
    [CreateAssetMenu(fileName = "PowerUpConfig", menuName = "ZombieBunker/PowerUp Config")]
    public class PowerUpConfig : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public float cost = 50f;
        public float multiplier = 1.5f;
        public ResourceType costResource = ResourceType.Cash;
        public PowerUpTarget target;
        public ResourceType affectedResourceType;
        public GameObject visualPrefab;
    }

    public enum PowerUpTarget
    {
        AllGeneratorsOfType,
        AllTurrets,
        SpecificGeneratorTier
    }
}
