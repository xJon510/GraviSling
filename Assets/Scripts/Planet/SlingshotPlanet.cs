using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private bool isOrbiting = false;

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
                    player.enabled = false; // TODO trigger real game-over state
                    isOrbiting = false;
                    yield break;
                }
            }

            angle += orbitSpeed * Time.deltaTime;

            Vector2 center = transform.position;
            Vector2 offset = (Vector2)(Quaternion.Euler(0, 0, angle) * Vector3.right) * currentRadius;
            rb.position = center + offset;

            Vector2 tangent = new Vector2(-Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad));
            rb.MoveRotation(Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);

            if (Keyboard.current.spaceKey.wasReleasedThisFrame)
            {
                break;
            }

            yield return null;
        }

        float finalAngle = angle + launchAngleOffset;
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
}
