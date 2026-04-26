using System.Collections;
using UnityEngine;

namespace ZombieBunker
{
    /// <summary>
    /// Makes eye objects track the XR controller, blink randomly,
    /// react with surprise scale, and swap mouth material to express mood.
    /// </summary>
    public class EyeTracker : MonoBehaviour
    {
        [Header("Eye Transforms")]
        [SerializeField] private Transform eyeLeft;
        [SerializeField] private Transform eyeRight;

        [Header("Target (assign XR Right Controller)")]
        [SerializeField] private Transform target;

        [Header("Idle Bob")]
        [SerializeField] private float idleBobAmplitude = 0.002f;
        [SerializeField] private float idleBobFreq = 1.2f;

        [Header("Blink")]
        [SerializeField] private float blinkIntervalMin = 2f;
        [SerializeField] private float blinkIntervalMax = 6f;

        [Header("Surprise")]
        [SerializeField] private float surpriseScale = 1.5f;

        [Header("Face / Mouth")]
        [SerializeField] private Renderer mouthRenderer;
        [SerializeField] private Material smileMat;
        [SerializeField] private Material frownMat;
        [SerializeField] private Material neutralMat;

        private Vector3 eyeLeftBasePos;
        private Vector3 eyeRightBasePos;
        private Vector3 eyeLeftBaseScale;
        private Vector3 eyeRightBaseScale;

        private void Start()
        {
            if (eyeLeft != null)
            {
                eyeLeftBasePos = eyeLeft.localPosition;
                eyeLeftBaseScale = eyeLeft.localScale;
            }
            if (eyeRight != null)
            {
                eyeRightBasePos = eyeRight.localPosition;
                eyeRightBaseScale = eyeRight.localScale;
            }

            StartCoroutine(BlinkRoutine());
        }

        private void Update()
        {
            if (target == null) return;

            if (eyeLeft != null)
                eyeLeft.LookAt(target);
            if (eyeRight != null)
                eyeRight.LookAt(target);

            float bob = Mathf.Sin(Time.time * idleBobFreq * 2f * Mathf.PI) * idleBobAmplitude;

            if (eyeLeft != null)
                eyeLeft.localPosition = eyeLeftBasePos + Vector3.up * bob;
            if (eyeRight != null)
                eyeRight.localPosition = eyeRightBasePos + Vector3.up * bob;
        }

        private IEnumerator BlinkRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(blinkIntervalMin, blinkIntervalMax));
                yield return StartCoroutine(DoBlink());
            }
        }

        private IEnumerator DoBlink()
        {
            // Squish Y scale to 0
            float elapsed = 0f;
            float blinkDown = 0.05f;
            while (elapsed < blinkDown)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / blinkDown;
                SetEyeScaleY(Mathf.Lerp(1f, 0f, t));
                yield return null;
            }
            SetEyeScaleY(0f);
            yield return new WaitForSeconds(0.04f);

            // Restore Y scale
            elapsed = 0f;
            float blinkUp = 0.05f;
            while (elapsed < blinkUp)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / blinkUp;
                SetEyeScaleY(Mathf.Lerp(0f, 1f, t));
                yield return null;
            }
            SetEyeScaleY(1f);
        }

        private void SetEyeScaleY(float yScale)
        {
            if (eyeLeft != null)
                eyeLeft.localScale = new Vector3(eyeLeftBaseScale.x, eyeLeftBaseScale.y * yScale, eyeLeftBaseScale.z);
            if (eyeRight != null)
                eyeRight.localScale = new Vector3(eyeRightBaseScale.x, eyeRightBaseScale.y * yScale, eyeRightBaseScale.z);
        }

        /// <summary>Call from AchievementManager, SurpriseSurgeManager, etc. for surprised reaction.</summary>
        public void TriggerSurprise()
        {
            StartCoroutine(SurpriseCoroutine());
        }

        private IEnumerator SurpriseCoroutine()
        {
            float elapsed = 0f;
            float inDuration = 0.1f;
            while (elapsed < inDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / inDuration;
                float s = Mathf.Lerp(1f, surpriseScale, t);
                if (eyeLeft != null)
                    eyeLeft.localScale = eyeLeftBaseScale * s;
                if (eyeRight != null)
                    eyeRight.localScale = eyeRightBaseScale * s;
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            elapsed = 0f;
            float outDuration = 0.2f;
            while (elapsed < outDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / outDuration;
                float s = Mathf.Lerp(surpriseScale, 1f, t);
                if (eyeLeft != null)
                    eyeLeft.localScale = eyeLeftBaseScale * s;
                if (eyeRight != null)
                    eyeRight.localScale = eyeRightBaseScale * s;
                yield return null;
            }

            if (eyeLeft != null) eyeLeft.localScale = eyeLeftBaseScale;
            if (eyeRight != null) eyeRight.localScale = eyeRightBaseScale;
        }

        /// <summary>
        /// Change the mouth expression. Mood: "smile", "frown", or "neutral".
        /// </summary>
        public void SetMood(string mood)
        {
            if (mouthRenderer == null) return;

            switch (mood)
            {
                case "smile":
                    if (smileMat != null) mouthRenderer.material = smileMat;
                    break;
                case "frown":
                    if (frownMat != null) mouthRenderer.material = frownMat;
                    break;
                default:
                    if (neutralMat != null) mouthRenderer.material = neutralMat;
                    break;
            }
        }

        /// <summary>Reset mood to neutral after a delay.</summary>
        public void SetMoodThenReset(string mood, float delay = 1f)
        {
            SetMood(mood);
            StartCoroutine(ResetMoodAfterDelay(delay));
        }

        private IEnumerator ResetMoodAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetMood("neutral");
        }
    }
}
