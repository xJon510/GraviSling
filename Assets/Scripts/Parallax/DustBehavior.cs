using UnityEngine;

public class DustBehavior : MonoBehaviour
{
    public float minSpin = -5f;
    public float maxSpin = 5f;
    public float driftRange = 0.05f;

    private float spinSpeed;
    private Vector2 drift;

    void Start()
    {
        spinSpeed = Random.Range(minSpin, maxSpin);
        drift = new Vector2(Random.Range(-driftRange, driftRange),
                            Random.Range(-driftRange, driftRange));

        // optional alpha random:
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var c = sr.color;
            c.a = Random.Range(0.5f, 1f);
            sr.color = c;
        }
    }

    void Update()
    {
        transform.Rotate(Vector3.forward, spinSpeed * Time.deltaTime);
        transform.position += (Vector3)(drift * Time.deltaTime);
    }
}
