using System.Collections.Generic;
using UnityEngine;

public class AsteroidRecycleSimple : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public MiniMapManager minimap;                // optional; will auto-find if left null

    [Header("Asteroid Prefabs & Pool")]
    public GameObject[] asteroidPrefabs;
    [Min(1)] public int poolSize = 40;

    [Header("Inner Area (centered on player, world units)")]
    [Tooltip("Half-width of the active area (match your minimap X range).")]
    public float halfWidth = 220f;
    [Tooltip("Half-height of the active area (match your minimap Y range).")]
    public float halfHeight = 120f;

    [Header("Outer Padding (wrapping band)")]
    [Tooltip("How far beyond the inner area before we wrap horizontally.")]
    public float padX = 70f;
    [Tooltip("How far beyond the inner area before we wrap vertically.")]
    public float padY = 70f;

    [Header("Wrap Feel")]
    [Tooltip("Inset so wrapped asteroids appear just inside the inner edge.")]
    public float inset = 6f;
    [Tooltip("Random jitter along the perpendicular axis when wrapping.")]
    public float jitter = 12f;
    public bool randomizeOnSpawn = true;
    public bool randomizeOnWrap = false;
    public Vector2 driftSpeedRange = new Vector2(0.5f, 2f);
    public Vector2 angularVelRange = new Vector2(-50f, 50f);

    // internals
    private readonly List<Transform> pool = new List<Transform>();
    private MiniMapManager _minimap;

    void Awake()
    {
        if (!player) player = GameObject.FindWithTag("Player")?.transform;
        _minimap = minimap ? minimap : FindObjectOfType<MiniMapManager>();
    }

    void Start()
    {
        Vector2 c = player ? (Vector2)player.position : Vector2.zero;

        for (int i = 0; i < poolSize; i++)
        {
            var prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
            var go = Instantiate(prefab);
            var t = go.transform;

            // Initial random position inside the inner area
            var pos = new Vector2(
                Random.Range(c.x - halfWidth, c.x + halfWidth),
                Random.Range(c.y - halfHeight, c.y + halfHeight)
            );
            t.position = pos;

            // Register once with minimap
            if (_minimap) _minimap.RegisterAsteroid(t);

            // Optional random motion
            if (randomizeOnSpawn) ApplyRandomMotion(go);

            pool.Add(t);
        }
    }

    void Update()
    {
        if (!player) return;

        Vector2 c = player.position;

        // Inner bounds
        float ixMin = c.x - halfWidth;
        float ixMax = c.x + halfWidth;
        float iyMin = c.y - halfHeight;
        float iyMax = c.y + halfHeight;

        // Outer (wrap) bounds
        float oxMin = ixMin - padX;
        float oxMax = ixMax + padX;
        float oyMin = iyMin - padY;
        float oyMax = iyMax + padY;

        foreach (var t in pool)
        {
            if (!t) continue;
            Vector3 p = t.position;
            bool wrapped = false;

            // Horizontal wrap
            if (p.x < oxMin)
            {
                p.x = ixMax - inset;
                p.y = Mathf.Clamp(p.y + Random.Range(-jitter, jitter), iyMin, iyMax);
                wrapped = true;
            }
            else if (p.x > oxMax)
            {
                p.x = ixMin + inset;
                p.y = Mathf.Clamp(p.y + Random.Range(-jitter, jitter), iyMin, iyMax);
                wrapped = true;
            }

            // Vertical wrap
            if (p.y < oyMin)
            {
                p.y = iyMax - inset;
                p.x = Mathf.Clamp(p.x + Random.Range(-jitter, jitter), ixMin, ixMax);
                wrapped = true;
            }
            else if (p.y > oyMax)
            {
                p.y = iyMin + inset;
                p.x = Mathf.Clamp(p.x + Random.Range(-jitter, jitter), ixMin, ixMax);
                wrapped = true;
            }

            if (wrapped)
            {
                t.position = p;
                if (randomizeOnWrap) ApplyRandomMotion(t.gameObject);
            }
        }
    }

    private void ApplyRandomMotion(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody2D>();
        if (!rb) return;

        rb.linearVelocity = Random.insideUnitCircle.normalized * Random.Range(driftSpeedRange.x, driftSpeedRange.y);
        rb.angularVelocity = Random.Range(angularVelRange.x, angularVelRange.y);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!player) return;
        Vector3 c = player.position;

        // inner box
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        DrawRect(c, halfWidth, halfHeight);

        // outer box
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        DrawRect(c, halfWidth + padX, halfHeight + padY);
    }
    void DrawRect(Vector3 c, float hw, float hh)
    {
        Vector3 a = new Vector3(c.x - hw, c.y - hh, 0);
        Vector3 b = new Vector3(c.x + hw, c.y - hh, 0);
        Vector3 d = new Vector3(c.x - hw, c.y + hh, 0);
        Vector3 e = new Vector3(c.x + hw, c.y + hh, 0);
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, e);
        Gizmos.DrawLine(e, d); Gizmos.DrawLine(d, a);
    }
#endif
}
