using System.Collections;
using UnityEngine;
using TMPro;

namespace ZombieBunker
{
    public class PopupMessage : MonoBehaviour
    {
        private float displayDuration = 5f;

        public void SetDuration(float duration)
        {
            displayDuration = duration;
            StartCoroutine(DisplayRoutine());
        }

        private IEnumerator DisplayRoutine()
        {
            Debug.Log($"PopupMessage: Showing popup for {displayDuration}s");
            
            // Display for duration
            yield return new WaitForSeconds(displayDuration);

            Debug.Log("PopupMessage: Fading out popup");
            
            // Fade out over 0.5 seconds
            var tmpText = GetComponent<TextMeshPro>();
            if (tmpText != null)
            {
                float fadeTime = 0.5f;
                float elapsed = 0f;
                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    var color = tmpText.color;
                    color.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                    tmpText.color = color;
                    yield return null;
                }
            }

            Debug.Log("PopupMessage: Destroying popup");
            Destroy(gameObject);
        }
    }
}
