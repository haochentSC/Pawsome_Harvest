using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZombieBunker
{
    public class ShopManager : MonoBehaviour
    {
        [Header("Shop Entries")]
        [SerializeField] private List<ShopEntryUI> entries = new List<ShopEntryUI>();
        [SerializeField] private bool autoFindEntries = true;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip purchaseClip;
        [SerializeField] private AudioClip failClip;

        [Header("MP3 Juice — Haptics")]
        [SerializeField] private HapticOnPurchase hapticFeedback;
        [SerializeField] private XRBaseController defaultController;

        [Header("Cooldown (Optional)")]
        [SerializeField] private CooldownTimer cooldownTimer;
        [SerializeField] private float cooldownDuration = 0.5f;

        private GeneratorSlot[] generatorSlots;

        private void Start()
        {
            generatorSlots = FindObjectsByType<GeneratorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (autoFindEntries && entries.Count == 0)
                GetComponentsInChildren(entries);

            foreach (var entry in entries)
                entry.Initialize(this);
        }

        public void PurchaseGenerator(GeneratorConfig config)
        {
            if (config == null) return;
            if (IsOnCooldown()) return;

            GeneratorSlot slot = FindAvailableSlot(config);
            if (slot == null)
            {
                PlayClip(failClip);
                return;
            }

            Generator generator = GeneratorManager.Instance.ActivateGenerator(config, slot);

            if (generator != null)
            {
                PlayClip(purchaseClip);
                StartCooldown();
                SendPurchaseHaptic();
            }
            else
            {
                PlayClip(failClip);
            }
        }

        public void PurchasePowerUp(PowerUpConfig config)
        {
            if (config == null) return;
            if (IsOnCooldown()) return;
            if (PowerUpManager.Instance == null) return;

            bool success = PowerUpManager.Instance.TryPurchasePowerUp(config);
            PlayClip(success ? purchaseClip : failClip);
            if (success)
            {
                StartCooldown();
                SendPurchaseHaptic();
            }
        }

        public void PurchaseRocketRoomUnlock()
        {
            if (IsOnCooldown()) return;
            if (UnlockManager.Instance == null) return;

            bool success = UnlockManager.Instance.TryUnlockRocketRoom();
            PlayClip(success ? purchaseClip : failClip);
            if (success)
            {
                StartCooldown();
                SendPurchaseHaptic();
            }
        }

        public void PurchaseTurretUpgrade(ResourceType costResource, float cost)
        {
            if (IsOnCooldown()) return;
            if (TurretManager.Instance == null || ResourceManager.Instance == null) return;

            if (ResourceManager.Instance.TrySpend(costResource, cost))
            {
                TurretManager.Instance.UpgradeTurret();
                PlayClip(purchaseClip);
                StartCooldown();
                SendPurchaseHaptic();
            }
            else
            {
                PlayClip(failClip);
            }
        }

        public void PurchaseCraft(ResourceType input, ResourceType output, float inputAmount, float outputAmount)
        {
            if (IsOnCooldown()) return;
            if (ResourceManager.Instance == null) return;

            if (ResourceManager.Instance.TrySpend(input, inputAmount))
            {
                ResourceManager.Instance.AddResource(output, outputAmount);
                PlayClip(purchaseClip);
                StartCooldown();
                SendPurchaseHaptic();
            }
            else
            {
                PlayClip(failClip);
            }
        }

        public GeneratorConfig GetCurrentConfig(List<GeneratorConfig> configs)
        {
            if (configs == null) return null;

            foreach (var config in configs)
            {
                if (FindAvailableSlot(config) != null)
                    return config;
            }
            return null;
        }

        public bool HasAvailableSlot(GeneratorConfig config)
        {
            return FindAvailableSlot(config) != null;
        }

        private GeneratorSlot FindAvailableSlot(GeneratorConfig config)
        {
            if (generatorSlots == null) return null;

            foreach (var slot in generatorSlots)
            {
                if (!slot.IsActive && slot.gameObject.activeInHierarchy && slot.GetConfig() == config)
                    return slot;
            }
            return null;
        }

        public void RefreshSlotCache()
        {
            generatorSlots = FindObjectsByType<GeneratorSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        public GeneratorSlot[] GetAllSlots() => generatorSlots;

        private bool IsOnCooldown()
        {
            return cooldownTimer != null && cooldownTimer.IsOnCooldown;
        }

        private void StartCooldown()
        {
            if (cooldownTimer != null)
                cooldownTimer.StartCooldown(cooldownDuration);
        }

        private void PlayClip(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        private void SendPurchaseHaptic()
        {
            var devices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
            foreach (var device in devices)
                device.SendHapticImpulse(0, 0.6f, 1.0f);
        }
    }
}
