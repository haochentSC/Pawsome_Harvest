using System;

/// <summary>
/// Serialisable container for the entire game state.
/// JsonUtility-friendly: only [Serializable] plain classes/primitives, no Dictionary.
///
/// Pet/Lottery sections are reserved fields so teammates can extend without breaking the schema.
/// </summary>
[Serializable]
public class SaveData
{
    // ── Economy ──────────────────────────────────────────────────────────────
    public float money;
    public float fertilizer;
    public bool  fertilizerUnlocked;

    // ── Upgrades ─────────────────────────────────────────────────────────────
    public int soilLevel;
    public int lightsLevel;
    public int irrigationLevel;

    // ── Pots ─────────────────────────────────────────────────────────────────
    public PotSaveData[] pots;

    // ── Meta (idle progress) ─────────────────────────────────────────────────
    /// <summary>UTC ISO-8601 string of the last save. Empty on first run.</summary>
    public string lastSaveIsoUtc;

    /// <summary>Cached money rate at save time — used to compute offline earnings before tick resumes.</summary>
    public float lastMoneyRate;

    // ── Reserved for teammates (null-safe defaults) ──────────────────────────
    public string petJsonBlob = "";       // PetCareManager.GetSaveStateJson()
    public string lotteryJsonBlob = "";   // LotteryManager.GetSaveStateJson()
}
