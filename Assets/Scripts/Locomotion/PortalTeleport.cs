using UnityEngine;


namespace ZombieBunker
{
    /// <summary>
    /// Portal teleportation: renders the destination view onto a quad surface.
    /// When the XR Origin enters the trigger collider, teleports the player to the destination.
    /// Requires a TeleportationProvider on the XR Origin and a Camera + RenderTexture for the portal view.
    /// </summary>
    public class PortalTeleport : MonoBehaviour
    {
        [SerializeField] private Transform destination;
        [SerializeField] private Camera portalCamera;
        [SerializeField] private RenderTexture portalTexture;
        [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationProvider teleportProvider;

        private void Update()
        {
            if (portalCamera == null || destination == null) return;

            // Mirror the player's Y rotation offset at the destination
            portalCamera.transform.position = destination.position;
            portalCamera.transform.rotation = destination.rotation;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (teleportProvider == null || destination == null) return;

            // Check if it's the XR Origin entering
            var xrOriginCheck = other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>();
            if (xrOriginCheck == null) return;

            var request = new UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportRequest
            {
                destinationPosition = destination.position,
                destinationRotation = destination.rotation,
                matchOrientation = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.MatchOrientation.WorldSpaceUp
            };
            teleportProvider.QueueTeleportRequest(request);
        }
    }
}
