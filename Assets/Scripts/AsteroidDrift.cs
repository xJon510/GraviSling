using UnityEngine;

public class AsteroidDrift : MonoBehaviour
{
    void Start()
    {
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Random.insideUnitCircle.normalized * Random.Range(0.6f, 1.6f);
            rb.angularVelocity = Random.Range(-40f, 40f);
        }
    }
}
