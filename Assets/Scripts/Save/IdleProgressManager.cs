using System;
using System.Collections;
using UnityEngine;

namespace ZombieBunker
{
    public class IdleProgressManager : MonoBehaviour
    {
        public static IdleProgressManager Instance { get; private set; }

        [SerializeField] private float maxIdleSeconds = 86400f; // 24 hours cap
        [SerializeField] private float idleEfficiency = 0.5f;   // 50% of normal rate while idle

        [Header("MP3 Juice — Welcome Back")]
        [SerializeField] private GameObject welcomeBackPanel;
        [SerializeField] private AudioSource bootUpAudioSource;
        [SerializeField] private AudioClip bootUpClip;

        public event Action<float, float, float> OnIdleProgressApplied; // bulletsGained, rocketsGained, cashGained

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ApplyIdleProgress(string lastPlayTimeStr)
        {
            if (string.IsNullOrEmpty(lastPlayTimeStr)) return;

            DateTime lastPlay;
            if (!DateTime.TryParse(lastPlayTimeStr, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out lastPlay))
                return;

            float elapsedSeconds = (float)(DateTime.UtcNow - lastPlay).TotalSeconds;
            if (elapsedSeconds <= 0f) return;

            elapsedSeconds = Mathf.Min(elapsedSeconds, maxIdleSeconds);
            float effectiveDt = elapsedSeconds * idleEfficiency;

            var rm = ResourceManager.Instance;
            if (rm == null) return;

            float bulletRate = rm.GetEffectiveRate(ResourceType.Bullets);
            float rocketRate = rm.AreRocketsUnlocked() ? rm.GetEffectiveRate(ResourceType.Rockets) : 0f;

            float bulletsGained = bulletRate * effectiveDt;
            float rocketsGained = rocketRate * effectiveDt;

            // Simplified cash from idle: based on turret rates
            float cashGained = 0f;
            var tm = TurretManager.Instance;
            if (tm != null)
            {
                float bulletCashRate = Mathf.Min(tm.GetBulletConsumptionRate(), bulletRate) *
                                       1f * tm.EfficiencyMultiplier;
                cashGained = bulletCashRate * effectiveDt;
            }

            rm.AddResource(ResourceType.Bullets, bulletsGained);
            rm.AddResource(ResourceType.Rockets, rocketsGained);
            rm.AddResource(ResourceType.Cash, cashGained);

            OnIdleProgressApplied?.Invoke(bulletsGained, rocketsGained, cashGained);

            // Welcome back juice
            if (welcomeBackPanel != null)
                StartCoroutine(ShowWelcomeBackPanel());

            if (bootUpAudioSource != null && bootUpClip != null)
                bootUpAudioSource.PlayOneShot(bootUpClip);
        }

        private IEnumerator ShowWelcomeBackPanel()
        {
            welcomeBackPanel.SetActive(true);
            welcomeBackPanel.transform.localScale = Vector3.zero;
            float scaleTarget = 1.1f;
            float k = 8f;
            float timer = 0f;
            while (timer < 0.2f)
            {
                float s = Mathf.Lerp(welcomeBackPanel.transform.localScale.x, scaleTarget, k * Time.deltaTime);
                welcomeBackPanel.transform.localScale = Vector3.one * s;
                timer += Time.deltaTime;
                yield return null;
            }
            scaleTarget = 1f;
            while (Mathf.Abs(welcomeBackPanel.transform.localScale.x - 1f) > 0.005f)
            {
                float s = Mathf.Lerp(welcomeBackPanel.transform.localScale.x, scaleTarget, k * Time.deltaTime);
                welcomeBackPanel.transform.localScale = Vector3.one * s;
                yield return null;
            }
            welcomeBackPanel.transform.localScale = Vector3.one;
        }
    }
}
