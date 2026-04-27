using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Per-prefab behavior for one pest (weed / bug / snail).
/// Static -- no AI, no movement. Optional cosmetic bob keeps it visually alive.
///
/// Lifecycle:
///   Spawn -> EaseScale + spawn puff + spawn sound (driven by FeedbackManager).
///   Grab  -> haptic + grab sparkle + grab sound.
///   Drain (bugs/snails only) -> periodic AddMoney(-x) on EconomyManager + damage puff at the pot.
///   Enter "Fireplace" trigger -> Die(): fire burst + death sound + AddMoney(+reward) + Destroy.
///
/// Wire on the prefab's XRGrabInteractable (in inspector):
///   selectEntered  -> Pest.OnGrabbed
///   selectExited   -> Pest.OnReleased
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Pest : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Data")]
    [Tooltip("ScriptableObject defining type, reward, drain. Required.")]
    [SerializeField] private PestData data;

    [Header("Cosmetic Bob (optional)")]
    [Tooltip("Vertical bob amplitude in metres. Set 0 to disable.")]
    [SerializeField] private float bobAmplitude = 0.03f;
    [Tooltip("Bob frequency in Hz.")]
    [SerializeField] private float bobFrequency = 1.5f;

    // ── Runtime ──────────────────────────────────────────────────────────────
    private bool      _isHeld;
    private bool      _isDying;
    private Vector3   _bobOrigin;
    private float     _bobPhaseOffset;
    private Coroutine _drainRoutine;

    public PestData Data => data;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        _bobOrigin      = transform.position;
        _bobPhaseOffset = Random.value * 10f;       // de-sync multiple pests

        // ── Spawn feedback (rubric hooks #1, #2) ─────────────────────────────
        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.EaseScale(transform, 0.4f);
            FeedbackManager.Instance.TriggerSpawnPuff(transform.position);
        }
        if (data != null && data.spawnSound != null)
            PlayOneShotAt(data.spawnSound, transform.position);

        // ── Drain loop (bugs / snails) ───────────────────────────────────────
        if (data != null && data.drainPerSecond > 0f)
            _drainRoutine = StartCoroutine(DrainLoop());
    }

    private void Update()
    {
        // Cosmetic bob -- skipped while held or while being destroyed.
        if (_isHeld || _isDying || bobAmplitude <= 0f) return;

        float y = Mathf.Sin((Time.time + _bobPhaseOffset) * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        transform.position = _bobOrigin + new Vector3(0f, y, 0f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // XRGrabInteractable hooks (wired in prefab inspector)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Wire to XRGrabInteractable.selectEntered.</summary>
    public void OnGrabbed(SelectEnterEventArgs args)
    {
        _isHeld = true;

        // ── Grab feedback ────────────────────────────────────────────────────
        if (FeedbackManager.Instance != null)
            FeedbackManager.Instance.TriggerGrabSparkle(transform.position);

        if (data != null && data.grabSound != null)
            PlayOneShotAt(data.grabSound, transform.position);

        // Haptic on the grabbing controller
        var controller = ResolveController(args);
        if (controller != null && FeedbackManager.Instance != null)
            FeedbackManager.Instance.TriggerHaptic(controller, 0.5f, 0.1f);
    }

    /// <summary>Wire to XRGrabInteractable.selectExited.</summary>
    public void OnReleased(SelectExitEventArgs args)
    {
        _isHeld = false;
        // Reseat the bob origin to wherever the pest just landed so it bobs from rest, not snaps.
        _bobOrigin = transform.position;
    }

    private static XRBaseController ResolveController(SelectEnterEventArgs args)
    {
        if (args == null || args.interactorObject == null) return null;
        var go = (args.interactorObject as MonoBehaviour)?.gameObject;
        if (go == null) return null;
        return go.GetComponentInParent<XRBaseController>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Drain loop -- bugs/snails subtract from EconomyManager when near an active pot.
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator DrainLoop()
    {
        var wait = new WaitForSeconds(data.drainTickInterval);
        while (true)
        {
            yield return wait;
            if (_isHeld || _isDying) continue;
            TryDrainNearestPot();
        }
    }

    private void TryDrainNearestPot()
    {
        if (PotManager.Instance == null || EconomyManager.Instance == null) return;

        var pots = PotManager.Instance.ActivePots;
        if (pots == null || pots.Count == 0) return;

        float bestDist = float.PositiveInfinity;
        PotSlot nearest = null;
        foreach (var p in pots)
        {
            if (p == null) continue;
            float d = Vector3.Distance(transform.position, p.ParticleAnchorPosition);
            if (d < bestDist) { bestDist = d; nearest = p; }
        }

        if (nearest == null || bestDist > data.drainRadius) return;

        float drain = data.drainPerSecond * data.drainTickInterval;
        EconomyManager.Instance.AddMoney(-drain);

        if (FeedbackManager.Instance != null)
            FeedbackManager.Instance.TriggerDamagePuff(nearest.ParticleAnchorPosition);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Death -- triggered by entering the fireplace.
    // ─────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (_isDying) return;
        if (!other.CompareTag("Fireplace")) return;
        Die();
    }

    private void Die()
    {
        _isDying = true;
        if (_drainRoutine != null) StopCoroutine(_drainRoutine);

        Vector3 pos = FireplaceZone.Instance != null
            ? FireplaceZone.Instance.transform.position
            : transform.position;

        // Reward
        if (data != null && EconomyManager.Instance != null)
            EconomyManager.Instance.AddMoney(data.rewardOnKill);

        // Death feedback (rubric hooks #3, #4)
        if (FeedbackManager.Instance != null)
            FeedbackManager.Instance.TriggerFireBurst(pos);

        if (data != null && data.deathSound != null)
        {
            var src = FireplaceZone.Instance != null ? FireplaceZone.Instance.SpatialAudioSource : null;
            if (src != null && FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlaySpatialSound(src, data.deathSound);
            else
                PlayOneShotAt(data.deathSound, pos);
        }

        // Tell the manager so it can decrement infestation + bump cleared count.
        PestManager.Instance?.PestCleared(this);

        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static void PlayOneShotAt(AudioClip clip, Vector3 worldPos)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, worldPos);
    }
}
