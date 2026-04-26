using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ZombieBunker
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Serializable]
        public class TutorialEntry
        {
            public string id;
            [TextArea(2, 4)] public string message;
            public Transform anchorPoint;
            public GameObject connectorLine;
            public float displayDuration = 5f;
            [HideInInspector] public bool hasShown;
        }

        [Header("Tutorial Settings")]
        [SerializeField] private GameObject tutorialPopupPrefab;
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("Tutorial Entries")]
        [SerializeField] private TutorialEntry firstGeneratorTutorial;
        [SerializeField] private TutorialEntry firstPowerUpTutorial;
        [SerializeField] private TutorialEntry rocketRoomTutorial;
        [SerializeField] private TutorialEntry clickerTutorial;
        [SerializeField] private TutorialEntry craftingTutorial;

        [Header("Extra Tutorials")]
        [SerializeField] private List<TutorialEntry> additionalTutorials = new List<TutorialEntry>();

        private Queue<TutorialEntry> pendingTutorials = new Queue<TutorialEntry>();
        private bool isShowingTutorial = false;

        public event Action<string> OnTutorialShown;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (clickerTutorial != null && !clickerTutorial.hasShown)
            {
                ShowTutorial(clickerTutorial);
            }
        }

        public void OnGeneratorPlaced(Generator generator)
        {
            if (firstGeneratorTutorial != null && !firstGeneratorTutorial.hasShown)
            {
                ShowTutorial(firstGeneratorTutorial);
            }
        }

        public void OnPowerUpPurchased(PowerUpConfig config)
        {
            if (firstPowerUpTutorial != null && !firstPowerUpTutorial.hasShown)
            {
                ShowTutorial(firstPowerUpTutorial);
            }
        }

        public void OnRocketRoomUnlocked()
        {
            if (rocketRoomTutorial != null && !rocketRoomTutorial.hasShown)
            {
                ShowTutorial(rocketRoomTutorial);
            }
        }

        public void OnCraftingDiscovered()
        {
            if (craftingTutorial != null && !craftingTutorial.hasShown)
            {
                ShowTutorial(craftingTutorial);
            }
        }

        public void ShowTutorialById(string id)
        {
            foreach (var tutorial in additionalTutorials)
            {
                if (tutorial.id == id && !tutorial.hasShown)
                {
                    ShowTutorial(tutorial);
                    return;
                }
            }
        }

        private void ShowTutorial(TutorialEntry entry)
        {
            if (entry.hasShown) return;
            entry.hasShown = true;

            if (isShowingTutorial)
            {
                pendingTutorials.Enqueue(entry);
                return;
            }

            StartCoroutine(DisplayTutorial(entry));
        }

        private IEnumerator DisplayTutorial(TutorialEntry entry)
        {
            isShowingTutorial = true;

            GameObject popup = null;
            if (tutorialPopupPrefab != null && entry.anchorPoint != null)
            {
                popup = Instantiate(tutorialPopupPrefab, entry.anchorPoint.position, entry.anchorPoint.rotation);
                var text = popup.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) text.text = entry.message;

                if (entry.connectorLine != null)
                    entry.connectorLine.SetActive(true);

                // Spring-physics bounce: starts at 0, overshoots ~1.2×, oscillates back to 1.
                // Tune springStiffness (speed) and springDamping (how quickly it settles).
                Vector3 originalScale = popup.transform.localScale;
                popup.transform.localScale = Vector3.zero;
                const float springStiffness = 65f;
                const float springDamping   = 9f;
                float springPos = 0f;   // current scale factor
                float springVel = 0f;   // current velocity
                while (true)
                {
                    float force = (1f - springPos) * springStiffness - springVel * springDamping;
                    springVel += force * Time.deltaTime;
                    springPos += springVel * Time.deltaTime;
                    popup.transform.localScale = originalScale * springPos;
                    // Settled when both displacement and velocity are negligible
                    if (Mathf.Abs(1f - springPos) < 0.001f && Mathf.Abs(springVel) < 0.001f)
                        break;
                    yield return null;
                }
                popup.transform.localScale = originalScale;

                // Fade in alpha as well
                var canvasGroup = popup.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0f;
                    float timer = 0f;
                    while (timer < fadeInDuration)
                    {
                        timer += Time.deltaTime;
                        canvasGroup.alpha = timer / fadeInDuration;
                        yield return null;
                    }
                    canvasGroup.alpha = 1f;
                }

                // Setup close button
                var closeButton = popup.GetComponentInChildren<UnityEngine.UI.Button>();
                if (closeButton != null)
                {
                    bool closed = false;
                    closeButton.onClick.AddListener(() => closed = true);

                    // Wait until button is pressed
                    while (!closed)
                    {
                        yield return null;
                    }
                }
            }

            OnTutorialShown?.Invoke(entry.id);

            // Fade out
            if (popup != null)
            {
                var canvasGroup = popup.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    float timer = 0f;
                    while (timer < fadeOutDuration)
                    {
                        timer += Time.deltaTime;
                        canvasGroup.alpha = 1f - (timer / fadeOutDuration);
                        yield return null;
                    }
                }
                Destroy(popup);
            }

            if (entry.connectorLine != null)
                entry.connectorLine.SetActive(false);

            isShowingTutorial = false;

            if (pendingTutorials.Count > 0)
            {
                var next = pendingTutorials.Dequeue();
                StartCoroutine(DisplayTutorial(next));
            }
        }

        public void ResetAllTutorials()
        {
            if (firstGeneratorTutorial != null) firstGeneratorTutorial.hasShown = false;
            if (firstPowerUpTutorial != null) firstPowerUpTutorial.hasShown = false;
            if (rocketRoomTutorial != null) rocketRoomTutorial.hasShown = false;
            if (clickerTutorial != null) clickerTutorial.hasShown = false;
            if (craftingTutorial != null) craftingTutorial.hasShown = false;
            foreach (var t in additionalTutorials) t.hasShown = false;
        }
    }
}
