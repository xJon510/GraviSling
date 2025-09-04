using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }
    private bool _paused = false;

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

    private void Awake()
    {
        Instance = this;
    }

    private void LateUpdate()
    {
        if (_paused) return;   // <-- NEW

        // == Position follow ==
        if (target != null)
        {
            Vector3 desiredPos = target.position + offset;
            Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed);
            transform.position = smoothedPos;
        }

        // == Dynamic FOV zoom based on velocity magnitude ==
        float currentSpeed = rb ? rb.linearVelocity.magnitude : 0f;

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

    /// <summary>Pause/unpause the follow without disabling the component.</summary>
    public void SetPaused(bool paused, bool snapToTargetOnResume = true)
    {
        _paused = paused;

        if (!paused && snapToTargetOnResume && target != null)
        {
            // Snap position to target so we don't lerp from an old spot on the first live frame.
            transform.position = target.position + offset;

            // Optional: snap FOV to its current target to avoid a one-frame pop.
            float currentSpeed = rb ? rb.linearVelocity.magnitude : 0f;
            float t = (currentSpeed <= speedThreshold) ? 0f
                : Mathf.InverseLerp(speedThreshold, speedThreshold * 2f, currentSpeed);
            cam.fieldOfView = Mathf.Lerp(minFOV, maxFOV, t);
        }
    }

}
