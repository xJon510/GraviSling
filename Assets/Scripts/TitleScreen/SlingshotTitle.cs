using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SlingshotTitle : MonoBehaviour
{
    public enum LaunchDir { East, West, North, South }

    [Header("Centering")]
    [Tooltip("If set, use this as the orbit center; otherwise use this planet's Collider2D bounds.center.")]
    public Transform orbitCenterOverride;

    [Header("Detection (visual only)")]
    public float captureRadius = 2f;

    [Header("Orbit")]
    public float orbitRadius = 2f;
    public float orbitSpeedDegPerSec = 180f;
    [Tooltip("Minimum full laps before we *allow* launch alignment check.")]
    public int minLapsBeforeLaunch = 1;
    [Tooltip("+1 = clockwise, -1 = counterclockwise")]
    public int orbitDirection = +1;

    [Header("Launch Alignment")]
    [Tooltip("How close the ship's tangent (facing) must be to the requested cardinal angle, in degrees.")]
    public float alignToleranceDeg = 10f;
    [Tooltip("Safety cap to avoid infinite orbiting if something is off.")]
    public int maxExtraLaps = 2;

    [Header("Launch")]
    public LaunchDir launchDirection = LaunchDir.East;
    public float launchAngleOffset = 0f;
    public float launchSpeed = 12f;

    [Header("FX (optional)")]
    public GameObject machRingPrefab;        // optional

    [Header("Title Drift Hook (optional)")]
    [Tooltip("If assigned (e.g., your PlayerTitle.cs), it will be disabled during orbit and re-enabled after launch.")]
    public MonoBehaviour driftControllerToDisable;

    private Collider2D _planetCol;
    private bool _busy;

    void Awake()
    {
        _planetCol = GetComponent<Collider2D>();
        if (_planetCol != null) _planetCol.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_busy) return;

        var rb = other.attachedRigidbody;
        if (rb == null) return; // needs a Rigidbody2D to puppet cleanly (Kinematic is fine)

        StartCoroutine(OrbitThenLaunch(rb));
    }

    private IEnumerator OrbitThenLaunch(Rigidbody2D rb)
    {
        _busy = true;

        // disable drift while we control the player (if provided)
        if (driftControllerToDisable != null) driftControllerToDisable.enabled = false;

        // zero motion
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // center to orbit around
        Vector2 center = orbitCenterOverride
            ? (Vector2)orbitCenterOverride.position
            : (_planetCol ? _planetCol.bounds.center : (Vector2)transform.position);

        // compute current angle and snap to orbit ring
        Vector2 fromCenter = rb.position - center;
        float angleDeg = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg;

        Vector2 startOffset = (Vector2)(Quaternion.Euler(0, 0, angleDeg) * Vector2.right) * orbitRadius;
        rb.position = center + startOffset;

        float lapAccumDeg = 0f;
        float lastAngleDeg = angleDeg;
        float desiredTangentDeg = CardinalAngleDeg(launchDirection) + launchAngleOffset;
        int lapsCompleted = 0;

        // ORBIT LOOP (physics step)
        while (true)
        {
            // advance angle
            angleDeg += orbitSpeedDegPerSec * orbitDirection * Time.fixedDeltaTime;

            // progress tracking
            float delta = Mathf.DeltaAngle(lastAngleDeg, angleDeg);
            lapAccumDeg += Mathf.Abs(delta);
            lastAngleDeg = angleDeg;

            // count completed laps
            while (lapAccumDeg >= 360f)
            {
                lapAccumDeg -= 360f;
                lapsCompleted++;
            }

            // place on circle
            Vector2 offset = (Vector2)(Quaternion.Euler(0, 0, angleDeg) * Vector2.right) * orbitRadius;
            rb.MovePosition(center + offset);

            // tangent (facing) angle depends on orbit direction
            float tangentDeg = angleDeg + (orbitDirection >= 0 ? 90f : -90f);
            rb.MoveRotation(tangentDeg);

            // launch condition:
            // at least min laps AND facing near desired cardinal, OR we've exceeded safety extra laps
            bool lapsOk = lapsCompleted >= Mathf.Max(1, minLapsBeforeLaunch);
            bool aligned = Mathf.Abs(Mathf.DeltaAngle(tangentDeg, desiredTangentDeg)) <= alignToleranceDeg;
            bool safety = lapsCompleted >= Mathf.Max(1, minLapsBeforeLaunch) + Mathf.Max(0, maxExtraLaps);

            if ((lapsOk && aligned) || safety)
                break;

            yield return new WaitForFixedUpdate();
        }

        // ensure no residual motion then launch purely cardinal
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // 1) snap rotation to the intended launch heading
        float desiredAngle = CardinalAngleDeg(launchDirection) + launchAngleOffset;
        // physics-friendly rotation set at release:
        rb.MoveRotation(desiredAngle);

        // 2) fire exactly along that heading
        Vector2 dir2 = AngleToVector2(desiredAngle);
        rb.linearVelocity = dir2 * launchSpeed;

        if (machRingPrefab != null)
        {
            float z = rb.rotation;
            var rot = Quaternion.Euler(0f, 0f, z - 100f);
            var ring = Instantiate(machRingPrefab, rb.position, rot);
            var p = ring.transform.position;
            ring.transform.position = new Vector3(p.x, p.y, p.z + 70f);
        }

        Vector2 dir = CardinalToVector2(launchDirection);
        rb.linearVelocity = dir * launchSpeed;

        // tiny cooldown
        yield return new WaitForSeconds(0.15f);
        _busy = false;
    }

    private static Vector2 CardinalToVector2(LaunchDir d)
    {
        switch (d)
        {
            case LaunchDir.East: return Vector2.right;   // 0�
            case LaunchDir.West: return Vector2.left;    // 180�
            case LaunchDir.North: return Vector2.up;      // 90�
            case LaunchDir.South: return Vector2.down;    // -90� (or 270�)
        }
        return Vector2.right;
    }

    private static float CardinalAngleDeg(LaunchDir d)
    {
        switch (d)
        {
            case LaunchDir.East: return 0f;
            case LaunchDir.North: return 90f;
            case LaunchDir.West: return 180f;
            case LaunchDir.South: return -90f; // equivalent to 270�
        }
        return 0f;
    }

    private static Vector2 AngleToVector2(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.7f);
        Vector3 center = orbitCenterOverride ? orbitCenterOverride.position : transform.position;
        Gizmos.DrawWireSphere(center, orbitRadius);

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, captureRadius);
    }
#endif
}
