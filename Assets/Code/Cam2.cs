using UnityEngine;

public class Cam2 : MonoBehaviour
{
    public Transform player;
    public Transform locationTarget;
    public Transform cam;
    public float camSpeed;       // used as maxSpeed for SmoothDamp (set <= 0 for unlimited)
    public float smoothTime = 0.2f;
    public float maxDistance;

    private Vector3 velocity;
    private Vector3 localOffset;

    private void Start()
    {
        Vector3 offset = cam.position - locationTarget.position;
        if (offset.sqrMagnitude > maxDistance * maxDistance)
            offset = offset.normalized * maxDistance;

        cam.position = locationTarget.position + offset;

        // Store the offset in the local space of the locationTarget so rotation changes move the camera
        localOffset = Quaternion.Inverse(locationTarget.rotation) * (cam.position - locationTarget.position);
    }

    void Update()
    {
        locationTarget.GetPositionAndRotation(out Vector3 targetPosition, out Quaternion targetRotation);

        // Compute the desired world position using the stored local offset rotated by the target rotation
        Vector3 desiredPosition = targetPosition + targetRotation * localOffset;

        // Use SmoothDamp to get ease-in/out movement. Use camSpeed as maxSpeed if > 0.
        float maxSpeed = camSpeed > 0f ? camSpeed : Mathf.Infinity;
        Vector3 smoothed = Vector3.SmoothDamp(cam.position, desiredPosition, ref velocity, smoothTime, maxSpeed, Time.deltaTime);

        // Clamp so the camera never exceeds maxDistance from the locationTarget
        Vector3 offsetFromTarget = smoothed - targetPosition;
        if (offsetFromTarget.sqrMagnitude > maxDistance * maxDistance)
            offsetFromTarget = offsetFromTarget.normalized * maxDistance;

        cam.position = targetPosition + offsetFromTarget;
        cam.LookAt(player);
    }
}
