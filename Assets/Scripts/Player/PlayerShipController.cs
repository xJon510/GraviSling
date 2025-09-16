using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerShipController : MonoBehaviour
{
    public float thrustForce = 6f;       // how strong WASD boost is
    public float dragAmount = 2f;        // slowdown when no input is pressed
    public float rotationSpeed = 360f;   // degrees per second to rotate toward travel direction
    public float maxThrustSpeed = 15f;

    public TMP_Text speedText;

    public RectTransform minimapIcon;   // ← Drag your minimap player icon here
    public float minimapRotationOffset = -90f;
    public float shipFacingOffsetDeg = -90f;

    public InputActionReference moveAction;

    private Rigidbody2D rb;

    // Pause Cache stuff
    private Vector2 _savedVel;
    private float _savedAngVel;
    private bool _savedSimulated;
    private bool _isPausedDrift;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CurrencyGem gem = other.GetComponent<CurrencyGem>();
        if (gem != null)
        {
            // picked up
            gem.Collect();
            // increase your currency here...
        }
    }

    private void FixedUpdate()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>() + JoystickInput.Vector;
        input = Vector2.ClampMagnitude(input, 1f);

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
            targetAngle += shipFacingOffsetDeg;
            float newAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }
        else if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            // No input � just face the drift direction slowly
            float targetAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            targetAngle += shipFacingOffsetDeg;
            float newAngle = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, rotationSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newAngle);
        }

        // update speed counter
        float speed = rb.linearVelocity.magnitude;
        RunStatsModel.I?.UpdateSpeed(speed);

        if (speedText != null)
            speedText.text = $"Speed: {speed:F1} km/s";

        // 4) rotate minimap icon
        if (minimapIcon != null)
        {
            float iconAngle = rb.rotation + shipFacingOffsetDeg - minimapRotationOffset;
            minimapIcon.localEulerAngles = new Vector3(0f, 0f, iconAngle);
        }
    }

    public void PauseDrift()
    {
        if (_isPausedDrift) return;
        _isPausedDrift = true;

        // cache state
        _savedVel = rb.linearVelocity;
        _savedAngVel = rb.angularVelocity;
        _savedSimulated = rb.simulated;

        // stop physics simulation (no drifting, no rotation)
        rb.simulated = false;
        // (Optional) if you want UI to show 0 speed while paused:
        rb.linearVelocity = Vector2.zero; rb.angularVelocity = 0f;
    }

    public void ResumeDrift()
    {
        if (!_isPausedDrift) return;
        _isPausedDrift = false;

        // restore physics
        rb.simulated = _savedSimulated;
        rb.linearVelocity = _savedVel;
        rb.angularVelocity = _savedAngVel;
    }

}
