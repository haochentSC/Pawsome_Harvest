using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace LotteryMachine
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class GrabbableReward : MonoBehaviour
    {
        [SerializeField] private bool enableMouseDrag = true;
        [SerializeField] private bool useGravityAfterPickup = true;
        [SerializeField] private bool hasBeenPickedUp;

        private Rigidbody rewardRigidbody;
        private XRGrabInteractable grabInteractable;
        private Camera dragCamera;
        private Plane dragPlane;
        private Vector3 dragOffset;
        private bool isMouseDragging;
        private bool originalIsKinematic;
        private bool originalUseGravity;

        public bool HasBeenPickedUp => hasBeenPickedUp;

        private void Awake()
        {
            rewardRigidbody = GetComponent<Rigidbody>();
            grabInteractable = GetComponent<XRGrabInteractable>();
        }

        private void OnEnable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnSelectEntered);
            }
        }

        private void OnDisable()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            }
        }

        public void MarkPickedUp()
        {
            if (hasBeenPickedUp)
            {
                return;
            }

            hasBeenPickedUp = true;
            if (rewardRigidbody != null && useGravityAfterPickup)
            {
                rewardRigidbody.useGravity = true;
                rewardRigidbody.isKinematic = false;
            }
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            MarkPickedUp();
        }

        private void OnMouseDown()
        {
            if (!enableMouseDrag)
            {
                return;
            }

            dragCamera = Camera.main;
            if (dragCamera == null)
            {
                return;
            }

            MarkPickedUp();
            isMouseDragging = true;
            dragPlane = new Plane(-dragCamera.transform.forward, transform.position);
            originalIsKinematic = rewardRigidbody != null && rewardRigidbody.isKinematic;
            originalUseGravity = rewardRigidbody != null && rewardRigidbody.useGravity;

            if (rewardRigidbody != null)
            {
                rewardRigidbody.useGravity = false;
                rewardRigidbody.isKinematic = true;
            }

            if (TryGetMouseWorldPoint(out var worldPoint))
            {
                dragOffset = transform.position - worldPoint;
            }
        }

        private void OnMouseDrag()
        {
            if (!isMouseDragging || dragCamera == null)
            {
                return;
            }

            if (TryGetMouseWorldPoint(out var worldPoint))
            {
                transform.position = worldPoint + dragOffset;
            }
        }

        private void OnMouseUp()
        {
            if (!isMouseDragging)
            {
                return;
            }

            isMouseDragging = false;
            if (rewardRigidbody != null)
            {
                rewardRigidbody.isKinematic = originalIsKinematic;
                rewardRigidbody.useGravity = useGravityAfterPickup || originalUseGravity;
            }
        }

        private bool TryGetMouseWorldPoint(out Vector3 worldPoint)
        {
            worldPoint = default;
            var ray = dragCamera.ScreenPointToRay(Input.mousePosition);
            if (!dragPlane.Raycast(ray, out var distance))
            {
                return false;
            }

            worldPoint = ray.GetPoint(distance);
            return true;
        }
    }
}
