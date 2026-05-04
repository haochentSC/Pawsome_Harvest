using UnityEngine;

namespace LotteryMachine
{
    [CreateAssetMenu(fileName = "RewardDefinition", menuName = "Lottery Machine/Reward Definition")]
    public sealed class RewardDefinition : ScriptableObject
    {
        [SerializeField] private string rewardId;
        [SerializeField] private string displayName;
        [SerializeField] private RewardRarity rarity = RewardRarity.Common;
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField] private Sprite cardArt;
        [SerializeField] private Material cardMaterial;
        [SerializeField] private GameObject rewardPrefab;

        public string RewardId => rewardId;
        public string DisplayName => displayName;
        public RewardRarity Rarity => rarity;
        public float Weight => weight;
        public Sprite CardArt => cardArt;
        public Material CardMaterial => cardMaterial;
        public GameObject RewardPrefab => rewardPrefab;

        public bool IsDrawable => !string.IsNullOrWhiteSpace(rewardId) && weight > 0f;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }

            if (string.IsNullOrWhiteSpace(rewardId))
            {
                rewardId = name.ToLowerInvariant().Replace(" ", "_");
            }
        }
    }
}
