using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ZombieBunker
{
    public class RandomEventManager : MonoBehaviour
    {
        public static RandomEventManager Instance { get; private set; }

        [Serializable]
        public class RandomEvent
        {
            public string eventName;
            [TextArea] public string description;
            [TextArea] public string acceptText;
            [TextArea] public string declineText;
            public ResourceType affectedResource;
            public float resourceCost;
            public float resourceReward;
            public float cashBonus;
        }

        [Header("Event Settings")]
        [SerializeField] private float initialDelay = 60f;
        [SerializeField] private float minInterval = 60f;
        [SerializeField] private float maxInterval = 180f;
        [SerializeField] private float eventDisplayDuration = 15f;

        [Header("Event UI")]
        [SerializeField] private GameObject eventPanelPrefab;
        [SerializeField] private Transform eventSpawnPoint;

        [Header("Events")]
        [SerializeField] private List<RandomEvent> possibleEvents = new List<RandomEvent>();

        [Header("MP3 Juice")]
        [SerializeField] private ParticleSystem popupSpawnParticles;
        [SerializeField] private ParticleSystem choiceSparks;
        [SerializeField] private AudioSource acceptAudioSource;
        [SerializeField] private AudioClip acceptClip;
        [SerializeField] private AudioSource declineAudioSource;
        [SerializeField] private AudioClip declineClip;
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.XRBaseController hapticController;
        [SerializeField] private float spawnHapticAmplitude = 0.35f;
        [SerializeField] private float spawnHapticDuration = 1.0f;
        [SerializeField] private float acceptHapticAmplitude = 0.5f;
        [SerializeField] private float acceptHapticDuration = 1.0f;
        [SerializeField] private float declineHapticAmplitude = 0.35f;
        [SerializeField] private float declineHapticDuration = 1.0f;

        private GameObject currentEventPanel;
        private RandomEvent currentEvent;
        private Coroutine eventCoroutine;

        public event Action<RandomEvent, bool> OnEventResolved;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (possibleEvents.Count == 0)
                SetupDefaultEvents();
        }

        private void Start()
        {
            if (eventPanelPrefab == null)
                Debug.LogWarning("RandomEventManager: eventPanelPrefab is not assigned! Events will not display.", this);
            if (eventSpawnPoint == null)
                Debug.LogWarning("RandomEventManager: eventSpawnPoint is not assigned! Events will not display.", this);

            eventCoroutine = StartCoroutine(EventSpawnLoop());
        }

        private void SetupDefaultEvents()
        {
            possibleEvents.Add(new RandomEvent
            {
                eventName = "Zombie Horde!",
                description = "A massive zombie horde is approaching!\nDivert ammo to the turrets for a cash bonus?",
                acceptText = "Divert Ammo",
                declineText = "Ignore",
                affectedResource = ResourceType.Bullets,
                resourceCost = 50f,
                resourceReward = 0f,
                cashBonus = 200f
            });

            possibleEvents.Add(new RandomEvent
            {
                eventName = "Supply Convoy",
                description = "A supply convoy is passing nearby.\nTrade cash for extra bullets?",
                acceptText = "Trade",
                declineText = "Pass",
                affectedResource = ResourceType.Cash,
                resourceCost = 100f,
                resourceReward = 75f,
                cashBonus = 0f
            });

            possibleEvents.Add(new RandomEvent
            {
                eventName = "Scavenger Offer",
                description = "A scavenger offers machine parts.\nSpend bullets to boost production temporarily?",
                acceptText = "Accept Deal",
                declineText = "Decline",
                affectedResource = ResourceType.Bullets,
                resourceCost = 30f,
                resourceReward = 0f,
                cashBonus = 100f
            });
        }

        private IEnumerator EventSpawnLoop()
        {
            yield return new WaitForSeconds(initialDelay);

            if (currentEventPanel == null && possibleEvents.Count > 0)
                ShowRandomEvent();

            while (true)
            {
                float interval = UnityEngine.Random.Range(minInterval, maxInterval);
                yield return new WaitForSeconds(interval);

                if (currentEventPanel == null && possibleEvents.Count > 0)
                {
                    ShowRandomEvent();
                }
            }
        }

        private void ShowRandomEvent()
        {
            currentEvent = possibleEvents[UnityEngine.Random.Range(0, possibleEvents.Count)];

            if (eventPanelPrefab != null && eventSpawnPoint != null)
            {
                currentEventPanel = Instantiate(eventPanelPrefab,
                    eventSpawnPoint.position, eventSpawnPoint.rotation);

                if (popupSpawnParticles != null)
                    popupSpawnParticles.Play();
                TriggerHaptic(spawnHapticAmplitude, spawnHapticDuration);

                // Overshoot ease-in
                StartCoroutine(EaseInPanel(currentEventPanel));

                // Set up text
                var texts = currentEventPanel.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = currentEvent.eventName;
                if (texts.Length > 1) texts[1].text = currentEvent.description;

                // Set up buttons
                var buttons = currentEventPanel.GetComponentsInChildren<Button>();
                if (buttons.Length > 0)
                    buttons[0].onClick.AddListener(AcceptEvent);
                if (buttons.Length > 1)
                    buttons[1].onClick.AddListener(DeclineEvent);

                StartCoroutine(AutoDismissEvent());
            }
        }

        private IEnumerator AutoDismissEvent()
        {
            yield return new WaitForSeconds(eventDisplayDuration);
            DismissEvent();
        }

        public void AcceptEvent()
        {
            if (currentEvent == null) return;

            var rm = ResourceManager.Instance;
            if (rm.TrySpend(currentEvent.affectedResource, currentEvent.resourceCost))
            {
                if (currentEvent.resourceReward > 0)
                    rm.AddResource(ResourceType.Bullets, currentEvent.resourceReward);
                if (currentEvent.cashBonus > 0)
                    rm.AddResource(ResourceType.Cash, currentEvent.cashBonus);

                OnEventResolved?.Invoke(currentEvent, true);
            }

            if (choiceSparks != null) choiceSparks.Play();
            if (acceptAudioSource != null && acceptClip != null) acceptAudioSource.PlayOneShot(acceptClip);
            TriggerHaptic(acceptHapticAmplitude, acceptHapticDuration);

            DismissEvent();
        }

        public void DeclineEvent()
        {
            if (choiceSparks != null) choiceSparks.Play();
            if (declineAudioSource != null && declineClip != null) declineAudioSource.PlayOneShot(declineClip);
            TriggerHaptic(declineHapticAmplitude, declineHapticDuration);
            OnEventResolved?.Invoke(currentEvent, false);
            DismissEvent();
        }

        private void TriggerHaptic(float amplitude, float duration)
        {
            if (hapticController != null)
                hapticController.SendHapticImpulse(amplitude, duration);
        }

        private IEnumerator EaseInPanel(GameObject panel)
        {
            panel.transform.localScale = Vector3.zero;
            float scaleTarget = 1.2f;
            float k = 10f;
            float timer = 0f;
            while (timer < 0.2f)
            {
                float s = Mathf.Lerp(panel.transform.localScale.x, scaleTarget, k * Time.deltaTime);
                panel.transform.localScale = Vector3.one * s;
                timer += Time.deltaTime;
                yield return null;
            }
            scaleTarget = 1f;
            while (panel != null && Mathf.Abs(panel.transform.localScale.x - 1f) > 0.005f)
            {
                float s = Mathf.Lerp(panel.transform.localScale.x, scaleTarget, k * Time.deltaTime);
                panel.transform.localScale = Vector3.one * s;
                yield return null;
            }
            if (panel != null) panel.transform.localScale = Vector3.one;
        }

        private void DismissEvent()
        {
            if (currentEventPanel != null)
            {
                Destroy(currentEventPanel);
                currentEventPanel = null;
            }
            currentEvent = null;
        }

        private void OnDestroy()
        {
            if (eventCoroutine != null)
                StopCoroutine(eventCoroutine);
        }
    }
}
