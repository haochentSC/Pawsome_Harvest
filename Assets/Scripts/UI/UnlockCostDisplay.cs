using UnityEngine;
using TMPro;

namespace ZombieBunker
{
    public class UnlockCostDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI statusText;

        private void Update()
        {
            if (UnlockManager.Instance == null) return;

            if (UnlockManager.Instance.IsRocketRoomUnlocked)
            {
                if (statusText != null) statusText.text = "UNLOCKED";
                if (costText != null) costText.gameObject.SetActive(false);
            }
            else
            {
                float cost = UnlockManager.Instance.RocketRoomUnlockCost;
                float current = ResourceManager.Instance.GetResourceCount(ResourceType.Bullets);
                if (costText != null) costText.text = $"{current:F0} / {cost:F0} Bullets";
                if (statusText != null) statusText.text = "LOCKED";
            }
        }
    }
}
