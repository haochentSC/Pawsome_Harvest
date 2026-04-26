using UnityEngine;
using UnityEngine.UI;

namespace ZombieBunker
{
    public class ClickerController : MonoBehaviour
    {
        [Header("Clicker Settings")]
        [SerializeField] private ResourceType resourceToIncrement = ResourceType.Bullets;
        [SerializeField] private float incrementAmount = 1f;
        [SerializeField] private float cooldownDuration = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField] private Animator buttonAnimator;
        [SerializeField] private string pressAnimTrigger = "Press";
        [SerializeField] private Transform buttonTransform;
        [SerializeField] private float pressDepth = 0.02f;
        [SerializeField] private float pressSpeed = 10f;
        [SerializeField] private AudioSource clickAudioSource;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private ParticleSystem clickParticles;

        [Header("Cooldown Display")]
        [SerializeField] private CooldownTimer cooldownTimer;

        [Header("UI")]
        [SerializeField] private Button clickButton;

        private Vector3 originalPosition;
        private bool isPressed = false;
        private float pressTimer = 0f;

        private void Awake()
        {
            if (buttonTransform != null)
                originalPosition = buttonTransform.localPosition;
        }

        private void OnEnable()
        {
            if (clickButton != null)
                clickButton.onClick.AddListener(OnPress);
        }

        private void OnDisable()
        {
            if (clickButton != null)
                clickButton.onClick.RemoveListener(OnPress);
        }

        private void Update()
        {
            if (isPressed)
            {
                pressTimer += Time.deltaTime * pressSpeed;
                if (pressTimer >= 1f)
                {
                    isPressed = false;
                    pressTimer = 0f;
                }

                if (buttonTransform != null)
                {
                    float t = isPressed ? Mathf.PingPong(pressTimer * 2f, 1f) : 0f;
                    buttonTransform.localPosition = originalPosition - new Vector3(0, pressDepth * (1f - t), 0);
                }
            }
            else if (buttonTransform != null)
            {
                buttonTransform.localPosition = Vector3.Lerp(
                    buttonTransform.localPosition, originalPosition, Time.deltaTime * pressSpeed);
            }
        }

        private void OnPress()
        {
            if (cooldownTimer != null && cooldownTimer.IsOnCooldown) return;

            ResourceManager.Instance.AddResource(resourceToIncrement, incrementAmount);

            if (cooldownTimer != null)
                cooldownTimer.StartCooldown(cooldownDuration);

            // Visual feedback
            isPressed = true;
            pressTimer = 0f;

            if (buttonAnimator != null)
                buttonAnimator.SetTrigger(pressAnimTrigger);

            if (clickAudioSource != null && clickSound != null)
                clickAudioSource.PlayOneShot(clickSound);

            if (clickParticles != null)
                clickParticles.Play();
        }

        public void SetResourceType(ResourceType type)
        {
            resourceToIncrement = type;
        }
    }
}
