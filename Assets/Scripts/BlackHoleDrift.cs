using UnityEngine;

public class BlackHoleDrift : MonoBehaviour
{
    // Starting speed of the black hole (units per second)
    public float startSpeed = 0.5f;

    // Rate at which it accelerates each second (units/sec^2)
    public float acceleration = 0.1f;

    private float currentSpeed;

    private void Start()
    {
        currentSpeed = startSpeed;
    }

    private void Update()
    {
        // Move to the right
        transform.position += Vector3.right * currentSpeed * Time.deltaTime;

        // Gradually increase speed
        currentSpeed += acceleration * Time.deltaTime;
    }
}
