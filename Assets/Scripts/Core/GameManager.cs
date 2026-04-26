using System.Collections;
using UnityEngine;
using TMPro;

namespace ZombieBunker
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Welcome Back UI (Idle Progress)")]
        [SerializeField] private GameObject welcomeBackPanel;
        [SerializeField] private TextMeshProUGUI welcomeBackText;
        [SerializeField] private float welcomeBackDuration = 5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private IEnumerator Start()
        {
            // Wait one frame to let all managers initialize
            yield return null;

            // Try to load saved game
            if (SaveManager.Instance != null && SaveManager.Instance.HasSaveData())
            {
                string lastPlayTime = SaveManager.Instance.GetLastPlayTime();
                SaveManager.Instance.LoadGame();

                // Apply idle progress
                if (IdleProgressManager.Instance != null && !string.IsNullOrEmpty(lastPlayTime))
                {
                    IdleProgressManager.Instance.OnIdleProgressApplied += ShowWelcomeBack;
                    IdleProgressManager.Instance.ApplyIdleProgress(lastPlayTime);
                    IdleProgressManager.Instance.OnIdleProgressApplied -= ShowWelcomeBack;
                }
            }
        }

        private void ShowWelcomeBack(float bullets, float rockets, float cash)
        {
            if (welcomeBackPanel == null) return;

            string message = "Welcome back, Commander!\n\nWhile you were away:\n";
            if (bullets > 0) message += $"  +{bullets:F0} Bullets\n";
            if (rockets > 0) message += $"  +{rockets:F0} Rockets\n";
            if (cash > 0) message += $"  +{cash:F0} Cash\n";

            if (welcomeBackText != null) welcomeBackText.text = message;
            welcomeBackPanel.SetActive(true);

            StartCoroutine(HideWelcomeBack());
        }

        private IEnumerator HideWelcomeBack()
        {
            yield return new WaitForSeconds(welcomeBackDuration);
            if (welcomeBackPanel != null)
                welcomeBackPanel.SetActive(false);
        }
    }
}
