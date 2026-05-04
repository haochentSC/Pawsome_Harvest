using UnityEngine;

namespace LotteryMachine
{
    public sealed class RewardResultLogger : MonoBehaviour
    {
        public void LogReward(RewardResult result)
        {
            Debug.Log($"Lottery reward won: {result.DisplayName} ({result.Rarity})", result.SpawnedObject);
        }
    }
}
