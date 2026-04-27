using UnityEngine;

/// <summary>
/// ScriptableObject defining one pest type (weed / bug / snail).
/// Create assets via: Assets > Create > Greenhouse > PestData
///
/// Three assets to create manually:
///   - Weed   (drain 0, reward 3)
///   - Bug    (drain 0.05/s, radius 0.6, reward 5)
///   - Snail  (drain 0.02/s, radius 0.8, reward 8)
/// </summary>
[CreateAssetMenu(menuName = "Greenhouse/PestData", fileName = "PestData_New")]
public class PestData : ScriptableObject
{
    [Header("Identity")]
    public PestType type = PestType.Weed;
    public string   displayName = "Pest";

    [Header("Economy")]
    [Tooltip("Coins added to EconomyManager when disposed in the fireplace.")]
    public int rewardOnKill = 5;

    [Header("Drain (Bug / Snail)")]
    [Tooltip("Money drained per second while within radius of an active pot. Set 0 for weeds.")]
    public float drainPerSecond = 0f;

    [Tooltip("Distance from a pot's particle anchor at which drain applies.")]
    public float drainRadius = 0.6f;

    [Tooltip("Seconds between drain ticks.")]
    public float drainTickInterval = 1f;

    [Header("Audio")]
    public AudioClip spawnSound;
    public AudioClip grabSound;
    public AudioClip deathSound;
}
