using System;
using LotteryMachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public static class GreenhouseLotteryStationInstaller
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string RootName = "LotteryStationRoot";
    private const string MachinePrefabPath = "Assets/LotteryMachine/Sample/Prefabs/LotteryMachine.prefab";
    private const string CounterPrefabPath = "Assets/LotteryMachine/Sample/Prefabs/LotteryCoinCounter.prefab";
    private const string BoardPrefabPath = "Assets/LotteryMachine/Sample/Prefabs/RewardDisplayBoard.prefab";

    [MenuItem("Tools/Greenhouse/Install Lottery Station")]
    public static void InstallLotteryStation()
    {
        OpenTargetScene();
        RemoveExistingStation();

        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;

        var machine = InstantiatePrefab(MachinePrefabPath, root.transform, "LotteryMachine");
        var counter = InstantiatePrefab(CounterPrefabPath, root.transform, "LotteryCoinCounter");
        var board = InstantiatePrefab(BoardPrefabPath, root.transform, "RewardDisplayBoard");

        if (machine == null || counter == null || board == null)
        {
            throw new InvalidOperationException("Lottery station install failed because one or more lottery prefabs could not be loaded.");
        }

        ConfigurePlacement(machine, counter, board);
        ConfigureIntegration(root, machine, counter);

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("Installed greenhouse lottery station into SampleScene.");
    }

    private static void OpenTargetScene()
    {
        if (!SceneManager.GetActiveScene().path.Equals(ScenePath, StringComparison.OrdinalIgnoreCase))
        {
            EditorSceneManager.OpenScene(ScenePath);
        }
    }

    private static void RemoveExistingStation()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            UnityEngine.Object.DestroyImmediate(existing);
        }
    }

    private static GameObject InstantiatePrefab(string prefabPath, Transform parent, string instanceName)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (asset == null)
        {
            Debug.LogError($"Missing lottery prefab at {prefabPath}.");
            return null;
        }

        var instance = PrefabUtility.InstantiatePrefab(asset, SceneManager.GetActiveScene()) as GameObject;
        if (instance == null)
        {
            return null;
        }

        instance.name = instanceName;
        instance.transform.SetParent(parent, true);
        return instance;
    }

    private static void ConfigurePlacement(GameObject machine, GameObject counter, GameObject board)
    {
        var facePlayer = Quaternion.Euler(0f, 180f, 0f);

        machine.transform.SetPositionAndRotation(new Vector3(-0.45f, 0f, -2.55f), facePlayer);
        machine.transform.localScale = Vector3.one * 0.82f;

        counter.transform.SetPositionAndRotation(new Vector3(0.78f, 0f, -2.35f), facePlayer);
        counter.transform.localScale = Vector3.one;

        board.transform.SetPositionAndRotation(new Vector3(0.72f, 1.38f, -2.82f), facePlayer);
        board.transform.localScale = Vector3.one;
    }

    private static void ConfigureIntegration(GameObject root, GameObject machine, GameObject counter)
    {
        var gameManager = machine.GetComponentInChildren<LotteryGameManager>(true);
        var station = counter.GetComponentInChildren<LotteryCoinCounterStation>(true);
        if (gameManager == null || station == null)
        {
            throw new InvalidOperationException("Lottery station install failed because required runtime components were not found.");
        }

        var bridge = root.AddComponent<GreenhouseLotteryBridge>();
        bridge.LotteryGameManager = gameManager;
        bridge.MoneyPerCoin = 1f;

        station.GameManager = gameManager;
        EditorUtility.SetDirty(station);
        EditorUtility.SetDirty(bridge);

        DisableDemoMachineButton(machine, "AddMoneyButton");
        DisableDemoMachineButton(machine, "ExchangeButton");
        RewireCounterExchange(counter, bridge);
    }

    private static void DisableDemoMachineButton(GameObject machine, string buttonName)
    {
        var buttonTransform = FindChildByName(machine.transform, buttonName);
        if (buttonTransform != null)
        {
            buttonTransform.gameObject.SetActive(false);
        }
    }

    private static void RewireCounterExchange(GameObject counter, GreenhouseLotteryBridge bridge)
    {
        var exchangeTransform = FindChildByName(counter.transform, "ExchangeButton");
        var exchangeButton = exchangeTransform != null
            ? exchangeTransform.GetComponent<LotteryPokeButton>()
            : null;

        if (exchangeButton == null)
        {
            throw new InvalidOperationException("Lottery counter ExchangeButton was not found.");
        }

        RemoveAllPersistentListeners(exchangeButton.PressedEvent);
        UnityEventTools.AddPersistentListener(exchangeButton.PressedEvent, bridge.ExchangeGreenhouseMoneyForCoin);
        EditorUtility.SetDirty(exchangeButton);
    }

    private static void RemoveAllPersistentListeners(UnityEvent unityEvent)
    {
        for (var i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(unityEvent, i);
        }
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root.name == childName)
        {
            return root;
        }

        for (var i = 0; i < root.childCount; i++)
        {
            var match = FindChildByName(root.GetChild(i), childName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
