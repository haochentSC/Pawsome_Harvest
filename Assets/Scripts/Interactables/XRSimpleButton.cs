using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Reusable XR button wrapper for XRI 3.x.
///
/// Place this on any GameObject that also has:
///   - XRSimpleInteractable component
///   - A Collider (Box, Sphere, etc.) -- required for ray/direct interaction
///
/// Wire the inspector UnityEvent onPressed to drive all behaviour.
/// The pressing controller is exposed as LastPressingController so
/// UpgradeStation (Prompt 7) can pass it to FeedbackManager.TriggerHaptic().
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class XRSimpleButton : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Interaction")]
    [Tooltip("Fired when the button is successfully pressed (after cooldown check).")]
    [SerializeField] public UnityEvent onPressed;

    [Tooltip("Seconds before the button can be pressed again. Prevents double-fires.")]
    [SerializeField] private float cooldownDuration = 0.3f;

    [Header("Audio")]
    [Tooltip("Played via FeedbackManager.PlayUISound() on press. Leave empty to skip.")]
    [SerializeField] private AudioClip clickSound;

    [Header("Visual Feedback")]
    [Tooltip("If assigned, this transform gets a quick scale-pop on press. " +
             "Typically point to the button's visual mesh child, not this root.")]
    [SerializeField] private Transform visualTransform;

    [Tooltip("How long the scale-pop ease takes in seconds.")]
    [SerializeField] private float popDuration = 0.15f;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable _interactable;
    private Collider              _collider;
    private bool                  _onCooldown;

    /// <summary>
    /// The XRBaseController that last pressed this button.
    /// UpgradeStation reads this to know which hand to send haptics to.
    /// May be null if the interactor had no controller (e.g. hand-tracking).
    /// </summary>
    public XRBaseController LastPressingController { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        _collider      = GetComponent<Collider>();

        // XRI 3.x uses an InteractionEvent (not a C# event).
        // Subscribe with AddListener.
        _interactable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDestroy()
    {
        if (_interactable != null)
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Interaction callback
    // ─────────────────────────────────────────────────────────────────────────

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (_onCooldown) return;

        // ── Extract controller from the interactor ────────────────────────────
        // args.interactorObject is the IXRSelectInteractor that triggered this.
        // Walk up the hierarchy to find the XRBaseController (ActionBasedController).
        LastPressingController = null;
        if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor baseInteractor)
        {
            LastPressingController = baseInteractor.GetComponentInParent<XRBaseController>();
        }

        // ── Fire the event ────────────────────────────────────────────────────
        onPressed?.Invoke();

        // ── Audio ─────────────────────────────────────────────────────────────
        if (clickSound != null && FeedbackManager.Instance != null)
            FeedbackManager.Instance.PlayUISound(clickSound);
        else if (clickSound != null)
            // Fallback if FeedbackManager not ready yet
            AudioSource.PlayClipAtPoint(clickSound, transform.position, 0.6f);

        // ── Visual pop ────────────────────────────────────────────────────────
        // ScalePop preserves the button's original scale -- safe on any button size.
        if (visualTransform != null && FeedbackManager.Instance != null)
            FeedbackManager.Instance.ScalePop(visualTransform, 1.25f, popDuration);

        // ── Cooldown ──────────────────────────────────────────────────────────
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        _onCooldown = true;
        yield return new WaitForSeconds(cooldownDuration);
        _onCooldown = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Enable or disable this button entirely.
    /// Disabling hides it from the XR Interaction Manager and disables its collider.
    /// Use this to show/hide buttons based on game state (e.g. hide Harvest until Mature).
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        if (_interactable != null) _interactable.enabled = enabled;
        if (_collider != null)      _collider.enabled     = enabled;

        // Also hide/show the visual if it exists
        if (visualTransform != null)
            visualTransform.gameObject.SetActive(enabled);
    }

    /// <summary>
    /// Convenience: simulate a button press from code (no XR input required).
    /// Useful for tutorial triggers or debug.
    /// </summary>
    public void SimulatePress()
    {
        if (_onCooldown) return;
        onPressed?.Invoke();
        StartCoroutine(CooldownRoutine());
    }
}
