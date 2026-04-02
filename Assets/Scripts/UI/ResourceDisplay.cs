using System.Collections;
using UnityEngine;
using TMPro;


/// <summary>
/// Drives the world-space HUD canvas.
/// Subscribes to EconomyManager events and updates text labels.
/// Also animates the generator count label with a lerped ease when the count changes.
/// </summary>
public class ResourceDisplay : MonoBehaviour
{
    public static ResourceDisplay Instance { get; private set; }
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Money")]
    [SerializeField] private TMP_Text moneyText;

    [Header("Fertilizer (hidden until unlocked)")]
    [SerializeField] private TMP_Text fertText;
    [SerializeField] private GameObject fertPanel;          // parent panel, hidden at start

    [Header("Generator Counter")]
    [SerializeField] private TMP_Text generatorCountText;   // e.g. "Generators: 2"
    [SerializeField] private float counterEaseDuration = 0.3f;

    [Header("Rate Display (optional)")]
    [SerializeField] private TMP_Text moneyRateText;        // e.g. "+0.30/s" -- can leave unassigned


    // trophies
    [SerializeField] private GameObject trophy1;
    [SerializeField] private GameObject trophy2;
    [SerializeField] private GameObject trophy3;



    // ── Private state ─────────────────────────────────────────────────────────
    private int _displayedGeneratorCount;
    private Coroutine _counterEaseCoroutine;

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
        // Subscribe to economy events
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnMoneyChanged      += OnMoneyChanged;
            EconomyManager.Instance.OnFertilizerChanged += OnFertilizerChanged;
            EconomyManager.Instance.OnRateChanged       += OnRateChanged;
        }
        else
        {
            Debug.LogWarning("[ResourceDisplay] EconomyManager not found. " +
                             "Make sure EconomyManager runs before ResourceDisplay (Script Execution Order).");
        }

        // Initial state
        SetFertilizerVisible(false);
        UpdateMoneyText(EconomyManager.Instance != null ? EconomyManager.Instance.GetMoney() : 0f);
        UpdateGeneratorCount(0);

        // Hide trophies at start
        if (trophy1 != null) trophy1.SetActive(false);
        if (trophy2 != null) trophy2.SetActive(false);
        if (trophy3 != null) trophy3.SetActive(false);
    }

    private void OnDestroy()
    {
        if (EconomyManager.Instance != null)
        {
            EconomyManager.Instance.OnMoneyChanged      -= OnMoneyChanged;
            EconomyManager.Instance.OnFertilizerChanged -= OnFertilizerChanged;
            EconomyManager.Instance.OnRateChanged       -= OnRateChanged;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Event handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void OnMoneyChanged(float newMoney)
    {
        UpdateMoneyText(newMoney);


    // ── Trophy unlocks ────────────────────────────────────────────────
    if (trophy1 != null && newMoney >= 500f && !trophy1.activeSelf)
    {
        trophy1.SetActive(true);
        FeedbackManager.Instance?.TriggerCoinParticles(
            trophy1.transform.position,
            0.5f
        );
    }

    if (trophy2 != null && newMoney >= 1000f && !trophy2.activeSelf)
    {
        trophy2.SetActive(true);
        FeedbackManager.Instance?.TriggerCoinParticles(
            trophy2.transform.position,
            0.5f
        );
    }

    if (trophy3 != null && newMoney >= 1500f && !trophy3.activeSelf)
    {
        trophy3.SetActive(true);
        FeedbackManager.Instance?.TriggerCoinParticles(
            trophy3.transform.position,
            0.5f
        );
    }
    }

    private void OnRateChanged(float newRate)
    {
        if (moneyRateText != null)
            moneyRateText.text = $"+{newRate:F2}/s";
    }

    private void OnFertilizerChanged(float newFert)
    {
        // First time we receive a fertilizer event, reveal the panel
        if (fertPanel != null && !fertPanel.activeSelf)
            SetFertilizerVisible(true);

        if (fertText != null)
            fertText.text = $"{newFert:F1} Fert";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API — called by PotManager (Prompt 6)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this whenever the number of active planted pots changes.
    /// Triggers a lerped count-up animation on the generator label.
    /// </summary>
    public void UpdateGeneratorCount(int newCount)
    {
        if (_counterEaseCoroutine != null)
            StopCoroutine(_counterEaseCoroutine);

        _counterEaseCoroutine = StartCoroutine(
            EaseCounterUI(generatorCountText, _displayedGeneratorCount, newCount, counterEaseDuration));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Internal helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateMoneyText(float value)
    {
        if (moneyText != null)
            moneyText.text = $"${value:F1}";
    }

    private void UpdateRateText()
    {
        if (moneyRateText == null || EconomyManager.Instance == null) return;
        moneyRateText.text = $"+{EconomyManager.Instance.CurrentMoneyRate:F2}/s";
    }

    public void SetFertilizerVisible(bool visible)
    {
        if (fertPanel != null)
            fertPanel.SetActive(visible);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Ease coroutine
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lerps the displayed integer from `from` to `to` over `duration` seconds.
    /// Uses a smooth step ease so it decelerates as it arrives at the target.
    /// </summary>
    private IEnumerator EaseCounterUI(TMP_Text label, int from, int to, float duration)
    {
        if (label == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = t * t * (3f - 2f * t);   // smoothstep ease

            int displayed = Mathf.RoundToInt(Mathf.Lerp(from, to, smoothT));
            label.text = $"Generators: {displayed}";
            yield return null;
        }

        // Snap to final value
        _displayedGeneratorCount = to;
        label.text = $"Generators: {to}";
    }
}
