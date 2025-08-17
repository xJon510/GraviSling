using System.Collections.Generic;
using UnityEngine;

public class AsteroidManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform blackHole;
    public LevelManager planetManager;    // so we can check planet positions

    [Header("Asteroid Prefabs")]
    public GameObject[] asteroidPrefabs;

    [Header("Spawn Logic")]
    public int desiredAsteroidsAhead = 10;
    public float spawnRangeMinX = 200f;
    public float spawnRangeMaxX = 700f;
    public float minY = -600f;
    public float maxY = 600f;
    public float minSpacingToPlanets = 75f;       // <� your "inner orbit" reject range

    [Header("Cleanup")]
    public float cleanupBuffer = 50f;

    List<GameObject> spawnedAsteroids = new List<GameObject>();

    private void Update()
    {
        TrySpawnAhead();
        CleanupBehind();
    }

    void TrySpawnAhead()
    {
        int ahead = 0;
        foreach (var a in spawnedAsteroids)
        {
            if (a != null && a.transform.position.x > player.position.x)
                ahead++;
        }

        while (ahead < desiredAsteroidsAhead)
        {
            if (TrySpawnAsteroid())
                ahead++;
            else
                break;
        }
    }

    bool TrySpawnAsteroid()
    {
        for (int tries = 0; tries < 10; tries++)
        {
            float spawnX = player.position.x + Random.Range(spawnRangeMinX, spawnRangeMaxX);
            float spawnY = Random.Range(minY, maxY);
            Vector2 pos = new Vector2(spawnX, spawnY);

            // reject if near a planet
            foreach (var p in planetManager.spawnedPlanets)
            {
                if (p == null) continue;
                if (Vector2.Distance(p.transform.position, pos) < minSpacingToPlanets)
                    goto TryAgain;
            }

            // ok spawn
            var prefab = asteroidPrefabs[Random.Range(0, asteroidPrefabs.Length)];
            var a = Instantiate(prefab, pos, Quaternion.identity);
            spawnedAsteroids.Add(a);

            MiniMapManager minimap = FindObjectOfType<MiniMapManager>();
            if (minimap != null)
                minimap.RegisterAsteroid(a.transform);

            // give it random drift
            var rb = a.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Random.insideUnitCircle.normalized * Random.Range(0.5f, 2f);
                rb.angularVelocity = Random.Range(-50f, 50f);
            }
            return true;

        TryAgain:;
        }
        return false;
    }

    void CleanupBehind()
    {
        float hx = blackHole.position.x;

        for (int i = spawnedAsteroids.Count - 1; i >= 0; i--)
        {
            if (spawnedAsteroids[i] == null)
            {
                spawnedAsteroids.RemoveAt(i);
                continue;
            }

            if (spawnedAsteroids[i].transform.position.x < hx - cleanupBuffer)
            {
                Destroy(spawnedAsteroids[i]);
                spawnedAsteroids.RemoveAt(i);
            }
        }
    }
}

