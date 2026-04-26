using UnityEngine;
using TMPro;

namespace ZombieBunker
{
    public class PrestigeUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI prestigeLevelText;
        [SerializeField] private TextMeshProUGUI prestigeCountText;
        [SerializeField] private TextMeshProUGUI prestigeMultiplierText;
        [SerializeField] private TextMeshProUGUI requirementText;
        [SerializeField] private GameObject canPrestigeIndicator;

        private void Start()
        {
            if (PrestigeManager.Instance != null)
                PrestigeManager.Instance.OnPrestigeActivated += OnPrestige;
            UpdateUI();
        }

        private void OnDestroy()
        {
            if (PrestigeManager.Instance != null)
                PrestigeManager.Instance.OnPrestigeActivated -= OnPrestige;
        }

        private void OnPrestige(int count)
        {
            UpdateUI();
        }

        private void Update()
        {
            if (PrestigeManager.Instance == null) return;

            if (canPrestigeIndicator != null)
                canPrestigeIndicator.SetActive(PrestigeManager.Instance.CanPrestige());

            UpdateUI();
        }

        private void UpdateUI()
        {
            if (PrestigeManager.Instance == null) return;

            int level = PrestigeManager.Instance.PrestigeCount;
            if (prestigeLevelText != null)
                prestigeLevelText.text = $"P{level}";
            if (prestigeCountText != null)
                prestigeCountText.text = $"Prestige: {level}";
            if (prestigeMultiplierText != null)
                prestigeMultiplierText.text = $"Bonus: x{PrestigeManager.Instance.PrestigeMultiplier:F1}";
            if (requirementText != null)
            {
                float bulletCost = PrestigeManager.Instance.GetCurrentBulletCost();
                float cashCost = PrestigeManager.Instance.GetCurrentCashCost();
                float rocketCost = PrestigeManager.Instance.GetCurrentRocketCost();
                requirementText.text = $"Cost: {bulletCost:F0} Bullets, ${cashCost:F0}, {rocketCost:F0} Rockets";
            }
        }
    }
}
