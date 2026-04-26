using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class OutlineOther : MonoBehaviour
{
    [SerializeField] private GameObject objectToOutline;
    [SerializeField] private bool useSkinnedOutline = false;

    private MonoBehaviour outline;
    private XRBaseInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable == null)
        {
            Debug.LogError("No XRBaseInteractable found on " + gameObject.name);
            return;
        }

        outline = useSkinnedOutline
            ? objectToOutline.GetComponent<OutlineSkinned>()
            : objectToOutline.GetComponent<Outline>();

        if (outline != null)
            outline.enabled = false;

        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (outline != null)
            outline.enabled = true;
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (outline != null)
            outline.enabled = false;
    }
}