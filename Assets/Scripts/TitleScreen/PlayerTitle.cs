using UnityEngine;

public class PlayerTitle : MonoBehaviour
{
    [Header("Drift Settings")]
    public Vector2 driftDirection = Vector2.right; // normalized direction
    public float driftSpeed = 2f;

    [Header("Loop Options")]
    [Tooltip("If true, will reset to the start position when off-screen.")]
    public bool loop = false;
    public Vector3 startPosition;
    public Rect screenBounds;

    private void Start()
    {
        // Normalize direction so speed is consistent
        driftDirection = driftDirection.normalized;

        // Save start position if we want to loop later
        startPosition = transform.position;

        // Define a basic screen bounds rectangle in world space (optional)
        Camera cam = Camera.main;
        Vector3 bottomLeft = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 topRight = cam.ViewportToWorldPoint(new Vector3(1, 1, 0));
        screenBounds = new Rect(bottomLeft.x, bottomLeft.y,
                                topRight.x - bottomLeft.x,
                                topRight.y - bottomLeft.y);
    }

    private void Update()
    {
        transform.Translate(driftDirection * driftSpeed * Time.deltaTime);

        // optional looping: if we drift off-screen, reset to start
        if (loop && !screenBounds.Contains(transform.position))
        {
            transform.position = startPosition;
        }
    }
}
