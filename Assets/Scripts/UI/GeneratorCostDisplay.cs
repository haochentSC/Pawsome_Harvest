using UnityEngine;
using TMPro;

namespace ZombieBunker
{
    public class GeneratorCostDisplay : MonoBehaviour
    {
        [SerializeField] private GeneratorConfig generatorConfig;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI rateText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private GameObject affordableIndicator;
        [SerializeField] private GameObject unaffordableIndicator;

        private void Start()
        {
            UpdateDisplay();
            if (GeneratorManager.Instance != null)
                GeneratorManager.Instance.OnGeneratorPlaced += OnGeneratorPlaced;
        }

        private void OnDestroy()
        {
            if (GeneratorManager.Instance != null)
                GeneratorManager.Instance.OnGeneratorPlaced -= OnGeneratorPlaced;
        }

        private void OnGeneratorPlaced(Generator gen)
        {
            UpdateDisplay();
        }

        private void Update()
        {
            UpdateAffordability();
        }

        public void UpdateDisplay()
        {
            if (generatorConfig == null) return;

            if (nameText != null) nameText.text = generatorConfig.displayName;
            if (costText != null)
            {
                float cost = GeneratorManager.Instance != null
                    ? GeneratorManager.Instance.GetCurrentCost(generatorConfig)
                    : generatorConfig.baseCost;
                costText.text = $"Cost: {cost:F0} {generatorConfig.resourceType}";
            }
            if (rateText != null) rateText.text = $"+{generatorConfig.baseRate:F1}/s";
            if (countText != null)
            {
                int count = GeneratorManager.Instance != null
                    ? GeneratorManager.Instance.GetGeneratorCount(generatorConfig)
                    : 0;
                countText.text = $"Owned: {count}";
            }
        }

        private void UpdateAffordability()
        {
            if (GeneratorManager.Instance == null) return;
            bool canAfford = GeneratorManager.Instance.CanAffordGenerator(generatorConfig);
            if (affordableIndicator != null) affordableIndicator.SetActive(canAfford);
            if (unaffordableIndicator != null) unaffordableIndicator.SetActive(!canAfford);
        }

        public void SetConfig(GeneratorConfig config)
        {
            generatorConfig = config;
            UpdateDisplay();
        }
    }
}
