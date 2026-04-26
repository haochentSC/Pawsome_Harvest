using UnityEngine;

namespace ZombieBunker
{
    [CreateAssetMenu(fileName = "AchievementConfig", menuName = "ZombieBunker/Achievement Config")]
    public class AchievementConfig : ScriptableObject
    {
        public string achievementName;
        [TextArea] public string description;
        public AchievementCondition condition;
        public ResourceType trackedResource;
        public float threshold;
        public int generatorCountThreshold;
        public bool requiresRocketUnlock;
        public GameObject trophyPrefab;
    }

    public enum AchievementCondition
    {
        ResourceTotal,
        GeneratorCount,
        RocketUnlock,
        PrestigeCount
    }
}
