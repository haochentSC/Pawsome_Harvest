using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieBunker
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        [SerializeField] private string saveKey = "ZombieBunkerSave";

        [Header("Inter-session Return Effect")]
        [SerializeField] private ParticleSystem returnParticles;

        public event Action OnSaveCompleted;
        public event Action OnLoadCompleted;

        private bool disableSaving = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void DisableSave()
        {
            disableSaving = true;
        }

        public void SaveGame()
        {
            if (disableSaving) return;
            var data = new SaveData();

            // Resources
            var rm = ResourceManager.Instance;
            if (rm != null)
            {
                data.bullets = rm.GetResourceCount(ResourceType.Bullets);
                data.rockets = rm.GetResourceCount(ResourceType.Rockets);
                data.cash = rm.GetResourceCount(ResourceType.Cash);
                data.bulletRate = rm.GetBaseRate(ResourceType.Bullets);
                data.rocketRate = rm.GetBaseRate(ResourceType.Rockets);
                data.bulletMultiplier = rm.GetRateMultiplier(ResourceType.Bullets);
                data.rocketMultiplier = rm.GetRateMultiplier(ResourceType.Rockets);
                data.totalBulletsProduced = rm.GetTotalProduced(ResourceType.Bullets);
                data.totalRocketsProduced = rm.GetTotalProduced(ResourceType.Rockets);
                data.totalCashProduced = rm.GetTotalProduced(ResourceType.Cash);
                data.rocketsUnlocked = rm.AreRocketsUnlocked();
            }

            // Unlock (rocket room state is covered by rocketsUnlocked above)

            // Turret
            var tm = TurretManager.Instance;
            if (tm != null)
            {
                data.turretLevel = tm.TurretLevel;
                data.turretEfficiency = tm.EfficiencyMultiplier;
            }

            // Generators
            var gm = GeneratorManager.Instance;
            if (gm != null)
            {
                var bulletConfigs = gm.GetBulletGeneratorConfigs();
                var rocketConfigs = gm.GetRocketGeneratorConfigs();
                var allConfigs = new List<GeneratorConfig>();
                allConfigs.AddRange(bulletConfigs);
                allConfigs.AddRange(rocketConfigs);

                foreach (var config in allConfigs)
                {
                    int count = gm.GetCountForConfig(config);
                    if (count > 0)
                    {
                        data.generators.Add(new GeneratorSaveEntry
                        {
                            configName = config.name,
                            count = count,
                            powerUpMultiplier = 1f
                        });
                    }
                }

                data.bulletAccumulatedMultiplier = gm.GetAccumulatedMultiplier(ResourceType.Bullets);
                data.rocketAccumulatedMultiplier = gm.GetAccumulatedMultiplier(ResourceType.Rockets);
            }

            // PowerUps
            var pm = PowerUpManager.Instance;
            if (pm != null)
            {
                foreach (var kvp in pm.GetAllPurchasedPowerUps())
                {
                    data.powerUps.Add(new PowerUpSaveEntry
                    {
                        configName = kvp.Key.name,
                        count = kvp.Value
                    });
                }
            }

            // Achievements
            var am = AchievementManager.Instance;
            if (am != null)
            {
                data.achievementsUnlocked = am.GetUnlockedAchievementNames();
            }

            // Prestige
            var prestige = PrestigeManager.Instance;
            if (prestige != null)
            {
                data.prestigeCount = prestige.PrestigeCount;
                data.prestigeMultiplier = prestige.PrestigeMultiplier;
            }

            // Intrigue
            var intrigue = IntrigueConsumptionManager.Instance;
            if (intrigue != null)
            {
                data.intrigueStage = intrigue.CurrentStage;
            }

            data.lastPlayTime = DateTime.UtcNow.ToString("o");

            string json = JsonUtility.ToJson(data, true);
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();

            OnSaveCompleted?.Invoke();
        }

        public SaveData LoadGame()
        {
            if (!PlayerPrefs.HasKey(saveKey))
                return null;

            string json = PlayerPrefs.GetString(saveKey);
            var data = JsonUtility.FromJson<SaveData>(json);

            if (data == null) return null;

            // Resources
            var rm = ResourceManager.Instance;
            if (rm != null)
            {
                rm.SetResourceCount(ResourceType.Bullets, data.bullets);
                rm.SetResourceCount(ResourceType.Rockets, data.rockets);
                rm.SetResourceCount(ResourceType.Cash, data.cash);
                rm.SetBaseRate(ResourceType.Bullets, data.bulletRate);
                rm.SetBaseRate(ResourceType.Rockets, data.rocketRate);
                rm.SetRateMultiplier(ResourceType.Bullets, data.bulletMultiplier);
                rm.SetRateMultiplier(ResourceType.Rockets, data.rocketMultiplier);
                rm.SetTotalProduced(ResourceType.Bullets, data.totalBulletsProduced);
                rm.SetTotalProduced(ResourceType.Rockets, data.totalRocketsProduced);
                rm.SetTotalProduced(ResourceType.Cash, data.totalCashProduced);
                rm.SetRocketsUnlocked(data.rocketsUnlocked);
            }

            // Unlock
            var um = UnlockManager.Instance;
            if (um != null)
            {
                if (data.rocketsUnlocked) um.SetRocketRoomUnlocked(true);
            }

            // Turret
            var tm = TurretManager.Instance;
            if (tm != null)
            {
                for (int i = 0; i < data.turretLevel; i++)
                    tm.UpgradeTurret();
                tm.SetEfficiencyMultiplier(data.turretEfficiency);
            }

            // Generators — find slots and activate saved generators
            var gm2 = GeneratorManager.Instance;
            if (gm2 != null)
            {
                // Restore accumulated power-up multipliers BEFORE activating generators
                // so that ActivateGeneratorFree applies the correct multiplier
                gm2.SetAccumulatedMultiplier(ResourceType.Bullets, data.bulletAccumulatedMultiplier);
                gm2.SetAccumulatedMultiplier(ResourceType.Rockets, data.rocketAccumulatedMultiplier);

                var allSlots = FindObjectsByType<GeneratorSlot>(FindObjectsSortMode.None);
                var allConfigs = new List<GeneratorConfig>();
                allConfigs.AddRange(gm2.GetBulletGeneratorConfigs());
                allConfigs.AddRange(gm2.GetRocketGeneratorConfigs());

                foreach (var entry in data.generators)
                {
                    GeneratorConfig config = allConfigs.Find(c => c.name == entry.configName);
                    if (config == null) continue;

                    for (int i = 0; i < entry.count; i++)
                    {
                        GeneratorSlot availableSlot = null;
                        foreach (var slot in allSlots)
                        {
                            if (!slot.IsActive && slot.GetConfig() == config)
                            {
                                availableSlot = slot;
                                break;
                            }
                        }
                        if (availableSlot != null)
                        {
                            gm2.ActivateGeneratorFree(config, availableSlot);
                        }
                    }
                }
            }

            // Power-ups — restore purchase counts so shop shows them as purchased
            var pm = PowerUpManager.Instance;
            if (pm != null && data.powerUps.Count > 0)
            {
                foreach (var entry in data.powerUps)
                {
                    foreach (var config in pm.AvailablePowerUps)
                    {
                        if (config.name == entry.configName)
                        {
                            pm.RestorePurchaseCount(config, entry.count);
                            break;
                        }
                    }
                }
            }

            // Prestige
            var prestige = PrestigeManager.Instance;
            if (prestige != null)
            {
                prestige.LoadPrestigeState(data.prestigeCount, data.prestigeMultiplier);
            }

            // Intrigue
            var intrigue = IntrigueConsumptionManager.Instance;
            if (intrigue != null)
            {
                intrigue.LoadStage(data.intrigueStage);
            }

            // Play return effect at the player's spawn position on every inter-session load.
            if (returnParticles != null)
                StartCoroutine(PlayReturnEffectAtCamera());

            OnLoadCompleted?.Invoke();
            return data;
        }

        public bool HasSaveData()
        {
            return PlayerPrefs.HasKey(saveKey);
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(saveKey);
        }

        public string GetLastPlayTime()
        {
            if (!HasSaveData()) return null;
            string json = PlayerPrefs.GetString(saveKey);
            var data = JsonUtility.FromJson<SaveData>(json);
            return data?.lastPlayTime;
        }

        private IEnumerator PlayReturnEffectAtCamera()
        {
            // Wait one frame so the XR rig has finished its first update.
            yield return null;

            Camera vrCam = Camera.main;
            if (vrCam == null)
            {
                foreach (Camera c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if (c.enabled && c.gameObject.activeInHierarchy) { vrCam = c; break; }
                }
            }

            if (vrCam != null)
                returnParticles.transform.position = vrCam.transform.position;

            returnParticles.Play();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }
    }
}
