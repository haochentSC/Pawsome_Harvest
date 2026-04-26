using UnityEngine;
using TMPro;

namespace ZombieBunker
{
    public class SurgeIndicatorUI : MonoBehaviour
    {
        [SerializeField] private GameObject surgeActivePanel;
        [SerializeField] private TextMeshProUGUI surgeStatusText;
        [SerializeField] private TextMeshProUGUI surgeTimerText;

        private float surgeEndTime = 0f;

        private void Start()
        {
            if (surgeActivePanel != null)
                surgeActivePanel.SetActive(false);

            if (SurpriseSurgeManager.Instance != null)
            {
                SurpriseSurgeManager.Instance.OnSurgeCollected += OnSurgeCollected;
                SurpriseSurgeManager.Instance.OnSurgeEffectEnded += OnSurgeEnded;
            }
        }

        private void OnDestroy()
        {
            if (SurpriseSurgeManager.Instance != null)
            {
                SurpriseSurgeManager.Instance.OnSurgeCollected -= OnSurgeCollected;
                SurpriseSurgeManager.Instance.OnSurgeEffectEnded -= OnSurgeEnded;
            }
        }

        private void OnSurgeCollected()
        {
            surgeEndTime = Time.time + 30f;
            if (surgeActivePanel != null) surgeActivePanel.SetActive(true);
            if (surgeStatusText != null) surgeStatusText.text = "SURGE ACTIVE! 2x Production!";
        }

        private void OnSurgeEnded()
        {
            if (surgeActivePanel != null) surgeActivePanel.SetActive(false);
        }

        private void Update()
        {
            if (SurpriseSurgeManager.Instance != null && SurpriseSurgeManager.Instance.IsSurgeActive)
            {
                float remaining = surgeEndTime - Time.time;
                if (surgeTimerText != null && remaining > 0f)
                    surgeTimerText.text = $"{remaining:F0}s remaining";
            }
        }
    }
}
