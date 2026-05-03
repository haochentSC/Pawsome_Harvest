using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// JSON save/load orchestrator for the planting + economy slice (htong9 alpha).
///
/// File path: Application.persistentDataPath/save.json
///
/// Triggers Save():
///   - OnApplicationPause(true)   (Quest home button)
///   - OnApplicationQuit
///   - Auto-save coroutine every 60s
///
/// Triggers Load():
///   - Start (after all manager Awakes — set Script Execution Order = -25)
///
/// Idle Progress:
///   On Load, computes elapsed = clamp(now - lastSave, 0, 8h)
///   Awards offlineCoins = lastMoneyRate * elapsed * 0.5
///   Surfaces a "Welcome back! +X coins" popup via TutorialManager (or WelcomeBackPopup if assigned).
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Auto-save")]
    [Tooltip("Save interval in seconds while game is running.")]
    [SerializeField] private float autoSaveInterval = 60f;

    [Header("Idle Progress")]
    [Tooltip("Multiplier on offline rate (50 % efficiency).")]
    [SerializeField] private float offlineEfficiency = 0.5f;

    [Tooltip("Cap offline-progress duration (8 h default).")]
    [SerializeField] private float offlineCapSeconds = 8f * 3600f;

    [Header("Welcome-back popup")]
    [Tooltip("Optional. If left empty, falls back to TutorialManager.ShowTutorial().")]
    [SerializeField] private GameObject welcomeBackPopupPrefab;
    [SerializeField] private float welcomeBackDuration = 5f;

    private string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    private bool _loadedThisSession;
    private Coroutine _autoSaveRoutine;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private IEnumerator Start()
    {
        // Wait one frame so every other manager's Awake/Start has run.
        yield return null;
        Load();
        _autoSaveRoutine = StartCoroutine(AutoSaveLoop());
    }

    private IEnumerator AutoSaveLoop()
    {
        var wait = new WaitForSeconds(autoSaveInterval);
        while (true)
        {
            yield return wait;
            Save();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) Save();
    }

    private void OnApplicationQuit() => Save();

    // ─────────────────────────────────────────────────────────────────────────
    // Save
    // ─────────────────────────────────────────────────────────────────────────

    public void Save()
    {
        try
        {
            var data = CollectState();
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] Saved → {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Save failed: {e}");
        }
    }

    private SaveData CollectState()
    {
        var data = new SaveData();

        if (EconomyManager.Instance != null)
        {
            data.money              = EconomyManager.Instance.GetMoney();
            data.fertilizer         = EconomyManager.Instance.GetFertilizer();
            data.fertilizerUnlocked = EconomyManager.Instance.IsFertilizerUnlocked();
            data.lastMoneyRate      = EconomyManager.Instance.CurrentMoneyRate;
        }

        if (UpgradeManager.Instance != null)
        {
            var (soil, lights, irrigation) = UpgradeManager.Instance.GetSaveState();
            data.soilLevel       = soil;
            data.lightsLevel     = lights;
            data.irrigationLevel = irrigation;
        }

        // Pots — read from PotManager via reflection-free public iteration.
        if (PotManager.Instance != null)
        {
            var slots = FindObjectsByType<PotSlot>(FindObjectsSortMode.None);
            data.pots = new PotSaveData[slots.Length];
            for (int i = 0; i < slots.Length; i++)
                data.pots[i] = slots[i].GetSaveData();
        }
        else
        {
            data.pots = Array.Empty<PotSaveData>();
        }

        data.lastSaveIsoUtc = DateTime.UtcNow.ToString("o");
        return data;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Load
    // ─────────────────────────────────────────────────────────────────────────

    public bool Load()
    {
        if (_loadedThisSession) return false;

        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveManager] No save file found — first run.");
            _loadedThisSession = true;
            return false;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                Debug.LogWarning("[SaveManager] Save file empty or unparseable.");
                _loadedThisSession = true;
                return false;
            }

            ApplyState(data);
            ApplyOfflineEarnings(data);
            _loadedThisSession = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Load failed: {e}");
            _loadedThisSession = true;
            return false;
        }
    }

    private void ApplyState(SaveData data)
    {
        // Order matters: upgrades feed multipliers into Economy, so restore upgrades first
        // (they call EconomyManager.SetSoilMultiplier internally), THEN write the saved
        // money/fert values, THEN restore the unlock state, THEN the pot states.
        UpgradeManager.Instance?.RestoreState(data.soilLevel, data.lightsLevel, data.irrigationLevel);
        EconomyManager.Instance?.RestoreState(data.money, data.fertilizer, data.fertilizerUnlocked);
        UnlockManager.Instance?.RestoreState(data.fertilizerUnlocked);

        if (data.pots != null && data.pots.Length > 0)
        {
            var slots = FindObjectsByType<PotSlot>(FindObjectsSortMode.None);
            int n = Mathf.Min(slots.Length, data.pots.Length);
            for (int i = 0; i < n; i++)
                slots[i].RestoreFromSave(data.pots[i]);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Idle Progress
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyOfflineEarnings(SaveData data)
    {
        if (string.IsNullOrEmpty(data.lastSaveIsoUtc)) return;
        if (!DateTime.TryParse(data.lastSaveIsoUtc, null,
                System.Globalization.DateTimeStyles.RoundtripKind, out var lastSave)) return;

        double elapsedSec = (DateTime.UtcNow - lastSave).TotalSeconds;
        elapsedSec = Math.Max(0, Math.Min(elapsedSec, offlineCapSeconds));
        if (elapsedSec <= 0) return;

        // Use the rate cached at save time. Falls back to recomputed rate if missing.
        float rate = data.lastMoneyRate;
        if (rate <= 0f && EconomyManager.Instance != null)
            rate = EconomyManager.Instance.CurrentMoneyRate;
        if (rate <= 0f) return;

        float earnings = (float)(rate * elapsedSec * offlineEfficiency);
        if (earnings <= 0f) return;

        EconomyManager.Instance?.ApplyOfflineProgress(earnings);
        ShowWelcomeBack(Mathf.RoundToInt(earnings), (int)elapsedSec);
    }

    private void ShowWelcomeBack(int coins, int elapsedSec)
    {
        string msg = $"Welcome back!\n+{coins} coins\n(away {FormatElapsed(elapsedSec)})";

        if (welcomeBackPopupPrefab != null)
        {
            var cam = Camera.main;
            Vector3 pos = cam != null
                ? cam.transform.position + cam.transform.forward * 1.0f
                : new Vector3(0f, 1.7f, 1.0f);

            var popup = Instantiate(welcomeBackPopupPrefab, pos, Quaternion.identity);
            var tmp   = popup.GetComponentInChildren<TMPro.TMP_Text>();
            if (tmp != null) tmp.text = msg;

            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.EaseScale(popup.transform, 0.5f);

            Destroy(popup, welcomeBackDuration);
        }
        else if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.ShowTutorial(msg);
        }

        Debug.Log($"[SaveManager] Offline progress: +{coins} coins after {elapsedSec}s.");
    }

    private static string FormatElapsed(int sec)
    {
        if (sec < 60)    return $"{sec}s";
        if (sec < 3600)  return $"{sec / 60}m";
        return $"{sec / 3600}h {(sec % 3600) / 60}m";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public utilities
    // ─────────────────────────────────────────────────────────────────────────

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            Debug.Log("[SaveManager] Save deleted.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] DeleteSave failed: {e}");
        }
    }

    [ContextMenu("Debug: Save Now")]
    private void DebugSave() => Save();

    [ContextMenu("Debug: Print Save Path")]
    private void DebugPrintPath() => Debug.Log($"[SaveManager] {SavePath}");

    [ContextMenu("Debug: Delete Save")]
    private void DebugDeleteSave() => DeleteSave();
}
