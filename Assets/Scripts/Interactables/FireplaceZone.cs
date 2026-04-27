using UnityEngine;

/// <summary>
/// Marker for the disposal pit. Pests check OnTriggerEnter for this object's tag ("Fireplace")
/// and call their own Die() routine -- this script holds nothing but a singleton reference
/// so feedback hooks (sound, particles) can locate the fireplace position.
///
/// Setup:
///   - GameObject with Box Collider, Is Trigger = true, Tag = "Fireplace".
///   - Optional child AudioSource with Spatial Blend = 1.0 for the death crackle.
///   - Optional Light child for ambient flicker.
/// </summary>
public class FireplaceZone : MonoBehaviour
{
    public static FireplaceZone Instance { get; private set; }

    [Tooltip("Optional spatial AudioSource for the death crackle. Spatial Blend should be 1.0.")]
    [SerializeField] private AudioSource spatialAudioSource;

    public AudioSource SpatialAudioSource => spatialAudioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
}
