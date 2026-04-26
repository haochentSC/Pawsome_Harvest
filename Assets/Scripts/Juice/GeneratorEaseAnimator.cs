using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ZombieBunker
{
    /// <summary>
    /// Animates a generator count bar and label when a generator is purchased.
    /// Uses v = k*(goal-current)*dt ease with a brief overshoot.
    /// </summary>
    public class GeneratorEaseAnimator : MonoBehaviour
    {
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image fillBar;
        [SerializeField] private int maxGenerators = 4;
        [SerializeField] private float easeK = 8f;

        private float targetFill = 0f;
        private float currentFill = 0f;
        private Vector3 baseScale;
        private bool bouncePlaying = false;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void Update()
        {
            currentFill = Mathf.Lerp(currentFill, targetFill, easeK * Time.deltaTime);
            if (fillBar != null)
                fillBar.fillAmount = currentFill;

            if (!bouncePlaying)
            {
                float s = Mathf.Lerp(transform.localScale.x, baseScale.x, easeK * Time.deltaTime);
                transform.localScale = new Vector3(s, s, s);
            }
        }

        /// <summary>Called by GeneratorSlot when a generator is purchased.</summary>
        public void TriggerEase(int newCount)
        {
            targetFill = maxGenerators > 0 ? (float)newCount / maxGenerators : 0f;

            if (countText != null)
                countText.text = "Generators: " + newCount;

            StopAllCoroutines();
            StartCoroutine(BounceIn());
        }

        // Damped spring: scale = base * (1 + A * e^(-d*t) * cos(w*t))
        // Gives ~1.3x initial peak, bounces back to ~0.9x, settles at 1x over ~1s
        private IEnumerator BounceIn()
        {
            bouncePlaying = true;
            float elapsed = 0f;
            float duration = 1.0f;
            float amplitude = 0.3f;
            float damping = 4f;
            float frequency = 12f;

            while (elapsed < duration)
            {
                float s = 1f + amplitude * Mathf.Exp(-damping * elapsed) * Mathf.Cos(frequency * elapsed);
                transform.localScale = baseScale * s;
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = baseScale;
            bouncePlaying = false;
        }
    }
}
