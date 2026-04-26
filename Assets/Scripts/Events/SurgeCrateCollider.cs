using UnityEngine;

namespace ZombieBunker
{
    public class SurgeCrateCollider : MonoBehaviour
    
    {
        [Header("Audio Clips")]
        public AudioSource spawnAudioSource;
        public AudioSource collectAudioSource;

        private void Start()
        {
            // Play spawn audio when the crate appears
            if (spawnAudioSource != null)
                spawnAudioSource.Play();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // Play collection audio
                if (collectAudioSource != null)
                    collectAudioSource.Play();

                // Notify the manager to apply surge
                SurpriseSurgeManager.Instance.CollectSurge(this.gameObject);

                // Optional: destroy after a short delay so collect sound plays
                Destroy(gameObject, collectAudioSource != null ? collectAudioSource.clip.length : 0f);
            }
        }
    }
}