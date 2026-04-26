// using UnityEngine;
// using ZombieBunker;

// namespace ZombieBunker
// {
//     public class AchievementPopupBootstrap : MonoBehaviour
//     {
//         private static bool initialized = false;

//         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
//         private static void EnsureManagerExists()
//         {
//             if (initialized)
//                 return;

//             var existingManager = FindObjectOfType<AchievementPopupManager>();
//             if (existingManager != null)
//             {
//                 initialized = true;
//                 return;
//             }

//             // Create manager if it doesn't exist
//             var managerGO = new GameObject("AchievementPopupManager");
//             managerGO.AddComponent<AchievementPopupManager>();
//             initialized = true;
//             Debug.Log("AchievementPopupBootstrap: Created AchievementPopupManager");
//         }

//         private void Awake()
//         {
//             EnsureManagerExists();
//         }
//     }
// }
