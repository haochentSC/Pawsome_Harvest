using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZombieBunker
{
    public class LevelUpRevealManager : MonoBehaviour
    {
        public static LevelUpRevealManager Instance { get; private set; }

        [Serializable]
        public class RevealEntry
        {
            public string label;
            public float bulletThreshold;
            public GameObject lockedVisual;
            public GameObject unlockedVisual;
            [HideInInspector] public bool revealed;
        }

        [SerializeField] private List<RevealEntry> revealEntries = new List<RevealEntry>();

        [Header("MP3 Juice")]
        [SerializeField] private AudioSource revealAudioSource;
        [SerializeField] private AudioClip revealFanfare;
        [SerializeField] private ParticleSystem goldSparkles;

        public event Action<RevealEntry> OnEntryRevealed;

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
            foreach (var entry in revealEntries)
            {
                if (!entry.revealed)
                {
                    if (entry.lockedVisual != null) entry.lockedVisual.SetActive(true);
                    if (entry.unlockedVisual != null) entry.unlockedVisual.SetActive(false);
                }
            }

            if (ResourceManager.Instance != null)
                ResourceManager.Instance.OnTotalProducedChanged += CheckRevealThresholds;
        }

        private void OnDestroy()
        {
            if (ResourceManager.Instance != null)
                ResourceManager.Instance.OnTotalProducedChanged -= CheckRevealThresholds;
        }

        private void CheckRevealThresholds(ResourceType type, float total)
        {
            if (type != ResourceType.Bullets) return;

            foreach (var entry in revealEntries)
            {
                if (entry.revealed) continue;
                if (total >= entry.bulletThreshold)
                {
                    Reveal(entry);
                }
            }
        }

        private void Reveal(RevealEntry entry)
        {
            entry.revealed = true;

            // Fade out the locked visual
            if (entry.lockedVisual != null)
                StartCoroutine(FadeOutAndDisable(entry.lockedVisual));

            if (entry.unlockedVisual != null)
                entry.unlockedVisual.SetActive(true);

            if (revealAudioSource != null && revealFanfare != null)
                revealAudioSource.PlayOneShot(revealFanfare);

            if (goldSparkles != null)
                goldSparkles.Play();

            OnEntryRevealed?.Invoke(entry);
        }

        private IEnumerator FadeOutAndDisable(GameObject go)
        {
            var canvasGroup = go.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                go.SetActive(false);
                yield break;
            }

            float duration = 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / duration);
                yield return null;
            }
            go.SetActive(false);
            canvasGroup.alpha = 1f;
        }

        public void ResetAll()
        {
            foreach (var entry in revealEntries)
            {
                entry.revealed = false;
                if (entry.lockedVisual != null) entry.lockedVisual.SetActive(true);
                if (entry.unlockedVisual != null) entry.unlockedVisual.SetActive(false);
            }
        }

        public List<RevealEntry> GetEntries() => revealEntries;
    }
}
