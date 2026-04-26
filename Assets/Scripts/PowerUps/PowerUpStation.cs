using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

namespace ZombieBunker
{
    public class PowerUpStation : MonoBehaviour
    {
        [SerializeField] private PowerUpConfig powerUpConfig;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private CooldownTimer cooldownTimer;
        [SerializeField] private float cooldownDuration = 1f;
        [SerializeField] private GameObject visualSpawnPoint;
        [SerializeField] private Button buyButton;

        [Header("MP3 Juice — Haptics")]
        [SerializeField] private HapticOnPurchase hapticFeedback;
        [SerializeField] private bool isExpensivePowerUp = false;
        [SerializeField] private XRBaseController defaultController;

        [Header("MP3 Juice — Sound")]
        [SerializeField] private AudioSource purchaseAudioSource;
        [SerializeField] private AudioClip purchaseClip;

        [Header("MP3 Juice — Eyes")]
        [SerializeField] private EyeTracker eyeTracker;

        private void OnEnable()
        {
            if (buyButton != null)
                buyButton.onClick.AddListener(OnBuyClicked);
            UpdateUI();
        }

        private void OnDisable()
        {
            if (buyButton != null)
                buyButton.onClick.RemoveListener(OnBuyClicked);
        }

        private void OnBuyClicked()
        {
            if (cooldownTimer != null && cooldownTimer.IsOnCooldown) return;
            TryPurchase();
        }

        public void TryPurchase()
        {
            if (powerUpConfig == null) return;

            if (PowerUpManager.Instance.TryPurchasePowerUp(powerUpConfig))
            {
                if (cooldownTimer != null)
                    cooldownTimer.StartCooldown(cooldownDuration);

                if (powerUpConfig.visualPrefab != null && visualSpawnPoint != null)
                {
                    Instantiate(powerUpConfig.visualPrefab, visualSpawnPoint.transform.position,
                        Quaternion.identity, visualSpawnPoint.transform);
                }

                // Haptic feedback
                if (hapticFeedback != null && defaultController != null)
                {
                    hapticFeedback.IsHeavyPurchase = isExpensivePowerUp;
                    hapticFeedback.TriggerHaptic(defaultController);
                }

                // Sound feedback
                if (purchaseAudioSource != null && purchaseClip != null)
                    purchaseAudioSource.PlayOneShot(purchaseClip);

                // Face expression
                if (eyeTracker != null)
                    eyeTracker.SetMoodThenReset("smile", 1.5f);

                UpdateUI();
            }
            else
            {
                // Failed purchase — frown
                if (eyeTracker != null)
                    eyeTracker.SetMoodThenReset("frown", 1f);
            }
        }

        private void UpdateUI()
        {
            if (powerUpConfig == null) return;
            if (nameText != null) nameText.text = powerUpConfig.displayName;
            if (costText != null) costText.text = $"Cost: {powerUpConfig.cost} {powerUpConfig.costResource}";
            if (descriptionText != null) descriptionText.text = powerUpConfig.description;
        }
    }
}
