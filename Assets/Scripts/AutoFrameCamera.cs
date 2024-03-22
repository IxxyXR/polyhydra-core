using UnityEngine;

public class AutoFrameCamera : MonoBehaviour
{
    public GameObject targetObject;
    public float azimuthAngle = 45.0f;
    public float polarAngle = 30.0f;
    public float zoom = 0.0f;

    private Vector3 targetPosition;
    private float distanceToTarget;

    void Update()
    {
        if (targetObject != null)
        {
            UpdateTargetPosition();
            UpdateCameraPosition();
        }
    }

    private void UpdateTargetPosition()
    {
        // Calculate the bounding box in world space
        Renderer targetRenderer = targetObject.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            Bounds bounds = targetRenderer.bounds;
            targetPosition = bounds.center;

            // Adjust distance based on bounding box size and desired zoom
            float maxBoundSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            distanceToTarget = (0.5f * maxBoundSize) / Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
            distanceToTarget += zoom;
        }
    }

    private void UpdateCameraPosition()
    {
        // Calculate camera position from azimuth and polar angles
        float azimuthRad = azimuthAngle * Mathf.Deg2Rad;
        float polarRad = polarAngle * Mathf.Deg2Rad;

        Vector3 cameraOffset = new Vector3(
            distanceToTarget * Mathf.Sin(polarRad) * Mathf.Cos(azimuthRad),
            distanceToTarget * Mathf.Cos(polarRad),
            distanceToTarget * Mathf.Sin(polarRad) * Mathf.Sin(azimuthRad)
        );

        transform.position = targetPosition + cameraOffset;
        transform.LookAt(targetPosition);
    }
}