using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using TMPro;

namespace ZombieBunker
{
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [SerializeField] private List<AchievementConfig> achievements = new List<AchievementConfig>();

        [Header("Trophy Display")]
        [SerializeField] private Transform trophyShelf;
        [SerializeField] private float trophySpacing = 0.3f;

        [Header("Popup Settings")]
        [SerializeField] private float popupDuration = 5f; // How long each popup shows
        [SerializeField] private float popupDistance = 2f; // Distance in front of XR camera
        [SerializeField] private Vector3 popupOffset = default; // Optional offset for popups
        [SerializeField] private AudioSource audio = null; // Optional audio to play for achievement

        [Header("MP3 Juice — Achievement")]
        [SerializeField] private AudioClip[] achievementSounds; // one per achievement index
        [SerializeField] private ParticleSystem goldBurst;
        [SerializeField] private ParticleSystem choiceSparks;
        [SerializeField] private EyeTracker eyeTracker;
        [SerializeField] private float achievementHapticAmplitude = 1.0f;
        [SerializeField] private float achievementHapticDuration = 1.0f;

        private HashSet<string> unlockedAchievements = new HashSet<string>();
        private List<GameObject> spawnedTrophies = new List<GameObject>();

        private Camera xrCamera;
        private Queue<string> popupQueue = new Queue<string>();
        private bool showingPopup = false;

        public event Action<AchievementConfig> OnAchievementUnlocked;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            DontDestroyOnLoad(gameObject);

            xrCamera = Camera.main;
            if (xrCamera == null)
            {
                var cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                foreach (var cam in cams)
                {
                    if (cam != null && cam.enabled)
                    {
                        xrCamera = cam;
                        break;
                    }
                }
            }

