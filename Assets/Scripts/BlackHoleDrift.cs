using UnityEngine;

public class BlackHoleDrift : MonoBehaviour
{
    // Starting speed of the black hole (units per second)
    public float startSpeed = 0.5f;

    // Rate at which it accelerates each second (units/sec^2)
    public float acceleration = 0.1f;

    [Header("Homing Settings")]
    public Transform player;           
    public float verticalChaseSpeed = 2f;       // how fast it lerps up/down toward player
    public float maxSpeed = 100f;

    [Header("Kill Settings")]
    public float killRadius = 1.5f;         // how close it has to get to eat the player
    public GameObject explosionPrefab;     // (optional) VFX when eaten

    public bool deadPlayer = false;

    public float currentSpeed;

    private void Start()
    {
        currentSpeed = startSpeed;

        MiniMapManager minimap = FindObjectOfType<MiniMapManager>();
        if (minimap != null)
            minimap.RegisterBlackHole(transform, new Vector2(-120f, 0f));
    }

    private void Update()
    {
        // Move to the right (X drift)
        transform.position += Vector3.right * currentSpeed * Time.deltaTime;

        // Home toward player's Y position (slowly)
        if (player != null)
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.Lerp(pos.y, player.position.y, verticalChaseSpeed * Time.deltaTime);
            transform.position = pos;
        }

        // Gradually increase speed
        currentSpeed += acceleration * Time.deltaTime;
        currentSpeed = Mathf.Min(currentSpeed, maxSpeed);

        float dist = Vector3.Distance(transform.position, player.position);
        float t = Mathf.InverseLerp(15f, 2f, dist); // you can keep using this for UI rumble if you want
        t = Mathf.Clamp01(t);

        // KILL WALL CHECK
        if (transform.position.x >= player.position.x && !deadPlayer)
        {
            Debug.Log("Player consumed by black hole!");

            if (explosionPrefab != null)
                Instantiate(explosionPrefab, player.position, Quaternion.identity);

            SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            player.GetComponent<PlayerShipController>().enabled = false;

            var prb = player.GetComponent<Rigidbody2D>();
            if (prb != null) prb.linearVelocity = Vector2.zero;

            GameOverUIManager.Instance.GameOver(
                RunStatsTracker.Instance.currentDistance,
                RunStatsTracker.Instance.currentTopSpeed
            );
            deadPlayer = true;
        }
    }
}
