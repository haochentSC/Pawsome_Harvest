using System;
using UnityEngine.Events;

namespace LotteryMachine
{
    [Serializable]
    public sealed class RewardResultEvent : UnityEvent<RewardResult>
    {
    }
}
