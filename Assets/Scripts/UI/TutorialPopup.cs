using UnityEngine;
using TMPro;

public class TutorialPopup : MonoBehaviour
{
    public TextMeshProUGUI tutorialText;

    private Vector3 originalScale;

    private void Awake()
    {
        // Store the starting scale so we can preserve the rectangle shape
        originalScale = transform.localScale;
    }

    public void ShowTutorial(string message)
    {
        tutorialText.text = message;
        gameObject.SetActive(true);
        StartCoroutine(PopupRoutine());
    }

    private System.Collections.IEnumerator PopupRoutine()
    {
        // Start at scale 0 but keep the original proportions
        transform.localScale = Vector3.zero;

        // ── Scale up for 3 seconds ───────────────────────────────
        float t = 0f;
        while (t < 3f)
        {
            t += Time.deltaTime;
            float s = t / 3f;   // 0 → 1
            transform.localScale = originalScale * s;
            yield return null;
        }

        // ── Pause for 5 seconds ──────────────────────────────────
        yield return new WaitForSeconds(5f);

        // ── Scale down for 3 seconds ─────────────────────────────
        t = 0f;
        while (t < 3f)
        {
            t += Time.deltaTime;
            float s = 1f - (t / 3f);  // 1 → 0
            transform.localScale = originalScale * s;
            yield return null;
        }

        // Hide when done
        gameObject.SetActive(false);
    }
}
