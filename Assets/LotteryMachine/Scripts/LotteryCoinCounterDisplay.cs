using TMPro;
using UnityEngine;

namespace LotteryMachine
{
    public sealed class LotteryCoinCounterDisplay : MonoBehaviour
    {
        [SerializeField] private LotteryGameManager gameManager;
        [SerializeField] private TMP_Text counterText;
        [SerializeField] private string format = "COINS: {0}";

        public LotteryGameManager GameManager => gameManager;
        public TMP_Text CounterText => counterText;

        public void Configure(LotteryGameManager manager, TMP_Text text)
        {
            if (isActiveAndEnabled && gameManager != null)
            {
                gameManager.BalanceChangedEvent.RemoveListener(Refresh);
            }

            gameManager = manager;
            counterText = text;

            if (isActiveAndEnabled && gameManager != null)
            {
                gameManager.BalanceChangedEvent.AddListener(Refresh);
            }

            Refresh();
        }

        private void Awake()
        {
            if (counterText == null)
            {
                counterText = GetComponentInChildren<TMP_Text>(true);
            }

            if (gameManager == null)
            {
                gameManager = GetComponentInParent<LotteryGameManager>();
            }
        }

        private void OnEnable()
        {
            if (gameManager == null)
            {
                gameManager = GetComponentInParent<LotteryGameManager>();
            }

            if (gameManager != null)
            {
                gameManager.BalanceChangedEvent.AddListener(Refresh);
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.BalanceChangedEvent.RemoveListener(Refresh);
            }
        }

        private void OnValidate()
        {
            if (counterText == null)
            {
                counterText = GetComponentInChildren<TMP_Text>(true);
            }

            if (gameManager == null)
            {
                gameManager = GetComponentInParent<LotteryGameManager>();
            }

            Refresh();
        }

        public void Refresh()
        {
            if (counterText == null)
            {
                return;
            }

            var count = gameManager != null ? gameManager.Coins : 0;
            counterText.text = string.Format(format, count);
        }
    }
}
