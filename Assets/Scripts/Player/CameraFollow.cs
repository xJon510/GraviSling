using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;                   // player
    public float followSpeed = 5f;             // how fast camera catches up
    public Vector3 offset = new Vector3(0, 0, -10);  // camera offset

    [Header("Zoom Settings")]
    public bool dynamicZoom = true;
    public float minZoom = 5f;   // smallest orthographicSize / FOV
    public float maxZoom = 10f;  // largest zoom-out
    public float zoomSpeed = 2f; // how quickly it zooms
    public float zoomVelocityMultiplier = 0.5f; // how much velocity affects zoom

    private Camera cam;
    private Rigidbody2D targetRb;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (target != null) targetRb = target.GetComponent<Rigidbody2D>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // --- Smooth follow position ---
        Vector3 desiredPos = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);

        // --- Dynamic zoom out based on velocity ---
        if (dynamicZoom && targetRb != null)
        {
            float speed = targetRb.linearVelocity.magnitude;
            float desiredZoom = Mathf.Lerp(minZoom, maxZoom, speed * zoomVelocityMultiplier);

            if (cam.orthographic)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, desiredZoom, zoomSpeed * Time.deltaTime);
            }
            else
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, desiredZoom, zoomSpeed * Time.deltaTime);
            }
        }
    }
}
