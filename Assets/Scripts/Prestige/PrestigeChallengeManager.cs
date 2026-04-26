using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

namespace ZombieBunker
{
    /// <summary>
    /// Manages Prestige Challenge Mode: resets the game with a rate penalty and
    /// disabled power-ups. If the player reaches the win threshold despite the penalty,
    /// they earn a special prestige bonus.
    /// Add a "Hard Mode Prestige" button on the Prestige Terminal and wire it to StartChallengeMode().
    /// </summary>
    public class PrestigeChallengeManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PrestigeManager prestigeManager;
        [SerializeField] private PowerUpManager powerUpManager;

        [Header("Challenge Config")]
        [SerializeField] private PowerUpConfig[] disabledPowerUps;
        [SerializeField] private float rateMultiplierPenalty = 0.5f;
        [SerializeField] private float winThreshold = 5000f;

        [Header("UI")]
        [SerializeField] private GameObject challengeActivePanel;
        [SerializeField] private TMP_Text challengeProgressText;

        [Header("Win Juice")]
        [SerializeField] private ParticleSystem winParticles;
        [SerializeField] private AudioSource winAudio;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private XRBaseController leftController;
        [SerializeField] private XRBaseController rightController;

        [Header("Special Bonus")]
        [SerializeField] private float specialPrestigeBonusMultiplier = 2.0f;

        private bool challengeActive = false;

        private void Update()
        {
            if (!challengeActive) return;
            if (ResourceManager.Instance == null) return;

            float produced = ResourceManager.Instance.GetTotalProduced(ResourceType.Bullets);

            if (challengeProgressText != null)
                challengeProgressText.text = $"Challenge: {produced:F0} / {winThreshold:F0}";

            if (produced >= winThreshold)
                TriggerWin();
        }

        /// <summary>Called from the Hard Mode Prestige button.</summary>
        public void StartChallengeMode()
        {
            if (prestigeManager == null || !prestigeManager.CanPrestige()) return;

            challengeActive = true;

            // Perform a normal prestige reset
            prestigeManager.TryPrestige();

            // Apply penalty to bullet rate
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.MultiplyRateMultiplier(ResourceType.Bullets, rateMultiplierPenalty);
                ResourceManager.Instance.MultiplyRateMultiplier(ResourceType.Rockets, rateMultiplierPenalty);
            }

            // Disable listed power-ups
            if (powerUpManager != null && disabledPowerUps != null)
            {
                foreach (var pu in disabledPowerUps)
                    powerUpManager.DisablePowerUp(pu);
            }

            if (challengeActivePanel != null)
                challengeActivePanel.SetActive(true);
        }

        private void TriggerWin()
        {
            challengeActive = false;

            if (challengeActivePanel != null)
                challengeActivePanel.SetActive(false);

            if (winParticles != null)
                winParticles.Play();

            if (winAudio != null && winClip != null)
                winAudio.PlayOneShot(winClip);

            if (leftController != null)
                leftController.SendHapticImpulse(0.9f, 1.0f);
            if (rightController != null)
                rightController.SendHapticImpulse(0.9f, 1.0f);

            GrantSpecialBonus();
        }

        private void GrantSpecialBonus()
        {
            if (ResourceManager.Instance == null) return;
            ResourceManager.Instance.MultiplyRateMultiplier(ResourceType.Bullets, specialPrestigeBonusMultiplier);
            ResourceManager.Instance.MultiplyRateMultiplier(ResourceType.Rockets, specialPrestigeBonusMultiplier);
            Debug.Log($"[PrestigeChallengeManager] Special bonus granted: x{specialPrestigeBonusMultiplier}");
        }
    }
}
