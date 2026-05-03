using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace LotteryMachine
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(XRSimpleInteractable))]
    [RequireComponent(typeof(XRPokeFilter))]
    public sealed class LotteryPokeButton : MonoBehaviour
    {
        [SerializeField] private Transform buttonVisual;
        [SerializeField, Min(0f)] private float pressDistance = 0.035f;
        [SerializeField, Min(0f)] private float returnDuration = 0.12f;
        [SerializeField] private UnityEvent pressed = new();

        private XRSimpleInteractable interactable;
        private Vector3 restLocalPosition;
        private Coroutine returnRoutine;

        public UnityEvent PressedEvent => pressed;

        private void Awake()
        {
            interactable = GetComponent<XRSimpleInteractable>();
            if (buttonVisual == null)
            {
                buttonVisual = transform;
            }

            restLocalPosition = buttonVisual.localPosition;
        }

        private void OnEnable()
        {
            if (interactable == null)
            {
                interactable = GetComponent<XRSimpleInteractable>();
            }

            interactable.selectEntered.AddListener(OnSelectEntered);
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnSelectEntered);
            }
        }

        public void Press()
        {
            PlayPressFeedback();
            pressed.Invoke();
        }

        private void OnMouseDown()
        {
            Press();
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            Press();
        }

        private void PlayPressFeedback()
        {
            if (buttonVisual == null)
            {
                return;
            }

            if (returnRoutine != null)
            {
                StopCoroutine(returnRoutine);
            }

            buttonVisual.localPosition = restLocalPosition + Vector3.forward * pressDistance;
            returnRoutine = StartCoroutine(ReturnVisual());
        }

        private IEnumerator ReturnVisual()
        {
            if (buttonVisual == null)
            {
                yield break;
            }

            if (returnDuration <= 0f)
            {
                buttonVisual.localPosition = restLocalPosition;
                returnRoutine = null;
                yield break;
            }

            var start = buttonVisual.localPosition;
            var elapsed = 0f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / returnDuration);
                buttonVisual.localPosition = Vector3.Lerp(start, restLocalPosition, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            buttonVisual.localPosition = restLocalPosition;
            returnRoutine = null;
        }
    }
}
