using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace ZombieBunker
{
    public class CameraShaker : MonoBehaviour
    {
        public static CameraShaker Instance { get; private set; }

        // No Inspector assignment needed — auto-found at runtime
        private Transform cameraOffset;
        private Coroutine activeShake;
        private float perlinOffsetX;
        private float perlinOffsetY;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            perlinOffsetX = Random.Range(0f, 100f);
            perlinOffsetY = Random.Range(0f, 100f);
        }

        private void Start()
        {
            // Camera Offset is the direct parent of Main Camera in the XR rig.
            // Shaking it offsets the entire view without conflicting with HMD tracking.
            // Try Camera.main first; if untagged, fall back to finding any active camera.
            Camera vrCam = Camera.main;
            if (vrCam == null)
            {
                foreach (Camera c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if (c.enabled && c.gameObject.activeInHierarchy)
                    {
                        vrCam = c;
                        break;
                    }
                }
            }

            if (vrCam != null && vrCam.transform.parent != null)
                cameraOffset = vrCam.transform.parent;

            if (cameraOffset == null)
                Debug.LogWarning("CameraShaker: could not find Camera Offset (parent of VR camera).", this);
            else
                Debug.Log($"CameraShaker: found shake target '{cameraOffset.name}'");
        }

        /// <summary>
        /// Shake the camera and fire a matching haptic pulse on both controllers.
        /// Uses UnityEngine.XR.InputDevices directly — no Inspector wiring required.
        /// </summary>
        public void Shake(float duration, float magnitude)
        {
            if (cameraOffset == null)
            {
                Debug.LogWarning("CameraShaker.Shake called but cameraOffset is null.", this);
                return;
            }

            if (activeShake != null)
                StopCoroutine(activeShake);

            activeShake = StartCoroutine(DoShake(duration, magnitude));

            // Query the XR runtime at call-time for any haptic-capable controller.
            // This works regardless of controller names or XRIT component setup.
            float hapticAmplitude = Mathf.Clamp01(magnitude * 4f);
            SendHapticsToAllControllers(hapticAmplitude, duration);
        }

        private void SendHapticsToAllControllers(float amplitude, float duration)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller,
                devices);

            foreach (var device in devices)
                device.SendHapticImpulse(0, amplitude, duration);
        }

        private IEnumerator DoShake(float duration, float magnitude)
        {
            float elapsed = 0f;
            float speed = 20f;

            while (elapsed < duration)
            {
                float t = 1f - (elapsed / duration);
                float currentMag = magnitude * t;

                float px = (Mathf.PerlinNoise(perlinOffsetX + elapsed * speed, 0f) - 0.5f) * 2f;
                float py = (Mathf.PerlinNoise(0f, perlinOffsetY + elapsed * speed) - 0.5f) * 2f;

                // Apply as a localPosition offset on top of whatever the current base is
                cameraOffset.localPosition = new Vector3(px * currentMag, py * currentMag, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Return Camera Offset to its natural resting position (0,0,0 local)
            cameraOffset.localPosition = Vector3.zero;
            activeShake = null;
        }
    }
}
