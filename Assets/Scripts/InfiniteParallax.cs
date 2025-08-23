using UnityEngine;

public class InfiniteParallax : MonoBehaviour
{
    public Transform cam;
    public Transform player;
    public float parallaxFactor = 0.9f;
    public float tileSize = 10000f;

    public EasterEggManager eggManager;

    private Vector3 lastCamPos;
    private Transform[] tiles = new Transform[3];

    private void Awake()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            tiles[i] = transform.GetChild(i);
        }
    }

    private void Start()
    {
        lastCamPos = cam.position;
    }

    private void LateUpdate()
    {
        // Move holder via parallax from camera
        Vector3 delta = cam.position - lastCamPos;
        transform.position += delta * parallaxFactor;
        lastCamPos = cam.position;

        // Find the leftmost tile
        Transform leftMost = tiles[0];
        for (int i = 1; i < tiles.Length; i++)
        {
            if (tiles[i].position.x < leftMost.position.x)
                leftMost = tiles[i];
        }

        // Calculate distance from player to that leftmost tile
        float dist = player.position.x - leftMost.position.x;

        // If player has moved more than a threshold ahead of that tile, recycle it forward.
        // Use a loop in case of large teleports / high speeds.
        const float recycleThreshold = 1300f; // your current value; tweak as needed
        while (dist > recycleThreshold)
        {
            leftMost.localPosition += new Vector3(tileSize * tiles.Length, 0f, 0f);

            // Notify manager once per shift
            if (eggManager != null)
                eggManager.OnBackgroundPanelShifted();

            // Recompute leftMost + dist after moving it
            leftMost = tiles[0];
            for (int i = 1; i < tiles.Length; i++)
                if (tiles[i].position.x < leftMost.position.x)
                    leftMost = tiles[i];

            dist = player.position.x - leftMost.position.x;
        }
    }
}
