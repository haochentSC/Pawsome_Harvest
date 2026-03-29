using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

/// <summary>
/// Central hub for ALL feedback output: particles, haptics, audio, and ease animations.
/// Contains zero game logic -- only dispatches effects when called by other systems.
///
/// Usage pattern:
///   FeedbackManager.Instance.TriggerCoinParticles(pos, rate);
///   FeedbackManager.Instance.TriggerHaptic(controller, 0.7f, 0.15f);
///   FeedbackManager.Instance.EaseScale(transform, curve, 0.4f);
/// </summary>
public class FeedbackManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static FeedbackManager Instance { get; private set; }

    // ── Inspector: Particles ──────────────────────────────────────────────────
    [Header("Particle Systems")]
    [Tooltip("Assign a ParticleSystem prefab or scene object for coin bursts.")]
    [SerializeField] private ParticleSystem coinParticleSystem;

    [Tooltip("Assign a ParticleSystem prefab or scene object for fertilizer bursts.")]
    [SerializeField] private ParticleSystem fertParticleSystem;

    [Header("Particle Settings")]
    [SerializeField] private int minParticlesPerBurst = 1;
    [SerializeField] private int maxParticlesPerBurst = 8;

    // ── Inspector: Audio ──────────────────────────────────────────────────────
    [Header("UI Audio (2D)")]
    [Tooltip("Shared 2D AudioSource for non-spatial UI sounds.")]
    [SerializeField] private AudioSource uiAudioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip upgradeBuyClip;
    [SerializeField] private AudioClip harvestClip;

    // ── Inspector: Ease Defaults ──────────────────────────────────────────────
    [Header("Default Ease Curves")]
    [Tooltip("Used when no curve is passed. Shape: ease-out-back (overshoot then settle).")]
    [SerializeField] private AnimationCurve defaultEaseOutBack;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        EnsureDefaultCurve();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PARTICLES
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Burst coin particles at a world position.
    /// ratePerPot scales the burst count so more active pots = more visible particles.
    /// Called by EconomyManager.Tick() via OnMoneyTick event.
    /// </summary>
    public void TriggerCoinParticles(Vector3 worldPos, float ratePerPot)
    {
        if (coinParticleSystem == null) return;
        int count = Mathf.Clamp(Mathf.CeilToInt(ratePerPot * 10f), minParticlesPerBurst, maxParticlesPerBurst);
        BurstParticlesAt(coinParticleSystem, worldPos, count);
    }

    /// <summary>
    /// Burst fertilizer particles at a world position.
    /// Called by EconomyManager.Tick() after the fertilizer station is unlocked.
    /// </summary>
    public void TriggerFertParticles(Vector3 worldPos, float ratePerPot)
    {
        if (fertParticleSystem == null) return;

        int count = Mathf.Clamp(Mathf.CeilToInt(ratePerPot * 8f), minParticlesPerBurst, maxParticlesPerBurst);
        BurstParticlesAt(fertParticleSystem, worldPos, count);
    }

    /// <summary>
    /// Moves a ParticleSystem to worldPos and emits count particles.
    /// Uses a single shared instance to avoid per-frame allocations.
    /// </summary>
    private void BurstParticlesAt(ParticleSystem ps, Vector3 worldPos, int count)
    {
        ps.transform.position = worldPos;

        var emitParams = new ParticleSystem.EmitParams();
        emitParams.ResetPosition();           // use the system's local origin after the move
        ps.Emit(emitParams, count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HAPTICS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Send a haptic impulse on a controller.
    /// amplitude: 0.0 - 1.0   duration: seconds
    ///
    /// Preset patterns:
    ///   Upgrade purchase  -> (0.7f, 0.15f)  strong pulse
    ///   Harvest           -> (0.5f, 0.10f)  satisfying pop
    ///   Clicker tap       -> (0.3f, 0.05f)  light tick
    /// </summary>
    public void TriggerHaptic(XRBaseController controller, float amplitude, float duration)
    {
        if (controller == null)
        {
            Debug.Log($"[Haptic] amplitude={amplitude} duration={duration}s (no controller ref -- ok in editor)");
            return;
        }

#if UNITY_EDITOR
        // Haptics require a real device. Log so we can verify the call fires.
        Debug.Log($"[Haptic] Would fire: amplitude={amplitude} duration={duration}s");
#endif

        controller.SendHapticImpulse(amplitude, duration);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Play a spatialized (3D) sound on an AudioSource that lives in the scene world.
    /// The AudioSource must have its spatialBlend set to 1.0 in the inspector.
    /// Used for: Fertilizer Station unlock chime.
    /// </summary>
    public void PlaySpatialSound(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        source.PlayOneShot(clip);
    }

    /// <summary>
    /// Play a non-spatial (2D) UI sound on the shared UI AudioSource.
    /// Used for: button clicks, upgrade purchase, harvest.
    /// </summary>
    public void PlayUISound(AudioClip clip)
    {
        if (uiAudioSource == null || clip == null) return;
        uiAudioSource.PlayOneShot(clip);
    }

    // Convenience wrappers for common clips
    public void PlayButtonClick()   => PlayUISound(buttonClickClip);
    public void PlayUpgradeBuy()    => PlayUISound(upgradeBuyClip);
    public void PlayHarvestSound()  => PlayUISound(harvestClip);

    // ─────────────────────────────────────────────────────────────────────────
    // EASE SCALE  (spawn from zero -- used by PotSlot seed planting)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawn animation: scales `target` from (0,0,0) up to its ORIGINAL local scale
    /// using an AnimationCurve over `duration` seconds.
    ///
    /// The object's scale at call-time is treated as the TARGET scale.
    /// Set the object to its final desired scale in the inspector, then call this
    /// to animate it in from zero.
    ///
    /// Pass null for curve to use the default ease-out-back.
    /// </summary>
    public Coroutine EaseScale(Transform target, AnimationCurve curve, float duration)
    {
        if (target == null) return null;
        AnimationCurve c = (curve != null && curve.length > 0) ? curve : defaultEaseOutBack;
        return StartCoroutine(EaseScaleSpawnRoutine(target, c, duration));
    }

    /// <summary>Shorthand using default ease-out-back curve.</summary>
    public Coroutine EaseScale(Transform target, float duration = 0.4f)
        => EaseScale(target, null, duration);

    private IEnumerator EaseScaleSpawnRoutine(Transform target, AnimationCurve curve, float duration)
    {
        if (target == null) yield break;

        // Save the intended final scale (set in inspector before calling this)
        Vector3 finalScale = target.localScale;
        target.localScale  = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t        = Mathf.Clamp01(elapsed / duration);
            float curveVal = curve.Evaluate(t);
            target.localScale = finalScale * curveVal;
            yield return null;
        }

        if (target != null)
            target.localScale = finalScale;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCALE POP  (button press feedback -- preserves object's existing scale)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Button press pop: briefly scales `target` up by `peakMultiplier` then returns
    /// to its original scale. Does NOT reset to (1,1,1) -- safe on any button size.
    ///
    /// Called by XRSimpleButton on press.
    /// </summary>
    public Coroutine ScalePop(Transform target, float peakMultiplier = 1.25f, float duration = 0.15f)
    {
        if (target == null) return null;
        return StartCoroutine(ScalePopRoutine(target, peakMultiplier, duration));
    }

    private IEnumerator ScalePopRoutine(Transform target, float peakMultiplier, float duration)
    {
        if (target == null) yield break;

        Vector3 originalScale = target.localScale;
        float   halfDuration  = duration * 0.5f;

        // Phase 1: scale up to peak
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float smoothT = t * t * (3f - 2f * t);
            target.localScale = originalScale * Mathf.Lerp(1f, peakMultiplier, smoothT);
            yield return null;
        }

        // Phase 2: return to original
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float smoothT = t * t * (3f - 2f * t);
            target.localScale = originalScale * Mathf.Lerp(peakMultiplier, 1f, smoothT);
            yield return null;
        }

        if (target != null)
            target.localScale = originalScale;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // EASE COUNTER UI (for TMP_Text number labels)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lerp a TMP_Text integer label from `from` to `to` over duration seconds.
    /// Uses smoothstep so it decelerates as it arrives.
    /// Prefix is prepended to the number (e.g. "Generators: ").
    ///
    /// Returns the Coroutine so the caller can StopCoroutine if needed.
    /// </summary>
    public Coroutine EaseCounterUI(TMP_Text label, int from, int to, float duration, string prefix = "")
    {
        if (label == null) return null;
        return StartCoroutine(EaseCounterRoutine(label, from, to, duration, prefix));
    }

    private IEnumerator EaseCounterRoutine(TMP_Text label, int from, int to, float duration, string prefix)
    {
        if (label == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (label == null) yield break;

            elapsed += Time.deltaTime;
            float t        = Mathf.Clamp01(elapsed / duration);
            float smoothT  = t * t * (3f - 2f * t);           // smoothstep
            int   value    = Mathf.RoundToInt(Mathf.Lerp(from, to, smoothT));
            label.text     = prefix + value.ToString();

            yield return null;
        }

        if (label != null)
            label.text = prefix + to.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// If no ease-out-back curve is set in the inspector, generate one in code.
    /// Shape: starts at 0, overshoots to ~1.1, settles at 1.0.
    /// </summary>
    private void EnsureDefaultCurve()
    {
        if (defaultEaseOutBack != null && defaultEaseOutBack.length > 0) return;

        defaultEaseOutBack = new AnimationCurve(
            new Keyframe(0f,    0f,   0f,    2.5f),   // start: zero, steep outgoing tangent
            new Keyframe(0.7f,  1.1f, 2.5f,  0f),    // overshoot peak
            new Keyframe(1f,    1f,   -0.5f, 0f)     // settle at 1.0
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DEBUG
    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Debug: Test Coin Particles at Origin")]
    private void DebugTestCoinParticles() =>
        TriggerCoinParticles(Vector3.up * 1.2f, 0.3f);

    [ContextMenu("Debug: Test Ease Scale on self")]
    private void DebugTestEase() =>
        EaseScale(transform, null, 0.5f);
}
