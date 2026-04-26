using UnityEngine;
using TMPro;
using System.Collections;

namespace ZombieBunker
{
    /// <summary>
    /// Displays achievement popups when achievements are unlocked.
    /// Subscribe to AchievementManager.OnAchievementUnlocked event.
    /// </summary>
    public class AchievementUI : MonoBehaviour
    {
        [SerializeField] private GameObject achievementPopupPrefab;
        [SerializeField] private Transform popupParent;
        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        private void Start()
        {
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked += ShowAchievementPopup;
            }
        }

        private void OnDestroy()
        {
            if (AchievementManager.Instance != null)
            {
                AchievementManager.Instance.OnAchievementUnlocked -= ShowAchievementPopup;
            }
        }

        private void ShowAchievementPopup(AchievementConfig achievement)
        {
            if (achievementPopupPrefab == null)
            {
                Debug.LogWarning("Achievement popup prefab not assigned!");
                return;
            }

            // Instantiate popup
            GameObject popupInstance = Instantiate(achievementPopupPrefab, popupParent);
            
            // Set text
            var titleText = popupInstance.GetComponentInChildren<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = $"Achievement Unlocked!\n{achievement.achievementName}";
            }

            // Get all text components - description might be in a second one
            var allTexts = popupInstance.GetComponentsInChildren<TextMeshProUGUI>();
            if (allTexts.Length > 1)
            {
                allTexts[1].text = achievement.description;
            }

            // Fade in and out
            StartCoroutine(FadePopup(popupInstance));
        }

        private IEnumerator FadePopup(GameObject popup)
        {
            var canvasGroup = popup.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = popup.AddComponent<CanvasGroup>();

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // Wait
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOutDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;

            // Destroy
            Destroy(popup);
        }
    }
}
