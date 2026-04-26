using System;
using System.Collections.Generic;

namespace ZombieBunker
{
    [Serializable]
    public class SaveData
    {
        public float bullets;
        public float rockets;
        public float cash;
        public float bulletRate;
        public float rocketRate;
        public float bulletMultiplier;
        public float rocketMultiplier;
        public float totalBulletsProduced;
        public float totalRocketsProduced;
        public float totalCashProduced;

        public bool rocketsUnlocked;
        public bool secondRoomUnlocked;

        public int turretLevel;
        public float turretEfficiency;

        public List<GeneratorSaveEntry> generators = new List<GeneratorSaveEntry>();
        public List<PowerUpSaveEntry> powerUps = new List<PowerUpSaveEntry>();
        public List<string> achievementsUnlocked = new List<string>();
        public List<string> tutorialsShown = new List<string>();
        public List<string> revealedEntries = new List<string>();

        public float bulletAccumulatedMultiplier = 1f;
        public float rocketAccumulatedMultiplier = 1f;

        public int prestigeCount;
        public float prestigeMultiplier;

        public string lastPlayTime;
        public int intrigueStage;
    }

    [Serializable]
    public class GeneratorSaveEntry
    {
        public string configName;
        public int count;
        public float powerUpMultiplier;
    }

    [Serializable]
    public class PowerUpSaveEntry
    {
        public string configName;
        public int count;
    }
}
