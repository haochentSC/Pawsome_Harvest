// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using TMPro;
// using ZombieBunker;

// namespace ZombieBunker
// {
//     public class AchievementPopupManager : MonoBehaviour
//     {
//         private static AchievementPopupManager instance;

//         private HashSet<ResourceType> achievedResources = new HashSet<ResourceType>();
//         private Camera xrCamera;

//         private Dictionary<ResourceType, string> achievementMessages = new Dictionary<ResourceType, string>
//         {
//             { ResourceType.Bullets, "Unlocked - TERMINATOR" },
//             { ResourceType.Cash, "Unlocked - FINALLY RICH" },
//             { ResourceType.Rockets, "Unlocked - TO THE MOON! 🚀" }
//         };

//         private Dictionary<ResourceType, float> thresholds = new Dictionary<ResourceType, float>
//         {
//             { ResourceType.Bullets, 100f },
//             { ResourceType.Cash, 100f },
//             { ResourceType.Rockets, 100f }
//         };

//         [SerializeField] private float popupDuration = 5f; // Duration of each popup
//         [SerializeField] private float popupDistance = 2f; // Distance in front of camera
//         [SerializeField] private Vector3 popupOffset = new Vector3(0f, 0f, 0f); // Local offset

//         private Queue<string> popupQueue = new Queue<string>();
//         private bool showingPopup = false;

//         private void Awake()
//         {
//             if (instance != null && instance != this)
//             {
//                 Destroy(gameObject);
//                 return;
//             }
//             instance = this;
//             DontDestroyOnLoad(gameObject);

//             FindXRCamera();
//         }

//         private void Start()
//         {
//             if (ResourceManager.Instance != null)
//             {
//                 ResourceManager.Instance.OnResourceChanged += OnResourceValueChanged;
//                 Debug.Log("AchievementPopupManager: Subscribed to ResourceManager.OnResourceChanged");
//             }
//         }

//         private void OnDestroy()
//         {
//             if (ResourceManager.Instance != null)
//                 ResourceManager.Instance.OnResourceChanged -= OnResourceValueChanged;
//         }

//         private void FindXRCamera()
//         {
//             xrCamera = Camera.main;
//             if (xrCamera == null)
//             {
//                 var cameras = FindObjectsOfType<Camera>();
//                 foreach (var cam in cameras)
//                 {
//                     if (cam != null && cam.enabled)
//                     {
//                         xrCamera = cam;
//                         break;
//                     }
//                 }
//             }

//             if (xrCamera != null)
//                 Debug.Log($"AchievementPopupManager: Found XR camera '{xrCamera.gameObject.name}'");
//             else
//                 Debug.LogWarning("AchievementPopupManager: No active camera found!");
//         }

//         private void OnResourceValueChanged(ResourceType type, float currentCount)
//         {
//             if (achievedResources.Contains(type))
//                 return;

//             if (thresholds.ContainsKey(type) && currentCount >= thresholds[type])
//             {
//                 achievedResources.Add(type);
//                 string message = achievementMessages[type];
//                 EnqueuePopup(message);
//                 Debug.Log($"AchievementPopupManager: Queued popup for {type}: {message}");
//             }
//         }

//         private void EnqueuePopup(string message)
//         {
//             popupQueue.Enqueue(message);
//             if (!showingPopup)
//                 StartCoroutine(ProcessPopupQueue());
//         }

//         private IEnumerator ProcessPopupQueue()
//         {
//             showingPopup = true;

//             while (popupQueue.Count > 0)
//             {
//                 string message = popupQueue.Dequeue();
//                 ShowPopup(message);
//                 yield return new WaitForSeconds(popupDuration);
//             }

//             showingPopup = false;
//         }

//         private void ShowPopup(string message)
//         {
//             if (xrCamera == null)
//             {
//                 Debug.LogWarning("AchievementPopupManager: XR Camera not found, cannot show popup");
//                 FindXRCamera();
//                 if (xrCamera == null)
//                     return;
//             }

//             // Create popup GameObject
//             var popupGO = new GameObject("AchievementPopup_" + message);
//             popupGO.transform.SetParent(xrCamera.transform, false);
//             popupGO.transform.localPosition = new Vector3(0f, 0f, popupDistance) + popupOffset;
//             popupGO.transform.localRotation = Quaternion.identity;

//             // Add TextMeshPro component
//             var tmpText = popupGO.AddComponent<TextMeshPro>();
//             tmpText.text = message;
//             tmpText.alignment = TextAlignmentOptions.Center;
//             tmpText.fontSize = 100;
//             tmpText.color = Color.yellow;
//             tmpText.enableWordWrapping = false;

//             var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
//             if (font != null)
//                 tmpText.font = font;

//             popupGO.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

//             // Add a simple lifecycle
//             var lifecycle = popupGO.AddComponent<PopupMessage>();
//             lifecycle.SetDuration(popupDuration);

//             Debug.Log($"AchievementPopupManager: Showing popup '{message}' at {popupGO.transform.position}");
//         }

//         public static void ResetAchievements()
//         {
//             if (instance != null)
//                 instance.achievedResources.Clear();
//         }
//     }
// }