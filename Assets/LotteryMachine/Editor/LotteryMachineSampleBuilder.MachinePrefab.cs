using System.Collections.Generic;
using LotteryMachine;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace LotteryMachine.EditorTools
{
    public static partial class LotteryMachineSampleBuilder
    {
        private static GameObject CreateCapsulePrefab(Material material)
        {
            var root = new GameObject("RewardCapsule");
            CreatePrimitiveChild("CapsuleShell", PrimitiveType.Sphere, root.transform, Vector3.zero, new Vector3(0.28f, 0.28f, 0.28f), material);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabsRoot + "/RewardCapsule.prefab");
            DestroyGeneratedObject(root);
            return saved;
        }

        private static GameObject CreatePrimitiveChild(
            string name,
            PrimitiveType type,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            Vector3? localEuler = null)
        {
            var child = GameObject.CreatePrimitive(type);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.Euler(localEuler ?? Vector3.zero);
            child.transform.localScale = localScale;

            var renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return child;
        }

        private static ParticleSystem CreateRewardRevealEffect(Transform parent, Material material)
        {
            var effectObject = new GameObject("RewardRevealBurst");
            effectObject.transform.SetParent(parent, false);
            effectObject.transform.localPosition = Vector3.zero;
            effectObject.transform.localRotation = Quaternion.identity;
            effectObject.transform.localScale = Vector3.one;

            var particles = effectObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = 0.45f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.62f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.75f, 1.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.065f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.92f, 0.42f, 0.95f), new Color(1f, 0.58f, 0.08f, 0.8f));
            main.gravityModifier = -0.08f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 46) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            shape.radiusThickness = 0.4f;

            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.18f, 0.42f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(1f, 0.92f, 0.42f), 0f),
                    new GradientColorKey(new Color(1f, 0.55f, 0.08f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.65f, 0.35f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.25f),
                new Keyframe(0.18f, 1f),
                new Keyframe(1f, 0f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var renderer = effectObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.maxParticleSize = 0.12f;
            renderer.sortingFudge = 1f;
            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return particles;
        }

        private static string CreateLotteryMachinePrefab(
            GameObject capsulePrefab,
            Material red,
            Material darkRed,
            Material metal,
            Material gold,
            Material glass,
            Material revealEffectMaterial,
            Material textMaterial)
        {
            var root = new GameObject("LotteryMachine");
            var machine = root.AddComponent<global::LotteryMachine.LotteryMachine>();
            var gameManager = root.AddComponent<LotteryGameManager>();
            var logger = root.AddComponent<RewardResultLogger>();

            CreatePrimitiveChild("Base", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.18f, 0f), new Vector3(1.25f, 0.36f, 0.85f), darkRed);
            CreatePrimitiveChild("Body", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.86f, 0f), new Vector3(1.05f, 1.25f, 0.72f), red);
            CreatePrimitiveChild("WindowDome", PrimitiveType.Sphere, root.transform, new Vector3(0f, 1.55f, -0.02f), new Vector3(0.92f, 0.58f, 0.62f), glass);
            CreatePrimitiveChild("RewardTray", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.36f, -0.63f), new Vector3(0.84f, 0.08f, 0.38f), metal);
            CreatePrimitiveChild("TrayLip", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.45f, -0.81f), new Vector3(0.9f, 0.12f, 0.06f), gold);
            CreatePrimitiveChild("CapsuleChute", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.85f, -0.47f), new Vector3(0.16f, 0.36f, 0.16f), metal, new Vector3(90f, 0f, 0f));
            var signPanel = CreatePrimitiveChild("SignPanel", PrimitiveType.Cube, root.transform, new Vector3(0f, 1.36f, -0.39f), new Vector3(0.72f, 0.18f, 0.04f), gold);
            AddTextMeshProLabel("Label", signPanel.transform, "POKENMON", new Vector3(0f, -0.04f, -0.84f), new Vector2(0.95f, 0.34f), 0.3f, textMaterial);

            var rewardParent = new GameObject("RewardInstances");
            rewardParent.transform.SetParent(root.transform, false);

            var capsuleSpawn = new GameObject("CapsuleSpawnPoint");
            capsuleSpawn.transform.SetParent(root.transform, false);
            capsuleSpawn.transform.localPosition = new Vector3(0f, 1.14f, -0.48f);

            var cardReveal = new GameObject("CardRevealPoint");
            cardReveal.transform.SetParent(root.transform, false);
            cardReveal.transform.localPosition = new Vector3(0f, 0.54f, -0.69f);
            var revealEffect = CreateRewardRevealEffect(cardReveal.transform, revealEffectMaterial);

            var coinSpawnPoint = new GameObject("CoinSpawnPoint");
            coinSpawnPoint.transform.SetParent(root.transform, false);
            coinSpawnPoint.transform.localPosition = new Vector3(0.34f, 0.98f, -0.74f);

            var leverRoot = new GameObject("PullLever");
            leverRoot.transform.SetParent(root.transform, false);
            leverRoot.transform.localPosition = new Vector3(0.398f, 0.92f, -0.036f);
            var leverCollider = leverRoot.AddComponent<BoxCollider>();
            leverCollider.size = new Vector3(0.82f, 0.28f, 0.28f);
            leverCollider.center = new Vector3(0.27f, 0f, 0f);
            var lever = leverRoot.AddComponent<LotteryLever>();
            leverRoot.AddComponent<LotteryLeverXrInteractable>();

            var leverVisual = new GameObject("LeverVisual");
            leverVisual.transform.SetParent(leverRoot.transform, false);
            CreatePrimitiveChild("LeverArm", PrimitiveType.Cylinder, leverVisual.transform, new Vector3(0.28f, 0f, 0f), new Vector3(0.045f, 0.32f, 0.045f), metal, new Vector3(0f, 0f, 90f));
            CreatePrimitiveChild("LeverKnob", PrimitiveType.Sphere, leverVisual.transform, new Vector3(0.61f, 0f, 0f), new Vector3(0.18f, 0.18f, 0.18f), gold);

            var addMoneyButton = CreatePokeButton(
                "AddMoneyButton",
                "ADD\nMONEY",
                root.transform,
                new Vector3(-0.542f, 0.88f, 0.164f),
                new Vector3(0f, 90f, 0f),
                new Vector3(0f, 0f, -0.047f),
                new Vector3(0f, 0.003f, -0.076f),
                gold,
                metal,
                textMaterial);
            var exchangeButton = CreatePokeButton(
                "ExchangeButton",
                "EXCHANGE",
                root.transform,
                new Vector3(-0.542f, 0.88f, -0.176f),
                new Vector3(0f, 90f, 0f),
                new Vector3(0f, 0f, -0.044f),
                new Vector3(0f, -0.01f, -0.073f),
                gold,
                metal,
                textMaterial);
            var coinPlacer = CreateCoinPlacer(root.transform, gold, metal);
            coinPlacer.GameManager = gameManager;

            var machineSerialized = new SerializedObject(machine);
            machineSerialized.FindProperty("rewardPool").objectReferenceValue = LoadSampleRewardPool();
            machineSerialized.FindProperty("capsuleSpawnPoint").objectReferenceValue = capsuleSpawn.transform;
            machineSerialized.FindProperty("cardRevealPoint").objectReferenceValue = cardReveal.transform;
            machineSerialized.FindProperty("rewardParent").objectReferenceValue = rewardParent.transform;
            machineSerialized.FindProperty("capsulePrefab").objectReferenceValue = capsulePrefab;
            machineSerialized.FindProperty("rewardRevealEffect").objectReferenceValue = revealEffect;
            machineSerialized.FindProperty("rewardRevealAudioSource").objectReferenceValue = null;
            machineSerialized.FindProperty("rewardRevealSound").objectReferenceValue = null;
            machineSerialized.FindProperty("rewardRevealVolume").floatValue = 0.9f;
            machineSerialized.ApplyModifiedPropertiesWithoutUndo();

            var leverSerialized = new SerializedObject(lever);
            leverSerialized.FindProperty("lotteryMachine").objectReferenceValue = machine;
            leverSerialized.FindProperty("coinPlacer").objectReferenceValue = coinPlacer;
            leverSerialized.FindProperty("leverVisual").objectReferenceValue = leverVisual.transform;
            leverSerialized.ApplyModifiedPropertiesWithoutUndo();

            var gameManagerSerialized = new SerializedObject(gameManager);
            gameManagerSerialized.FindProperty("lotteryMachine").objectReferenceValue = machine;
            gameManagerSerialized.FindProperty("coinPrefab").objectReferenceValue = LoadPhysicalCoinPrefab();
            gameManagerSerialized.FindProperty("coinSpawnPoint").objectReferenceValue = coinSpawnPoint.transform;
            gameManagerSerialized.FindProperty("coinParent").objectReferenceValue = rewardParent.transform;
            gameManagerSerialized.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(machine.RewardCompletedEvent, logger.LogReward);
            UnityEventTools.AddPersistentListener(addMoneyButton.PressedEvent, gameManager.AddMoney);
            UnityEventTools.AddPersistentListener(exchangeButton.PressedEvent, gameManager.ExchangeMoneyForCoin);

            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabsRoot + "/LotteryMachine.prefab");
            DestroyGeneratedObject(root);
            return AssetDatabase.GetAssetPath(saved);
        }

        private static string CreateRewardDisplayBoardPrefab(Material boardMaterial, Material slotMaterial, Material textMaterial)
        {
            var root = CreateRewardDisplayBoardRoot(boardMaterial, slotMaterial, textMaterial);
            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabsRoot + "/RewardDisplayBoard.prefab");
            DestroyGeneratedObject(root);
            return AssetDatabase.GetAssetPath(saved);
        }

        private static string CreateLotteryCoinCounterPrefab(Material bodyMaterial, Material buttonMaterial, Material housingMaterial, Material textMaterial)
        {
            var root = new GameObject("LotteryCoinCounter");
            var station = root.AddComponent<LotteryCoinCounterStation>();

            CreatePrimitiveChild("Base", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.88f, 0.3f, 0.58f), bodyMaterial);
            CreatePrimitiveChild("Top", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.34f, 0f), new Vector3(0.94f, 0.08f, 0.64f), housingMaterial);

            var displayPanel = CreatePrimitiveChild("CoinCounter", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.43f, -0.33f), new Vector3(0.48f, 0.16f, 0.04f), buttonMaterial);
            DestroyGeneratedCollider(displayPanel);
            var counterText = AddTextMeshProLabel("Value", displayPanel.transform, "COINS: 0", new Vector3(0f, -0.035f, -0.84f), new Vector2(0.9f, 0.34f), 0.3f, textMaterial);
            var counterDisplay = displayPanel.AddComponent<LotteryCoinCounterDisplay>();

            var coinParent = new GameObject("CounterCoinInstances");
            coinParent.transform.SetParent(root.transform, false);

            var coinSpawnPoint = new GameObject("ExtractCoinSpawnPoint");
            coinSpawnPoint.transform.SetParent(root.transform, false);
            coinSpawnPoint.transform.localPosition = new Vector3(0.32f, 0.52f, -0.42f);

            var exchangeButton = CreatePokeButton(
                "ExchangeButton",
                "EXCHANGE",
                root.transform,
                new Vector3(-0.27f, 0.45f, -0.33f),
                Vector3.zero,
                new Vector3(0f, 0f, -0.044f),
                new Vector3(0f, -0.01f, -0.073f),
                buttonMaterial,
                housingMaterial,
                textMaterial);
            var extractButton = CreatePokeButton(
                "ExtractButton",
                "EXTRACT",
                root.transform,
                new Vector3(0.27f, 0.45f, -0.33f),
                Vector3.zero,
                new Vector3(0f, 0f, -0.044f),
                new Vector3(0f, -0.01f, -0.073f),
                buttonMaterial,
                housingMaterial,
                textMaterial);

            var depositSocket = CreateCounterDepositSocket(root.transform, station, buttonMaterial, housingMaterial);

            var stationSerialized = new SerializedObject(station);
            stationSerialized.FindProperty("counterDisplay").objectReferenceValue = counterDisplay;
            stationSerialized.FindProperty("coinPrefab").objectReferenceValue = LoadPhysicalCoinPrefab();
            stationSerialized.FindProperty("coinSpawnPoint").objectReferenceValue = coinSpawnPoint.transform;
            stationSerialized.FindProperty("coinParent").objectReferenceValue = coinParent.transform;
            stationSerialized.ApplyModifiedPropertiesWithoutUndo();

            counterDisplay.Configure(null, counterText);

            UnityEventTools.AddPersistentListener(exchangeButton.PressedEvent, station.Exchange);
            UnityEventTools.AddPersistentListener(extractButton.PressedEvent, station.ExtractCoin);

            var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabsRoot + "/LotteryCoinCounter.prefab");
            DestroyGeneratedObject(root);
            return AssetDatabase.GetAssetPath(saved);
        }

        private static GameObject CreateRewardDisplayBoardRoot(Material boardMaterial, Material slotMaterial, Material textMaterial)
        {
            var boardRoot = new GameObject("RewardDisplayBoard");

            var board = boardRoot.AddComponent<RewardDisplayBoard>();
            var backing = CreatePrimitiveChild("Backing", PrimitiveType.Cube, boardRoot.transform, Vector3.zero, new Vector3(1.36f, 0.92f, 0.06f), boardMaterial);
            DestroyGeneratedCollider(backing);
            AddReadableSignText("Title", boardRoot.transform, "CARD DISPLAY", new Vector3(0f, 0.38f, -0.045f), new Vector2(8f, 1f), 2.8f, textMaterial);

            var trophyAnchor = new GameObject("CompletionTrophyAnchor").transform;
            trophyAnchor.SetParent(boardRoot.transform, false);
            trophyAnchor.localPosition = new Vector3(0f, 0.58f, 0.12f);
            trophyAnchor.localRotation = Quaternion.identity;
            trophyAnchor.localScale = Vector3.one * 0.16f;

            var rewards = LoadSampleRewardDefinitions();
            var slotConfigurations = new List<RewardDisplayBoard.RewardDisplaySlotConfiguration>(rewards.Length);
            const float columnSpacing = 0.25f;
            const float rowSpacing = 0.34f;
            for (var i = 0; i < rewards.Length; i++)
            {
                var row = i / 5;
                var column = i % 5;
                var reward = rewards[i];
                var slotName = reward != null ? reward.DisplayName : $"Slot {i + 1}";
                var slotRoot = new GameObject($"Slot_{i + 1:00}_{slotName}");
                slotRoot.transform.SetParent(boardRoot.transform, false);
                slotRoot.transform.localPosition = new Vector3((column - 2) * columnSpacing, 0.15f - row * rowSpacing, -0.045f);

                var slotVisual = CreatePrimitiveChild("SlotFrame", PrimitiveType.Cube, slotRoot.transform, Vector3.zero, new Vector3(0.19f, 0.27f, 0.018f), slotMaterial);
                DestroyGeneratedCollider(slotVisual);

                var anchor = new GameObject("CardAnchor").transform;
                anchor.SetParent(slotRoot.transform, false);
                anchor.localPosition = new Vector3(0f, 0f, -0.055f);
                anchor.localRotation = Quaternion.Euler(-90f, 0f, 0f);

                var triggerObject = new GameObject("DropTrigger");
                triggerObject.transform.SetParent(slotRoot.transform, false);
                triggerObject.transform.localPosition = Vector3.zero;
                var triggerCollider = triggerObject.AddComponent<BoxCollider>();
                triggerCollider.isTrigger = true;
                triggerCollider.size = new Vector3(0.23f, 0.31f, 0.18f);
                var socket = triggerObject.AddComponent<XRSocketInteractor>();
                var trigger = triggerObject.AddComponent<RewardDisplaySlot>();
                trigger.Configure(board, socket, anchor);

                slotConfigurations.Add(new RewardDisplayBoard.RewardDisplaySlotConfiguration(reward, anchor, trigger));
            }

            board.ConfigureSlots(slotConfigurations);
            board.ConfigureCompletionTrophy(LoadCompletionTrophyPrefab(), trophyAnchor);
            return boardRoot;
        }

        private static RewardDefinition[] LoadSampleRewardDefinitions()
        {
            return new[]
            {
                LoadSampleReward("Leafbit"),
                LoadSampleReward("Aquapup"),
                LoadSampleReward("Pebblebun"),
                LoadSampleReward("Coallala"),
                LoadSampleReward("Sparkit"),
                LoadSampleReward("Mossaur"),
                LoadSampleReward("Abysspup"),
                LoadSampleReward("Mystifox"),
                LoadSampleReward("Flameling"),
                LoadSampleReward("Astralwyrm")
            };
        }

        private static RewardDefinition LoadSampleReward(string rewardName)
        {
            return AssetDatabase.LoadAssetAtPath<RewardDefinition>($"{SampleRoot}/Rewards/{rewardName}.asset");
        }

        private static RewardPool LoadSampleRewardPool()
        {
            return AssetDatabase.LoadAssetAtPath<RewardPool>($"{SampleRoot}/Rewards/PhokemonRewardPool.asset");
        }

        private static LotteryPokeButton CreatePokeButton(
            string name,
            string label,
            Transform parent,
            Vector3 localPosition,
            Vector3 localEuler,
            Vector3 buttonVisualLocalPosition,
            Vector3 labelLocalPosition,
            Material faceMaterial,
            Material housingMaterial,
            Material textMaterial)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = Quaternion.Euler(localEuler);

            var housing = CreatePrimitiveChild("Housing", PrimitiveType.Cube, root.transform, Vector3.zero, new Vector3(0.28f, 0.16f, 0.08f), housingMaterial);
            DestroyGeneratedCollider(housing);

            var visual = CreatePrimitiveChild("ButtonVisual", PrimitiveType.Cube, root.transform, buttonVisualLocalPosition, new Vector3(0.2f, 0.11f, 0.055f), faceMaterial);
            DestroyGeneratedCollider(visual);

            var collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0f, -0.03f);
            collider.size = new Vector3(0.26f, 0.14f, 0.12f);

            var interactable = root.AddComponent<XRSimpleInteractable>();
            var pokeFilter = root.AddComponent<XRPokeFilter>();
            pokeFilter.pokeInteractable = interactable;
            pokeFilter.pokeCollider = collider;

            var pokeButton = root.AddComponent<LotteryPokeButton>();
            var serialized = new SerializedObject(pokeButton);
            serialized.FindProperty("buttonVisual").objectReferenceValue = visual.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AddTextMeshProLabel("Label", root.transform, label, labelLocalPosition, new Vector2(0.22f, 0.13f), 0.06f, textMaterial);
            return pokeButton;
        }

        private static LotteryCoinPlacer CreateCoinPlacer(Transform parent, Material borderMaterial, Material housingMaterial)
        {
            var root = new GameObject("CoinPlacer");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0.33998f, 0.88f, -0.374f);
            root.transform.localScale = new Vector3(0.7733686f, 1f, 1f);

            var collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 0f, -0.03f);
            collider.size = new Vector3(0.26f, 0.14f, 0.12f);
            var socket = root.AddComponent<XRSocketInteractor>();
            var attachTransform = new GameObject("CoinSocketAttach").transform;
            attachTransform.SetParent(root.transform, false);
            attachTransform.localPosition = new Vector3(0f, 0f, -0.03f);
            var coinPlacer = root.AddComponent<LotteryCoinPlacer>();
            coinPlacer.Configure(socket, attachTransform);

            CreatePrimitiveChild("boarder 1", PrimitiveType.Cube, root.transform, new Vector3(0.003f, -0.095f, -0.015f), new Vector3(0.4f, 0.04f, 0.1f), borderMaterial);
            CreatePrimitiveChild("boarder 3", PrimitiveType.Cube, root.transform, new Vector3(-0.167f, -0.011f, -0.015f), new Vector3(0.06f, 0.2f, 0.1f), borderMaterial);
            CreatePrimitiveChild("boarder 4", PrimitiveType.Cube, root.transform, new Vector3(0.173f, -0.011f, -0.015f), new Vector3(0.06f, 0.2f, 0.1f), borderMaterial);
            CreatePrimitiveChild("boarder 2", PrimitiveType.Cube, root.transform, new Vector3(0.003f, 0.074f, -0.015f), new Vector3(0.4f, 0.04f, 0.1f), borderMaterial);

            var housing = CreatePrimitiveChild("Housing", PrimitiveType.Cube, root.transform, new Vector3(0.01f, 0f, 0f), new Vector3(0.316f, 0.16f, 0.08f), housingMaterial);
            DestroyGeneratedCollider(housing);
            return coinPlacer;
        }

        private static LotteryCoinCounterDepositSocket CreateCounterDepositSocket(
            Transform parent,
            LotteryCoinCounterStation station,
            Material borderMaterial,
            Material housingMaterial)
        {
            var root = new GameObject("DepositSocket");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, 0.45f, 0.18f);

            var collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 0f, -0.03f);
            collider.size = new Vector3(0.26f, 0.14f, 0.12f);
            var socket = root.AddComponent<XRSocketInteractor>();
            var attachTransform = new GameObject("DepositSocketAttach").transform;
            attachTransform.SetParent(root.transform, false);
            attachTransform.localPosition = new Vector3(0f, 0f, -0.03f);
            var depositSocket = root.AddComponent<LotteryCoinCounterDepositSocket>();
            depositSocket.Configure(station, socket, attachTransform);

            CreatePrimitiveChild("SlotBottom", PrimitiveType.Cube, root.transform, new Vector3(0f, -0.095f, -0.015f), new Vector3(0.4f, 0.04f, 0.1f), borderMaterial);
            CreatePrimitiveChild("SlotLeft", PrimitiveType.Cube, root.transform, new Vector3(-0.167f, -0.011f, -0.015f), new Vector3(0.06f, 0.2f, 0.1f), borderMaterial);
            CreatePrimitiveChild("SlotRight", PrimitiveType.Cube, root.transform, new Vector3(0.173f, -0.011f, -0.015f), new Vector3(0.06f, 0.2f, 0.1f), borderMaterial);
            CreatePrimitiveChild("SlotTop", PrimitiveType.Cube, root.transform, new Vector3(0f, 0.074f, -0.015f), new Vector3(0.4f, 0.04f, 0.1f), borderMaterial);

            var housing = CreatePrimitiveChild("Housing", PrimitiveType.Cube, root.transform, new Vector3(0f, 0f, 0f), new Vector3(0.316f, 0.16f, 0.08f), housingMaterial);
            DestroyGeneratedCollider(housing);
            return depositSocket;
        }

        private static GameObject LoadPhysicalCoinPrefab()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Coin.prefab");
        }

        private static GameObject LoadCompletionTrophyPrefab()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BTM_Assets/BTM_Items_Gems/Prefabs/Trophy.prefab");
        }

        private static void DestroyGeneratedCollider(GameObject gameObject)
        {
            var collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static TextMeshPro AddTextMeshProLabel(
            string name,
            Transform parent,
            string text,
            Vector3 localPosition,
            Vector2 size,
            float fontSize,
            Material material)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(MeshRenderer), typeof(TextMeshPro));
            var rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.localPosition = localPosition;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            var textMesh = textObject.GetComponent<TextMeshPro>();
            var defaultFontAsset = TMP_Settings.defaultFontAsset;
            if (defaultFontAsset != null)
            {
                textMesh.font = defaultFontAsset;
                textMesh.fontSharedMaterial = material != null ? material : defaultFontAsset.material;
            }

            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.color = Color.black;
            textMesh.horizontalAlignment = HorizontalAlignmentOptions.Center;
            textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;
            textMesh.enableWordWrapping = false;
            textMesh.richText = false;

            var renderer = textObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = textMesh.fontSharedMaterial;
            }

            return textMesh;
        }

        private static void AddReadableSignText(
            string name,
            Transform parent,
            string text,
            Vector3 localPosition,
            Vector2 size,
            float fontSize,
            Material material)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(MeshRenderer), typeof(TextMeshPro));
            var rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.identity;
            rect.localPosition = localPosition;
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = new Vector3(0.08f, 0.08f, 0.08f);

            var textMesh = textObject.GetComponent<TextMeshPro>();
            var defaultFontAsset = TMP_Settings.defaultFontAsset;
            if (defaultFontAsset != null)
            {
                textMesh.font = defaultFontAsset;
                textMesh.fontSharedMaterial = material != null ? material : defaultFontAsset.material;
            }

            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.color = Color.black;
            textMesh.horizontalAlignment = HorizontalAlignmentOptions.Center;
            textMesh.verticalAlignment = VerticalAlignmentOptions.Middle;
            textMesh.enableWordWrapping = false;
            textMesh.richText = false;

            var renderer = textObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = textMesh.fontSharedMaterial;
            }
        }
    }
}
