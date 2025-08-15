using UnityEngine;

public class Planet : MonoBehaviour
{
    [Header("Gravity Settings")]
    public float gravityRadius = 5f;         // how far the gravity influence reaches
    public float gravityPullStrength = 10f;  // how strong the pull is

    [Header("Slingshot Boost")]
    public float innerRadius = 2f;          // how close before we get a boost
    public float slingshotForce = 6f;       // how strong the tangential boost is
    public bool oneTimeBoost = true;        // only boost once per pass?

    private bool hasBoostedThisPass = false;


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, gravityRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, innerRadius);
    }

    private void FixedUpdate()
    {
        PlayerShipController player = FindObjectOfType<PlayerShipController>();
        if (player == null) return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // vector planet->player
        Vector2 diff = rb.position - (Vector2)transform.position;
        float dist = diff.magnitude;
        Vector2 dirToPlanet = -diff.normalized;

        // 1) pull inward if within gravityRadius
        if (dist < gravityRadius)
        {
            rb.linearVelocity += dirToPlanet * gravityPullStrength * Time.fixedDeltaTime;

            // 2) if close enough -> give tangential boost (gravity assist)
            if (dist < innerRadius)
            {
                if (!oneTimeBoost || !hasBoostedThisPass)
                {
                    Vector2 tangent = new Vector2(-dirToPlanet.y, dirToPlanet.x);
                    rb.linearVelocity += tangent.normalized * slingshotForce;
                    hasBoostedThisPass = true;
                }
            }
            else
            {
                // reset boost flag once we exit inner zone
                hasBoostedThisPass = false;
            }
        }
        else
        {
            hasBoostedThisPass = false;
        }
    }
}
