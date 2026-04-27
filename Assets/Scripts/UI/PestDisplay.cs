using UnityEngine;
using TMPro;

/// <summary>
/// World-space label that shows the running pest-cleared count.
/// Subscribes to PestManager.OnTotalClearedChanged.
/// Place on a Canvas (World Space render mode) near the garden.
/// </summary>
public class PestDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private string   prefix = "Pests Cleared: ";

    private void OnEnable()
    {
        if (PestManager.Instance != null)
        {
            PestManager.Instance.OnTotalClearedChanged += UpdateLabel;
            UpdateLabel(PestManager.Instance.TotalCleared);
        }
        else
        {
            UpdateLabel(0);
        }
    }

    private void Start()
    {
        // PestManager may not have existed during OnEnable if script execution order placed
        // this Display ahead of Managers Awake. Re-subscribe defensively.
        if (PestManager.Instance != null && label != null)
        {
            PestManager.Instance.OnTotalClearedChanged -= UpdateLabel;
            PestManager.Instance.OnTotalClearedChanged += UpdateLabel;
            UpdateLabel(PestManager.Instance.TotalCleared);
        }
    }

    private void OnDisable()
    {
        if (PestManager.Instance != null)
            PestManager.Instance.OnTotalClearedChanged -= UpdateLabel;
    }

    private void UpdateLabel(int total)
    {
        if (label != null)
            label.text = prefix + total;
    }
}
