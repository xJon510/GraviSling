using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SlingshotPlanet : MonoBehaviour
{
    [Header("Visual/Collision")]
    public float planetRadius = 1f;      // “surface” radius, crash when orbitRadius reaches this
    public float orbitRadius = 2f;
    public float shipFacingOffsetDeg = -90f;

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
    public GameObject machRingPrefab;

    private TrailRenderer trail;
    private bool isOrbiting = false;
    private int orbitDir = 1;     // +1 = CW, -1 = CCW

    public static SlingshotPlanet Active;
    private Rigidbody2D cachedRb;
    private PlayerShipController cachedPlayer;
    private float currentAngleDeg;


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
            trail = player.GetComponentInChildren<TrailRenderer>(true);
            StartCoroutine(OrbitAndCharge(player));
        }
    }

    IEnumerator OrbitAndCharge(PlayerShipController player)
    {
        isOrbiting = true;
        Active = this;
        cachedPlayer = player;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        cachedRb = rb;

        player.enabled = false;

        // Determine orbit direction based on incoming velocity vs entry vector
        Vector2 toPlanet = (Vector2)transform.position - rb.position;
        Vector2 incomingVel = rb.linearVelocity.normalized;
        float crossZ = toPlanet.x * incomingVel.y - toPlanet.y * incomingVel.x;   // 2D cross product z-value
        orbitDir = (crossZ > 0f) ? -1 : 1; // positive → CW, negative → CCW

        float orbitSpeed = baseOrbitSpeed;
        float launchSpeed = baseLaunchSpeed;
        currentAngleDeg = Mathf.Atan2(rb.position.y - transform.position.y,
                                  rb.position.x - transform.position.x) * Mathf.Rad2Deg;

        float chargeTimer = 0f;
        float currentRadius = orbitRadius;

        // --- swallow any pre-orbit taps/flags ---
        BoostInput.ClearReleaseFlag();
        bool prevPressed = true;        // force a *fresh* press after capture
        bool charging = false;       // only charge after fresh press in orbit

        while (true)
        {
            charging = BoostInput.Pressed;

            if (trail != null)
                trail.gameObject.SetActive(charging);

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

                    SFXTitleManager.Instance?.PlayExplosion();

                    StartCoroutine(DelayedGameOver(0.8f));

                    yield break;
                }
            }

            currentAngleDeg += orbitSpeed * orbitDir * Time.deltaTime;
            Vector2 center = transform.position;
            Vector2 offset = (Vector2)(Quaternion.Euler(0, 0, currentAngleDeg) * Vector3.right) * currentRadius;
            rb.position = center + offset;

            Vector2 tangent = new Vector2(-Mathf.Sin(currentAngleDeg * Mathf.Deg2Rad), Mathf.Cos(currentAngleDeg * Mathf.Deg2Rad));
            tangent *= orbitDir; // flip tangent if CCW
            float tangentDeg = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

            rb.MoveRotation(tangentDeg + shipFacingOffsetDeg);

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

            if (BoostInput.WasReleasedThisFrame())
                break;

            yield return null;
        }

        float finalAngle = currentAngleDeg + (launchAngleOffset * orbitDir);
        Vector2 dir = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad), Mathf.Sin(finalAngle * Mathf.Deg2Rad)).normalized;
        rb.linearVelocity = dir * launchSpeed;

        // Spawn Mach Ring on release
        if (machRingPrefab != null)
        {
            float z = player.transform.eulerAngles.z;
            Quaternion rot = Quaternion.Euler(0f, 0f, z);   // or z - 79f if that lines up better
            Instantiate(machRingPrefab, player.transform.position, rot);
        }

        SFXTitleManager.Instance?.PlayMachRingExplosionBySpeed(launchSpeed);

        if (trail != null)
        {
            trail.Clear();
            trail.gameObject.SetActive(false);
        }

        player.enabled = true;
        isOrbiting = false;
        if (Active == this) Active = null;
        cachedRb = null;
        cachedPlayer = null;
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

        GameOverUIManager.Instance.GameOver();
    }

    public void ReseedFromCurrentPosition()
    {
        if (!isOrbiting || !cachedRb) return;

        Vector2 center = transform.position;
        Vector2 offset = cachedRb.position - center;

        // reseed the driving angle so the coroutine uses the correct value after unpause
        currentAngleDeg = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;

        // compute tangent and set rotation immediately (not MoveRotation)
        Vector2 tangent = new Vector2(-Mathf.Sin(currentAngleDeg * Mathf.Deg2Rad),
                                       Mathf.Cos(currentAngleDeg * Mathf.Deg2Rad));
        tangent *= orbitDir;
        float tangentDeg = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

        // Force immediate orientation so visuals match before next physics step
        cachedRb.SetRotation(tangentDeg + shipFacingOffsetDeg);
    }
}
