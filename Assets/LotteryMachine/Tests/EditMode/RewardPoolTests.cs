using LotteryMachine;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public sealed class RewardPoolTests
{
    private const string SampleMachinePrefabPath = "Assets/LotteryMachine/Sample/Prefabs/LotteryMachine.prefab";
    private const string SampleDisplayBoardPrefabPath = "Assets/LotteryMachine/Sample/Prefabs/RewardDisplayBoard.prefab";
    private const string SampleCoinCounterPrefabPath = "Assets/LotteryMachine/Sample/Prefabs/LotteryCoinCounter.prefab";
    private const string CompletionTrophyPrefabPath = "Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Trophy.prefab";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const float DisplaySlotWidth = 0.19f;
    private const float DisplaySlotHeight = 0.27f;
    private const float DisplaySlotFillRatio = 0.93f;
    private const float CompletionTrophyFloatHeight = 0.08f;
    private static readonly Vector3 CompletionTrophyAnchorPosition = new Vector3(0f, 0.58f, 0.12f);
    private static readonly Vector3 CompletionTrophyAnchorScale = Vector3.one * 0.16f;

    [Test]
    public void WeightedPoolReturnsConfiguredRewards()
    {
        var common = CreateReward("common", 9f);
        var rare = CreateReward("rare", 1f);
        var pool = CreatePool(common, rare);

        Assert.That(pool.TryDraw(0f, out var first), Is.True);
        Assert.That(first, Is.SameAs(common));

        Assert.That(pool.TryDraw(1f, out var last), Is.True);
        Assert.That(last, Is.SameAs(rare));
    }

    [Test]
    public void EmptyPoolFailsGracefully()
    {
        var pool = ScriptableObject.CreateInstance<RewardPool>();

        Assert.That(pool.TryDraw(out var reward), Is.False);
        Assert.That(reward, Is.Null);
    }

    [Test]
    public void DuplicateRewardsAreAllowed()
    {
        var reward = CreateReward("repeat", 1f);
        var pool = CreatePool(reward);

        Assert.That(pool.TryDraw(0.25f, out var first), Is.True);
        Assert.That(pool.TryDraw(0.75f, out var second), Is.True);
        Assert.That(first, Is.SameAs(second));
    }

    [Test]
    public void LotteryMachineEmitsSelectedRewardResult()
    {
        var rewardPrefab = CreateRewardPrefab("Event Reward Prefab");
        var reward = CreateReward("event_reward", 1f, rewardPrefab);
        var pool = CreatePool(reward);
        var machineObject = new GameObject("LotteryMachine Test");
        var revealPoint = new GameObject("Reveal Point").transform;
        var machine = machineObject.AddComponent<global::LotteryMachine.LotteryMachine>();
        machine.RewardPool = pool;
        SetPrivateField(machine, "cardRevealPoint", revealPoint);

        RewardResult captured = default;
        var eventCount = 0;
        machine.RewardCompleted += result =>
        {
            captured = result;
            eventCount++;
        };

        Assert.That(machine.DrawImmediateForTests(out var result), Is.True);

        Assert.That(eventCount, Is.EqualTo(1));
        Assert.That(captured.Reward, Is.SameAs(reward));
        Assert.That(result.Reward, Is.SameAs(reward));
        Assert.That(result.SpawnedObject, Is.Not.Null);
        Assert.That(result.SpawnedObject.GetComponent<GrabbableReward>(), Is.Not.Null);
        Assert.That(result.SpawnedObject.GetComponent<RewardCardInstance>(), Is.Not.Null);
        Assert.That(result.SpawnedObject.GetComponent<RewardCardInstance>().Reward, Is.SameAs(reward));
        Assert.That(result.SpawnedObject.GetComponent<RewardCardInstance>().RewardId, Is.EqualTo("event_reward"));
        Assert.That(captured.RewardId, Is.EqualTo("event_reward"));

        Object.DestroyImmediate(result.SpawnedObject);
        Object.DestroyImmediate(machineObject);
        Object.DestroyImmediate(revealPoint.gameObject);
        Object.DestroyImmediate(rewardPrefab);
    }

    [Test]
    public void LotteryMachineImmediateDrawSupportsRewardRevealSound()
    {
        var rewardPrefab = CreateRewardPrefab("Audio Reward Prefab");
        var reward = CreateReward("audio_reward", 1f, rewardPrefab);
        var machineObject = new GameObject("LotteryMachine Audio Test");
        var revealPoint = new GameObject("Reveal Point").transform;
        var audioSource = machineObject.AddComponent<AudioSource>();
        var audioClip = AudioClip.Create("Reward Reveal Test Clip", 64, 1, 44100, false);
        var machine = machineObject.AddComponent<global::LotteryMachine.LotteryMachine>();
        machine.RewardPool = CreatePool(reward);
        SetPrivateField(machine, "cardRevealPoint", revealPoint);
        SetPrivateField(machine, "rewardRevealAudioSource", audioSource);
        SetPrivateField(machine, "rewardRevealSound", audioClip);
        SetPrivateField(machine, "rewardRevealVolume", 0.6f);

        Assert.That(machine.DrawImmediateForTests(out var result), Is.True);

        Assert.That(result.Reward, Is.SameAs(reward));
        Assert.That(result.SpawnedObject, Is.Not.Null);

        Object.DestroyImmediate(result.SpawnedObject);
        Object.DestroyImmediate(machineObject);
        Object.DestroyImmediate(revealPoint.gameObject);
        Object.DestroyImmediate(rewardPrefab);
        Object.DestroyImmediate(audioClip);
    }

    [Test]
    public void LotteryMachinePreservesPreviousRewardIfItWasNotPickedUp()
    {
        var rewardPrefab = CreateRewardPrefab("Unpicked Reward Prefab");
        var reward = CreateReward("unpicked_reward", 1f, rewardPrefab);
        var machineObject = new GameObject("LotteryMachine Test");
        var revealPoint = new GameObject("Reveal Point").transform;
        var machine = machineObject.AddComponent<global::LotteryMachine.LotteryMachine>();
        machine.RewardPool = CreatePool(reward);
        SetPrivateField(machine, "cardRevealPoint", revealPoint);

        Assert.That(machine.DrawImmediateForTests(out var firstResult), Is.True);
        var firstObject = firstResult.SpawnedObject;
        Assert.That(firstObject, Is.Not.Null);

        Assert.That(machine.DrawImmediateForTests(out var secondResult), Is.True);

        Assert.That(firstObject, Is.Not.Null);
        Assert.That(secondResult.SpawnedObject, Is.Not.Null);
        Assert.That(firstObject, Is.Not.SameAs(secondResult.SpawnedObject));

        Object.DestroyImmediate(firstObject);
        Object.DestroyImmediate(secondResult.SpawnedObject);
        Object.DestroyImmediate(machineObject);
        Object.DestroyImmediate(revealPoint.gameObject);
        Object.DestroyImmediate(rewardPrefab);
    }

    [Test]
    public void LotteryMachinePreservesPreviousRewardIfItWasPickedUp()
    {
        var rewardPrefab = CreateRewardPrefab("Preserved Reward Prefab");
        var reward = CreateReward("preserved_reward", 1f, rewardPrefab);
        var machineObject = new GameObject("LotteryMachine Test");
        var revealPoint = new GameObject("Reveal Point").transform;
        var machine = machineObject.AddComponent<global::LotteryMachine.LotteryMachine>();
        machine.RewardPool = CreatePool(reward);
        SetPrivateField(machine, "cardRevealPoint", revealPoint);

        Assert.That(machine.DrawImmediateForTests(out var firstResult), Is.True);
        var firstObject = firstResult.SpawnedObject;
        firstObject.GetComponent<GrabbableReward>().MarkPickedUp();

        Assert.That(machine.DrawImmediateForTests(out var secondResult), Is.True);

        Assert.That(firstObject, Is.Not.Null);
        Assert.That(secondResult.SpawnedObject, Is.Not.Null);
        Assert.That(firstObject, Is.Not.SameAs(secondResult.SpawnedObject));

        Object.DestroyImmediate(firstObject);
        Object.DestroyImmediate(secondResult.SpawnedObject);
        Object.DestroyImmediate(machineObject);
        Object.DestroyImmediate(revealPoint.gameObject);
        Object.DestroyImmediate(rewardPrefab);
    }

    [Test]
    public void GameManagerAddMoneyIncrementsMoney()
    {
        var manager = CreateGameManager();

        manager.AddMoney();

        Assert.That(manager.Money, Is.EqualTo(1));
        Assert.That(manager.Coins, Is.Zero);

        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void GameManagerExchangeTurnsMoneyIntoCoin()
    {
        var manager = CreateGameManager(startingMoney: 1);

        Assert.That(manager.TryExchangeMoneyForCoin(), Is.True);

        Assert.That(manager.Money, Is.Zero);
        Assert.That(manager.Coins, Is.EqualTo(1));

        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void GameManagerStoredExchangeSpendsMoneyWithoutSpawningPhysicalCoin()
    {
        var manager = CreateGameManager(startingMoney: 1);
        manager.CoinPrefab = CreatePhysicalCoin("Unused Stored Exchange Coin Prefab");

        Assert.That(manager.TryExchangeMoneyForStoredCoin(), Is.True);

        Assert.That(manager.Money, Is.Zero);
        Assert.That(manager.Coins, Is.EqualTo(1));
        Assert.That(manager.LastSpawnedCoin, Is.Null);

        Object.DestroyImmediate(manager.CoinPrefab);
        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void GameManagerStoredExchangeFailsWithoutMoney()
    {
        var manager = CreateGameManager();

        Assert.That(manager.TryExchangeMoneyForStoredCoin(), Is.False);

        Assert.That(manager.Money, Is.Zero);
        Assert.That(manager.Coins, Is.Zero);

        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void GameManagerExchangeSpawnsPhysicalCoinWhenCoinPrefabIsConfigured()
    {
        var coinPrefab = CreatePhysicalCoin("Exchange Coin Prefab");
        var parent = new GameObject("Coin Parent").transform;
        var spawnPoint = new GameObject("Coin Spawn Point").transform;
        spawnPoint.position = new Vector3(1f, 2f, 3f);
        spawnPoint.rotation = Quaternion.Euler(0f, 45f, 0f);
        var manager = CreateGameManager(startingMoney: 1);
        manager.CoinPrefab = coinPrefab;
        manager.CoinSpawnPoint = spawnPoint;
        manager.CoinParent = parent;

        Assert.That(manager.TryExchangeMoneyForCoin(), Is.True);

        Assert.That(manager.Money, Is.Zero);
        Assert.That(manager.Coins, Is.EqualTo(1));
        Assert.That(manager.LastSpawnedCoin, Is.Not.Null);
        Assert.That(manager.LastSpawnedCoin.transform.parent, Is.SameAs(parent));
        Assert.That(manager.LastSpawnedCoin.transform.position, Is.EqualTo(spawnPoint.position));
        var spawnedCoin = manager.LastSpawnedCoin.GetComponent<LotteryCoin>();
        Assert.That(spawnedCoin, Is.Not.Null);
        Assert.That(spawnedCoin.CountedInInventory, Is.True);
        Assert.That(manager.TryRegisterCoinPickup(spawnedCoin), Is.False);
        Assert.That(manager.Coins, Is.EqualTo(1));
        Assert.That(manager.LastSpawnedCoin.GetComponent<XRGrabInteractable>(), Is.Not.Null);

        Object.DestroyImmediate(manager.LastSpawnedCoin);
        Object.DestroyImmediate(manager.gameObject);
        Object.DestroyImmediate(parent.gameObject);
        Object.DestroyImmediate(spawnPoint.gameObject);
        Object.DestroyImmediate(coinPrefab);
    }

    [Test]
    public void GameManagerRegistersEnvironmentCoinPickupOnce()
    {
        var manager = CreateGameManager();
        var coinObject = CreatePhysicalCoin("Environment Coin");
        var coin = coinObject.GetComponent<LotteryCoin>();

        Assert.That(manager.TryRegisterCoinPickup(coin), Is.True);
        Assert.That(manager.TryRegisterCoinPickup(coin), Is.False);

        Assert.That(manager.Coins, Is.EqualTo(1));
        Assert.That(coin.CountedInInventory, Is.True);

        Object.DestroyImmediate(coinObject);
        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void GameManagerExchangeFailsWithoutMoney()
    {
        var manager = CreateGameManager();

        Assert.That(manager.TryExchangeMoneyForCoin(), Is.False);

        Assert.That(manager.Money, Is.Zero);
        Assert.That(manager.Coins, Is.Zero);

        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void GameManagerDrawWithCoinSpendsOneCoinWhenMachineStartsDraw()
    {
        var machineObject = new GameObject("LotteryMachine Test");
        var machine = machineObject.AddComponent<global::LotteryMachine.LotteryMachine>();
        machine.RewardPool = CreatePool(CreateReward("drawable_reward", 1f));
        var manager = CreateGameManager(startingCoins: 1, lotteryMachine: machine);

        Assert.That(manager.TryDrawWithCoin(), Is.True);

        Assert.That(manager.Coins, Is.Zero);

        Object.DestroyImmediate(manager.gameObject);
        Object.DestroyImmediate(machineObject);
    }

    [Test]
    public void GameManagerDrawFailureDoesNotSpendCoin()
    {
        var machineObject = new GameObject("LotteryMachine Test");
        var machine = machineObject.AddComponent<global::LotteryMachine.LotteryMachine>();
        var manager = CreateGameManager(startingCoins: 1, lotteryMachine: machine);

        LogAssert.Expect(LogType.Warning, "Lottery draw failed because no drawable reward is available.");
        Assert.That(manager.TryDrawWithCoin(), Is.False);

        Assert.That(manager.Coins, Is.EqualTo(1));

        Object.DestroyImmediate(manager.gameObject);
        Object.DestroyImmediate(machineObject);
    }

    [Test]
    public void CoinPlacerFilterAcceptsOnlyPhysicalCoins()
    {
        var placer = CreateCoinPlacer(out var placerObject);
        var coin = CreatePhysicalCoin("Accepted Coin");
        var nonCoin = CreateRewardPrefab("Rejected Non Coin");
        nonCoin.AddComponent<Rigidbody>();
        nonCoin.AddComponent<XRGrabInteractable>();

        Assert.That(placer.Process(null, coin.GetComponent<XRGrabInteractable>()), Is.True);
        Assert.That(placer.Process(null, nonCoin.GetComponent<XRGrabInteractable>()), Is.False);

        Object.DestroyImmediate(coin);
        Object.DestroyImmediate(nonCoin);
        Object.DestroyImmediate(placerObject);
    }

    [Test]
    public void CoinPlacerConsumesCoinAndArmsOneLeverCredit()
    {
        var manager = CreateGameManager();
        var placer = CreateCoinPlacer(out var placerObject);
        placer.GameManager = manager;
        var coin = CreatePhysicalCoin("Socketed Coin");
        var grabInteractable = coin.GetComponent<XRGrabInteractable>();
        Assert.That(manager.TryRegisterCoinPickup(coin.GetComponent<LotteryCoin>()), Is.True);

        Assert.That(placer.TryPlaceSocketInteractable(grabInteractable), Is.True);

        Assert.That(coin == null, Is.True);
        Assert.That(manager.Coins, Is.Zero);
        Assert.That(placer.ArmedCoinCount, Is.EqualTo(1));
        Assert.That(placer.TryConsumeArmedCoin(), Is.True);
        Assert.That(placer.HasArmedCoin, Is.False);

        Object.DestroyImmediate(placerObject);
        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void CoinPlacerCanArmWithUncountedCoinWithoutNegativeInventory()
    {
        var manager = CreateGameManager();
        var placer = CreateCoinPlacer(out var placerObject);
        placer.GameManager = manager;
        var coin = CreatePhysicalCoin("Uncounted Socketed Coin");

        Assert.That(placer.TryPlaceSocketInteractable(coin.GetComponent<XRGrabInteractable>()), Is.True);

        Assert.That(manager.Coins, Is.Zero);
        Assert.That(placer.ArmedCoinCount, Is.EqualTo(1));

        Object.DestroyImmediate(placerObject);
        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void CounterDepositSocketConsumesUncountedCoinAndIncrementsStoredCount()
    {
        var manager = CreateGameManager();
        var station = CreateCounterStation(out var stationObject, manager);
        var depositSocket = CreateCounterDepositSocket(station, out var socketObject);
        var coinObject = CreatePhysicalCoin("Counter Deposit Coin");
        var coin = coinObject.GetComponent<LotteryCoin>();
        coin.SetPickupCountingEnabled(false);

        Assert.That(depositSocket.Process(null, coinObject.GetComponent<XRGrabInteractable>()), Is.True);
        Assert.That(depositSocket.TryPlaceSocketInteractable(coinObject.GetComponent<XRGrabInteractable>()), Is.True);

        Assert.That(coinObject == null, Is.True);
        Assert.That(manager.Coins, Is.EqualTo(1));

        Object.DestroyImmediate(socketObject);
        Object.DestroyImmediate(stationObject);
        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void CounterDepositSocketRejectsAlreadyCountedCoinToAvoidDoubleCounting()
    {
        var manager = CreateGameManager(startingCoins: 1);
        var station = CreateCounterStation(out var stationObject, manager);
        var depositSocket = CreateCounterDepositSocket(station, out var socketObject);
        var coinObject = CreatePhysicalCoin("Already Counted Counter Coin");
        var coin = coinObject.GetComponent<LotteryCoin>();
        coin.MarkCountedInInventory();

        Assert.That(depositSocket.Process(null, coinObject.GetComponent<XRGrabInteractable>()), Is.False);
        Assert.That(depositSocket.TryPlaceSocketInteractable(coinObject.GetComponent<XRGrabInteractable>()), Is.False);

        Assert.That(coinObject, Is.Not.Null);
        Assert.That(manager.Coins, Is.EqualTo(1));

        Object.DestroyImmediate(coinObject);
        Object.DestroyImmediate(socketObject);
        Object.DestroyImmediate(stationObject);
        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void CounterStationExtractSpendsStoredCoinAndSpawnsUncountedPhysicalCoin()
    {
        var manager = CreateGameManager(startingCoins: 1);
        var coinPrefab = CreatePhysicalCoin("Counter Extract Coin Prefab");
        var spawnPoint = new GameObject("Counter Extract Spawn Point").transform;
        spawnPoint.position = new Vector3(4f, 5f, 6f);
        var parent = new GameObject("Counter Extract Parent").transform;
        var station = CreateCounterStation(out var stationObject, manager, coinPrefab, spawnPoint, parent);

        Assert.That(station.TryExtractCoin(), Is.True);

        Assert.That(manager.Coins, Is.Zero);
        Assert.That(station.LastExtractedCoin, Is.Not.Null);
        Assert.That(station.LastExtractedCoin.transform.parent, Is.SameAs(parent));
        Assert.That(station.LastExtractedCoin.transform.position, Is.EqualTo(spawnPoint.position));
        var extractedCoin = station.LastExtractedCoin.GetComponent<LotteryCoin>();
        Assert.That(extractedCoin.CountedInInventory, Is.False);
        Assert.That(extractedCoin.CountPickupInInventory, Is.False);
        Assert.That(extractedCoin.RegisterPickup(), Is.False);

        Object.DestroyImmediate(station.LastExtractedCoin);
        Object.DestroyImmediate(stationObject);
        Object.DestroyImmediate(parent.gameObject);
        Object.DestroyImmediate(spawnPoint.gameObject);
        Object.DestroyImmediate(coinPrefab);
        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void CounterStationExtractFailsWhenNoStoredCoinsAreAvailable()
    {
        var manager = CreateGameManager();
        var coinPrefab = CreatePhysicalCoin("Unused Counter Extract Coin Prefab");
        var station = CreateCounterStation(out var stationObject, manager, coinPrefab);

        Assert.That(station.TryExtractCoin(), Is.False);

        Assert.That(manager.Coins, Is.Zero);
        Assert.That(station.LastExtractedCoin, Is.Null);

        Object.DestroyImmediate(stationObject);
        Object.DestroyImmediate(coinPrefab);
        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void LeverRequiresArmedCoinBeforeStartingDraw()
    {
        var reward = CreateReward("lever_reward", 1f);
        var machineObject = new GameObject("LotteryMachine Lever Test");
        var machine = machineObject.AddComponent<global::LotteryMachine.LotteryMachine>();
        machine.RewardPool = CreatePool(reward);
        var placer = CreateCoinPlacer(out var placerObject);
        var leverObject = new GameObject("Lever Test");
        var lever = leverObject.AddComponent<LotteryLever>();
        SetPrivateField(lever, "lotteryMachine", machine);
        lever.CoinPlacer = placer;

        Assert.That(lever.Pull(), Is.False);
        Assert.That(machine.IsDrawing, Is.False);

        var coin = CreatePhysicalCoin("Lever Coin");
        Assert.That(placer.TryPlaceSocketInteractable(coin.GetComponent<XRGrabInteractable>()), Is.True);
        Assert.That(lever.Pull(), Is.True);

        Assert.That(placer.HasArmedCoin, Is.False);
        Assert.That(machine.IsDrawing, Is.True);

        Object.DestroyImmediate(leverObject);
        Object.DestroyImmediate(placerObject);
        Object.DestroyImmediate(machineObject);
    }

    [Test]
    public void CoinCounterDisplayRefreshesFromManagerBalance()
    {
        var manager = CreateGameManager();
        var displayObject = new GameObject("Coin Counter Display Test");
        var text = displayObject.AddComponent<TextMeshPro>();
        var display = displayObject.AddComponent<LotteryCoinCounterDisplay>();
        display.Configure(manager, text);

        Assert.That(text.text, Is.EqualTo("COINS: 0"));

        var coin = CreatePhysicalCoin("Displayed Coin");
        Assert.That(manager.TryRegisterCoinPickup(coin.GetComponent<LotteryCoin>()), Is.True);

        Assert.That(text.text, Is.EqualTo("COINS: 1"));

        Object.DestroyImmediate(coin);
        Object.DestroyImmediate(displayObject);
        Object.DestroyImmediate(manager.gameObject);
    }

    [Test]
    public void DisplayBoardPlacesNewCardInConfiguredFixedSlot()
    {
        var reward = CreateReward("board_reward", 1f);
        var boardObject = new GameObject("Reward Display Board Test");
        var anchor = new GameObject("Board Reward Anchor").transform;
        anchor.SetParent(boardObject.transform, false);
        anchor.localPosition = new Vector3(1f, 2f, 3f);
        var board = boardObject.AddComponent<RewardDisplayBoard>();
        board.ConfigureSlots(new[]
        {
            new RewardDisplayBoard.RewardDisplaySlotConfiguration(reward, anchor, null)
        });

        var card = CreateCardInstance("Board Reward Card", reward);
        var cardObject = card.gameObject;
        var expectedScale = GetExpectedDisplayScale(cardObject);

        Assert.That(board.TryPlaceCard(card), Is.True);

        Assert.That(cardObject.transform.parent, Is.SameAs(anchor));
        Assert.That(cardObject.transform.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(cardObject.transform.localRotation, Is.EqualTo(Quaternion.identity));
        Assert.That(cardObject.transform.localScale.x, Is.EqualTo(expectedScale.x).Within(0.0001f));
        Assert.That(cardObject.transform.localScale.y, Is.EqualTo(expectedScale.y).Within(0.0001f));
        Assert.That(cardObject.transform.localScale.z, Is.EqualTo(expectedScale.z).Within(0.0001f));
        Assert.That(board.IsRewardPlaced("board_reward"), Is.True);
        Assert.That(board.PlacedCount, Is.EqualTo(1));
        Assert.That(cardObject.GetComponent<Collider>().enabled, Is.False);
        Assert.That(cardObject.GetComponent<Rigidbody>().isKinematic, Is.True);
        Assert.That(cardObject.GetComponent<GrabbableReward>().enabled, Is.False);

        Object.DestroyImmediate(boardObject);
        Object.DestroyImmediate(cardObject);
    }

    [Test]
    public void DisplayBoardFitsCardFromRootColliderWhenChildRendererExtendsBounds()
    {
        var reward = CreateReward("decorated_board_reward", 1f);
        var boardObject = new GameObject("Reward Display Board Test");
        var anchor = new GameObject("Decorated Reward Anchor").transform;
        anchor.SetParent(boardObject.transform, false);
        var board = boardObject.AddComponent<RewardDisplayBoard>();
        board.ConfigureSlots(new[]
        {
            new RewardDisplayBoard.RewardDisplaySlotConfiguration(reward, anchor, null)
        });

        var card = CreateCardInstance("Decorated Board Reward Card", reward);
        var cardObject = card.gameObject;
        var expectedScale = GetExpectedDisplayScale(cardObject);
        var decoration = GameObject.CreatePrimitive(PrimitiveType.Cube);
        decoration.name = "Oversized Decorative Renderer";
        decoration.transform.SetParent(cardObject.transform, false);
        decoration.transform.localPosition = new Vector3(0f, 0f, 4f);
        decoration.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

        Assert.That(board.TryPlaceCard(card), Is.True);

        Assert.That(cardObject.transform.parent, Is.SameAs(anchor));
        Assert.That(cardObject.transform.localScale.x, Is.EqualTo(expectedScale.x).Within(0.0001f));
        Assert.That(cardObject.transform.localScale.y, Is.EqualTo(expectedScale.y).Within(0.0001f));
        Assert.That(cardObject.transform.localScale.z, Is.EqualTo(expectedScale.z).Within(0.0001f));

        Object.DestroyImmediate(boardObject);
        Object.DestroyImmediate(cardObject);
    }

    [Test]
    public void DisplayBoardDiscardsDuplicateCardAndKeepsOriginal()
    {
        var reward = CreateReward("duplicate_reward", 1f);
        var boardObject = new GameObject("Reward Display Board Test");
        var anchor = new GameObject("Duplicate Reward Anchor").transform;
        anchor.SetParent(boardObject.transform, false);
        var board = boardObject.AddComponent<RewardDisplayBoard>();
        board.ConfigureSlots(new[]
        {
            new RewardDisplayBoard.RewardDisplaySlotConfiguration(reward, anchor, null)
        });

        var firstCard = CreateCardInstance("First Duplicate Card", reward);
        var firstCardObject = firstCard.gameObject;
        var secondCard = CreateCardInstance("Second Duplicate Card", reward);
        var secondCardObject = secondCard.gameObject;

        Assert.That(board.TryPlaceCard(firstCard), Is.True);
        Assert.That(board.TryPlaceCard(secondCard), Is.True);

        Assert.That(firstCardObject, Is.Not.Null);
        Assert.That(firstCardObject.transform.parent, Is.SameAs(anchor));
        Assert.That(secondCardObject == null, Is.True);
        Assert.That(board.PlacedCount, Is.EqualTo(1));
        Assert.That(board.IsRewardPlaced("duplicate_reward"), Is.True);

        Object.DestroyImmediate(boardObject);
        Object.DestroyImmediate(firstCardObject);
    }

    [Test]
    public void DisplayBoardShowsCompletionTrophyAfterAllConfiguredCardsArePlaced()
    {
        var rewards = new[]
        {
            CreateReward("completion_reward_a", 1f),
            CreateReward("completion_reward_b", 1f)
        };
        var trophyPrefab = CreateCompletionTrophyPrefab("Completion Trophy Prefab");
        var board = CreateDisplayBoardWithCompletionTrophy(rewards, trophyPrefab, out var boardObject, out var trophyAnchor);
        var firstCard = CreateCardInstance("Completion Card A", rewards[0]);
        var secondCard = CreateCardInstance("Completion Card B", rewards[1]);

        Assert.That(board.CompletionTrophyShown, Is.False);
        Assert.That(board.SpawnedCompletionTrophy, Is.Null);

        Assert.That(board.TryPlaceCard(firstCard), Is.True);

        Assert.That(board.CompletionTrophyShown, Is.False);
        Assert.That(board.SpawnedCompletionTrophy, Is.Null);

        Assert.That(board.TryPlaceCard(secondCard), Is.True);

        var trophy = board.SpawnedCompletionTrophy;
        Assert.That(board.CompletionTrophyShown, Is.True);
        Assert.That(trophy, Is.Not.Null);
        Assert.That(trophy.transform.parent, Is.SameAs(trophyAnchor));
        Assert.That(trophy.transform.localPosition, Is.EqualTo(Vector3.zero));
        Assert.That(trophy.transform.localRotation, Is.EqualTo(Quaternion.identity));
        Assert.That(trophy.transform.localScale, Is.EqualTo(Vector3.one));
        Assert.That(trophy.GetComponent<Collider>().enabled, Is.False);
        Assert.That(trophy.GetComponent<Rigidbody>().isKinematic, Is.True);
        Assert.That(trophy.GetComponent<XRGrabInteractable>().enabled, Is.False);
        Assert.That(trophy.GetComponent<GrabbableReward>().enabled, Is.False);

        Object.DestroyImmediate(boardObject);
        Object.DestroyImmediate(trophyPrefab);
    }

    [Test]
    public void DisplayBoardDuplicateCardDoesNotCreateAdditionalCompletionTrophies()
    {
        var reward = CreateReward("single_completion_reward", 1f);
        var trophyPrefab = CreateCompletionTrophyPrefab("Single Completion Trophy Prefab");
        var board = CreateDisplayBoardWithCompletionTrophy(new[] { reward }, trophyPrefab, out var boardObject, out var trophyAnchor);
        var firstCard = CreateCardInstance("First Completion Card", reward);
        var secondCard = CreateCardInstance("Duplicate Completion Card", reward);
        var secondCardObject = secondCard.gameObject;

        Assert.That(board.TryPlaceCard(firstCard), Is.True);
        var firstTrophy = board.SpawnedCompletionTrophy;

        Assert.That(board.TryPlaceCard(secondCard), Is.True);

        Assert.That(board.SpawnedCompletionTrophy, Is.SameAs(firstTrophy));
        Assert.That(trophyAnchor.childCount, Is.EqualTo(1));
        Assert.That(secondCardObject == null, Is.True);

        Object.DestroyImmediate(boardObject);
        Object.DestroyImmediate(trophyPrefab);
    }

    [Test]
    public void DisplayBoardIgnoresUnknownCard()
    {
        var configuredReward = CreateReward("configured_reward", 1f);
        var unknownReward = CreateReward("unknown_reward", 1f);
        var boardObject = new GameObject("Reward Display Board Test");
        var anchor = new GameObject("Configured Reward Anchor").transform;
        anchor.SetParent(boardObject.transform, false);
        var board = boardObject.AddComponent<RewardDisplayBoard>();
        board.ConfigureSlots(new[]
        {
            new RewardDisplayBoard.RewardDisplaySlotConfiguration(configuredReward, anchor, null)
        });

        var unknownCard = CreateCardInstance("Unknown Reward Card", unknownReward);
        var unknownCardObject = unknownCard.gameObject;

        Assert.That(board.TryPlaceCard(unknownCard), Is.False);

        Assert.That(unknownCardObject, Is.Not.Null);
        Assert.That(unknownCardObject.transform.parent, Is.Null);
        Assert.That(board.PlacedCount, Is.Zero);
        Assert.That(board.IsRewardPlaced("configured_reward"), Is.False);

        Object.DestroyImmediate(boardObject);
        Object.DestroyImmediate(unknownCardObject);
    }

    [Test]
    public void DisplaySocketFilterAcceptsKnownCard()
    {
        var reward = CreateReward("socket_reward", 1f);
        var board = CreateDisplayBoardWithSocket(reward, out var boardObject, out _, out var slot);
        var card = CreateCardInstance("Known Socket Card", reward);
        var cardObject = card.gameObject;
        var grabInteractable = card.GetComponent<XRGrabInteractable>();

        Assert.That(slot.Process(null, grabInteractable), Is.True);
        Assert.That(board.CanAcceptCard(card), Is.True);

        Object.DestroyImmediate(cardObject);
        Object.DestroyImmediate(boardObject);
    }

    [Test]
    public void DisplaySocketFilterRejectsUnknownCard()
    {
        var configuredReward = CreateReward("socket_configured_reward", 1f);
        var unknownReward = CreateReward("socket_unknown_reward", 1f);
        CreateDisplayBoardWithSocket(configuredReward, out var boardObject, out _, out var slot);
        var card = CreateCardInstance("Unknown Socket Card", unknownReward);
        var cardObject = card.gameObject;

        Assert.That(slot.Process(null, card.GetComponent<XRGrabInteractable>()), Is.False);

        Object.DestroyImmediate(cardObject);
        Object.DestroyImmediate(boardObject);
    }

    [Test]
    public void DisplaySocketSelectionRoutesCardToConfiguredFixedSlot()
    {
        var rewardA = CreateReward("socket_reward_a", 1f);
        var rewardB = CreateReward("socket_reward_b", 1f);
        var boardObject = new GameObject("Reward Display Board Test");
        var board = boardObject.AddComponent<RewardDisplayBoard>();
        var anchorA = CreateAnchor("Socket Reward A Anchor", boardObject.transform);
        var anchorB = CreateAnchor("Socket Reward B Anchor", boardObject.transform);
        var slotA = CreateSocketSlot("Socket A", boardObject.transform, board, anchorA);
        var slotB = CreateSocketSlot("Socket B", boardObject.transform, board, anchorB);
        board.ConfigureSlots(new[]
        {
            new RewardDisplayBoard.RewardDisplaySlotConfiguration(rewardA, anchorA, slotA),
            new RewardDisplayBoard.RewardDisplaySlotConfiguration(rewardB, anchorB, slotB)
        });

        var card = CreateCardInstance("Socket Routed Card", rewardB);
        var cardObject = card.gameObject;
        var grabInteractable = card.GetComponent<XRGrabInteractable>();

        Assert.That(slotA.TryPlaceSocketInteractable(grabInteractable), Is.True);

        Assert.That(card.transform.parent, Is.SameAs(anchorB));
        Assert.That(board.IsRewardPlaced("socket_reward_b"), Is.True);
        Assert.That(board.IsRewardPlaced("socket_reward_a"), Is.False);

        Object.DestroyImmediate(cardObject);
        Object.DestroyImmediate(boardObject);
    }

    [Test]
    public void DisplaySocketPlacementDiscardsDuplicateCardAndKeepsOriginal()
    {
        var reward = CreateReward("socket_duplicate_reward", 1f);
        var board = CreateDisplayBoardWithSocket(reward, out var boardObject, out var anchor, out var slot);
        var firstCard = CreateCardInstance("First Socket Duplicate Card", reward);
        var firstCardObject = firstCard.gameObject;
        var secondCard = CreateCardInstance("Second Socket Duplicate Card", reward);
        var secondCardObject = secondCard.gameObject;

        Assert.That(board.TryPlaceCard(firstCard), Is.True);
        Assert.That(slot.TryPlaceSocketInteractable(secondCard.GetComponent<XRGrabInteractable>()), Is.True);

        Assert.That(firstCardObject, Is.Not.Null);
        Assert.That(firstCardObject.transform.parent, Is.SameAs(anchor));
        Assert.That(secondCardObject == null, Is.True);
        Assert.That(board.PlacedCount, Is.EqualTo(1));

        Object.DestroyImmediate(boardObject);
        Object.DestroyImmediate(firstCardObject);
    }

    [Test]
    public void DisplayBoardTriggerFallbackStillPlacesUnselectedCard()
    {
        var reward = CreateReward("trigger_fallback_reward", 1f);
        var board = CreateDisplayBoardWithSocket(reward, out var boardObject, out var anchor, out _);
        var card = CreateCardInstance("Trigger Fallback Card", reward);
        var cardObject = card.gameObject;

        Assert.That(board.TryPlaceFromCollider(card.GetComponent<Collider>()), Is.True);

        Assert.That(cardObject.transform.parent, Is.SameAs(anchor));
        Assert.That(board.IsRewardPlaced("trigger_fallback_reward"), Is.True);

        Object.DestroyImmediate(cardObject);
        Object.DestroyImmediate(boardObject);
    }

    [Test]
    public void DisplayBoardTriggerFallbackIgnoresCardSelectedByNonSocketInteractor()
    {
        var reward = CreateReward("selected_trigger_reward", 1f);
        var board = CreateDisplayBoardWithSocket(reward, out var boardObject, out _, out _);
        var interactorObject = new GameObject("Direct Interactor Test");
        var interactorCollider = interactorObject.AddComponent<SphereCollider>();
        interactorCollider.isTrigger = true;
        var directInteractor = interactorObject.AddComponent<XRDirectInteractor>();
        var card = CreateCardInstance("Selected Trigger Card", reward);
        var cardObject = card.gameObject;
        var grabInteractable = card.GetComponent<XRGrabInteractable>();
        SetPrivateField(grabInteractable, "<isSelected>k__BackingField", true);
        SetPrivateField(grabInteractable, "<firstInteractorSelecting>k__BackingField", (IXRSelectInteractor)directInteractor);

        Assert.That(grabInteractable.isSelected, Is.True);
        Assert.That(board.TryPlaceFromCollider(card.GetComponent<Collider>()), Is.False);
        Assert.That(card.transform.parent, Is.Null);
        Assert.That(board.PlacedCount, Is.Zero);

        Object.DestroyImmediate(interactorObject);
        Object.DestroyImmediate(cardObject);
        Object.DestroyImmediate(boardObject);
    }

    [Test]
    public void SampleMachinePrefabDoesNotContainDisplayBoard()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SampleMachinePrefabPath);

        Assert.That(prefab, Is.Not.Null);
        Assert.That(prefab.GetComponentsInChildren<RewardDisplayBoard>(true), Is.Empty);
    }

    [Test]
    public void SampleMachinePrefabMatchesCurrentBuilderContract()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SampleMachinePrefabPath);
        var samplePool = AssetDatabase.LoadAssetAtPath<RewardPool>("Assets/LotteryMachine/Sample/Rewards/PhokemonRewardPool.asset");

        Assert.That(prefab, Is.Not.Null);
        Assert.That(samplePool, Is.Not.Null);

        var machine = prefab.GetComponent<global::LotteryMachine.LotteryMachine>();
        Assert.That(machine, Is.Not.Null);
        var machineSerialized = new SerializedObject(machine);
        Assert.That(machineSerialized.FindProperty("rewardPool").objectReferenceValue, Is.SameAs(samplePool));
        Assert.That(machineSerialized.FindProperty("rewardRevealAudioSource").objectReferenceValue, Is.Null);
        Assert.That(machineSerialized.FindProperty("rewardRevealSound").objectReferenceValue, Is.Null);
        Assert.That(prefab.GetComponentsInChildren<AudioSource>(true), Is.Empty);

        var signLabelObject = FindRequiredTransform(prefab.transform, "SignPanel/Label");
        Assert.That(signLabelObject.GetComponent<TextMesh>(), Is.Null);
        var signLabel = signLabelObject.GetComponent<TextMeshPro>();
        Assert.That(signLabel, Is.Not.Null);
        Assert.That(signLabel.text, Is.EqualTo("POKENMON"));

        Assert.That(prefab.transform.Find("DrawButton"), Is.Null);

        var addMoneyButton = FindRequiredTransform(prefab.transform, "AddMoneyButton");
        AssertLocalTransform(addMoneyButton, new Vector3(-0.542f, 0.88f, 0.164f), new Vector3(0f, 90f, 0f), Vector3.one);
        var addMoneyPokeButton = addMoneyButton.GetComponent<LotteryPokeButton>();
        Assert.That(addMoneyPokeButton, Is.Not.Null);
        AssertSinglePersistentListener(addMoneyPokeButton.PressedEvent, "AddMoney");

        var exchangeButton = FindRequiredTransform(prefab.transform, "ExchangeButton");
        AssertLocalTransform(exchangeButton, new Vector3(-0.542f, 0.88f, -0.176f), new Vector3(0f, 90f, 0f), Vector3.one);
        var exchangePokeButton = exchangeButton.GetComponent<LotteryPokeButton>();
        Assert.That(exchangePokeButton, Is.Not.Null);
        AssertSinglePersistentListener(exchangePokeButton.PressedEvent, "ExchangeMoneyForCoin");

        var coinPlacer = FindRequiredTransform(prefab.transform, "CoinPlacer");
        AssertLocalTransform(coinPlacer, new Vector3(0.33998f, 0.88f, -0.374f), Vector3.zero, new Vector3(0.7733686f, 1f, 1f));
        Assert.That(coinPlacer.GetComponent<XRSimpleInteractable>(), Is.Null);
        Assert.That(coinPlacer.GetComponent<XRSocketInteractor>(), Is.Not.Null);
        var coinPlacerComponent = coinPlacer.GetComponent<LotteryCoinPlacer>();
        Assert.That(coinPlacerComponent, Is.Not.Null);
        Assert.That(FindRequiredTransform(coinPlacer, "CoinSocketAttach"), Is.Not.Null);
        Assert.That(coinPlacer.GetComponent<LotteryPokeButton>(), Is.Null);
        Assert.That(FindRequiredTransform(coinPlacer, "boarder 1").GetComponent<BoxCollider>(), Is.Not.Null);
        Assert.That(FindRequiredTransform(coinPlacer, "boarder 2").GetComponent<BoxCollider>(), Is.Not.Null);
        Assert.That(FindRequiredTransform(coinPlacer, "boarder 3").GetComponent<BoxCollider>(), Is.Not.Null);
        Assert.That(FindRequiredTransform(coinPlacer, "boarder 4").GetComponent<BoxCollider>(), Is.Not.Null);
        Assert.That(FindRequiredTransform(coinPlacer, "Housing").GetComponent<BoxCollider>(), Is.Null);

        var pullLever = FindRequiredTransform(prefab.transform, "PullLever");
        AssertLocalTransform(pullLever, new Vector3(0.398f, 0.92f, -0.036f), Vector3.zero, Vector3.one);
        Assert.That(pullLever.GetComponent<LotteryLever>().CoinPlacer, Is.SameAs(coinPlacerComponent));

        var coinSpawnPoint = FindRequiredTransform(prefab.transform, "CoinSpawnPoint");
        AssertLocalTransform(coinSpawnPoint, new Vector3(0.34f, 0.98f, -0.74f), Vector3.zero, Vector3.one);

        var gameManager = prefab.GetComponent<LotteryGameManager>();
        Assert.That(gameManager.CoinPrefab, Is.SameAs(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Coin.prefab")));
        Assert.That(gameManager.CoinSpawnPoint, Is.SameAs(coinSpawnPoint));
        Assert.That(coinPlacerComponent.GameManager, Is.SameAs(gameManager));

        Assert.That(prefab.transform.Find("CoinCounter"), Is.Null);
    }

    [Test]
    public void SampleDisplayBoardPrefabHasConfiguredSlots()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SampleDisplayBoardPrefabPath);

        Assert.That(prefab, Is.Not.Null);
        var boards = prefab.GetComponentsInChildren<RewardDisplayBoard>(true);
        Assert.That(boards, Has.Length.EqualTo(1));

        var board = boards[0];
        Assert.That(board.Slots, Has.Count.EqualTo(10));
        var trophyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CompletionTrophyPrefabPath);
        var trophyAnchor = FindRequiredTransform(prefab.transform, "CompletionTrophyAnchor");
        Assert.That(board.CompletionTrophyPrefab, Is.SameAs(trophyPrefab));
        Assert.That(board.CompletionTrophyAnchor, Is.SameAs(trophyAnchor));
        AssertLocalTransform(trophyAnchor, CompletionTrophyAnchorPosition, Vector3.zero, CompletionTrophyAnchorScale);
        Assert.That(new SerializedObject(board).FindProperty("completionTrophyFloatHeight").floatValue, Is.EqualTo(CompletionTrophyFloatHeight).Within(0.0001f));
        foreach (var slot in board.Slots)
        {
            Assert.That(slot.Reward, Is.Not.Null);
            Assert.That(slot.Anchor, Is.Not.Null);
            Assert.That(slot.Trigger, Is.Not.Null);
        }
    }

    [Test]
    public void SampleCoinCounterPrefabMatchesCounterContract()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SampleCoinCounterPrefabPath);
        var sampleCoinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Coin.prefab");

        Assert.That(prefab, Is.Not.Null);
        var station = prefab.GetComponent<LotteryCoinCounterStation>();
        Assert.That(station, Is.Not.Null);
        Assert.That(station.CoinPrefab, Is.SameAs(sampleCoinPrefab));
        Assert.That(new SerializedObject(station).FindProperty("gameManager").objectReferenceValue, Is.Null);

        var coinCounter = FindRequiredTransform(prefab.transform, "CoinCounter");
        var display = coinCounter.GetComponent<LotteryCoinCounterDisplay>();
        Assert.That(display, Is.Not.Null);
        Assert.That(display.GameManager, Is.Null);
        var valueLabel = FindRequiredTransform(coinCounter, "Value");
        Assert.That(valueLabel.GetComponent<TextMesh>(), Is.Null);
        Assert.That(valueLabel.GetComponent<TextMeshPro>().text, Is.EqualTo("COINS: 0"));

        var exchangeButton = FindRequiredTransform(prefab.transform, "ExchangeButton").GetComponent<LotteryPokeButton>();
        Assert.That(exchangeButton, Is.Not.Null);
        AssertSinglePersistentListener(exchangeButton.PressedEvent, typeof(LotteryCoinCounterStation), "Exchange");

        var extractButton = FindRequiredTransform(prefab.transform, "ExtractButton").GetComponent<LotteryPokeButton>();
        Assert.That(extractButton, Is.Not.Null);
        AssertSinglePersistentListener(extractButton.PressedEvent, typeof(LotteryCoinCounterStation), "ExtractCoin");

        var depositSocket = FindRequiredTransform(prefab.transform, "DepositSocket");
        Assert.That(depositSocket.GetComponent<XRSocketInteractor>(), Is.Not.Null);
        var depositSocketComponent = depositSocket.GetComponent<LotteryCoinCounterDepositSocket>();
        Assert.That(depositSocketComponent, Is.Not.Null);
        Assert.That(depositSocketComponent.Station, Is.SameAs(station));
        Assert.That(FindRequiredTransform(depositSocket, "DepositSocketAttach"), Is.Not.Null);

        var coinSpawnPoint = FindRequiredTransform(prefab.transform, "ExtractCoinSpawnPoint");
        Assert.That(station.CoinSpawnPoint, Is.SameAs(coinSpawnPoint));
    }

    [Test]
    public void SampleSceneContainsFiveLooseDepositOnlyCounterCoins()
    {
        var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);

            var coinParent = GameObject.Find("LooseCounterCoins");
            Assert.That(coinParent, Is.Not.Null);

            var gameManager = Object.FindFirstObjectByType<LotteryGameManager>();
            Assert.That(gameManager, Is.Not.Null);

            var station = Object.FindFirstObjectByType<LotteryCoinCounterStation>();
            Assert.That(station, Is.Not.Null);
            Assert.That(station.GameManager, Is.SameAs(gameManager));

            var coins = coinParent.GetComponentsInChildren<LotteryCoin>(true);
            Assert.That(coins, Has.Length.EqualTo(5));

            foreach (var coin in coins)
            {
                Assert.That(coin.GetComponent<XRGrabInteractable>(), Is.Not.Null);
                Assert.That(coin.CountedInInventory, Is.False);
                Assert.That(coin.CountPickupInInventory, Is.False);
                Assert.That(coin.GameManager, Is.SameAs(gameManager));
            }
        }
        finally
        {
            if (sceneSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
            }
        }
    }

    private static RewardDefinition CreateReward(string id, float weight, GameObject rewardPrefab = null)
    {
        var reward = ScriptableObject.CreateInstance<RewardDefinition>();
        SetPrivateField(reward, "rewardId", id);
        SetPrivateField(reward, "displayName", id);
        SetPrivateField(reward, "weight", weight);
        SetPrivateField(reward, "rewardPrefab", rewardPrefab);
        return reward;
    }

    private static GameObject CreateRewardPrefab(string name)
    {
        var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = name;
        prefab.transform.localScale = new Vector3(0.58f, 0.035f, 0.82f);
        return prefab;
    }

    private static RewardCardInstance CreateCardInstance(string name, RewardDefinition reward)
    {
        var cardObject = CreateRewardPrefab(name);
        cardObject.AddComponent<Rigidbody>();
        if (cardObject.GetComponent<XRGrabInteractable>() == null)
        {
            cardObject.AddComponent<XRGrabInteractable>();
        }

        cardObject.AddComponent<GrabbableReward>();

        var card = cardObject.AddComponent<RewardCardInstance>();
        card.Initialize(reward);
        return card;
    }

    private static GameObject CreateCompletionTrophyPrefab(string name)
    {
        var trophyPrefab = CreateRewardPrefab(name);
        trophyPrefab.AddComponent<Rigidbody>();
        trophyPrefab.AddComponent<XRGrabInteractable>();
        trophyPrefab.AddComponent<GrabbableReward>();
        return trophyPrefab;
    }

    private static Vector3 GetExpectedDisplayScale(GameObject cardObject)
    {
        var originalScale = cardObject.transform.localScale;
        var collider = cardObject.GetComponent<BoxCollider>();
        var footprint = collider != null ? new Vector2(collider.size.x, collider.size.z) : Vector2.one;
        var scaledFootprint = new Vector2(
            footprint.x * Mathf.Abs(originalScale.x),
            footprint.y * Mathf.Abs(originalScale.z));
        var targetDisplaySize = new Vector2(DisplaySlotWidth, DisplaySlotHeight) * DisplaySlotFillRatio;
        var expectedScaleMultiplier = Mathf.Min(targetDisplaySize.x / scaledFootprint.x, targetDisplaySize.y / scaledFootprint.y);
        return originalScale * expectedScaleMultiplier;
    }

    private static GameObject CreatePhysicalCoin(string name)
    {
        var coinObject = CreateRewardPrefab(name);
        LotteryCoin.PrepareCoinObject(coinObject, false);
        return coinObject;
    }

    private static LotteryCoinPlacer CreateCoinPlacer(out GameObject placerObject)
    {
        placerObject = new GameObject("Coin Placer Test");
        var collider = placerObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        var socket = placerObject.AddComponent<XRSocketInteractor>();
        var placer = placerObject.AddComponent<LotteryCoinPlacer>();
        placer.Configure(socket, null);
        return placer;
    }

    private static LotteryCoinCounterStation CreateCounterStation(
        out GameObject stationObject,
        LotteryGameManager manager = null,
        GameObject coinPrefab = null,
        Transform coinSpawnPoint = null,
        Transform coinParent = null)
    {
        stationObject = new GameObject("Coin Counter Station Test");
        var station = stationObject.AddComponent<LotteryCoinCounterStation>();
        station.GameManager = manager;
        station.CoinPrefab = coinPrefab;
        station.CoinSpawnPoint = coinSpawnPoint;
        station.CoinParent = coinParent;
        return station;
    }

    private static LotteryCoinCounterDepositSocket CreateCounterDepositSocket(
        LotteryCoinCounterStation station,
        out GameObject socketObject)
    {
        socketObject = new GameObject("Counter Deposit Socket Test");
        socketObject.transform.SetParent(station.transform, false);
        var collider = socketObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        var socket = socketObject.AddComponent<XRSocketInteractor>();
        var depositSocket = socketObject.AddComponent<LotteryCoinCounterDepositSocket>();
        depositSocket.Configure(station, socket, null);
        return depositSocket;
    }

    private static RewardDisplayBoard CreateDisplayBoardWithSocket(
        RewardDefinition reward,
        out GameObject boardObject,
        out Transform anchor,
        out RewardDisplaySlot slot)
    {
        boardObject = new GameObject("Reward Display Board Test");
        var board = boardObject.AddComponent<RewardDisplayBoard>();
        anchor = CreateAnchor("Reward Anchor", boardObject.transform);
        slot = CreateSocketSlot("Reward Socket", boardObject.transform, board, anchor);
        board.ConfigureSlots(new[]
        {
            new RewardDisplayBoard.RewardDisplaySlotConfiguration(reward, anchor, slot)
        });
        return board;
    }

    private static RewardDisplayBoard CreateDisplayBoardWithCompletionTrophy(
        RewardDefinition[] rewards,
        GameObject trophyPrefab,
        out GameObject boardObject,
        out Transform trophyAnchor)
    {
        boardObject = new GameObject("Reward Display Board Test");
        var board = boardObject.AddComponent<RewardDisplayBoard>();
        var slotConfigurations = new System.Collections.Generic.List<RewardDisplayBoard.RewardDisplaySlotConfiguration>();
        for (var i = 0; i < rewards.Length; i++)
        {
            var anchor = CreateAnchor($"Reward Anchor {i}", boardObject.transform);
            slotConfigurations.Add(new RewardDisplayBoard.RewardDisplaySlotConfiguration(rewards[i], anchor, null));
        }

        trophyAnchor = CreateAnchor("CompletionTrophyAnchor", boardObject.transform);
        trophyAnchor.localPosition = CompletionTrophyAnchorPosition;
        trophyAnchor.localScale = CompletionTrophyAnchorScale;
        board.ConfigureSlots(slotConfigurations);
        board.ConfigureCompletionTrophy(trophyPrefab, trophyAnchor);
        return board;
    }

    private static Transform CreateAnchor(string name, Transform parent)
    {
        var anchor = new GameObject(name).transform;
        anchor.SetParent(parent, false);
        return anchor;
    }

    private static RewardDisplaySlot CreateSocketSlot(string name, Transform parent, RewardDisplayBoard board, Transform anchor)
    {
        var slotObject = new GameObject(name);
        slotObject.transform.SetParent(parent, false);
        var collider = slotObject.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        var socket = slotObject.AddComponent<XRSocketInteractor>();
        var slot = slotObject.AddComponent<RewardDisplaySlot>();
        slot.Configure(board, socket, anchor);
        return slot;
    }

    private static RewardPool CreatePool(params RewardDefinition[] rewards)
    {
        var pool = ScriptableObject.CreateInstance<RewardPool>();
        SetPrivateField(pool, "rewards", new System.Collections.Generic.List<RewardDefinition>(rewards));
        return pool;
    }

    private static LotteryGameManager CreateGameManager(int startingMoney = 0, int startingCoins = 0, global::LotteryMachine.LotteryMachine lotteryMachine = null)
    {
        var managerObject = new GameObject("LotteryGameManager Test");
        var manager = managerObject.AddComponent<LotteryGameManager>();
        SetPrivateField(manager, "money", startingMoney);
        SetPrivateField(manager, "coins", startingCoins);
        SetPrivateField(manager, "lotteryMachine", lotteryMachine);
        return manager;
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        var targetType = target.GetType();
        System.Reflection.FieldInfo field = null;
        while (targetType != null && field == null)
        {
            field = targetType.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            targetType = targetType.BaseType;
        }

        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static Transform FindRequiredTransform(Transform parent, string path)
    {
        var child = parent.Find(path);
        Assert.That(child, Is.Not.Null, $"Expected to find child path '{path}'.");
        return child;
    }

    private static void AssertLocalTransform(Transform transform, Vector3 expectedPosition, Vector3 expectedEuler, Vector3 expectedScale)
    {
        AssertVector3(transform.localPosition, expectedPosition);
        AssertVector3(transform.localEulerAngles, expectedEuler);
        AssertVector3(transform.localScale, expectedScale);
    }

    private static void AssertVector3(Vector3 actual, Vector3 expected)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.0001f));
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.0001f));
        Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.0001f));
    }

    private static void AssertSinglePersistentListener(UnityEvent unityEvent, string methodName)
    {
        AssertSinglePersistentListener(unityEvent, typeof(LotteryGameManager), methodName);
    }

    private static void AssertSinglePersistentListener(UnityEvent unityEvent, System.Type targetType, string methodName)
    {
        Assert.That(unityEvent.GetPersistentEventCount(), Is.EqualTo(1));
        Assert.That(unityEvent.GetPersistentTarget(0), Is.InstanceOf(targetType));
        Assert.That(unityEvent.GetPersistentMethodName(0), Is.EqualTo(methodName));
    }
}
