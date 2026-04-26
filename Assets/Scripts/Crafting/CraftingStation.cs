using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using TMPro;

namespace ZombieBunker
{
    public class CraftingStation : MonoBehaviour
    {
        [Header("Conversion Settings")]
        [SerializeField] private ResourceType inputResource = ResourceType.Bullets;
        [SerializeField] private ResourceType outputResource = ResourceType.Cash;
        [SerializeField] private float inputAmount = 10f;
        [SerializeField] private float outputAmount = 5f;

        [Header("UI")]
        [SerializeField] private Button convertButton;
        [SerializeField] private CooldownTimer cooldownTimer;
        [SerializeField] private float cooldownDuration = 5f;

        [Header("Display")]
        [SerializeField] private TextMeshProUGUI conversionRateText;
        [SerializeField] private TextMeshProUGUI inputAmountText;
        [SerializeField] private TextMeshProUGUI outputAmountText;

        [Header("MP3 Juice")]
        [SerializeField] private ParticleSystem craftSparks;
        [SerializeField] private AudioSource craftAudioSource;
        [SerializeField] private AudioClip craftClip;

        public event Action<ResourceType, ResourceType, float, float> OnConversionPerformed;

        public ResourceType InputResource => inputResource;
        public ResourceType OutputResource => outputResource;
        public float InputAmount => inputAmount;
        public float OutputAmount => outputAmount;

        private bool localCooldownActive;
        private Coroutine localCooldownRoutine;

        private void Start()
        {
            if (convertButton != null)
                convertButton.onClick.AddListener(OnConvertClicked);

            if (cooldownTimer != null)
                cooldownTimer.OnCooldownComplete += OnCooldownComplete;

            UpdateUI();
        }

        private void OnDestroy()
        {
            if (convertButton != null)
                convertButton.onClick.RemoveListener(OnConvertClicked);

            if (cooldownTimer != null)
                cooldownTimer.OnCooldownComplete -= OnCooldownComplete;
        }

        private void OnConvertClicked()
        {
            if (IsOnAnyCooldown()) return;
            TryConvert();
        }

        public bool TryConvert()
        {
            if (IsOnAnyCooldown())
                return false;

            if (!ResourceManager.Instance.CanAfford(inputResource, inputAmount))
                return false;

            ResourceManager.Instance.TrySpend(inputResource, inputAmount);
            ResourceManager.Instance.AddResource(outputResource, outputAmount);

            StartStationCooldown();

            if (craftSparks != null) craftSparks.Play();
            if (craftAudioSource != null && craftClip != null) craftAudioSource.PlayOneShot(craftClip);
            SendPurchaseHaptic();

            OnConversionPerformed?.Invoke(inputResource, outputResource, inputAmount, outputAmount);

            var tutorial = TutorialManager.Instance;
            if (tutorial != null)
                tutorial.OnCraftingDiscovered();

            return true;
        }

        private bool IsOnAnyCooldown()
        {
            return localCooldownActive || (cooldownTimer != null && cooldownTimer.IsOnCooldown);
        }

        private void StartStationCooldown()
        {
            localCooldownActive = true;

            if (convertButton != null)
                convertButton.interactable = false;

            if (cooldownTimer != null)
            {
                cooldownTimer.StartCooldown(cooldownDuration);
                return;
            }

            if (localCooldownRoutine != null)
                StopCoroutine(localCooldownRoutine);
            localCooldownRoutine = StartCoroutine(LocalCooldownFallback());
        }

        private IEnumerator LocalCooldownFallback()
        {
            yield return new WaitForSeconds(cooldownDuration);
            OnCooldownComplete();
            localCooldownRoutine = null;
        }

        private void OnCooldownComplete()
        {
            localCooldownActive = false;
            if (convertButton != null)
                convertButton.interactable = true;
        }

        private void UpdateUI()
        {
            if (conversionRateText != null)
                conversionRateText.text = $"{inputAmount:F0} {inputResource} → {outputAmount:F0} {outputResource}";
            if (inputAmountText != null)
                inputAmountText.text = $"{inputAmount:F0} {inputResource}";
            if (outputAmountText != null)
                outputAmountText.text = $"{outputAmount:F0} {outputResource}";
        }

        private void SendPurchaseHaptic()
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
            foreach (var device in devices)
                device.SendHapticImpulse(0, 0.6f, 1.0f);
        }
    }
}
