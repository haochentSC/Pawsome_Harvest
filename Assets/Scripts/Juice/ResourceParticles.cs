using UnityEngine;

namespace ZombieBunker
{
    /// <summary>
    /// Drives a ParticleSystem emission rate based on a resource's production rate.
    /// Attach to the same GameObject as (or a child of) the resource display.
    /// </summary>
    public class ResourceParticles : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private ParticleSystem particleSystem;
        [SerializeField] private float rateMultiplier = 2f;
        [SerializeField] private float maxEmissionRate = 50f;

        [Header("Optional Audio (pitch shifts with rate)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float minPitch = 0.8f;
        [SerializeField] private float maxPitch = 1.5f;
        [SerializeField] private float maxRateForPitch = 10f;

        private void Update()
        {
            if (ResourceManager.Instance == null || particleSystem == null) return;

            float rate = ResourceManager.Instance.GetEffectiveRate(resourceType);

            var emission = particleSystem.emission;
            emission.rateOverTime = Mathf.Clamp(rate * rateMultiplier, 0f, maxEmissionRate);

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, rate / Mathf.Max(1f, maxRateForPitch));
            }
        }
    }
}
