using System.IO;
using UnityEditor;
using UnityEngine;

namespace LotteryMachine.EditorTools
{
    public static partial class LotteryMachineSampleBuilder
    {
        private const string SampleRoot = "Assets/LotteryMachine/Sample";
        private const string AudioRoot = SampleRoot + "/Audio";
        private const string MaterialsRoot = SampleRoot + "/Materials";
        private const string PrefabsRoot = SampleRoot + "/Prefabs";

        [MenuItem("Tools/Lottery Machine/Build Sample Content")]
        public static void BuildSampleContent()
        {
            if (SampleContentAlreadyExists())
            {
                Debug.LogWarning("Sample lottery machine content already exists. Build Sample Content now refuses to overwrite the existing machine prefab.");
                return;
            }

            EnsureFolder("Assets/LotteryMachine");
            EnsureFolder(SampleRoot);
            EnsureFolder(MaterialsRoot);
            EnsureFolder(PrefabsRoot);

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var red = CreateMaterial("Machine_Red", new Color(0.78f, 0.08f, 0.09f), litShader);
            var darkRed = CreateMaterial("Machine_DarkRed", new Color(0.42f, 0.02f, 0.04f), litShader);
            var metal = CreateMaterial("Machine_Metal", new Color(0.55f, 0.58f, 0.62f), litShader, 0.6f, 0.25f);
            var gold = CreateMaterial("Machine_Gold", new Color(1f, 0.67f, 0.14f), litShader, 0.45f, 0.2f);
            var signTextMaterial = CreateTextMeshProMaterial("Machine_Sign_TMP", Color.white, Color.white, 0.15f);
            var glass = CreateMaterial("Dome_Glass", new Color(0.48f, 0.8f, 1f, 0.36f), litShader, 0.1f, 0.05f);
            var capsuleMaterial = CreateMaterial("RewardCapsule_Gold", new Color(1f, 0.82f, 0.22f), litShader, 0.35f, 0.2f);
            var revealEffectMaterial = CreateParticleMaterial("RewardRevealBurst_Gold", new Color(1f, 0.72f, 0.18f, 0.85f));

            var capsulePrefab = CreateCapsulePrefab(capsuleMaterial);
            var machinePrefabPath = CreateLotteryMachinePrefab(capsulePrefab, red, darkRed, metal, gold, glass, revealEffectMaterial, signTextMaterial);
            var displayBoardPrefabPath = CreateRewardDisplayBoardPrefab(darkRed, gold, signTextMaterial);
            var coinCounterPrefabPath = CreateLotteryCoinCounterPrefab(darkRed, gold, metal, signTextMaterial);

            ConfigureSampleScene(machinePrefabPath, displayBoardPrefabPath, coinCounterPrefabPath, metal);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Lottery machine sample generated at {SampleRoot}.");
        }

        [MenuItem("Tools/Lottery Machine/Build Coin Counter Prefab")]
        public static void BuildCoinCounterPrefab()
        {
            EnsureFolder("Assets/LotteryMachine");
            EnsureFolder(SampleRoot);
            EnsureFolder(MaterialsRoot);
            EnsureFolder(PrefabsRoot);

            var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var darkRed = CreateMaterial("Machine_DarkRed", new Color(0.42f, 0.02f, 0.04f), litShader);
            var metal = CreateMaterial("Machine_Metal", new Color(0.55f, 0.58f, 0.62f), litShader, 0.6f, 0.25f);
            var gold = CreateMaterial("Machine_Gold", new Color(1f, 0.67f, 0.14f), litShader, 0.45f, 0.2f);
            var signTextMaterial = CreateTextMeshProMaterial("Machine_Sign_TMP", Color.white, Color.white, 0.15f);

            CreateLotteryCoinCounterPrefab(darkRed, gold, metal, signTextMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Lottery coin counter prefab generated at {PrefabsRoot}/LotteryCoinCounter.prefab.");
        }

        private static bool SampleContentAlreadyExists()
        {
            return File.Exists(PrefabsRoot + "/LotteryMachine.prefab");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(string.IsNullOrEmpty(parent) ? "Assets" : parent, name);
        }
    }
}
