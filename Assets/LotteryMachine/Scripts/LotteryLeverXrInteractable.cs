using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace LotteryMachine
{
    [RequireComponent(typeof(Collider))]
    public sealed class LotteryLeverXrInteractable : XRSimpleInteractable
    {
        [SerializeField] private LotteryLever lever;

        protected override void Awake()
        {
            base.Awake();
            if (lever == null)
            {
                lever = GetComponent<LotteryLever>();
            }
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            if (lever != null)
            {
                lever.Pull();
            }
        }
    }
}
