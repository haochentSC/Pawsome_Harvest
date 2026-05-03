using System.Collections.Generic;
using UnityEngine;

namespace LotteryMachine
{
    [CreateAssetMenu(fileName = "RewardPool", menuName = "Lottery Machine/Reward Pool")]
    public sealed class RewardPool : ScriptableObject
    {
        [SerializeField] private List<RewardDefinition> rewards = new();

        public IReadOnlyList<RewardDefinition> Rewards => rewards;

        public bool TryDraw(out RewardDefinition reward)
        {
            return TryDraw(Random.value, out reward);
        }

        public bool TryDraw(float normalizedRoll, out RewardDefinition reward)
        {
            reward = null;
            var totalWeight = GetTotalDrawableWeight();
            if (totalWeight <= 0f)
            {
                Debug.LogWarning($"Reward pool '{name}' has no drawable rewards.", this);
                return false;
            }

            var roll = Mathf.Clamp01(normalizedRoll) * totalWeight;
            var accumulated = 0f;

            for (var i = 0; i < rewards.Count; i++)
            {
                var candidate = rewards[i];
                if (candidate == null || !candidate.IsDrawable)
                {
                    continue;
                }

                accumulated += candidate.Weight;
                if (roll <= accumulated)
                {
                    reward = candidate;
                    return true;
                }
            }

            reward = GetLastDrawableReward();
            return reward != null;
        }

        public float GetTotalDrawableWeight()
        {
            var total = 0f;
            for (var i = 0; i < rewards.Count; i++)
            {
                var reward = rewards[i];
                if (reward != null && reward.IsDrawable)
                {
                    total += reward.Weight;
                }
            }

            return total;
        }

        private RewardDefinition GetLastDrawableReward()
        {
            for (var i = rewards.Count - 1; i >= 0; i--)
            {
                var reward = rewards[i];
                if (reward != null && reward.IsDrawable)
                {
                    return reward;
                }
            }

            return null;
        }
    }
}
