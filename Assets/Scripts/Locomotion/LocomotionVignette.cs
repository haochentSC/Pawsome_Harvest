using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZombieBunker
{
    /// <summary>
    /// Applies a vignette post-process effect during VR locomotion to reduce motion sickness.
    /// Requires a Global Volume with a Vignette override in the scene.
    /// </summary>
    public class LocomotionVignette : MonoBehaviour
    {
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private float targetIntensityMoving = 0.45f;
        [SerializeField] private float easeK = 5f;
        [SerializeField] private CharacterController xrCharController;

        private Vignette vignette;

        private void Start()
        {
            if (postProcessVolume != null)
                postProcessVolume.profile.TryGet(out vignette);
        }

        private void Update()
        {
            if (vignette == null) return;

            bool isMoving = xrCharController != null && xrCharController.velocity.magnitude > 0.1f;
            float targetIntensity = isMoving ? targetIntensityMoving : 0f;
            vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, easeK * Time.deltaTime);
        }
    }
}
