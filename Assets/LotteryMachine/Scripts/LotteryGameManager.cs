using UnityEngine;
using UnityEngine.Events;

namespace LotteryMachine
{
    public sealed class LotteryGameManager : MonoBehaviour
    {
        [Header("Balances")]
        [SerializeField, Min(0)] private int startingMoney;
        [SerializeField, Min(0)] private int startingCoins;

        [Header("Lottery")]
        [SerializeField] private LotteryMachine lotteryMachine;

        [Header("Physical Coins")]
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private Transform coinSpawnPoint;
        [SerializeField] private Transform coinParent;

        [Header("Events")]
        [SerializeField] private UnityEvent balanceChanged = new();
        [SerializeField] private UnityEvent exchangeFailed = new();
        [SerializeField] private UnityEvent drawFailed = new();

        private int money;
        private int coins;
        private GameObject lastSpawnedCoin;

        public int Money => money;
        public int Coins => coins;
        public GameObject LastSpawnedCoin => lastSpawnedCoin;
        public UnityEvent BalanceChangedEvent => balanceChanged;
        public UnityEvent ExchangeFailedEvent => exchangeFailed;
        public UnityEvent DrawFailedEvent => drawFailed;

        public LotteryMachine LotteryMachine
        {
            get => lotteryMachine;
            set => lotteryMachine = value;
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

        private void Awake()
        {
            money = startingMoney;
            coins = startingCoins;
            balanceChanged.Invoke();
        }

        public void AddMoney()
        {
            money++;
            balanceChanged.Invoke();
        }

        public bool TryExchangeMoneyForCoin()
        {
            if (money <= 0)
            {
                exchangeFailed.Invoke();
                return false;
            }

            if (coinPrefab != null)
            {
                var spawnedCoin = SpawnPhysicalCoin();
                if (spawnedCoin == null)
                {
                    exchangeFailed.Invoke();
                    return false;
                }

                money--;
                coins++;
                balanceChanged.Invoke();
                return true;
            }

            money--;
            coins++;
            balanceChanged.Invoke();
            return true;
        }

        public void ExchangeMoneyForCoin()
        {
            TryExchangeMoneyForCoin();
        }

        public bool TryExchangeMoneyForStoredCoin()
        {
            if (money <= 0)
            {
                exchangeFailed.Invoke();
                return false;
            }

            money--;
            coins++;
            balanceChanged.Invoke();
            return true;
        }

        public void ExchangeMoneyForStoredCoin()
        {
            TryExchangeMoneyForStoredCoin();
        }

        public bool TryDepositStoredCoin(LotteryCoin coin)
        {
            if (coin == null || coin.CountedInInventory)
            {
                return false;
            }

            coin.GameManager = this;
            coin.MarkCountedInInventory();
            coins++;
            balanceChanged.Invoke();
            return true;
        }

        public bool TrySpendStoredCoin()
        {
            if (coins <= 0)
            {
                return false;
            }

            coins--;
            balanceChanged.Invoke();
            return true;
        }

        public bool TryDrawWithCoin()
        {
            if (coins <= 0 || lotteryMachine == null)
            {
                drawFailed.Invoke();
                return false;
            }

            if (!lotteryMachine.TryStartDraw())
            {
                drawFailed.Invoke();
                return false;
            }

            coins--;
            balanceChanged.Invoke();
            return true;
        }

        public void DrawWithCoin()
        {
            TryDrawWithCoin();
        }

        public bool TryRegisterCoinPickup(LotteryCoin coin)
        {
            if (coin == null || coin.CountedInInventory)
            {
                return false;
            }

            coin.GameManager = this;
            coin.MarkCountedInInventory();
            coins++;
            balanceChanged.Invoke();
            return true;
        }

        public bool TrySpendCoinFromInventory(LotteryCoin coin)
        {
            if (coin == null || !coin.CountedInInventory || coins <= 0)
            {
                return false;
            }

            coin.ClearCountedInInventory();
            coins--;
            balanceChanged.Invoke();
            return true;
        }

        private GameObject SpawnPhysicalCoin()
        {
            if (coinPrefab == null)
            {
                return null;
            }

            var spawnTransform = coinSpawnPoint != null ? coinSpawnPoint : transform;
            lastSpawnedCoin = Instantiate(coinPrefab, spawnTransform.position, spawnTransform.rotation, coinParent);
            var coin = LotteryCoin.PrepareCoinObject(lastSpawnedCoin, true, this);
            if (coin != null)
            {
                coin.MarkCountedInInventory();
            }

            return lastSpawnedCoin;
        }
    }
}
