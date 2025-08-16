using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;        // reference to player transform
    public Transform blackHole;     // reference to black hole transform

    [Header("Planet Prefabs")]
    public GameObject[] planetPrefabs; // pool of prefabs to use

    [Header("Spawn Logic")]
    public int desiredPlanetsAhead = 5;
    public float spawnRangeMinX = 300f;
    public float spawnRangeMaxX = 600f;
    public float minY = -500f;
    public float maxY = 500f;
    public float minPlanetSpacing = 40f;

    [Header("Cleanup")]
    public float cleanupBuffer = 50f;   // how far behind black hole before deleting

    private readonly List<GameObject> spawnedPlanets = new List<GameObject>();

    private void Update()
    {
        TrySpawnAhead();
        CleanupBehind();
    }

    void TrySpawnAhead()
    {
        int countAhead = 0;
        foreach (var p in spawnedPlanets)
        {
            if (p != null && p.transform.position.x > player.position.x)
                countAhead++;
        }

        while (countAhead < desiredPlanetsAhead)
        {
            if (TrySpawnPlanetAhead())
                countAhead++;
            else
                break;
        }
    }

    bool TrySpawnPlanetAhead()
    {
        for (int attempts = 0; attempts < 10; attempts++)
        {
            float spawnX = player.position.x + Random.Range(spawnRangeMinX, spawnRangeMaxX);
            float spawnY = Random.Range(minY, maxY);
            Vector2 spawnPos = new Vector2(spawnX, spawnY);

            // Make sure this spawn location is not too close to any existing planet
            bool ok = true;
            foreach (var p in spawnedPlanets)
            {
                if (p == null) continue;
                if (Vector2.Distance(p.transform.position, spawnPos) < minPlanetSpacing)
                {
                    ok = false;
                    break;
                }
            }

            if (!ok) continue;

            GameObject prefab = planetPrefabs[Random.Range(0, planetPrefabs.Length)];
            GameObject newPlanet = Instantiate(prefab, spawnPos, Quaternion.identity);
            spawnedPlanets.Add(newPlanet);
            return true;
        }
        return false;
    }

    void CleanupBehind()
    {
        float holeX = blackHole.position.x;

        for (int i = spawnedPlanets.Count - 1; i >= 0; i--)
        {
            if (spawnedPlanets[i] == null)
            {
                spawnedPlanets.RemoveAt(i);
                continue;
            }

            float px = spawnedPlanets[i].transform.position.x;
            if (px < holeX - cleanupBuffer)
            {
                Destroy(spawnedPlanets[i]);
                spawnedPlanets.RemoveAt(i);
            }
        }
    }
}
