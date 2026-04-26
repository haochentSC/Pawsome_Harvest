using UnityEngine;
using TMPro;

namespace ZombieBunker
{
    public class ResourceDisplay : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI rateText;
        [SerializeField] private string countFormat = "{0:F0}";
        [SerializeField] private string rateFormat = "+{0:F1}/s";

        private void Update()
        {
            if (ResourceManager.Instance == null) return;

            if (countText != null)
                countText.text = string.Format(countFormat, ResourceManager.Instance.GetResourceCount(resourceType));

            if (rateText != null)
            {
                float netRate = ResourceManager.Instance.GetNetRate(resourceType);
                string sign = netRate >= 0f ? "+" : "";
                rateText.text = $"{sign}{netRate:F1}/s";
            }
        }
    }
}
