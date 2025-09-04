using UnityEngine;

public class JigglePlanet : MonoBehaviour
{
    [Header("Jiggle Settings")]
    [Tooltip("How far the planet moves from its original position on each axis.")]
    public Vector2 amplitude = new Vector2(0.1f, 0.1f);

    [Tooltip("How fast the planet jiggles on each axis.")]
    public Vector2 frequency = new Vector2(2f, 2f);

    private Vector3 startPos;

    void Awake()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float x = Mathf.Sin(Time.time * frequency.x) * amplitude.x;
        float y = Mathf.Cos(Time.time * frequency.y) * amplitude.y;

        transform.localPosition = startPos + new Vector3(x, y, 0f);
    }
}
