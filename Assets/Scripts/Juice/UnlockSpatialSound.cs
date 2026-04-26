using System.Collections;
using UnityEngine;

namespace ZombieBunker
{
    /// <summary>
    /// Plays a spatialized sound when a UI element is unlocked.
    /// Attach to the door or unlocked panel. AudioSource should have Spatial Blend = 1.
    /// Optionally slides the door open with an ease instead of instant disable.
    /// </summary>
    public class UnlockSpatialSound : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip unlockClip;

        [Header("Door Slide-Open Ease (optional)")]
        [SerializeField] private bool animateDoorOpen = false;
        [SerializeField] private Transform doorTransform;
        [SerializeField] private Vector3 slideDirection = Vector3.up;
        [SerializeField] private float slideDistance = 3f;
        [SerializeField] private float slideEaseK = 5f;

        public void PlayUnlockSound()
        {
            if (audioSource != null && unlockClip != null)
            {
                audioSource.clip = unlockClip;
                audioSource.Play();
            }

            if (animateDoorOpen && doorTransform != null)
            {
                StartCoroutine(SlideOpen(doorTransform));
            }
        }

        private IEnumerator SlideOpen(Transform door)
        {
            Vector3 startPos = door.localPosition;
            Vector3 endPos = startPos + slideDirection.normalized * slideDistance;

            while (Vector3.Distance(door.localPosition, endPos) > 0.02f)
            {
                door.localPosition = Vector3.Lerp(door.localPosition, endPos, slideEaseK * Time.deltaTime);
                yield return null;
            }

            door.localPosition = endPos;
            door.gameObject.SetActive(false);
        }
    }
}
