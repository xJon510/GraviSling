using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerShipController : MonoBehaviour
{
    public float thrustForce = 6f;       // how strong WASD boost is
    public float dragAmount = 2f;        // slowdown when no input is pressed
    public float rotationSpeed = 360f;   // degrees per second to rotate toward travel direction
    public float maxThrustSpeed = 15f;

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
            float currentSpeed = rb.linearVelocity.magnitude;
            Vector2 velDir = rb.linearVelocity.normalized;
            Vector2 inputDir = input.normalized;

            float dot = Vector2.Dot(velDir, inputDir); // +1: same way, -1: opposite

            float throttle = 1f;
            if (dot > 0.5f)  // mostly pushing forward in same direction
            {
                throttle = Mathf.InverseLerp(maxThrustSpeed, 0f, currentSpeed);
            }

            rb.linearVelocity += inputDir * (thrustForce * throttle) * Time.fixedDeltaTime;
        }
        else
        {
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
            // No input � just face the drift direction slowly
            float targetAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            float newAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }
    }
}
