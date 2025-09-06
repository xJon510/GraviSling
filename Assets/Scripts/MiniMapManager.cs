using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniMapManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;            // world-space player
    public RectTransform minimapRect;   // UI RectTransform
    public float worldRangeX = 600f;    // how far ±X around player to show on minimap
    public float worldRangeY = 600f;    // ±Y around player

    [Header("Prefabs")]
    public GameObject[] planetIconPrefabs; 
    public GameObject asteroidIconPrefab;
    public GameObject blackHoleIconPrefab;

    [Header("Gem Prefabs")]
    public GameObject blueGemIconPrefab;
    public GameObject redGemIconPrefab;

    [Header("Performance")]
    [Tooltip("If >0, spread icon updates across multiple frames (lower = smoother UI, higher = less lag). 0 = update all every frame.")]
    public int staggerFrameCount = 0;

    private class Entry
    {
        public Transform worldTarget;
        public RectTransform iconRect;
        public Vector2 offset;
        public GameObject prefabRef;
    }

    private readonly List<Entry> tracked = new();
    private readonly Dictionary<GameObject, Queue<GameObject>> pool = new();

    private int updateIndex = 0;

    void Update()
    {
        if (tracked.Count == 0) return;

        // update budget: all if staggerFrameCount==0, else a slice
        int updatesThisFrame = tracked.Count;
        if (staggerFrameCount > 0)
            updatesThisFrame = Mathf.CeilToInt(tracked.Count / (float)staggerFrameCount);

        for (int i = 0; i < updatesThisFrame; i++)
        {
            if (tracked.Count == 0) break;

            updateIndex %= tracked.Count; // wrap around
            var e = tracked[updateIndex];

            if (e.worldTarget == null || !e.worldTarget.gameObject.activeSelf)
            {
                // recycle icon
                RecycleEntry(e);
                tracked.RemoveAt(updateIndex);
                // don't advance updateIndex since we removed current
                continue;
            }

            UpdateEntryPosition(e);
            updateIndex++;
        }
    }

    private void UpdateEntryPosition(Entry e)
    {
        Vector3 offset = e.worldTarget.position - player.position;

        float mapX = Mathf.Clamp(offset.x / worldRangeX, -1.1f, 1.1f);
        float mapY = Mathf.Clamp(offset.y / worldRangeY, -1.1f, 1.1f);

        float halfW = minimapRect.rect.width * 0.5f;
        float halfH = minimapRect.rect.height * 0.5f;
        Vector2 anchored = new Vector2(mapX * halfW, mapY * halfH) + e.offset;

        e.iconRect.anchoredPosition = anchored;
    }

    private void RecycleEntry(Entry e)
    {
        if (!pool.ContainsKey(e.prefabRef))
            pool[e.prefabRef] = new Queue<GameObject>();

        e.iconRect.gameObject.SetActive(false);
        pool[e.prefabRef].Enqueue(e.iconRect.gameObject);
    }

    private GameObject GetFromPool(GameObject prefab)
    {
        if (pool.TryGetValue(prefab, out var q) && q.Count > 0)
        {
            var go = q.Dequeue();
            go.SetActive(true);
            return go;
        }
        return Instantiate(prefab, minimapRect);
    }

    // ---------------------- Public API ----------------------

    public void RegisterPlanet(Transform worldTarget, int iconIndex)
    {
        iconIndex = Mathf.Clamp(iconIndex, 0, planetIconPrefabs.Length - 1);
        var prefab = planetIconPrefabs[iconIndex];
        Register(worldTarget, prefab, Vector2.zero);
    }

    public void RegisterAsteroid(Transform worldTarget)
    {
        Register(worldTarget, asteroidIconPrefab, Vector2.zero);
    }

    public void RegisterBlackHole(Transform worldTarget, Vector2 offset)
    {
        Register(worldTarget, blackHoleIconPrefab, offset);
    }

    public void RegisterGem(Transform worldTarget, int rarityIndex)
    {
        GameObject prefab = null;
        if (rarityIndex == 2) prefab = blueGemIconPrefab;
        else if (rarityIndex == 3) prefab = redGemIconPrefab;
        if (prefab != null) Register(worldTarget, prefab, Vector2.zero);
    }

    private void Register(Transform worldTarget, GameObject prefab, Vector2 offset)
    {
        var icon = GetFromPool(prefab);
        var rect = icon.GetComponent<RectTransform>();

        tracked.Add(new Entry
        {
            worldTarget = worldTarget,
            iconRect = rect,
            offset = offset,
            prefabRef = prefab
        });

        // Make sure the icon has no layout components (ContentSizeFitter, LayoutGroup, etc.)
        // Just an Image/CanvasRenderer. This avoids layout rebuild cost.
    }
}
