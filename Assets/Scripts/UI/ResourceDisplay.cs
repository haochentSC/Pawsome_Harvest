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
