using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZombieBunker
{
    public class PrestigeManager : MonoBehaviour
    {
        public static PrestigeManager Instance { get; private set; }

        [Header("Prestige Settings")]
        [SerializeField] private float prestigeMultiplierPerTier = 1.5f;
        [SerializeField] private Button prestigeButton;

        [Header("Prestige Costs")]
        [SerializeField] private float baseBulletCost = 5000f;
        [SerializeField] private float baseCashCost = 5000f;
        [SerializeField] private float baseRocketCost = 100f;
        [SerializeField] private float costScalePerLevel = 1.15f;

        [Header("UI")]
        [SerializeField] private GameObject prestigeUI;

        [Header("MP3 Juice — Scene-Wide")]
        [SerializeField] private CameraShaker cameraShaker;
        [SerializeField] private ParticleSystem prestigeParticles;
        [SerializeField] private Light[] sceneLights;
        [SerializeField] private AudioSource prestigeAudioSource;
        [SerializeField] private AudioClip prestigeResetClip;
        [SerializeField] private XRBaseController leftController;
        [SerializeField] private XRBaseController rightController;
        [SerializeField] private EyeTracker eyeTracker;

        private int prestigeCount = 0;
        private float prestigeMultiplier = 1f;

        public event Action<int> OnPrestigeActivated;

        public int PrestigeCount => prestigeCount;
        public float PrestigeMultiplier => prestigeMultiplier;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (prestigeButton != null)
                prestigeButton.onClick.AddListener(OnPrestigeClicked);
        }

        private void OnDestroy()
        {
            if (prestigeButton != null)
                prestigeButton.onClick.RemoveListener(OnPrestigeClicked);
        }

        private void OnPrestigeClicked()
        {
            TryPrestige();
        }

        public float GetCurrentBulletCost() => baseBulletCost * Mathf.Pow(costScalePerLevel, prestigeCount);
        public float GetCurrentCashCost() => baseCashCost * Mathf.Pow(costScalePerLevel, prestigeCount);
        public float GetCurrentRocketCost() => baseRocketCost * Mathf.Pow(costScalePerLevel, prestigeCount);

        public bool CanPrestige()
        {
            if (ResourceManager.Instance == null) return false;
            return ResourceManager.Instance.CanAfford(ResourceType.Bullets, GetCurrentBulletCost())
                && ResourceManager.Instance.CanAfford(ResourceType.Cash, GetCurrentCashCost())
                && ResourceManager.Instance.CanAfford(ResourceType.Rockets, GetCurrentRocketCost());
        }

        public bool TryPrestige()
        {
            if (!CanPrestige()) return false;

            // Scene-wide juice — frown before reset
            if (eyeTracker != null) eyeTracker.SetMood("frown");

            var shaker = cameraShaker != null ? cameraShaker : CameraShaker.Instance;
            if (shaker != null)
                shaker.Shake(2.0f, 0.3f);

            if (prestigeParticles != null) prestigeParticles.Play();
            if (prestigeAudioSource != null && prestigeResetClip != null)
                prestigeAudioSource.PlayOneShot(prestigeResetClip);
            if (leftController != null) leftController.SendHapticImpulse(1.0f, 1.0f);
            if (rightController != null) rightController.SendHapticImpulse(1.0f, 1.0f);
            if (sceneLights != null && sceneLights.Length > 0)
                StartCoroutine(FlickerLights());

            // Spend prestige costs before reset
            ResourceManager.Instance.TrySpend(ResourceType.Bullets, GetCurrentBulletCost());
            ResourceManager.Instance.TrySpend(ResourceType.Cash, GetCurrentCashCost());
            ResourceManager.Instance.TrySpend(ResourceType.Rockets, GetCurrentRocketCost());

            prestigeCount++;
            prestigeMultiplier = Mathf.Pow(prestigeMultiplierPerTier, prestigeCount);

            // Reset all systems
            ResourceManager.Instance.ResetForPrestige();
            GeneratorManager.Instance.ClearAllGenerators();
            PowerUpManager.Instance.ClearAllPowerUps();
            TurretManager.Instance.ResetForPrestige();
            UnlockManager.Instance.ResetForPrestige();

            var levelReveal = LevelUpRevealManager.Instance;
            if (levelReveal != null) levelReveal.ResetAll();

            var tutorial = TutorialManager.Instance;
            if (tutorial != null) tutorial.ResetAllTutorials();

            // Reset generator slots
            var slots = FindObjectsByType<GeneratorSlot>(FindObjectsSortMode.None);
            foreach (var slot in slots)
                slot.ResetSlot();

            // Apply prestige multiplier to all future generation
            ResourceManager.Instance.MultiplyRateMultiplier(ResourceType.Bullets, prestigeMultiplier);
            ResourceManager.Instance.MultiplyRateMultiplier(ResourceType.Rockets, prestigeMultiplier);

            AchievementManager.Instance?.CheckPrestigeAchievements(prestigeCount);

            // Reset mood after prestige
            if (eyeTracker != null) StartCoroutine(ResetEyeMoodDelay(1.5f));

            OnPrestigeActivated?.Invoke(prestigeCount);
            return true;
        }

        private IEnumerator FlickerLights()
        {
            float[] origIntensities = new float[sceneLights.Length];
            for (int i = 0; i < sceneLights.Length; i++)
                origIntensities[i] = sceneLights[i].intensity;

            for (int flicker = 0; flicker < 3; flicker++)
            {
                foreach (var light in sceneLights)
                    if (light != null) light.intensity = 0f;
                yield return new WaitForSeconds(0.08f);
                for (int i = 0; i < sceneLights.Length; i++)
                    if (sceneLights[i] != null) sceneLights[i].intensity = origIntensities[i];
                yield return new WaitForSeconds(0.1f);
            }
        }

        private IEnumerator ResetEyeMoodDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (eyeTracker != null) eyeTracker.SetMood("neutral");
        }

        public void LoadPrestigeState(int count, float multiplier)
        {
            prestigeCount = count;
            prestigeMultiplier = multiplier;

            if (prestigeMultiplier > 1f)
            {
                ResourceManager.Instance.MultiplyRateMultiplier(ResourceType.Bullets, prestigeMultiplier);
                ResourceManager.Instance.MultiplyRateMultiplier(ResourceType.Rockets, prestigeMultiplier);
            }
        }
    }
}
