using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZombieBunker
{
    public class UnlockManager : MonoBehaviour
    {
        public static UnlockManager Instance { get; private set; }

        [Header("Rocket Room Unlock")]
        [SerializeField] private float rocketRoomUnlockCost = 500f;
        [SerializeField] private ResourceType rocketRoomCostResource = ResourceType.Bullets;
        [SerializeField] private GameObject rocketRoomDoor;
        [SerializeField] private GameObject rocketRoomContent;
        [SerializeField] private GameObject rocketRoomUI;
        [SerializeField] private Button unlockButton;
        [SerializeField] private Animator doorAnimator;
        [SerializeField] private string doorOpenTrigger = "Open";

        [Header("Default Rocket Generator")]
        [SerializeField] private GeneratorSlot defaultRocketGeneratorSlot;

        [Header("Weenie Settings")]
        [SerializeField] private GameObject weenieViewport;

        [Header("MP3 Juice")]
        [SerializeField] private UnlockSpatialSound unlockSpatialSound;
        [SerializeField] private CameraShaker cameraShaker;

        private bool rocketRoomUnlocked = false;

        public event Action OnRocketRoomUnlocked;

        public bool IsRocketRoomUnlocked => rocketRoomUnlocked;
        public float RocketRoomUnlockCost => rocketRoomUnlockCost;

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
            if (rocketRoomContent != null) rocketRoomContent.SetActive(false);
            if (rocketRoomUI != null) rocketRoomUI.SetActive(false);
            if (weenieViewport != null) weenieViewport.SetActive(true);

            if (unlockButton != null)
                unlockButton.onClick.AddListener(OnUnlockClicked);
        }

        private void OnDestroy()
        {
            if (unlockButton != null)
                unlockButton.onClick.RemoveListener(OnUnlockClicked);
        }

        private void OnUnlockClicked()
        {
            TryUnlockRocketRoom();
        }

        public bool TryUnlockRocketRoom()
        {
            if (rocketRoomUnlocked) return false;
            if (!ResourceManager.Instance.TrySpend(ResourceType.Cash, rocketRoomUnlockCost))
                return false;

            rocketRoomUnlocked = true;
            ResourceManager.Instance.SetRocketsUnlocked(true);

            if (rocketRoomContent != null) rocketRoomContent.SetActive(true);
            if (rocketRoomUI != null) rocketRoomUI.SetActive(true);
            if (weenieViewport != null) weenieViewport.SetActive(false);

            if (unlockSpatialSound != null)
                unlockSpatialSound.PlayUnlockSound();

            var shaker = cameraShaker != null ? cameraShaker : CameraShaker.Instance;
            if (shaker != null)
                shaker.Shake(2.0f, 0.15f);

            if (doorAnimator != null)
                doorAnimator.SetTrigger(doorOpenTrigger);
            else if (rocketRoomDoor != null)
                rocketRoomDoor.SetActive(false);

            if (unlockButton != null)
                unlockButton.gameObject.SetActive(false);

            TurretManager turretMgr = TurretManager.Instance;
            if (turretMgr != null)
                turretMgr.SetRocketTurretActive(true);

            // Auto-activate the default rocket generator so the player starts with net +1 rockets
            if (defaultRocketGeneratorSlot != null && GeneratorManager.Instance != null)
            {
                GeneratorConfig config = defaultRocketGeneratorSlot.GetConfig();
                if (config != null)
                    GeneratorManager.Instance.ActivateGeneratorFree(config, defaultRocketGeneratorSlot);
            }

            // Re-cache slots in ShopManager so rocket room slots are discoverable
            ShopManager shopMgr = FindFirstObjectByType<ShopManager>();
            if (shopMgr != null)
                shopMgr.RefreshSlotCache();

            TutorialManager tutorialMgr = FindFirstObjectByType<TutorialManager>();
            if (tutorialMgr != null)
                tutorialMgr.OnRocketRoomUnlocked();

            OnRocketRoomUnlocked?.Invoke();
            return true;
        }

        public void SetRocketRoomUnlocked(bool unlocked)
        {
            if (unlocked && !rocketRoomUnlocked)
            {
                rocketRoomUnlocked = true;
                ResourceManager.Instance.SetRocketsUnlocked(true);
                if (rocketRoomContent != null) rocketRoomContent.SetActive(true);
                if (rocketRoomUI != null) rocketRoomUI.SetActive(true);
                if (weenieViewport != null) weenieViewport.SetActive(false);
                if (rocketRoomDoor != null) rocketRoomDoor.SetActive(false);
                if (unlockButton != null) unlockButton.gameObject.SetActive(false);
                TurretManager.Instance?.SetRocketTurretActive(true);

                ShopManager shopMgr = FindFirstObjectByType<ShopManager>();
                if (shopMgr != null)
                    shopMgr.RefreshSlotCache();
            }
        }

        public void ResetForPrestige()
        {
            rocketRoomUnlocked = false;

            if (rocketRoomContent != null) rocketRoomContent.SetActive(false);
            if (rocketRoomUI != null) rocketRoomUI.SetActive(false);
            if (weenieViewport != null) weenieViewport.SetActive(true);
            if (rocketRoomDoor != null) rocketRoomDoor.SetActive(true);
            if (unlockButton != null) unlockButton.gameObject.SetActive(true);

            ResourceManager.Instance.SetRocketsUnlocked(false);
            TurretManager.Instance?.SetRocketTurretActive(false);
        }
    }
}
