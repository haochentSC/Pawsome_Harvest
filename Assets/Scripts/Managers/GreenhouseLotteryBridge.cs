using System;
using LotteryMachine;
using UnityEngine;

public sealed class GreenhouseLotteryBridge : MonoBehaviour, ILotteryCoinExchangeProvider
{
    [Serializable]
    private sealed class LotterySaveState
    {
        public int storedCoins;
    }

    [SerializeField] private LotteryGameManager lotteryGameManager;
    [SerializeField, Min(0f)] private float moneyPerCoin = 1f;

    public LotteryGameManager LotteryGameManager
    {
        get => ResolveLotteryGameManager();
        set => lotteryGameManager = value;
    }

    public float MoneyPerCoin
    {
        get => moneyPerCoin;
        set => moneyPerCoin = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        ResolveLotteryGameManager();
    }

    private void OnValidate()
    {
        moneyPerCoin = Mathf.Max(0f, moneyPerCoin);
        if (lotteryGameManager == null)
        {
            lotteryGameManager = GetComponentInChildren<LotteryGameManager>(true);
        }
    }

    public bool TryExchangeGreenhouseMoneyForCoin()
    {
        var manager = ResolveLotteryGameManager();
        if (manager == null || EconomyManager.Instance == null)
        {
            manager?.ExchangeFailedEvent.Invoke();
            return false;
        }

        if (!EconomyManager.Instance.SpendMoney(moneyPerCoin))
        {
            manager.ExchangeFailedEvent.Invoke();
            return false;
        }

        manager.GrantStoredCoins(1);
        return true;
    }

    public bool TryExchangeMoneyForStoredCoin(LotteryCoinCounterStation station)
    {
        return TryExchangeGreenhouseMoneyForCoin();
    }

    public void ExchangeGreenhouseMoneyForCoin()
    {
        TryExchangeGreenhouseMoneyForCoin();
    }

    public string GetSaveStateJson()
    {
        var manager = ResolveLotteryGameManager();
        var state = new LotterySaveState
        {
            storedCoins = manager != null ? manager.Coins : 0
        };

        return JsonUtility.ToJson(state);
    }

    public void RestoreStateJson(string json)
    {
        var manager = ResolveLotteryGameManager();
        if (manager == null || string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        try
        {
            var state = JsonUtility.FromJson<LotterySaveState>(json);
            if (state != null)
            {
                manager.RestoreStoredCoins(state.storedCoins);
            }
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"[GreenhouseLotteryBridge] Could not restore lottery state: {exception.Message}", this);
        }
    }

    private LotteryGameManager ResolveLotteryGameManager()
    {
        if (lotteryGameManager != null)
        {
            return lotteryGameManager;
        }

        lotteryGameManager = GetComponentInChildren<LotteryGameManager>(true);
        if (lotteryGameManager == null)
        {
            lotteryGameManager = FindFirstObjectByType<LotteryGameManager>();
        }

        return lotteryGameManager;
    }
}
