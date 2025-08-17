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

    private float currentSpeed;

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
    }
}
