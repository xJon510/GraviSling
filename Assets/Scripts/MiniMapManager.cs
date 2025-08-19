using System.Collections.Generic;
using UnityEngine;

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

    private class Entry
    {
        public Transform worldTarget;
        public RectTransform iconRect;
        public Vector2 offset;
    }

    private readonly List<Entry> tracked = new();

    void Update()
    {
        foreach (var e in tracked.ToArray())
        {
            if (e.worldTarget == null)
            {
                // world object destroyed
                Destroy(e.iconRect.gameObject);
                tracked.Remove(e);
                continue;
            }

            // convert world position to minimap coords
            Vector3 offset = e.worldTarget.position - player.position;

            float mapX = Mathf.Clamp(offset.x / worldRangeX, -1.1f, 1.1f);  // normalized [-1..1]
            float mapY = Mathf.Clamp(offset.y / worldRangeY, -1.1f, 1.1f);

            // convert to rect anchoredPosition
            float halfW = minimapRect.rect.width * 0.5f;
            float halfH = minimapRect.rect.height * 0.5f;
            Vector2 anchored = new Vector2(mapX * halfW, mapY * halfH) + e.offset;
            e.iconRect.anchoredPosition = anchored;
        }
    }

    // PUBLIC API for your spawners:
    public void RegisterPlanet(Transform worldTarget, int iconIndex)
    {
        // safety clamp – if you give me 10 planets but only supply 3 icons I won’t explode
        iconIndex = Mathf.Clamp(iconIndex, 0, planetIconPrefabs.Length - 1);

        var icon = Instantiate(planetIconPrefabs[iconIndex], minimapRect);
        tracked.Add(new Entry
        {
            worldTarget = worldTarget,
            iconRect = icon.GetComponent<RectTransform>()
        });
    }

    public void RegisterAsteroid(Transform worldTarget)
    {
        var icon = Instantiate(asteroidIconPrefab, minimapRect);
        tracked.Add(new Entry
        {
            worldTarget = worldTarget,
            iconRect = icon.GetComponent<RectTransform>()
        });
    }

    public void RegisterBlackHole(Transform worldTarget, Vector2 offset)
    {
        var icon = Instantiate(blackHoleIconPrefab, minimapRect);
        tracked.Add(new Entry
        {
            worldTarget = worldTarget,
            iconRect = icon.GetComponent<RectTransform>(),
            offset = offset
        });
    }
}
