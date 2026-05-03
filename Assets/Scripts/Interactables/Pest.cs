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

    [Header("Wander (bugs / snails only -- weeds stay rooted)")]
    [Tooltip("Radius of the circular wander path in metres.")]
    [SerializeField] private float wanderRadius = 0.12f;
    [Tooltip("Angular speed of the wander circle in radians/sec. Very slow on purpose.")]
    [SerializeField] private float wanderSpeed = 0.25f;
    [Tooltip("If true, the pest yaws to face its motion direction along the circle.")]
    [SerializeField] private bool wanderFaceMotion = true;

    // ── Runtime ──────────────────────────────────────────────────────────────
    private bool             _isHeld;
    private bool             _isDying;
    private Vector3          _bobOrigin;
    private float            _bobPhaseOffset;
    private float            _wanderPhaseOffset;
    private Quaternion       _baseRotation;
    private Rigidbody        _rb;
    private Coroutine        _drainRoutine;
    private XRBaseController _lastGrabController;

    public PestData Data => data;

    /// <summary>
    /// Drain this pest is *currently* applying per second (0 if not draining right now).
    /// Mirrors the gating in TryDrainNearestPot: held, dying, no pots, or out-of-range = 0.
    /// </summary>
    public float CurrentDrainPerSecond
    {
        get
        {
            if (data == null || data.drainPerSecond <= 0f) return 0f;
            if (_isHeld || _isDying) return 0f;
            if (PotManager.Instance == null) return 0f;
            var pots = PotManager.Instance.ActivePots;
            if (pots == null || pots.Count == 0) return 0f;

            float bestDist = float.PositiveInfinity;
            foreach (var p in pots)
            {
                if (p == null) continue;
                float d = Vector3.Distance(transform.position, p.ParticleAnchorPosition);
                if (d < bestDist) bestDist = d;
            }
            return bestDist <= data.drainRadius ? data.drainPerSecond : 0f;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Existing prefabs were serialized before the wander fields were added, so Unity
        // loads them as 0 (type default) regardless of the C# inline initializer. Snap
        // any 0 back to a sensible runtime default so bugs/snails wander out-of-the-box.
        if (wanderRadius <= 0f) wanderRadius = 0.12f;
        if (wanderSpeed  <= 0f) wanderSpeed  = 0.25f;
    }

    private void Start()
    {
        _bobOrigin         = transform.position;
        _bobPhaseOffset    = Random.value * 10f;                    // de-sync vertical bob
        _wanderPhaseOffset = Random.value * Mathf.PI * 2f;          // de-sync circular path
        _baseRotation      = transform.rotation;                    // preserve spawn yaw

        // Disable physics while autonomous so transform writes (bob/wander) aren't fought
        // by gravity or collider depenetration. XRGrabInteractable will toggle this during
        // a grab; OnReleased reasserts kinematic + zero velocity.
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.useGravity  = false;
            _rb.isKinematic = true;
        }

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
        if (_isHeld || _isDying) return;

        float y = bobAmplitude > 0f
            ? Mathf.Sin((Time.time + _bobPhaseOffset) * bobFrequency * Mathf.PI * 2f) * bobAmplitude
            : 0f;

        // Weeds are rooted -- only bugs and snails wander.
        bool wanders = data != null
                    && data.type != PestType.Weed
                    && wanderRadius > 0f
                    && wanderSpeed  > 0f;

        if (wanders)
        {
            float t = Time.time * wanderSpeed + _wanderPhaseOffset;
            float x = Mathf.Cos(t) * wanderRadius;
            float z = Mathf.Sin(t) * wanderRadius;
            transform.position = _bobOrigin + new Vector3(x, y, z);

            if (wanderFaceMotion)
            {
                // Tangent to the circle = motion direction.
                Vector3 tangent = new Vector3(-Mathf.Sin(t), 0f, Mathf.Cos(t));
                transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
            }
        }
        else
        {
            transform.position = _bobOrigin + new Vector3(0f, y, 0f);
        }
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

        // Haptic on the grabbing controller (cached so Die() can also fire on it).
        var controller = ResolveController(args);
        if (controller != null) _lastGrabController = controller;
        if (controller != null && FeedbackManager.Instance != null)
            FeedbackManager.Instance.TriggerHaptic(controller, 0.5f, 0.1f);
    }

    /// <summary>Wire to XRGrabInteractable.selectExited.</summary>
    public void OnReleased(SelectExitEventArgs args)
    {
        _isHeld = false;
        // Reseat the bob origin to wherever the pest just landed so it bobs from rest, not snaps.
        _bobOrigin = transform.position;

        // Reassert kinematic + no gravity so wander resumes cleanly. XRGrabInteractable
        // may have flipped these during the grab depending on its movement type.
        if (_rb != null)
        {
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.useGravity      = false;
            _rb.isKinematic     = true;
        }
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
            ? FireplaceZone.Instance.transform.position + Vector3.up * 0.5f
            : transform.position;

        // Reward
        if (data != null && EconomyManager.Instance != null)
            EconomyManager.Instance.AddMoney(data.rewardOnKill);

        // Death feedback (rubric hooks #3, #4)
        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.TriggerFireBurst(pos);
            if (_lastGrabController != null)
                FeedbackManager.Instance.TriggerHaptic(_lastGrabController, 1.0f, 1.0f);
        }

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
