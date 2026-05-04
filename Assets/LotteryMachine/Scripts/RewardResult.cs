using System;
using UnityEngine;

namespace LotteryMachine
{
    [Serializable]
    public struct RewardResult
    {
        [SerializeField] private RewardDefinition reward;
        [SerializeField] private string rewardId;
        [SerializeField] private string displayName;
        [SerializeField] private RewardRarity rarity;
        [SerializeField] private GameObject spawnedObject;
        [SerializeField] private int drawIndex;

        public RewardResult(RewardDefinition reward, GameObject spawnedObject, int drawIndex)
        {
            this.reward = reward;
            rewardId = reward != null ? reward.RewardId : string.Empty;
            displayName = reward != null ? reward.DisplayName : string.Empty;
            rarity = reward != null ? reward.Rarity : RewardRarity.Common;
            this.spawnedObject = spawnedObject;
            this.drawIndex = drawIndex;
        }

        public RewardDefinition Reward => reward;
        public string RewardId => rewardId;
        public string DisplayName => displayName;
        public RewardRarity Rarity => rarity;
        public GameObject SpawnedObject => spawnedObject;
        public int DrawIndex => drawIndex;
    }
}
