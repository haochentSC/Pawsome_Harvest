using UnityEngine;

public class AudioTrigger : MonoBehaviour
{
    public AudioSource audioSource; // Assign in inspector

    public void PlayAudio()
    {
        if (audioSource != null)
            audioSource.Play();
    }
}