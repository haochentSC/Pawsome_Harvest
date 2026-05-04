using UnityEngine;

namespace LotteryMachine
{
    public sealed class RewardCardInstance : MonoBehaviour
    {
        [SerializeField] private RewardDefinition reward;
        [SerializeField] private string rewardId;

        public RewardDefinition Reward => reward;
        public string RewardId => rewardId;
        public string DisplayName => reward != null ? reward.DisplayName : string.Empty;

        public void Initialize(RewardDefinition rewardDefinition)
        {
            reward = rewardDefinition;
            rewardId = rewardDefinition != null ? rewardDefinition.RewardId : string.Empty;
        }

        private void OnValidate()
        {
            rewardId = reward != null ? reward.RewardId : rewardId;
        }
    }
}
