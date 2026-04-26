using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using ZombieBunker;

public class XRButton : MonoBehaviour
{
    public Animator animator;
    public string animationTrigger = "BulletClick";
    public ResourceManager resourceManager;
    public ParticleSystem particles;
    public AudioSource audio;
    public Button clickButton;
    public CooldownTimer cooldownTimer;

    private XRSimpleInteractable interactable;
    private bool onCooldown = false;
    [SerializeField] private float cooldown = 5f;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        // AutoWireCooldownReferences();
    }

    private void OnEnable()
    {
        if (clickButton != null)
            clickButton.onClick.AddListener(OnSelected);

        if (interactable != null)
            interactable.selectEntered.AddListener(OnSelected);
    }

    private void OnDisable()
    {
        if (clickButton != null)
            clickButton.onClick.RemoveListener(OnSelected);

        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnSelected);
    }

    private void OnSelected()
    {
        makeBullet();
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        makeBullet();
    }

    public void makeBullet()
    {
        if (onCooldown || (cooldownTimer != null && cooldownTimer.IsOnCooldown)) return;

        if (resourceManager != null)
            resourceManager.AddResource(ResourceType.Bullets, 1);

        if (animator != null)
            animator.SetTrigger(animationTrigger);

        if (cooldownTimer != null)
            cooldownTimer.StartCooldown(cooldown);

        StartCoroutine(CooldownRoutine());
    }

    private System.Collections.IEnumerator CooldownRoutine()
    {
        onCooldown = true;
        if (interactable != null)
            interactable.enabled = false;
        if (clickButton != null)
            clickButton.interactable = false;

        yield return new WaitForSeconds(cooldown);

        onCooldown = false;
        if (interactable != null)
            interactable.enabled = true;
        if (clickButton != null)
            clickButton.interactable = true;
    }

    public void triggerParticles()
    {
        particles.Play();
        audio.Play();
    }

    private void AutoWireCooldownReferences()
    {
        if (clickButton == null)
            clickButton = GetComponentInChildren<Button>(true);

        if (cooldownTimer == null)
            cooldownTimer = GetComponent<CooldownTimer>();
        if (cooldownTimer == null)
            cooldownTimer = GetComponentInChildren<CooldownTimer>(true);
        if (cooldownTimer == null)
            cooldownTimer = gameObject.AddComponent<CooldownTimer>();

        if (cooldownTimer == null || clickButton == null)
            return;

        var fillImage = clickButton.GetComponent<Image>();
        var visual = clickButton.gameObject;
        var tmpText = clickButton.GetComponentInChildren<TextMeshProUGUI>(true);
        var legacyText = clickButton.GetComponentInChildren<Text>(true);

        cooldownTimer.ConfigureVisualsIfMissing(fillImage, visual, tmpText, legacyText);
    }
}
