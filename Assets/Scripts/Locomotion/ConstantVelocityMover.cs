using System.Collections;
using UnityEngine;

namespace ZombieBunker
{
    /// <summary>
    /// Moves the XR Origin at a constant velocity in a straight line to a waypoint.
    /// No acceleration or deceleration — pure constant speed.
    /// Wire waypoint buttons' OnClick() to MoveTo(waypointTransform).
    /// </summary>
    public class ConstantVelocityMover : MonoBehaviour
    {
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private float speed = 2f;

        private bool isMoving = false;

        /// <summary>Called by waypoint button to move to a destination.</summary>
        public void MoveTo(Transform waypoint)
        {
            if (waypoint == null || isMoving) return;
            StartCoroutine(MoveCoroutine(waypoint.position));
        }

        private IEnumerator MoveCoroutine(Vector3 destination)
        {
            isMoving = true;

            Vector3 direction = (destination - xrOrigin.position).normalized;

            while (Vector3.Distance(xrOrigin.position, destination) > 0.05f)
            {
                xrOrigin.position += direction * speed * Time.deltaTime;
                yield return null;
            }

            xrOrigin.position = destination;
            isMoving = false;
        }

        public bool IsMoving => isMoving;
    }
}
