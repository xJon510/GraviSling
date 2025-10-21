using UnityEngine;
using UnityEngine.UI;

/// Drop this on the SHIP (orbiter) UI object (must have a RectTransform).
/// Assign `planet` to the center UI object. Both should share the same parent.
public class UIShipOrbit : MonoBehaviour
{
    [Header("References")]
    public RectTransform planet;
    public RectTransform ship;   // if null, auto-grab own RectTransform in Start

    [Header("Orbit")]
    public float orbitSpeedDeg = 180f;   // degrees per second
    public int orbitDir = 1;             // +1 = CW, -1 = CCW (matches your SlingshotPlanet usage)
    public float shipFacingOffsetDeg = -90f;

    // internals
    Vector2 centerStartLS;
    Vector2 shipStartLS;
    float startAngleDeg;
    float radius;
    float angleDeg;

    void Start()
    {
        if (!ship) ship = GetComponent<RectTransform>();
        if (!ship || !planet) return;

        // Require same local space to keep math simple and avoid unexpected repositioning
        if (ship.parent != planet.parent) { Debug.LogWarning("[UIShipOrbit] planet & ship must share the same parent."); enabled = false; return; }

        // Cache initial local positions (won’t move anything on assignment)
        centerStartLS = planet.localPosition;
        shipStartLS = ship.localPosition;

        Vector2 initialOffset = shipStartLS - centerStartLS;      // local-space radius & phase
        radius = initialOffset.magnitude;
        if (radius < 1e-5f) { radius = 50f; initialOffset = Vector2.right * radius; }

        startAngleDeg = Mathf.Atan2(initialOffset.y, initialOffset.x) * Mathf.Rad2Deg;
        angleDeg = startAngleDeg;                             // start exactly where you placed it
    }

    void Update()
    {
        if (!Application.isPlaying || !ship || !planet) return;

        angleDeg += orbitSpeedDeg * orbitDir * Time.deltaTime;

        Vector2 centerLS = planet.localPosition;
        Vector2 offset = new Vector2(Mathf.Cos(angleDeg * Mathf.Deg2Rad), Mathf.Sin(angleDeg * Mathf.Deg2Rad)) * radius;
        ship.localPosition = centerLS + offset;

        Vector2 tangent = new Vector2(-Mathf.Sin(angleDeg * Mathf.Deg2Rad), Mathf.Cos(angleDeg * Mathf.Deg2Rad)) * orbitDir;
        float tangentDeg = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        ship.localEulerAngles = new Vector3(0f, 0f, tangentDeg + shipFacingOffsetDeg);
    }
}
