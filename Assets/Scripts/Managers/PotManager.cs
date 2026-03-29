using UnityEngine;

/// <summary>
/// Tracks all PotSlots in the scene. Keeps EconomyManager's active pot count accurate.
///
/// Usage:
///   - Assign all PotSlot GameObjects to the `potSlots` array in the inspector.
///   - PotSlot.SetState() calls PotManager.Instance.NotifyPotStateChanged() on every state change.
///   - PotManager recounts active pots and calls EconomyManager.SetActivePotCount().
///   - EconomyManager.Tick() calls GetActivePotPositions() to fire coin particles at real anchors.
/// </summary>
public class PotManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static PotManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Pot Slots")]
    [Tooltip("Drag all PotSlot GameObjects here. Order doesn't matter.")]
    [SerializeField] private PotSlot[] potSlots;

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
        // Set initial count (all pots start Empty, so this will be 0)
        NotifyPotStateChanged();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API — called by PotSlot.SetState()
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called every time any pot changes state.
    /// Recounts active pots and updates EconomyManager.
    /// </summary>
    public void NotifyPotStateChanged()
    {
        int activeCount = GetActivePotCount();

        if (EconomyManager.Instance != null)
            EconomyManager.Instance.SetActivePotCount(activeCount);

        if (ResourceDisplay.Instance != null)
            ResourceDisplay.Instance.UpdateGeneratorCount(activeCount);
    }

    /// <summary>
    /// Returns the average world position of all active pots' ParticleAnchors.
    /// EconomyManager fires one coin particle burst here each tick.
    /// Falls back to (0, 1.5, 1.5) if no pots are active.
    /// </summary>
    public Vector3 GetCenterOfActivePots()
    {
        if (potSlots == null || potSlots.Length == 0)
            return new Vector3(0f, 1.5f, 1.5f);

        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (var slot in potSlots)
        {
            if (slot != null && slot.IsActive)
            {
                sum += slot.ParticleAnchorPosition;
                count++;
            }
        }

        return count > 0 ? sum / count : new Vector3(0f, 1.5f, 1.5f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private int GetActivePotCount()
    {
        if (potSlots == null) return 0;

        int count = 0;
        foreach (var slot in potSlots)
        {
            if (slot != null && slot.IsActive)
                count++;
        }
        return count;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Debug
    // ─────────────────────────────────────────────────────────────────────────

    [ContextMenu("Debug: Print Pot States")]
    private void DebugPrintStates()
    {
        if (potSlots == null) { Debug.Log("[PotManager] No pots assigned."); return; }
        foreach (var slot in potSlots)
        {
            if (slot != null)
                Debug.Log($"[PotManager] {slot.name}: {slot.State}");
        }
        Debug.Log($"[PotManager] Active count: {GetActivePotCount()}");
    }
}
