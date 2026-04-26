using UnityEngine;
using TMPro;

namespace ZombieBunker
{
    public enum ResourceDisplayMode
    {
        Count,
        Rate,
        TotalProduced
    }

    public class ResourceUIText : MonoBehaviour
    {
        [SerializeField] private ResourceType resourceType; // Resource to display
        [SerializeField] private ResourceDisplayMode displayMode = ResourceDisplayMode.Count;
        [SerializeField] private TMP_Text resourceText;

        private void OnEnable()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged += UpdateResourceText;
                ResourceManager.Instance.OnRateChanged += UpdateResourceText;
                ResourceManager.Instance.OnTotalProducedChanged += UpdateResourceText;

                // Initialize text immediately
                RefreshText();
            }
        }

        private void OnDisable()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged -= UpdateResourceText;
                ResourceManager.Instance.OnRateChanged -= UpdateResourceText;
                ResourceManager.Instance.OnTotalProducedChanged -= UpdateResourceText;
            }
        }

        private void UpdateResourceText(ResourceType type, float value)
        {
            if (type != resourceType) return; // Only update the relevant resource
            RefreshText();
        }

        private void RefreshText()
        {
            if (ResourceManager.Instance == null) return;

            switch (displayMode)
            {
                case ResourceDisplayMode.Count:
                    resourceText.text = Mathf.FloorToInt(ResourceManager.Instance.GetResourceCount(resourceType)).ToString();
                    break;

                case ResourceDisplayMode.Rate:
                    float rate = ResourceManager.Instance.GetEffectiveRate(resourceType);
                    resourceText.text = rate.ToString("F1") + "/s"; // format with 1 decimal
                    break;

                case ResourceDisplayMode.TotalProduced:
                    resourceText.text = Mathf.FloorToInt(ResourceManager.Instance.GetTotalProduced(resourceType)).ToString();
                    break;
            }
        }
    }
}