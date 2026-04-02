using UnityEngine;
using TMPro;

public class ClickerCooldown : MonoBehaviour
{
    public float cooldownTime = 5f;
    public TextMeshPro countdownText;
    public AudioSource audioSource;

    private bool isCoolingDown = false;

    // This is the function you will call from the button OnClick
    public void StartCooldown()
    {
        if (!isCoolingDown)
            StartCoroutine(CooldownRoutine());
    }

    private System.Collections.IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;

        // Hide the button
        gameObject.SetActive(false);

        // Show countdown
        countdownText.gameObject.SetActive(true);

        float remaining = cooldownTime;

        while (remaining > 0f)
        {
            countdownText.text = remaining.ToString("F1");
            remaining -= Time.deltaTime;
            yield return null;
        }

        // Clear text
        countdownText.text = "";

        // Hide countdown
        countdownText.gameObject.SetActive(false);

        // Play sound
        if (audioSource != null)
            audioSource.Play();

        // Show button again
        gameObject.SetActive(true);

        isCoolingDown = false;
    }
}
