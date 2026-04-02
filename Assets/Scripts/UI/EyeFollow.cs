using UnityEngine;

public class EyeFollow : MonoBehaviour
{
    public Transform target;        // Main Camera
    public Transform forwardMarker; // EyeLeft_Forward or EyeRight_Forward

    [Header("Y Rotation Limits")]
    public float minY = -50f;
    public float maxY = 50f;

    [Header("Fixed X Rotation")]
    public float fixedX = -20f;

    void Update()
    {
        if (target == null || forwardMarker == null)
            return;

        // Direction from eye to target
        Vector3 dir = target.position - forwardMarker.position;

        // Only care about left/right
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            return;

        // Compute smooth signed Y angle
        float signedY = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

        // 1. Reverse the sign
        signedY = -signedY;

        // 2. Scale down by 1/10
        signedY *= 0.1f;

        // 3. Clamp
        signedY = Mathf.Clamp(signedY, minY, maxY);

        // Apply rotation: fixed X, clamped Y, zero Z
        transform.rotation = Quaternion.Euler(fixedX, signedY, 0f);
    }
}
