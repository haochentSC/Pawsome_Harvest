using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton on the Managers GameObject. Owns the pest spawn loop,
/// the alive-pest list, the cleared count, and the infestation flag.
///
/// Spawn rule: pests appear at a random offset around a randomly chosen ACTIVE pot
/// (PotManager.ActivePots). If no pots are planted, no pest spawns -- the threat
/// only exists once there's something to drain.
/// </summary>
public class PestManager : MonoBehaviour
{
    public static PestManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Pest Prefabs")]
    [SerializeField] private GameObject weedPrefab;
    [SerializeField] private GameObject bugPrefab;
    [SerializeField] private GameObject snailPrefab;

    [Header("Spawn")]
    [Tooltip("Seconds between spawn attempts.")]
    [SerializeField] private float spawnInterval = 40f;

    [Tooltip("Spawn within this radius (XZ) around the chosen active pot.")]
    [SerializeField] private float spawnRadiusAroundPot = 0.5f;

    [Tooltip("Spawn this much above the pot's particle anchor (so pests don't clip the soil).")]
    [SerializeField] private float spawnHeightOffset = 0.05f;

    [Tooltip("Hard cap on simultaneously alive pests.")]
    [SerializeField] private int maxAlive = 4;

    [Tooltip("Pests don't start spawning until the player's money reaches this amount.")]
    [SerializeField] private float moneyThresholdToStart = 200f;

    [Tooltip("After the money threshold is reached, wait this long before the first spawn attempt.")]
    [SerializeField] private float postThresholdDelay = 5f;

    [Header("Spawn Mix (relative weights)")]
    [SerializeField] private float weedWeight  = 1f;
    [SerializeField] private float bugWeight   = 1f;
    [SerializeField] private float snailWeight = 1f;

    [Header("Infestation")]
    [Tooltip("isInfested flips true when alive count reaches or exceeds this.")]
    [SerializeField] private int infestationThreshold = 3;

    [Tooltip("Point Light flipped between safe/danger colors based on infestation state.")]
    [SerializeField] private Light infestationLight;
    [SerializeField] private Color safeColor   = Color.green;
    [SerializeField] private Color dangerColor = Color.red;

    [Header("Tutorial Popups")]
    [Tooltip("Shared TutorialPopup component (the same one ResourceDisplay uses for trophy messages). Leave null to disable popups.")]
    [SerializeField] private TutorialPopup tutorialPopup;
    [TextArea] [SerializeField] private string popupOnThresholdReached =
        "Careful! Your crops can attract weeds and pests over time.";
    [TextArea] [SerializeField] private string popupOnFirstPestSpawn =
        "Pick up the weed and throw it in the fire to save your crops and earn extra money!";
    [TextArea] [SerializeField] private string popupOnFirstPestCleared =
        "Great! Keep clearing pests to protect your crops.";

    // One-shot guards so each popup fires exactly once per session.
    private bool _shownThresholdPopup;
    private bool _shownFirstSpawnPopup;
    private bool _shownFirstClearedPopup;

    // ── State ────────────────────────────────────────────────────────────────
    private readonly List<Pest> _alive = new();
    private int  _totalCleared;
    private bool _isInfested;

    public int  AliveCount   => _alive.Count;
    public int  TotalCleared => _totalCleared;

    /// <summary>Sum of money drain (per second) currently being applied by all alive pests in range of pots.</summary>
    public float GetActiveDrainPerSecond()
    {
        float total = 0f;
        for (int i = 0; i < _alive.Count; i++)
        {
            if (_alive[i] == null) continue;
            total += _alive[i].CurrentDrainPerSecond;
        }
        return total;
    }
    public bool IsInfested   => _isInfested;

    /// <summary>Fired whenever the cleared total changes. PestDisplay subscribes to this.</summary>
    public event Action<int> OnTotalClearedChanged;

