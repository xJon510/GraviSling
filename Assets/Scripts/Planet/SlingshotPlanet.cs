using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SlingshotPlanet : MonoBehaviour
{
    [Header("Visual/Collision")]
    public float planetRadius = 1f;      // “surface” radius, crash when orbitRadius reaches this
    public float orbitRadius = 2f;

    [Header("Orbit Settings")]
    public float baseOrbitSpeed = 180f;
    public float baseLaunchSpeed = 12f;
    public float launchAngleOffset = 0f;

    [Header("Charge Settings")]
    public float chargeRate = 80f;       // how fast orbit/launch speed increase per second
    public float shrinkRate = 1f;        // orbitRadius shrink speed per second
    public float maxChargeTime = 2.5f;   // fallback crash timer

    public TMP_Text speedText;

    public RectTransform minimapIcon;  
    public float minimapRotationOffset = -90f;

    public GameObject explosionPlayerPrefab;

    private bool isOrbiting = false;
    private int orbitDir = 1;     // +1 = CW, -1 = CCW

    void Awake()
    {
        // Find the UI element once automatically:
        var speedGO = GameObject.Find("ScreenSpaceCanvas/Speed");
        if (speedGO != null)
            speedText = speedGO.GetComponent<TMP_Text>();

        var playerIcon = GameObject.Find("ScreenSpaceCanvas/MiniMap/MiniMapPlayer");
        if (playerIcon != null)
            minimapIcon = playerIcon.GetComponent<RectTransform>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOrbiting) return;

        PlayerShipController player = other.GetComponent<PlayerShipController>();
        if (player != null)
        {
            StartCoroutine(OrbitAndCharge(player));
        }
    }

    IEnumerator OrbitAndCharge(PlayerShipController player)
    {
        isOrbiting = true;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        player.enabled = false;

        // Determine orbit direction based on incoming velocity vs entry vector
        Vector2 toPlanet = (Vector2)transform.position - rb.position;
        Vector2 incomingVel = rb.linearVelocity.normalized;
        float crossZ = toPlanet.x * incomingVel.y - toPlanet.y * incomingVel.x;   // 2D cross product z-value
        orbitDir = (crossZ > 0f) ? -1 : 1; // positive → CW, negative → CCW

        float orbitSpeed = baseOrbitSpeed;
        float launchSpeed = baseLaunchSpeed;
        float angle = Mathf.Atan2(rb.position.y - transform.position.y,
                                  rb.position.x - transform.position.x) * Mathf.Rad2Deg;

        float chargeTimer = 0f;
        float currentRadius = orbitRadius;

        while (true)
        {
            bool charging = Keyboard.current.spaceKey.isPressed;
            if (charging)
            {
                orbitSpeed += chargeRate * Time.deltaTime;
                launchSpeed += chargeRate * Time.deltaTime;
                currentRadius = Mathf.MoveTowards(currentRadius, planetRadius, shrinkRate * Time.deltaTime);

                chargeTimer += Time.deltaTime;
                if (chargeTimer > maxChargeTime || currentRadius <= planetRadius)
                {
                    Debug.Log("Overcharged and crashed!");
                    rb.linearVelocity = Vector2.zero;
                    player.enabled = false;

                    SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.enabled = false;

                    Instantiate(explosionPlayerPrefab, player.transform.position, Quaternion.identity);
                    isOrbiting = false;

                    StartCoroutine(DelayedGameOver(0.8f));

                    yield break;
                }
            }

            angle += orbitSpeed * orbitDir * Time.deltaTime;
            Vector2 center = transform.position;
            Vector2 offset = (Vector2)(Quaternion.Euler(0, 0, angle) * Vector3.right) * currentRadius;
            rb.position = center + offset;

            Vector2 tangent = new Vector2(-Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad));
            tangent *= orbitDir; // flip tangent if CCW
            rb.MoveRotation(Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);

            if (speedText != null)
            {
                float uiSpeed = (orbitSpeed * 0.5f) + (launchSpeed * 0.5f);
                speedText.text = $"Speed: {uiSpeed:F1} km/s";
            }

            if (minimapIcon != null)
            {
                float iconAngle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg + minimapRotationOffset;
                minimapIcon.localEulerAngles = new Vector3(0f, 0f, iconAngle);
            }

            if (Keyboard.current.spaceKey.wasReleasedThisFrame)
                break;

            yield return null;
        }

        float finalAngle = angle + (launchAngleOffset * orbitDir);
        Vector2 dir = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad), Mathf.Sin(finalAngle * Mathf.Deg2Rad)).normalized;
        rb.linearVelocity = dir * launchSpeed;

        player.enabled = true;
        isOrbiting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, orbitRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, planetRadius);
    }

    private IEnumerator DelayedGameOver(float delay)
    {
        yield return new WaitForSeconds(delay);

        GameOverUIManager.Instance.GameOver(
            RunStatsTracker.Instance.currentDistance,
            RunStatsTracker.Instance.currentTopSpeed
        );
    }
}
