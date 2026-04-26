using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using TMPro;

namespace ZombieBunker
{
    public class IntrigueConsumptionManager : MonoBehaviour
    {
        public static IntrigueConsumptionManager Instance { get; private set; }

        [Serializable]
        public class IntrigueStage
        {
            [TextArea(2, 4)] public string responseText;
            public float resourceCost = 25f;
            public ResourceType costResource = ResourceType.Cash;
        }

        [Header("Intrigue Settings")]
        [SerializeField] private List<IntrigueStage> stages = new List<IntrigueStage>();
        [SerializeField] private string completionReward = "A mysterious blueprint...";
        [SerializeField] private float completionBonusAmount = 500f;
        [SerializeField] private ResourceType completionBonusResource = ResourceType.Cash;

        [Header("UI")]
        [SerializeField] private Button consumeButton;
        [SerializeField] private TextMeshProUGUI responseDisplay;
        [SerializeField] private TextMeshProUGUI costDisplay;
        [SerializeField] private CooldownTimer cooldownTimer;
        [SerializeField] private float cooldownDuration = 2f;
        [SerializeField] private GameObject completionVisual;

        [Header("MP3 Juice")]
        [SerializeField] private AudioSource transmissionAudioSource;
        [SerializeField] private AudioClip transmissionClip;
        [SerializeField] private AudioClip finalFanfareClip;
        [SerializeField] private ParticleSystem terminalExplosion;
        [SerializeField] private XRBaseController hapticController;

        private int currentStage = 0;
        private bool completed = false;

        public event Action<int, string> OnStageAdvanced;
        public event Action OnIntrigueCompleted;

        public int CurrentStage => currentStage;
        public bool IsCompleted => completed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (stages.Count == 0)
                SetupDefaultStages();
        }

        private void Start()
        {
            if (consumeButton != null)
                consumeButton.onClick.AddListener(OnConsumeClicked);
            if (completionVisual != null)
                completionVisual.SetActive(false);
            UpdateUI();
        }

        private void OnDestroy()
        {
            if (consumeButton != null)
                consumeButton.onClick.RemoveListener(OnConsumeClicked);
        }

        private void SetupDefaultStages()
        {
            stages.Add(new IntrigueStage
            {
                responseText = "You feed cash into the mysterious terminal...\n\"SIGNAL DETECTED...\"",
                resourceCost = 25f,
                costResource = ResourceType.Cash
            });
            stages.Add(new IntrigueStage
            {
                responseText = "The terminal flickers.\n\"DECRYPTING COORDINATES...\"",
                resourceCost = 50f,
                costResource = ResourceType.Cash
            });
            stages.Add(new IntrigueStage
            {
                responseText = "Static fills the screen.\n\"WARNING: UNKNOWN ENTITY APPROACHING...\"",
                resourceCost = 75f,
                costResource = ResourceType.Cash
            });
            stages.Add(new IntrigueStage
            {
                responseText = "The bunker shakes.\n\"TRANSMISSION COMPLETE. SUPPLY DROP INBOUND.\"",
                resourceCost = 100f,
                costResource = ResourceType.Cash
            });
        }

        private void OnConsumeClicked()
        {
            if (cooldownTimer != null && cooldownTimer.IsOnCooldown) return;
            TryConsume();
        }

        public bool TryConsume()
        {
            if (completed) return false;
            if (currentStage >= stages.Count) return false;

            var stage = stages[currentStage];
            if (!ResourceManager.Instance.TrySpend(stage.costResource, stage.resourceCost))
                return false;

            if (cooldownTimer != null)
                cooldownTimer.StartCooldown(cooldownDuration);

            if (responseDisplay != null)
            {
                responseDisplay.text = stage.responseText;
                StartCoroutine(EaseInText(responseDisplay.transform));
            }

            // Sound
            if (transmissionAudioSource != null && transmissionClip != null)
                transmissionAudioSource.PlayOneShot(transmissionClip);

            // Haptic
            if (hapticController != null)
                hapticController.SendHapticImpulse(0.4f, 1.0f);

            OnStageAdvanced?.Invoke(currentStage, stage.responseText);

            currentStage++;

            if (currentStage >= stages.Count)
            {
                completed = true;
                ResourceManager.Instance.AddResource(completionBonusResource, completionBonusAmount);
                if (completionVisual != null)
                    completionVisual.SetActive(true);
                if (responseDisplay != null)
                    responseDisplay.text += $"\n\n{completionReward}";

                // Final stage juice
                if (terminalExplosion != null) terminalExplosion.Play();
                if (transmissionAudioSource != null && finalFanfareClip != null)
                    transmissionAudioSource.PlayOneShot(finalFanfareClip);
                if (hapticController != null)
                    hapticController.SendHapticImpulse(0.8f, 1.0f);

                OnIntrigueCompleted?.Invoke();
            }

            UpdateUI();
            return true;
        }

        private void UpdateUI()
        {
            if (completed)
            {
                if (costDisplay != null) costDisplay.text = "COMPLETE";
                return;
            }

            if (currentStage < stages.Count && costDisplay != null)
            {
                var stage = stages[currentStage];
                costDisplay.text = $"Cost: {stage.resourceCost:F0} {stage.costResource}";
            }
        }

        private IEnumerator EaseInText(Transform t)
        {
            t.localScale = Vector3.zero;
            float k = 12f;
            while (Mathf.Abs(t.localScale.x - 1f) > 0.005f)
            {
                float s = Mathf.Lerp(t.localScale.x, 1f, k * Time.deltaTime);
                t.localScale = Vector3.one * s;
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        public void LoadStage(int stage)
        {
            currentStage = stage;
            if (currentStage >= stages.Count)
            {
                completed = true;
                if (completionVisual != null) completionVisual.SetActive(true);
            }
            UpdateUI();
        }
    }
}