    /// <summary>Fired whenever the infestation flag flips.</summary>
    public event Action<bool> OnInfestationChanged;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ApplyInfestationVisual(false);
        StartCoroutine(SpawnLoop());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Spawn loop
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator SpawnLoop()
    {
        while (EconomyManager.Instance == null ||
               EconomyManager.Instance.GetMoney() < moneyThresholdToStart)
        {
            yield return null;
        }

        TryShowPopup(ref _shownThresholdPopup, popupOnThresholdReached);

        if (postThresholdDelay > 0f) yield return new WaitForSeconds(postThresholdDelay);

        var wait = new WaitForSeconds(spawnInterval);
        while (true)
        {
            yield return wait;
            if (_alive.Count >= maxAlive) continue;
            TrySpawnNearActivePot();
        }
    }

    private void TrySpawnNearActivePot()
    {
        if (PotManager.Instance == null) return;
        var pots = PotManager.Instance.ActivePots;
        if (pots == null || pots.Count == 0) return;

        PotSlot pot = pots[UnityEngine.Random.Range(0, pots.Count)];
        Vector2 offset2D = UnityEngine.Random.insideUnitCircle * spawnRadiusAroundPot;
        Vector3 spawnPos = pot.ParticleAnchorPosition
                         + new Vector3(offset2D.x, spawnHeightOffset, offset2D.y);

        GameObject prefab = PickPrefab();
        if (prefab == null) return;

        var go = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
        var pest = go.GetComponent<Pest>();
        if (pest == null)
        {
            Debug.LogWarning("[PestManager] Spawned prefab is missing Pest component.");
            Destroy(go);
            return;
        }

        _alive.Add(pest);
        TryShowPopup(ref _shownFirstSpawnPopup, popupOnFirstPestSpawn);
        UpdateInfestationState();
    }

    private void TryShowPopup(ref bool guard, string message)
    {
        if (guard || tutorialPopup == null || string.IsNullOrEmpty(message)) return;
        guard = true;
        tutorialPopup.ShowTutorial(message);
    }

    private GameObject PickPrefab()
    {
        // Weighted random over the three types, skipping any unassigned slot.
        float wWeed  = weedPrefab  != null ? Mathf.Max(0f, weedWeight)  : 0f;
        float wBug   = bugPrefab   != null ? Mathf.Max(0f, bugWeight)   : 0f;
        float wSnail = snailPrefab != null ? Mathf.Max(0f, snailWeight) : 0f;
        float total  = wWeed + wBug + wSnail;
        if (total <= 0f) return null;

        float roll = UnityEngine.Random.value * total;
        if (roll < wWeed) return weedPrefab;
        if (roll < wWeed + wBug) return bugPrefab;
        return snailPrefab;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API -- called by Pest.Die()
    // ─────────────────────────────────────────────────────────────────────────

    public void PestCleared(Pest p)
    {
        _alive.Remove(p);
        _totalCleared++;
        OnTotalClearedChanged?.Invoke(_totalCleared);
        TryShowPopup(ref _shownFirstClearedPopup, popupOnFirstPestCleared);
        UpdateInfestationState();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Infestation
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateInfestationState()
    {
        // Strip out null entries (in case anything was destroyed without notifying).
        _alive.RemoveAll(p => p == null);

        bool nowInfested = _alive.Count >= infestationThreshold;
        if (nowInfested == _isInfested) return;

        _isInfested = nowInfested;
        ApplyInfestationVisual(_isInfested);
        OnInfestationChanged?.Invoke(_isInfested);
    }

    private void ApplyInfestationVisual(bool infested)
    {
        if (infestationLight != null)
            infestationLight.color = infested ? dangerColor : safeColor;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Debug
    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Debug: Spawn One Pest Now")]
    private void DebugSpawnOne() => TrySpawnNearActivePot();

    [ContextMenu("Debug: Print State")]
    private void DebugPrintState() =>
        Debug.Log($"[PestManager] Alive={_alive.Count} Cleared={_totalCleared} Infested={_isInfested}");
}
