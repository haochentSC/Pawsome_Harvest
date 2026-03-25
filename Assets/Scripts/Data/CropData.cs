using UnityEngine;

/// <summary>
/// ScriptableObject that defines one crop type.
/// Create assets via: Assets > Create > Greenhouse > CropData
///
/// Three assets to create manually in the editor:
///   - Tomato  (growTime 30s, seedCost  5, harvestBonus 2.0)
///   - Herb    (growTime 20s, seedCost  3, harvestBonus 1.2)
///   - Flower  (growTime 45s, seedCost 10, harvestBonus 3.5)
/// </summary>
[CreateAssetMenu(menuName = "Greenhouse/CropData", fileName = "CropData_New")]
public class CropData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Display name shown in UI.")]
    public string cropName = "Crop";

    [Header("Economy")]
    [Tooltip("Money spent when ButtonPlant is pressed.")]
    public float seedCost = 5f;

    [Tooltip("One-time money bonus added to wallet on harvest (on top of passive rate).")]
    public float harvestBonus = 2f;

    [Header("Timing")]
    [Tooltip("Seconds from Seeded → Mature.")]
    public float growTime = 30f;

    [Header("Visuals")]
    [Tooltip("Prefab shown at Stage 0 (just planted). Swap via GameObject.SetActive.")]
    public GameObject seedlingPrefab;

    [Tooltip("Prefab shown at Stage 1 (mature). Swap via GameObject.SetActive.")]
    public GameObject maturePrefab;
}
