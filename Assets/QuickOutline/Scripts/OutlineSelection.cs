using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
public class OutlineSelection : MonoBehaviour
{
    private Outline outline;
    private XRGrabInteractable grabInteractable;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        outline.enabled = false;

        grabInteractable.hoverEntered.AddListener(OnHoverEnter);
        grabInteractable.hoverExited.AddListener(OnHoverExit);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        outline.enabled = true;
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        outline.enabled = false;
    }
}