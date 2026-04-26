using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieBunker
{
    public class SurpriseSurgeManager : MonoBehaviour
    {
        public static SurpriseSurgeManager Instance { get; private set; }

        [Header("Surge Settings")]
        [SerializeField] private float initialDelay = 90f;
        [SerializeField] private float minInterval = 60f;
        [SerializeField] private float maxInterval = 180f;
        [SerializeField] private float surgeLifetime = 10f;
        [SerializeField] private float surgeMultiplier = 2f;
        [SerializeField] private float surgeDuration = 30f;

        [Header("Surge Crate")]
        [SerializeField] private GameObject surgeCratePrefab;
        [SerializeField] private Transform[] spawnPoints;

        [Header("MP3 Juice")]
        [SerializeField] private ParticleSystem grabExplosion;
        [SerializeField] private AudioSource grabAudioSource;
        [SerializeField] private AudioClip grabClip;
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.XRBaseController hapticController;
        [SerializeField] private EyeTracker eyeTracker;
        [SerializeField] private SurgeIndicatorUI surgeIndicatorUI;

        private bool surgeActive = false;
        private GameObject currentCrate;
        private Coroutine spawnCoroutine;

        public event Action OnSurgeCollected;
        public event Action OnSurgeExpired;
        public event Action OnSurgeEffectEnded;

        public bool IsSurgeActive => surgeActive;

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
            if (surgeCratePrefab == null)
                Debug.LogWarning("SurpriseSurgeManager: surgeCratePrefab is not assigned! Surges will not spawn.", this);
            if (spawnPoints == null || spawnPoints.Length == 0)
                Debug.LogWarning("SurpriseSurgeManager: spawnPoints are not assigned! Surges will not spawn.", this);

            spawnCoroutine = StartCoroutine(SurgeSpawnLoop());
        }

        private IEnumerator SurgeSpawnLoop()
        {
            yield return new WaitForSeconds(initialDelay);

            if (!surgeActive)
                SpawnSurgeCrate();

            while (true)
            {
                float interval = UnityEngine.Random.Range(minInterval, maxInterval);
                yield return new WaitForSeconds(interval);

                if (!surgeActive)
                    SpawnSurgeCrate();
            }
        }

        private void SpawnSurgeCrate()
        {
            if (surgeCratePrefab == null || spawnPoints == null || spawnPoints.Length == 0)
                return;

            Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            currentCrate = Instantiate(surgeCratePrefab, spawnPoint.position, spawnPoint.rotation);

            var button = currentCrate.GetComponentInChildren<Button>();
            if (button != null)
                button.onClick.AddListener(OnCrateClicked);

            StartCoroutine(CrateLifetime());
        }

        private IEnumerator CrateLifetime()
        {
            yield return new WaitForSeconds(surgeLifetime);

            if (currentCrate != null)
            {
                Destroy(currentCrate);
                currentCrate = null;
                OnSurgeExpired?.Invoke();
            }
        }

        private void OnCrateClicked()
        {
            if (currentCrate != null)
            {
                if (grabExplosion != null)
                    grabExplosion.transform.position = currentCrate.transform.position;

                Destroy(currentCrate);
                currentCrate = null;
            }

            if (grabExplosion != null) grabExplosion.Play();
            if (grabAudioSource != null && grabClip != null) grabAudioSource.PlayOneShot(grabClip);
            if (hapticController != null) hapticController.SendHapticImpulse(0.9f, 1.0f);
            if (eyeTracker != null) { eyeTracker.TriggerSurprise(); eyeTracker.SetMood("smile"); }

            StartCoroutine(ApplySurgeEffect());
            OnSurgeCollected?.Invoke();
        }

        public void CollectSurge(GameObject crate)
        {
            if (crate != currentCrate)
                return;

            if (grabExplosion != null)
                grabExplosion.transform.position = currentCrate.transform.position;

            Destroy(currentCrate);
            currentCrate = null;

            if (grabExplosion != null) grabExplosion.Play();
            if (grabAudioSource != null && grabClip != null) grabAudioSource.PlayOneShot(grabClip);
            if (hapticController != null) hapticController.SendHapticImpulse(0.9f, 1.0f);
            if (eyeTracker != null) { eyeTracker.TriggerSurprise(); eyeTracker.SetMood("smile"); }

            StartCoroutine(ApplySurgeEffect());
            OnSurgeCollected?.Invoke();

            // Show popup about the surge
            string surgeMessage = $"Surge Collected! Multiplier: x{surgeMultiplier} for {surgeDuration} seconds";
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.ShowPopupMessage(surgeMessage);
        }

        private IEnumerator ApplySurgeEffect()
        {
            surgeActive = true;
            GeneratorManager.Instance.ApplyTemporaryMultiplier(surgeMultiplier);

            yield return new WaitForSeconds(surgeDuration);

            GeneratorManager.Instance.RemoveTemporaryMultiplier(surgeMultiplier);
            surgeActive = false;
            OnSurgeEffectEnded?.Invoke();
        }

        private void OnDestroy()
        {
            if (spawnCoroutine != null)
                StopCoroutine(spawnCoroutine);
        }
    }
}
