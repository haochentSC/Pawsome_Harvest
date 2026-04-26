using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ZombieBunker
{
    /// <summary>
    /// Fires haptic feedback on the interacting XR controller when a power-up is purchased.
    /// Supports a single light pulse or a double heavy pulse.
    /// </summary>
    public class HapticOnPurchase : MonoBehaviour
    {
        [SerializeField] private float lightAmplitude = 0.4f;
        [SerializeField] private float lightDuration = 1.0f;
        [SerializeField] private float heavyAmplitude = 0.8f;
        [SerializeField] private float heavyDuration = 1.0f;
        [SerializeField] private bool isHeavyPurchase = false;

        public bool IsHeavyPurchase
        {
            get => isHeavyPurchase;
            set => isHeavyPurchase = value;
        }

        public void TriggerHaptic(XRBaseController controller)
        {
            if (controller == null) return;

            if (!isHeavyPurchase)
            {
                controller.SendHapticImpulse(lightAmplitude, lightDuration);
            }
            else
            {
                StartCoroutine(DoublePulse(controller));
            }
        }

        private IEnumerator DoublePulse(XRBaseController controller)
        {
            controller.SendHapticImpulse(heavyAmplitude, heavyDuration);
            yield return new WaitForSeconds(heavyDuration + 0.2f);
            controller.SendHapticImpulse(heavyAmplitude * 0.6f, lightDuration);
        }
    }
}