            // Load previously unlocked achievements and spawn trophies
            LoadUnlockedAchievements();
        }

        private void Start()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnTotalProducedChanged += OnTotalProducedChanged;
                ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
            }
            if (GeneratorManager.Instance != null)
                GeneratorManager.Instance.OnGeneratorCountChanged += OnGeneratorCountChanged;
            if (UnlockManager.Instance != null)
                UnlockManager.Instance.OnRocketRoomUnlocked += OnRocketRoomUnlocked;
        }

        private void OnDestroy()
        {
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnTotalProducedChanged -= OnTotalProducedChanged;
                ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
            }
            if (GeneratorManager.Instance != null)
                GeneratorManager.Instance.OnGeneratorCountChanged -= OnGeneratorCountChanged;
            if (UnlockManager.Instance != null)
                UnlockManager.Instance.OnRocketRoomUnlocked -= OnRocketRoomUnlocked;
        }

        public void ShowPopupMessage(string message)
        {
            EnqueuePopup(message);
        }

        #region Event Handlers

        private void OnTotalProducedChanged(ResourceType type, float total)
        {
            CheckResourceAchievements(type, total);
        }

        private void OnResourceChanged(ResourceType type, float count)
        {
            // Some achievements may track current count
        }

        private void OnGeneratorCountChanged(int totalCount)
        {
            CheckGeneratorAchievements(totalCount);
        }

        private void OnRocketRoomUnlocked()
        {
            foreach (var achievement in achievements)
            {
                if (achievement.condition == AchievementCondition.RocketUnlock)
                    TryUnlock(achievement);
            }
        }

        #endregion

        #region Achievement Checks

        private void CheckResourceAchievements(ResourceType type, float total)
        {
            foreach (var achievement in achievements)
            {
                if (achievement.condition == AchievementCondition.ResourceTotal &&
                    achievement.trackedResource == type &&
                    total >= achievement.threshold)
                {
                    TryUnlock(achievement);
                }
            }
        }

        private void CheckGeneratorAchievements(int count)
        {
            foreach (var achievement in achievements)
            {
                if (achievement.condition == AchievementCondition.GeneratorCount &&
                    count >= achievement.generatorCountThreshold)
                {
                    TryUnlock(achievement);
                }
            }
        }

        public void CheckPrestigeAchievements(int prestigeCount)
        {
            foreach (var achievement in achievements)
            {
                if (achievement.condition == AchievementCondition.PrestigeCount &&
                    prestigeCount >= achievement.threshold)
                {
                    TryUnlock(achievement);
                }
            }
        }

        #endregion

        #region Unlock / Trophy / Popup

        private void TryUnlock(AchievementConfig achievement)
        {
            if (unlockedAchievements.Contains(achievement.achievementName))
                return;

            unlockedAchievements.Add(achievement.achievementName);
            SaveAchievement(achievement.achievementName);

            // Spawn trophy with ease
            SpawnTrophy(achievement);

            // Distinct achievement sound
            int idx = achievements.IndexOf(achievement);
            if (audio != null && achievementSounds != null && idx >= 0 && idx < achievementSounds.Length && achievementSounds[idx] != null)
                audio.PlayOneShot(achievementSounds[idx]);
            else if (audio != null)
                audio.Play();

            if (goldBurst != null) goldBurst.Play();
            if (choiceSparks != null) StartCoroutine(PlaySparksAtCamera());
            if (eyeTracker != null) { eyeTracker.TriggerSurprise(); eyeTracker.SetMood("smile"); }
            SendHapticsToAllControllers(achievementHapticAmplitude, achievementHapticDuration);

            // Enqueue popup
            EnqueuePopup("Achievement: " + achievement.achievementName);

            OnAchievementUnlocked?.Invoke(achievement);
        }

        private void SendHapticsToAllControllers(float amplitude, float duration)
        {
            var devices = new System.Collections.Generic.List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
            foreach (var device in devices)
                device.SendHapticImpulse(0, amplitude, duration);
        }

        private void SpawnTrophy(AchievementConfig achievement)
        {
            if (achievement.trophyPrefab == null || trophyShelf == null) return;

            Vector3 position = trophyShelf.position +
                               trophyShelf.right * (spawnedTrophies.Count * trophySpacing);

            var trophy = Instantiate(achievement.trophyPrefab, position, trophyShelf.rotation, trophyShelf);
            spawnedTrophies.Add(trophy);

            // Ease trophy in from scale 0 with overshoot
            StartCoroutine(EaseTrophyIn(trophy));
        }

        private IEnumerator EaseTrophyIn(GameObject trophy)
        {
            trophy.transform.localScale = Vector3.zero;
            float scaleTarget = 1.2f;
            float k = 8f;
            float timer = 0f;
            while (timer < 0.2f)
            {
                float s = Mathf.Lerp(trophy.transform.localScale.x, scaleTarget, k * Time.deltaTime);
                trophy.transform.localScale = Vector3.one * s;
                timer += Time.deltaTime;
                yield return null;
            }
            scaleTarget = 1f;
            while (Mathf.Abs(trophy.transform.localScale.x - 1f) > 0.005f)
            {
                float s = Mathf.Lerp(trophy.transform.localScale.x, scaleTarget, k * Time.deltaTime);
                trophy.transform.localScale = Vector3.one * s;
                yield return null;
            }
            trophy.transform.localScale = Vector3.one;
        }

        private IEnumerator PlaySparksAtCamera()
        {
            yield return null; // wait one frame for XR rig to settle

            Camera vrCam = Camera.main;
            if (vrCam == null)
            {
                foreach (Camera c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if (c.enabled && c.gameObject.activeInHierarchy) { vrCam = c; break; }
                }
            }

            if (vrCam != null)
                choiceSparks.transform.position = vrCam.transform.position;

            choiceSparks.Play();
        }

        #endregion

        #region Popup Queue Logic

        private void EnqueuePopup(string message)
        {
            popupQueue.Enqueue(message);
            if (!showingPopup)
                StartCoroutine(ProcessPopupQueue());
        }

        private IEnumerator ProcessPopupQueue()
        {
            showingPopup = true;

            while (popupQueue.Count > 0)
            {
                string message = popupQueue.Dequeue();
                ShowPopup(message);
                yield return new WaitForSeconds(popupDuration);
            }

            showingPopup = false;
        }

        private void ShowPopup(string message)
        {
            if (xrCamera == null) return;

            var popupGO = new GameObject("AchievementPopup_" + message);
            popupGO.transform.SetParent(xrCamera.transform, false);
            popupGO.transform.localPosition = new Vector3(0f, 0f, popupDistance) + popupOffset;
            popupGO.transform.localRotation = Quaternion.identity;

            var tmpText = popupGO.AddComponent<TextMeshPro>();
            tmpText.text = message;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.fontSize = 100;
            tmpText.color = Color.yellow;
            tmpText.enableWordWrapping = false;

            var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (font != null) tmpText.font = font;

            popupGO.transform.localScale = Vector3.one * 0.01f;

            var lifecycle = popupGO.AddComponent<PopupMessage>();
            if (audio != null) audio.Play();
            lifecycle.SetDuration(popupDuration);
        }

        #endregion

        #region Persistence

        private void SaveAchievement(string achievementName)
        {
            PlayerPrefs.SetInt("Achievement_" + achievementName, 1);
            PlayerPrefs.Save();
        }

        private bool IsAchievementUnlocked(string achievementName)
        {
            return PlayerPrefs.GetInt("Achievement_" + achievementName, 0) == 1;
        }

        private void LoadUnlockedAchievements()
        {
            foreach (var achievement in achievements)
            {
                if (IsAchievementUnlocked(achievement.achievementName))
                {
                    unlockedAchievements.Add(achievement.achievementName);
                    SpawnTrophy(achievement);
                }
            }
        }

        #endregion

        #region Public Methods

        public List<string> GetUnlockedAchievementNames()
        {
            return new List<string>(unlockedAchievements);
        }

        public void LoadUnlockedAchievements(List<string> names)
        {
            foreach (string name in names)
            {
                if (unlockedAchievements.Contains(name)) continue;

                unlockedAchievements.Add(name);
                foreach (var achievement in achievements)
                {
                    if (achievement.achievementName == name)
                    {
                        SpawnTrophy(achievement);
                        break;
                    }
                }
            }
        }

        public bool IsUnlocked(string achievementName)
        {
            return unlockedAchievements.Contains(achievementName);
        }

        public void ResetAchievements()
        {
            unlockedAchievements.Clear();
            foreach (var trophy in spawnedTrophies)
            {
                if (trophy != null) Destroy(trophy);
            }
            spawnedTrophies.Clear();

            foreach (var achievement in achievements)
            {
                PlayerPrefs.DeleteKey("Achievement_" + achievement.achievementName);
            }
            PlayerPrefs.Save();
        }

        #endregion
    }
}