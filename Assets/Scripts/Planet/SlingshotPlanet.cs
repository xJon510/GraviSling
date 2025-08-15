using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SlingshotPlanet : MonoBehaviour
{
    public float orbitRadius = 2f;
    public float orbitSpeed = 180f;      // degrees per second
    public float launchSpeed = 12f;
    public float launchAngleOffset = 0f; // optional extra kick angle
    public bool ejectOnSpace = true;

    private bool isOrbiting = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isOrbiting) return;

        PlayerShipController player = other.GetComponent<PlayerShipController>();
        if (player != null)
        {
            StartCoroutine(OrbitForever(player));
        }
    }

    IEnumerator OrbitForever(PlayerShipController player)
    {
        isOrbiting = true;
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        player.enabled = false; // disable thrust

        // determine starting angle
        Vector2 startDir = (rb.position - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(startDir.y, startDir.x) * Mathf.Rad2Deg;

        while (true)
        {
            // increment angle at fixed rate
            angle += orbitSpeed * Time.deltaTime;

            Vector2 center = transform.position;
            Vector2 orbitOffset = (Vector2)(Quaternion.Euler(0, 0, angle) * Vector3.right) * orbitRadius;
            rb.position = center + orbitOffset;

            // rotate player tangent to the orbit
            Vector2 tangent = new Vector2(-Mathf.Sin(angle * Mathf.Deg2Rad), Mathf.Cos(angle * Mathf.Deg2Rad));
            rb.MoveRotation(Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);

            // eject on space
            if (ejectOnSpace && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                break;
            }

            yield return null;
        }

        // launch toward tangent direction (+ offset)
        float finalAngle = angle + launchAngleOffset;
        Vector2 launchDir = new Vector2(Mathf.Cos(finalAngle * Mathf.Deg2Rad), Mathf.Sin(finalAngle * Mathf.Deg2Rad)).normalized;

        rb.linearVelocity = launchDir * launchSpeed;
        player.enabled = true;
        isOrbiting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, orbitRadius);
    }
}
