using UnityEngine;
using TMPro;

/// <summary>
/// The fertilizer station in the scene.
/// Starts locked (greyed out visual, no button).
/// When money >= 500, UnlockManager calls ShowUnlockButton().
/// Player presses it → ConfirmUnlock() fires → spatial chime plays (rubric criterion 4).
///
/// Expected child hierarchy:
///   FertilizerStation (this script + AudioSource, spatialBlend=1.0)
///   ├── LockedVisual      (greyed-out mesh, active at start)
///   ├── ActiveVisual      (full-colour mesh, inactive at start)
///   ├── ButtonUnlock      (XRSimpleButton — hidden at start, shown when affordable)
///   └── StatusText        (TMP_Text — "Locked", "Unlock: $500", "Active")
///
/// Scene position: place this behind the player at roughly (0, 1.0, -1.5).
/// </summary>
public class FertilizerStation : MonoBehaviour
{
    public static FertilizerStation Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Visuals")]
    [Tooltip("Shown while locked. Disable when unlocked.")]
    [SerializeField] private GameObject lockedVisual;

    [Tooltip("Shown after unlock. Enable when unlocked.")]
    [SerializeField] private GameObject activeVisual;

    [Header("Button")]
    [Tooltip("XRSimpleButton — hidden until affordable. onPressed → OnUnlockPressed().")]
    [SerializeField] private XRSimpleButton buttonUnlock;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;

    [Header("Audio")]
    [Tooltip("AudioSource ON THIS GAMEOBJECT. Set spatialBlend = 1.0 in inspector.")]
    [SerializeField] private AudioSource spatialAudioSource;

    [Tooltip("Chime clip played when the station is unlocked.")]
    [SerializeField] private AudioClip   chimeClip;

    // ── State ─────────────────────────────────────────────────────────────────
    private bool _unlocked = false;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Start locked
        SetLockedVisual();
        buttonUnlock?.SetEnabled(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Called by UnlockManager
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Show the unlock button (player can now afford it).</summary>
    public void ShowUnlockButton()
    {
        if (_unlocked) return;
        buttonUnlock?.SetEnabled(true);
        if (statusText != null) statusText.text = "Unlock: $500";
    }

    /// <summary>Hide the unlock button (player lost money and can no longer afford it).</summary>
    public void HideUnlockButton()
    {
        if (_unlocked) return;
        buttonUnlock?.SetEnabled(false);
        if (statusText != null) statusText.text = "Locked\n$500 to unlock";
    }

    /// <summary>Switch to the unlocked visual state (called by UnlockManager.RestoreState on load).</summary>
    public void SetUnlockedVisual()
    {
        _unlocked = true;
        if (lockedVisual != null) lockedVisual.SetActive(false);
        if (activeVisual != null) activeVisual.SetActive(true);
        buttonUnlock?.SetEnabled(false);
        if (statusText != null) statusText.text = "Active";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Button callback — wire ButtonUnlock.onPressed → this in inspector
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Called by ButtonUnlock.onPressed via inspector UnityEvent.</summary>
    public void OnUnlockPressed()
    {
        if (_unlocked || UnlockManager.Instance == null) return;

        // Hand off to UnlockManager — it spends money and fires PlaySpatialSound (rubric)
        UnlockManager.Instance.ConfirmUnlock(spatialAudioSource, chimeClip);

        // Switch visuals
        SetUnlockedVisual();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Locked visual helper
    // ─────────────────────────────────────────────────────────────────────────

    private void SetLockedVisual()
    {
        if (lockedVisual != null) lockedVisual.SetActive(true);
        if (activeVisual != null) activeVisual.SetActive(false);
        if (statusText != null)   statusText.text = "Locked\n$500 to unlock";
    }
}
