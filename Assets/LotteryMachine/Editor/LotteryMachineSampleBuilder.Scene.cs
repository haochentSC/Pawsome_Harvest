using System;
using LotteryMachine;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LotteryMachine.EditorTools
{
    public static partial class LotteryMachineSampleBuilder
    {
        private const string LooseCounterCoinsName = "LooseCounterCoins";
        private const int LooseCounterCoinCount = 5;
        private static readonly Vector3 CoinCounterScenePosition = new Vector3(-2.97f, 0f, -0.06f);
        private static readonly Quaternion CoinCounterSceneRotation = Quaternion.identity;
        private static readonly Vector3 DisplayBoardScenePosition = new Vector3(1.22f, 1.02f, 0.18f);
        private static readonly Quaternion DisplayBoardSceneRotation = Quaternion.Euler(0f, -10f, 0f);

        private static void ConfigureSampleScene(
            string machinePrefabPath,
            string displayBoardPrefabPath,
            string coinCounterPrefabPath,
            Material floorMaterial)
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            if (!SceneManager.GetActiveScene().path.Equals(scenePath, StringComparison.OrdinalIgnoreCase))
            {
                EditorSceneManager.OpenScene(scenePath);
            }

            foreach (var existing in UnityEngine.Object.FindObjectsByType<global::LotteryMachine.LotteryMachine>(FindObjectsSortMode.None))
            {
                DestroyGeneratedObject(existing.gameObject);
            }

            var floor = GameObject.Find("Lottery Demo Floor") ?? GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Lottery Demo Floor";
            floor.transform.position = new Vector3(0f, -0.03f, 0.4f);
            floor.transform.localScale = new Vector3(4.5f, 0.06f, 4.5f);
            var floorRenderer = floor.GetComponent<Renderer>();
            if (floorRenderer != null)
            {
                floorRenderer.sharedMaterial = floorMaterial;
            }

            var machineAsset = AssetDatabase.LoadAssetAtPath<GameObject>(machinePrefabPath);
            var instance = PrefabUtility.InstantiatePrefab(machineAsset) as GameObject;
            if (instance != null)
            {
                instance.name = "LotteryMachine Demo Instance";
                instance.transform.position = new Vector3(0f, 0f, 0.6f);
                instance.transform.rotation = Quaternion.identity;
            }

            foreach (var existing in UnityEngine.Object.FindObjectsByType<RewardDisplayBoard>(FindObjectsSortMode.None))
            {
                DestroyGeneratedObject(existing.gameObject);
            }

            var displayBoardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(displayBoardPrefabPath);
            if (displayBoardAsset != null)
            {
                var displayBoardInstance = PrefabUtility.InstantiatePrefab(displayBoardAsset) as GameObject;
                if (displayBoardInstance != null)
                {
                    displayBoardInstance.name = "RewardDisplayBoard Demo Instance";
                    displayBoardInstance.transform.position = DisplayBoardScenePosition;
                    displayBoardInstance.transform.rotation = DisplayBoardSceneRotation;
                }
            }

            ConfigureCoinCounterSceneInstance(coinCounterPrefabPath);
            ConfigureLooseCounterCoins();

            var camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 1.45f, -3.0f);
                camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.85f, 0.6f) - camera.transform.position, Vector3.up);
            }

            var light = GameObject.Find("Directional Light");
            if (light != null)
            {
                light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            }

            EnsureXrSceneObjects();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        private static void ConfigureCoinCounterSceneInstance(string coinCounterPrefabPath)
        {
            foreach (var existing in UnityEngine.Object.FindObjectsByType<LotteryCoinCounterStation>(FindObjectsSortMode.None))
            {
                DestroyGeneratedObject(existing.gameObject);
            }

            var counterAsset = AssetDatabase.LoadAssetAtPath<GameObject>(coinCounterPrefabPath);
            if (counterAsset == null)
            {
                return;
            }

            var counterInstance = PrefabUtility.InstantiatePrefab(counterAsset) as GameObject;
            if (counterInstance == null)
            {
                return;
            }

            counterInstance.name = "LotteryCoinCounter";
            counterInstance.transform.position = CoinCounterScenePosition;
            counterInstance.transform.rotation = CoinCounterSceneRotation;

            var station = counterInstance.GetComponent<LotteryCoinCounterStation>();
            if (station != null)
            {
                station.GameManager = UnityEngine.Object.FindFirstObjectByType<LotteryGameManager>();
            }
        }

        private static void ConfigureLooseCounterCoins()
        {
            var existingParent = GameObject.Find(LooseCounterCoinsName);
            if (existingParent != null)
            {
                DestroyGeneratedObject(existingParent);
            }

            var coinPrefab = LoadPhysicalCoinPrefab();
            var gameManager = UnityEngine.Object.FindFirstObjectByType<LotteryGameManager>();
            if (coinPrefab == null || gameManager == null)
            {
                return;
            }

            var parent = new GameObject(LooseCounterCoinsName);
            var coinSpacing = 0.18f;
            var firstCoinPosition = CoinCounterScenePosition + new Vector3(-0.36f, 0.9f, -0.56f);

            for (var i = 0; i < LooseCounterCoinCount; i++)
            {
                var coinInstance = PrefabUtility.InstantiatePrefab(coinPrefab) as GameObject;
                if (coinInstance == null)
                {
                    continue;
                }

                coinInstance.name = $"LooseCounterCoin_{i + 1:00}";
                coinInstance.transform.SetParent(parent.transform, true);
                coinInstance.transform.position = firstCoinPosition + new Vector3(i * coinSpacing, 0f, 0f);
                coinInstance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                var coin = LotteryCoin.PrepareCoinObject(coinInstance, true, gameManager);
                if (coin != null)
                {
                    coin.ClearCountedInInventory();
                    coin.SetPickupCountingEnabled(false);
                }
            }
        }

        private static void DestroyGeneratedObject(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            ClearSelectionIfUnderRoot(root);

            foreach (var textMesh in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (textMesh != null)
                {
                    textMesh.enabled = false;
                }
            }

            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ClearSelectionIfUnderRoot(GameObject root)
        {
            foreach (var selectedObject in Selection.objects)
            {
                if (IsSelectionUnderRoot(selectedObject, root))
                {
                    Selection.objects = Array.Empty<UnityEngine.Object>();
                    return;
                }
            }
        }

        private static bool IsSelectionUnderRoot(UnityEngine.Object selectedObject, GameObject root)
        {
            if (selectedObject == null || root == null)
            {
                return false;
            }

            if (selectedObject == root)
            {
                return true;
            }

            if (selectedObject is GameObject selectedGameObject)
            {
                return selectedGameObject.transform.IsChildOf(root.transform);
            }

            if (selectedObject is Component selectedComponent)
            {
                return selectedComponent != null && selectedComponent.transform.IsChildOf(root.transform);
            }

            return false;
        }

        private static void EnsureXrSceneObjects()
        {
            var managerType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager, Unity.XR.Interaction.Toolkit");
            if (managerType != null && GameObject.Find("XR Interaction Manager") == null)
            {
                var manager = new GameObject("XR Interaction Manager");
                manager.AddComponent(managerType);
            }

            var originType = Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");
            if (originType != null && GameObject.Find("XR Origin (Lottery Demo)") == null)
            {
                var origin = new GameObject("XR Origin (Lottery Demo)");
                origin.transform.position = new Vector3(0f, 0f, -1.6f);
                origin.AddComponent(originType);
            }
        }
    }
}
