using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Rigidbody2D rb;

    [Header("Position Follow")]
    public float smoothSpeed = 0.125f;
    public Vector3 offset;

    [Header("Zoom Settings")]
    public float minFOV = 120f;
    public float maxFOV = 140f;
    public float zoomSpeed = 2f;
    public float speedThreshold = 20f;

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        // == Position follow ==
        if (target != null)
        {
            Vector3 desiredPos = target.position + offset;
            Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);
            transform.position = smoothedPos;
        }

        // == Dynamic FOV zoom based on velocity magnitude ==
        float currentSpeed = rb.linearVelocity.magnitude;

        float targetFOV;
        if (currentSpeed <= speedThreshold)
        {
            targetFOV = minFOV;
        }
        else
        {
            float t = Mathf.InverseLerp(speedThreshold, speedThreshold * 2f, currentSpeed);
            targetFOV = Mathf.Lerp(minFOV, maxFOV, t);
        }

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }
}
