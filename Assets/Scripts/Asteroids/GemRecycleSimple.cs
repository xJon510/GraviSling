using System.Collections.Generic;
using UnityEngine;

public class GemRecycleSimple : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public MiniMapManager minimap; // optional; auto-finds if null

    [Header("Gem Prefabs (0=common..n=rare)")]
    public GameObject[] gemPrefabs;
    [Tooltip("Weights must match gemPrefabs length (will be normalized).")]
    public float[] rarityWeights = { 0.80f, 0.15f, 0.04f, 0.01f };

    [Header("Pool")]
    [Min(1)] public int poolSize = 24;

    [Header("Inner Area (centered on player, world units)")]
    public float halfWidth = 220f;
    public float halfHeight = 120f;

    [Header("Outer Padding (wrap band)")]
    public float padX = 60f;
    public float padY = 60f;

    [Header("Spawn/Wrapping Feel")]
    [Tooltip("How far OUTSIDE inner area to place on initial spawn.")]
    public float initialSpawnInset = 12f;
    [Tooltip("When wrapping, place just INSIDE the opposite inner edge.")]
    public float inset = 6f;
    [Tooltip("Random jitter along perpendicular axis when wrapping.")]
    public float jitter = 10f;

    [Header("Motion")]
    public bool randomizeOnSpawn = true;
    public bool randomizeOnWrap = false;
    public float inwardSpeedBias = 0.35f;            // bias initial drift toward center
    public Vector2 driftSpeedRange = new Vector2(0.25f, 0.9f);
    public Vector2 angularVelRange = new Vector2(-30f, 30f);

    // --- internals ---
    struct GemEntry
    {
        public Transform t;
        public Rigidbody2D rb;
        public int rarityIdx;
        public CurrencyGem gem;
    }
    private readonly List<GemEntry> pool = new();
    private MiniMapManager _minimap;

    void Awake()
    {
        if (!player) player = GameObject.FindWithTag("Player")?.transform;
        _minimap = minimap ? minimap : FindObjectOfType<MiniMapManager>();
        NormalizeWeights();
    }

    void Start()
    {
        _minimap = minimap ? minimap : FindObjectOfType<MiniMapManager>();
        Vector2 c = player ? (Vector2)player.position : Vector2.zero;

        for (int i = 0; i < poolSize; i++)
        {
            int rIdx = PickWeightedIndex();
            var prefab = gemPrefabs[Mathf.Clamp(rIdx, 0, gemPrefabs.Length - 1)];
            var go = Instantiate(prefab);
            var t = go.transform;

            // >>> spawn INSIDE the inner area (fully visible around player) <<<
            t.position = RandomPointInsideInner(c);

            var entry = new GemEntry
            {
                t = t,
                rb = go.GetComponent<Rigidbody2D>(),
                rarityIdx = rIdx,
                gem = go.GetComponent<CurrencyGem>()
            };

            if (entry.gem)
            {
                entry.gem.SetManager(this);
                entry.gem.SetPlayer(player);
            }

            if (_minimap) _minimap.RegisterGem(t, rIdx);

            // random drift, no inward bias since they’re already inside
            if (randomizeOnSpawn) ApplyRandomMotion(entry, fromPos: t.position, center: c, biasInward: false);

            pool.Add(entry);
        }
    }

    void Update()
    {
        if (!player) return;

        Vector2 c = player.position;

        // Inner box
        float ixMin = c.x - halfWidth, ixMax = c.x + halfWidth;
        float iyMin = c.y - halfHeight, iyMax = c.y + halfHeight;

        // Outer (wrap) box
        float oxMin = ixMin - padX, oxMax = ixMax + padX;
        float oyMin = iyMin - padY, oyMax = iyMax + padY;

        for (int i = 0; i < pool.Count; i++)
        {
            var e = pool[i];
            if (!e.t) continue;

            Vector3 p = e.t.position;
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
                e.t.position = p;
                if (randomizeOnWrap) ApplyRandomMotion(e, fromPos: p, center: c, biasInward: false);
            }
        }
    }

    // ---- public API from CurrencyGem ----
    public void Recycle(CurrencyGem gem)
    {
        if (!gem) return;
        // find entry (linear scan is fine for small pool sizes)
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].gem == gem)
            {
                var e = pool[i];
                Vector2 c = player ? (Vector2)player.position : Vector2.zero;

                // reposition outside, reset physics & state
                Vector2 pos = RandomPointJustOutsideInner(c);
                e.t.position = pos;

                if (e.rb) e.rb.simulated = true;
                if (randomizeOnSpawn) ApplyRandomMotion(e, fromPos: pos, center: c, biasInward: true);

                gem.ResetForReuse(); // re-enable collider/scale/flags
                return;
            }
        }
    }

    // ---- helpers ----
    Vector2 RandomPointJustOutsideInner(Vector2 center)
    {
        float ixMin = center.x - halfWidth, ixMax = center.x + halfWidth;
        float iyMin = center.y - halfHeight, iyMax = center.y + halfHeight;

        int side = Random.Range(0, 4); // 0=L,1=R,2=B,3=T
        switch (side)
        {
            case 0:
                return new Vector2(ixMin - (initialSpawnInset + padX * Random.value),
                                       Random.Range(iyMin - padY, iyMax + padY));
            case 1:
                return new Vector2(ixMax + (initialSpawnInset + padX * Random.value),
                                       Random.Range(iyMin - padY, iyMax + padY));
            case 2:
                return new Vector2(Random.Range(ixMin - padX, ixMax + padX),
                                       iyMin - (initialSpawnInset + padY * Random.value));
            default:
                return new Vector2(Random.Range(ixMin - padX, ixMax + padX),
                                       iyMax + (initialSpawnInset + padY * Random.value));
        }
    }

    void ApplyRandomMotion(GemEntry e, Vector2 fromPos, Vector2 center, bool biasInward)
    {
        if (!e.rb) return;

        Vector2 dirIn = (center - fromPos).sqrMagnitude > 0.001f
            ? (center - fromPos).normalized
            : Random.insideUnitCircle.normalized;

        Vector2 rand = Random.insideUnitCircle.normalized;
        Vector2 dir = biasInward
            ? Vector2.Lerp(rand, dirIn, Mathf.Clamp01(inwardSpeedBias)).normalized
            : rand;

        e.rb.linearVelocity = dir * Random.Range(driftSpeedRange.x, driftSpeedRange.y);
        e.rb.angularVelocity = Random.Range(angularVelRange.x, angularVelRange.y);
    }

    void NormalizeWeights()
    {
        if (rarityWeights == null || rarityWeights.Length == 0) return;
        float sum = 0f;
        for (int i = 0; i < rarityWeights.Length; i++) sum += Mathf.Max(0f, rarityWeights[i]);
        if (sum <= 0f) return;
        for (int i = 0; i < rarityWeights.Length; i++) rarityWeights[i] = Mathf.Max(0f, rarityWeights[i]) / sum;
    }

    int PickWeightedIndex()
    {
        if (rarityWeights == null || rarityWeights.Length == 0) return 0;
        float r = Random.value;
        float cum = 0f;
        for (int i = 0; i < rarityWeights.Length; i++)
        {
            cum += rarityWeights[i];
            if (r <= cum) return i;
        }
        return rarityWeights.Length - 1;
    }
    Vector2 RandomPointInsideInner(Vector2 center)
    {
        float ixMin = center.x - halfWidth, ixMax = center.x + halfWidth;
        float iyMin = center.y - halfHeight, iyMax = center.y + halfHeight;
        return new Vector2(Random.Range(ixMin, ixMax), Random.Range(iyMin, iyMax));
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!player) return;
        Vector3 c = player.position;

        // inner
        Gizmos.color = new Color(0f, 1f, 0f, 0.25f);
        DrawRect(c, halfWidth, halfHeight);

        // outer
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        DrawRect(c, halfWidth + padX, halfHeight + padY);
    }
    void DrawRect(Vector3 c, float hw, float hh)
    {
        Vector3 a = new(c.x - hw, c.y - hh, 0);
        Vector3 b = new(c.x + hw, c.y - hh, 0);
        Vector3 d = new(c.x - hw, c.y + hh, 0);
        Vector3 e = new(c.x + hw, c.y + hh, 0);
        Gizmos.DrawLine(a, b); Gizmos.DrawLine(b, e);
        Gizmos.DrawLine(e, d); Gizmos.DrawLine(d, a);
    }
#endif
}
