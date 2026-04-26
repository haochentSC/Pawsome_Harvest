using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

namespace ZombieBunker
{
    public class CooldownTimer : MonoBehaviour
    {
        [SerializeField] private Image cooldownFillImage;
        [SerializeField] private GameObject cooldownVisual;
        [SerializeField] private TextMeshProUGUI cooldownText;
        [SerializeField] private Text cooldownLegacyText;
        [SerializeField] private string cooldownSuffix = "s";

        [Header("MP3 Juice — Sound")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip cooldownStartClip;
        [SerializeField] private AudioClip cooldownEndClip;

        [Header("MP3 Juice — Haptic")]
        [SerializeField] private XRBaseController hapticController;

        [Header("MP3 Juice — Ring Flash")]
        [SerializeField] private Image ringFlashImage;

        private float cooldownRemaining = 0f;
        private float cooldownDuration = 0f;
        private string defaultTmpText = string.Empty;
        private string defaultLegacyText = string.Empty;
        private bool defaultTextCaptured = false;

        public bool IsOnCooldown => cooldownRemaining > 0f;
        public float CooldownProgress => cooldownDuration > 0f ? 1f - (cooldownRemaining / cooldownDuration) : 1f;
        public float CooldownRemaining => Mathf.Max(0f, cooldownRemaining);

        public event Action OnCooldownComplete;

        private void Awake()
        {
            CaptureDefaultTextIfNeeded();
        }

        private void Update()
        {
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining -= Time.deltaTime;
                UpdateVisuals();

                if (cooldownRemaining <= 0f)
                {
                    cooldownRemaining = 0f;
                    if (cooldownVisual != null)
                        cooldownVisual.SetActive(false);
                    UpdateVisuals();

                    if (audioSource != null && cooldownEndClip != null)
                        audioSource.PlayOneShot(cooldownEndClip);

                    if (ringFlashImage != null)
                        StartCoroutine(FlashRing());

                    OnCooldownComplete?.Invoke();
                }
            }
        }

        public void StartCooldown(float duration)
        {
            CaptureDefaultTextIfNeeded();

            cooldownDuration = duration;
            cooldownRemaining = duration;

            if (cooldownVisual != null)
                cooldownVisual.SetActive(true);

            if (audioSource != null && cooldownStartClip != null)
                audioSource.PlayOneShot(cooldownStartClip);

            if (hapticController != null)
                hapticController.SendHapticImpulse(0.3f, 1.0f);

            UpdateVisuals();
        }

        public void ResetCooldown()
        {
            cooldownRemaining = 0f;
            if (cooldownVisual != null)
                cooldownVisual.SetActive(false);
            UpdateVisuals();
        }

        public void ConfigureVisualsIfMissing(Image fillImage, GameObject visual, TextMeshProUGUI tmpText, Text legacyText)
        {
            if (cooldownFillImage == null)
                cooldownFillImage = fillImage;
            if (cooldownVisual == null)
                cooldownVisual = visual;
            if (cooldownText == null)
                cooldownText = tmpText;
            if (cooldownLegacyText == null)
                cooldownLegacyText = legacyText;

            CaptureDefaultTextIfNeeded();
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            CaptureDefaultTextIfNeeded();

            if (cooldownFillImage != null)
                cooldownFillImage.fillAmount = CooldownProgress;

            string textValue = IsOnCooldown
                ? $"{Mathf.CeilToInt(CooldownRemaining)}{cooldownSuffix}"
                : null;

            if (cooldownText != null)
                cooldownText.text = IsOnCooldown ? textValue : defaultTmpText;

            if (cooldownLegacyText != null)
                cooldownLegacyText.text = IsOnCooldown ? textValue : defaultLegacyText;
        }

        private IEnumerator FlashRing()
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Color baseColor = ringFlashImage.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(1f, 0f, elapsed / duration);
                ringFlashImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
                yield return null;
            }
            ringFlashImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a);
        }

        private void CaptureDefaultTextIfNeeded()
        {
            if (defaultTextCaptured)
                return;

            if (cooldownText != null)
                defaultTmpText = cooldownText.text;

            if (cooldownLegacyText != null)
                defaultLegacyText = cooldownLegacyText.text;

            defaultTextCaptured = true;
        }
    }
}
