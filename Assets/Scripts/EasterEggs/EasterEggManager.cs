using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class EasterEggManager : MonoBehaviour
{
    [Serializable]
    public class EggDef
    {
        public string id;                     // "lost_satellite"
        public GameObject prefab;
        [Range(0.01f, 5f)] public float weight = 1f;   // common/uncommon via weights
        public float minCooldown = 8f;        // seconds between spawns considered
        public int poolSize = 2;              // small pools
        public float lifespan = 16f;          // seconds visible before forced despawn
        [Header("Spawn")]
        public float spawnOffsetX = 0.6f;     // screens to the right
        public float biasYPortion = 0.4f;     // ±0.4 * screenHeight around player
        [Range(0f, 1f)] public float ignoreBiasChance = 0.15f;
        [Header("Visual")]
        [Range(0.02f, 0.12f)] public float screenHeightFraction = 0.06f; // ~6%
    }

    public Camera cam;
    public Transform player;
    public List<EggDef> eggs = new();
    public Transform eggLayerParent;                  // drag your EasterEggLayer here
    public Transform backgroundLayerParent;
    public string backgroundLayerId = "Shooting_Star";
    public string autoFindLayerName = "EasterEggLayer";
    public bool inheritLayerZ = true;
    public bool overrideSortingLayer = false;         // leave false if parent already handles sorting
    public string sortingLayerName = "BG_Eggs";       // used only if overrideSortingLayer = true
    public int sortingOrder = 0;
    public int maxConcurrent = 1;

    [Header("Spawn Chance")]
    [Range(0f, 1f)]
    public float spawnChance = 0.5f;

    [Header("Black Hole Cleanup")]
    public bool useBlackHoleCleanup = true;
    public Transform blackHole;        // assign in inspector
    public float cleanupBuffer = 10f;  // world units behind the hole to delete

    float _cooldownTimer;
    readonly Dictionary<string, Queue<GameObject>> _pools = new();
    readonly HashSet<GameObject> _active = new();
    readonly Dictionary<GameObject, string> _activeIds = new();

    void Awake()
    {
        if (eggLayerParent == null)
        {
            var found = GameObject.Find(autoFindLayerName);
            if (found != null) eggLayerParent = found.transform;
        }

        foreach (var def in eggs)
        {
            var q = new Queue<GameObject>(def.poolSize);

            Transform parent = (def.id == backgroundLayerId && backgroundLayerParent != null) ? backgroundLayerParent : (eggLayerParent != null ? eggLayerParent : transform);

            for (int i = 0; i < def.poolSize; i++)
            {
                var go = Instantiate(def.prefab, parent);
                go.SetActive(false);

                if (overrideSortingLayer)
                    SetSorting(go, sortingLayerName, sortingOrder);

                q.Enqueue(go);
            }
            _pools[def.id] = q;
        }
    }

    // Call this from your background-panel “shifted” event, OR poll in Update:
    public void OnBackgroundPanelShifted()
    {
        if (Random.value <= spawnChance)
        {
            TrySpawn();
        }
    }

    void Update()
    {
        _cooldownTimer -= Time.deltaTime;
        // If you don’t have a panel-shift callback yet, uncomment:
        // if (_cooldownTimer <= 0f) TrySpawn();
        CleanupBehindBlackHole();
    }

    void TrySpawn()
    {
        if (_cooldownTimer > 0f) return;
        if (_active.Count >= maxConcurrent) return;

        // Weighted pick
        EggDef pick = WeightedPick();
        if (pick == null) return;

        // Respect per-egg cooldown
        _cooldownTimer = pick.minCooldown;

        // Pull from pool
        if (!_pools[pick.id].TryDequeue(out var go) || go == null)
            return;

        // Position
        var halfH = cam.orthographicSize;
        var halfW = halfH * cam.aspect;

        float spawnX = cam.transform.position.x + halfW + (pick.spawnOffsetX * 2f * halfW);
        float yCenter = player != null ? player.position.y : cam.transform.position.y;
        float yRange = pick.biasYPortion * (2f * halfH);

        bool ignoreBias = Random.value < pick.ignoreBiasChance;
        float spawnY = ignoreBias
            ? Random.Range(cam.transform.position.y - halfH, cam.transform.position.y + halfH)
            : Mathf.Clamp(yCenter + Random.Range(-yRange, yRange), cam.transform.position.y - halfH, cam.transform.position.y + halfH);

        go.transform.position = new Vector3(spawnX, spawnY, 0f);

        // Scale to % of screen height
        ScaleToScreenHeight(go.transform, pick.screenHeightFraction);

        // ensure correct parent (in case the pool was created before you set eggLayerParent)
        if (go.transform.parent != (eggLayerParent != null ? eggLayerParent : transform))
            go.transform.SetParent(eggLayerParent != null ? eggLayerParent : transform, true);

        // position (sync Z with layer so parallax/renderer order is correct)
        float z = inheritLayerZ && eggLayerParent != null ? eggLayerParent.position.z : 0f;
        go.transform.position = new Vector3(spawnX, spawnY, z);

        // Lifetime & recycle
        var egg = go.GetComponent<IEasterEgg>();
        if (egg != null) egg.Activate(this, pick.lifespan, cam);

        go.SetActive(true);
        _active.Add(go);
        _activeIds[go] = pick.id;
    }

    public void Recycle(GameObject go)
    {
        if (go == null) return;
        if (_activeIds.TryGetValue(go, out var id))
        {
            Recycle(id, go);
            _activeIds.Remove(go);
        }
        else
        {
            // Fallback if somehow unknown
            go.SetActive(false);
            _active.Remove(go);
        }
    }

    public void Recycle(string id, GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        _active.Remove(go);
        _activeIds.Remove(go);
        if (!_pools.TryGetValue(id, out var q)) return;
        q.Enqueue(go);
    }

    EggDef WeightedPick()
    {
        float total = 0f;
        foreach (var e in eggs) total += e.weight;
        if (total <= 0f) return null;
        float r = Random.value * total, acc = 0f;
        foreach (var e in eggs)
        {
            acc += e.weight;
            if (r <= acc) return e;
        }
        return eggs[^1];
    }

    static void SetSorting(GameObject go, string layer, int order)
    {
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingLayerName = layer;
            sr.sortingOrder = order;
        }
    }

    static void ScaleToScreenHeight(Transform t, float fraction)
    {
        // Assumes the root has a SpriteRenderer determining size; adjusts localScale
        var sr = t.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        var cam = Camera.main;
        float worldH = cam.orthographicSize * 2f;
        float targetH = worldH * Mathf.Clamp01(fraction);
        float spriteH = sr.sprite.bounds.size.y;
        float scale = targetH / Mathf.Max(0.0001f, spriteH);
        t.localScale = Vector3.one * scale;
    }

    void CleanupBehindBlackHole()
    {
        if (!useBlackHoleCleanup || blackHole == null) return;

        float holeX = blackHole.position.x;
        // copy to avoid modifying while iterating
        _toClean ??= new List<GameObject>(8);
        _toClean.Clear();

        foreach (var go in _active)
        {
            if (go == null) continue;
            if (go.transform.position.x < holeX - cleanupBuffer)
                _toClean.Add(go);
        }

        foreach (var go in _toClean)
        {
            Recycle(go);
        }
    }
    List<GameObject> _toClean;
}

public interface IEasterEgg
{
    void Activate(EasterEggManager mgr, float lifespan, Camera cam);
}
