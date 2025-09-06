using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pooled, camera-safe planet streamer.
/// - Spawns ahead of the player (never inside camera view, with margin)
/// - Recycles planets that fall behind the black hole
/// - Budgeted work per frame to avoid spikes
/// </summary>
public class PlanetManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform blackHole;
    public Camera mainCamera; // if null, uses Camera.main

    [Header("Planet Prefabs")]
    public GameObject[] planetPrefabs;

    [Header("Spawn Window")]
    [Tooltip("How many planets we want ahead of the player at all times.")]
    public int desiredPlanetsAhead = 5;

    [Tooltip("Min/Max forward distance from player where new planets may spawn (world X).")]
    public float spawnRangeMinX = 300f;
    public float spawnRangeMaxX = 600f;

    [Tooltip("Vertical band for spawns.")]
    public float minY = -500f;
    public float maxY = 500f;

    [Tooltip("Minimum distance to any other planet.")]
    public float minPlanetSpacing = 40f;

    [Header("Camera Safety")]
    [Tooltip("Extra padding so nothing appears inside/near the right edge of the view.")]
    public float cameraRightPadding = 24f;

    [Header("Cleanup")]
    [Tooltip("How far behind the black hole before we recycle a planet.")]
    public float cleanupBuffer = 50f;

    [Header("Pooling")]
    [Tooltip("Initial prewarm count per prefab.")]
    public int initialPoolPerPrefab = 3;

    [Tooltip("Max number of spawn/recycle ops processed per frame.")]
    public int opsBudgetPerFrame = 6;

    // ---------- internals ----------
    private readonly List<GameObject> _spawnedPlanets = new List<GameObject>(256);

    // pool per prefab index
    private readonly List<Queue<GameObject>> _pools = new List<Queue<GameObject>>(16);

    // scratch lists to avoid GC
    private readonly List<GameObject> _scratchToRecycle = new List<GameObject>(64);

    private MiniMapManager _minimap;
    private Camera Cam => mainCamera != null ? mainCamera : Camera.main;

    void Awake()
    {
        _minimap = FindObjectOfType<MiniMapManager>();

        // init pools
        _pools.Clear();
        for (int i = 0; i < planetPrefabs.Length; i++)
        {
            _pools.Add(new Queue<GameObject>(Mathf.Max(1, initialPoolPerPrefab)));
        }

        // optional light prewarm so first frame doesn’t stall
        PrewarmPools();
    }

    void Update()
    {
        int ops = 0;

        // Fill ahead (budgeted)
        ops += TrySpawnAheadBudgeted(opsBudgetPerFrame - ops);

        // Cleanup behind (budgeted)
        ops += CleanupBehindBudgeted(opsBudgetPerFrame - ops);
    }

    // ---------------------- Spawning ----------------------

    private int TrySpawnAheadBudgeted(int budget)
    {
        if (budget <= 0) return 0;

        // count planets strictly ahead of the player
        int countAhead = 0;
        var playerX = player.position.x;
        for (int i = 0; i < _spawnedPlanets.Count; i++)
        {
            var p = _spawnedPlanets[i];
            if (p != null && p.activeSelf && p.transform.position.x > playerX)
                countAhead++;
        }

        int ops = 0;
        while (countAhead < desiredPlanetsAhead && ops < budget)
        {
            if (TrySpawnPlanetAhead())
            {
                countAhead++;
                ops++;
            }
            else
            {
                // couldn't find a valid spot this frame—bail and try next frame
                break;
            }
        }
        return ops;
    }

    private bool TrySpawnPlanetAhead()
    {
        // Compute a safe right-edge so we never spawn inside the view.
        float safeRightEdge = GetCameraRightEdgeWorldX() + cameraRightPadding;

        for (int attempts = 0; attempts < 12; attempts++)
        {
            float spawnX = player.position.x + Random.Range(spawnRangeMinX, spawnRangeMaxX);
            if (spawnX <= safeRightEdge) spawnX = safeRightEdge + Random.Range(8f, 32f);

            float spawnY = Random.Range(minY, maxY);
            Vector2 spawnPos = new Vector2(spawnX, spawnY);

            // spacing check
            bool ok = true;
            for (int i = 0; i < _spawnedPlanets.Count; i++)
            {
                var p = _spawnedPlanets[i];
                if (p == null || !p.activeSelf) continue;
                if (Vector2.Distance(p.transform.position, spawnPos) < minPlanetSpacing)
                {
                    ok = false;
                    break;
                }
            }
            if (!ok) continue;

            int prefabIndex = Random.Range(0, planetPrefabs.Length);
            var go = GetFromPool(prefabIndex);
            go.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            go.SetActive(true);

            // (Optional) If you have a component that needs reset per reuse, do it here.

            _spawnedPlanets.Add(go);

            if (_minimap != null)
                _minimap.RegisterPlanet(go.transform, prefabIndex);

            return true;
        }
        return false;
    }

    private float GetCameraRightEdgeWorldX()
    {
        var cam = Cam;
        if (cam == null || !cam.orthographic) return player.position.x; // fallback

        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;
        return cam.transform.position.x + halfWidth;
    }

    // ---------------------- Cleanup / Recycle ----------------------

    private int CleanupBehindBudgeted(int budget)
    {
        if (budget <= 0) return 0;

        float holeX = blackHole.position.x;
        int ops = 0;

        // Build a small list of items to recycle (don’t mutate while iterating)
        _scratchToRecycle.Clear();
        for (int i = 0; i < _spawnedPlanets.Count && ops < budget; i++)
        {
            var go = _spawnedPlanets[i];
            if (go == null || !go.activeSelf) continue;

            if (go.transform.position.x < holeX - cleanupBuffer)
            {
                _scratchToRecycle.Add(go);
                ops++;
            }
        }

        // Recycle them
        for (int i = 0; i < _scratchToRecycle.Count; i++)
        {
            var go = _scratchToRecycle[i];
            ReturnToPool(go);
            _spawnedPlanets.Remove(go);
        }

        return ops;
    }

    // ---------------------- Pooling ----------------------

    private class PooledTag : MonoBehaviour
    {
        public int prefabIndex;
    }

    private void PrewarmPools()
    {
        for (int i = 0; i < planetPrefabs.Length; i++)
        {
            var pool = _pools[i];
            while (pool.Count < initialPoolPerPrefab)
            {
                var go = CreateInstance(i);
                go.SetActive(false);
                pool.Enqueue(go);
            }
        }
    }

    private GameObject GetFromPool(int prefabIndex)
    {
        var pool = _pools[prefabIndex];
        if (pool.Count > 0)
        {
            var go = pool.Dequeue();
            return go;
        }
        return CreateInstance(prefabIndex);
    }

    private void ReturnToPool(GameObject go)
    {
        if (go == null) return;
        var tag = go.GetComponent<PooledTag>();
        if (tag == null) { go.SetActive(false); return; }

        go.SetActive(false);
        _pools[tag.prefabIndex].Enqueue(go);
    }

    private GameObject CreateInstance(int prefabIndex)
    {
        var prefab = planetPrefabs[prefabIndex];
        var go = Instantiate(prefab);
        var tag = go.GetComponent<PooledTag>();
        if (tag == null) tag = go.AddComponent<PooledTag>();
        tag.prefabIndex = prefabIndex;
        return go;
    }

    // ---------------------- Gizmos ----------------------

    private void OnDrawGizmosSelected()
    {
        if (player == null) return;

        // spawn window box
        Gizmos.color = Color.yellow;
        float startX = player.position.x + spawnRangeMinX;
        float endX = player.position.x + spawnRangeMaxX;
        Vector3 bottomLeft = new Vector3(startX, minY, 0f);
        Vector3 topRight = new Vector3(endX, maxY, 0f);
        Vector3 center = (bottomLeft + topRight) * 0.5f;
        Vector3 size = new Vector3(endX - startX, maxY - minY, 0f);
        Gizmos.DrawWireCube(center, size);

        // camera right edge + padding
        if (Cam != null && Cam.orthographic)
        {
            Gizmos.color = Color.cyan;
            float right = GetCameraRightEdgeWorldX() + cameraRightPadding;
            Gizmos.DrawLine(new Vector3(right, minY, 0f), new Vector3(right, maxY, 0f));
        }
    }
}
