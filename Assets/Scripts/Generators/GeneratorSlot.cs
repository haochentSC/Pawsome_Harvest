using System.Collections;
using UnityEngine;

namespace ZombieBunker
{
    public class GeneratorSlot : MonoBehaviour
    {
        [SerializeField] private GeneratorConfig generatorConfig;
        [SerializeField] private GameObject generatorObject;

        [Header("MP3 Juice")]
        [SerializeField] private GeneratorEaseAnimator easeAnimator;
        [SerializeField] private ParticleSystem sparkBurst;

        private bool isActive = false;
        private bool easeInActive = false;
        private bool wobblePhase = false; // false = easing to 1.2, true = easing back to 1.0

        public GeneratorConfig GetConfig() => generatorConfig;
        public bool IsActive => isActive;

        private void Start()
        {
            if (generatorObject != null && !isActive)
                generatorObject.SetActive(false);
        }

        private void Update()
        {
            if (easeInActive && generatorObject != null)
            {
                float k = 4f; // Reduced from 8f for 2x slower animation
                Vector3 current = generatorObject.transform.localScale;
                Vector3 target = wobblePhase ? Vector3.one : Vector3.one * 1.2f; // Wobble: 0 → 1.2 → 1.0
                Vector3 next = Vector3.Lerp(current, target, k * Time.deltaTime);
                generatorObject.transform.localScale = next;

                // Switch to second phase when reaching 1.2, or finish when reaching 1.0
                if (!wobblePhase && Vector3.Distance(next, Vector3.one * 1.2f) < 0.01f)
                {
                    wobblePhase = true; // Switch to phase 2: ease back to 1.0
                }
                else if (wobblePhase && Vector3.Distance(next, Vector3.one) < 0.01f)
                {
                    generatorObject.transform.localScale = Vector3.one;
                    easeInActive = false;
                    wobblePhase = false; // Reset for next activation
                }
            }
        }

        public Generator Activate()
        {
            if (isActive || generatorConfig == null || generatorObject == null) return null;

            // Set scale to zero so it eases in
            generatorObject.transform.localScale = Vector3.zero;
            generatorObject.SetActive(true);
            easeInActive = true;

            Generator generator = generatorObject.GetComponent<Generator>();
            if (generator == null)
                generator = generatorObject.AddComponent<Generator>();
            generator.Initialize(generatorConfig);
            isActive = true;

            // Notify ease animator (requires knowing total count — let GeneratorManager call TriggerEase separately)
            if (sparkBurst != null)
                sparkBurst.Play();

            return generator;
        }

        /// <summary>Called by GeneratorManager after activating to pass the new count to the animator.</summary>
        public void NotifyEaseAnimator(int newCount)
        {
            if (easeAnimator != null)
                easeAnimator.TriggerEase(newCount);
        }

        public void ResetSlot()
        {
            isActive = false;
            easeInActive = false;
            if (generatorObject != null)
            {
                generatorObject.SetActive(false);
                generatorObject.transform.localScale = Vector3.one;
            }
        }
    }
}
