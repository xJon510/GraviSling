using System.Collections.Generic;
using UnityEngine;

public class GemManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform blackHole;
    public LevelManager planetManager;

    [Header("Gem Prefabs (in rarity order 0=common..3=rare)")]
    public GameObject[] gemPrefabs;

    [Header("Spawn Logic")]
    public int desiredGemsAhead = 5;
    public float spawnRangeMinX = 200f;
    public float spawnRangeMaxX = 600f;
    public float minY = -550f;
    public float maxY = 550f;
    public float minSpacingToPlanets = 50f;

    [Header("Cleanup")]
    public float cleanupBuffer = 50f;

    // 80%, 15%, 4%, 1% for Yellow, Green, Blue, Red
    private readonly float[] rarityWeights = { 0.80f, 0.15f, 0.04f, 0.01f };

    private List<GameObject> spawnedGems = new List<GameObject>();

    private void Update()
    {
        TrySpawnAhead();
        CleanupBehind();
    }

    void TrySpawnAhead()
    {
        int ahead = 0;
        foreach (var g in spawnedGems)
        {
            if (g != null && g.transform.position.x > player.position.x)
                ahead++;
        }

        while (ahead < desiredGemsAhead)
        {
            if (TrySpawnGem())
                ahead++;
            else
                break;
        }
    }

    bool TrySpawnGem()
    {
        for (int tries = 0; tries < 10; tries++)
        {
            float spawnX = player.position.x + Random.Range(spawnRangeMinX, spawnRangeMaxX);
            float spawnY = Random.Range(minY, maxY);
            Vector2 pos = new Vector2(spawnX, spawnY);

            foreach (var p in planetManager.spawnedPlanets)
            {
                if (p == null) continue;
                if (Vector2.Distance(p.transform.position, pos) < minSpacingToPlanets)
                    goto TryAgain;
            }

            // Weighted selection
            float roll = Random.value;
            float cumulative = 0f;
            int index = 0;
            for (int i = 0; i < rarityWeights.Length; i++)
            {
                cumulative += rarityWeights[i];
                if (roll <= cumulative)
                {
                    index = i;
                    break;
                }
            }

            var prefab = gemPrefabs[Mathf.Clamp(index, 0, gemPrefabs.Length - 1)];
            var g = Instantiate(prefab, pos, Quaternion.identity);
            spawnedGems.Add(g);

            MiniMapManager minimap = FindObjectOfType<MiniMapManager>();
            if (minimap != null)
            {
                minimap.RegisterGem(g.transform, index);
            }

            var rb = g.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Random.insideUnitCircle.normalized * Random.Range(0.2f, 1f);
                rb.angularVelocity = Random.Range(-30f, 30f);
            }
            return true;

        TryAgain:;
        }
        return false;
    }

    void CleanupBehind()
    {
        float hx = blackHole.position.x;
        for (int i = spawnedGems.Count - 1; i >= 0; i--)
        {
            if (spawnedGems[i] == null)
            {
                spawnedGems.RemoveAt(i);
                continue;
            }

            if (spawnedGems[i].transform.position.x < hx - cleanupBuffer)
            {
                Destroy(spawnedGems[i]);
                spawnedGems.RemoveAt(i);
            }
        }
    }
}
