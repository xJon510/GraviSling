using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerShipController : MonoBehaviour
{
    public float thrustForce = 6f;       // how strong WASD boost is
    public float dragAmount = 2f;        // slowdown when no input is pressed
    public float rotationSpeed = 360f;   // degrees per second to rotate toward travel direction

    public InputActionReference moveAction;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();

        // 1) Apply thrust in input direction
        if (input.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity += input.normalized * thrustForce * Time.fixedDeltaTime;
        }
        else
        {
            // 2) drag when no input
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, dragAmount * Time.fixedDeltaTime);
        }

        // 3) Rotate ship based on input direction first, then fallback to velocity when drifting
        if (input.sqrMagnitude > 0.01f)
        {
            // Face where the player is currently *trying* to thrust
            float targetAngle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            float newAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }
        else if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            // No input – just face the drift direction slowly
            float targetAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            float newAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }
    }
}
