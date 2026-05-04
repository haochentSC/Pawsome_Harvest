using UnityEngine;

namespace LotteryMachine
{
    public interface ILotteryCoinExchangeProvider
    {
        bool TryExchangeMoneyForStoredCoin(LotteryCoinCounterStation station);
    }

    public sealed class LotteryCoinCounterStation : MonoBehaviour
    {
        [SerializeField] private LotteryGameManager gameManager;
        [SerializeField] private LotteryCoinCounterDisplay counterDisplay;
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private Transform coinSpawnPoint;
        [SerializeField] private Transform coinParent;

        public LotteryGameManager GameManager
        {
            get => ResolveGameManager();
            set
            {
                gameManager = value;
                RefreshDisplayBinding();
            }
        }

        public GameObject CoinPrefab
        {
            get => coinPrefab;
            set => coinPrefab = value;
        }

        public Transform CoinSpawnPoint
        {
            get => coinSpawnPoint;
            set => coinSpawnPoint = value;
        }

        public Transform CoinParent
        {
            get => coinParent;
            set => coinParent = value;
        }

        public GameObject LastExtractedCoin { get; private set; }

        private void Awake()
        {
            ResolveGameManager();
            RefreshDisplayBinding();
        }

        private void OnEnable()
        {
            ResolveGameManager();
            RefreshDisplayBinding();
        }

        private void OnValidate()
        {
            if (counterDisplay == null)
            {
                counterDisplay = GetComponentInChildren<LotteryCoinCounterDisplay>(true);
            }
        }

        public bool TryExchange()
        {
            var exchangeProvider = ResolveExchangeProvider();
            if (exchangeProvider != null)
            {
                return exchangeProvider.TryExchangeMoneyForStoredCoin(this);
            }

            var manager = ResolveGameManager();
            if (manager == null)
            {
                return false;
            }

            return manager.TryExchangeMoneyForStoredCoin();
        }

        public void Exchange()
        {
            TryExchange();
        }

        public bool TryDepositCoin(LotteryCoin coin)
        {
            var manager = ResolveGameManager();
            return manager != null && manager.TryDepositStoredCoin(coin);
        }

        public bool TryExtractCoin()
        {
            var manager = ResolveGameManager();
            if (manager == null || coinPrefab == null || !manager.TrySpendStoredCoin())
            {
                return false;
            }

            LastExtractedCoin = SpawnExtractedCoin(manager);
            return LastExtractedCoin != null;
        }

        public void ExtractCoin()
        {
            TryExtractCoin();
        }

        private GameObject SpawnExtractedCoin(LotteryGameManager manager)
        {
            var spawnTransform = coinSpawnPoint != null ? coinSpawnPoint : transform;
            var spawnedCoin = Instantiate(coinPrefab, spawnTransform.position, spawnTransform.rotation, coinParent);
            var coin = LotteryCoin.PrepareCoinObject(spawnedCoin, true, manager);
            if (coin != null)
            {
                coin.ClearCountedInInventory();
                coin.SetPickupCountingEnabled(false);
            }

            return spawnedCoin;
        }

        private ILotteryCoinExchangeProvider ResolveExchangeProvider()
        {
            var parentBehaviours = GetComponentsInParent<MonoBehaviour>(true);
            for (var i = 0; i < parentBehaviours.Length; i++)
            {
                if (parentBehaviours[i] is ILotteryCoinExchangeProvider parentProvider)
                {
                    return parentProvider;
                }
            }

            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ILotteryCoinExchangeProvider provider)
                {
                    return provider;
                }
            }

            return null;
        }

        private LotteryGameManager ResolveGameManager()
        {
            if (gameManager != null)
            {
                return gameManager;
            }

            gameManager = GetComponentInParent<LotteryGameManager>();
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<LotteryGameManager>();
            }

            return gameManager;
        }

        private void RefreshDisplayBinding()
        {
            if (counterDisplay == null)
            {
                counterDisplay = GetComponentInChildren<LotteryCoinCounterDisplay>(true);
            }

            if (counterDisplay != null)
            {
                counterDisplay.Configure(gameManager, counterDisplay.CounterText);
            }
        }
    }
}
