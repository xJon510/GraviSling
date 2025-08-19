using System.Collections.Generic;
using UnityEngine;

public class DustManager : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;             // reference to player/camera
    public Transform blackHole;          // cleanup reference (optional)

    [Header("Dust Prefabs")]
    public GameObject[] dustPrefabs;     // your tinted cloud / dust assets

    [Header("Spawn Settings")]
    public int desiredDustAhead = 30;
    public float spawnMinX = 200f;
    public float spawnMaxX = 800f;
    public float minY = -400f;
    public float maxY = 400f;
    public float minSpacing = 30f;

    [Header("Cleanup")]
    public float cleanupBuffer = 100f;

    private readonly List<GameObject> spawnedDust = new List<GameObject>();

    private void Update()
    {
        TrySpawnAhead();
        CleanupBehind();
    }

    void TrySpawnAhead()
    {
        int countAhead = 0;
        foreach (var d in spawnedDust)
        {
            if (d != null && d.transform.position.x > player.position.x)
                countAhead++;
        }

        while (countAhead < desiredDustAhead)
        {
            if (TrySpawnOneAhead())
                countAhead++;
            else
                break;
        }
    }

    bool TrySpawnOneAhead()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector2 spawnPos = new Vector2(
                player.position.x + Random.Range(spawnMinX, spawnMaxX),
                Random.Range(minY, maxY)
            );

            // avoid overlapping other dust too closely
            bool ok = true;
            foreach (var d in spawnedDust)
            {
                if (d == null) continue;
                if (Vector2.Distance(d.transform.position, spawnPos) < minSpacing)
                {
                    ok = false;
                    break;
                }
            }
            if (!ok) continue;

            var prefab = dustPrefabs[Random.Range(0, dustPrefabs.Length)];
            var dust = Instantiate(prefab, spawnPos, Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)), this.transform);
            spawnedDust.Add(dust);
            return true;
        }
        return false;
    }

    void CleanupBehind()
    {
        float xRef = blackHole ? blackHole.position.x : player.position.x;

        for (int i = spawnedDust.Count - 1; i >= 0; i--)
        {
            if (spawnedDust[i] == null)
            {
                spawnedDust.RemoveAt(i);
                continue;
            }
            if (spawnedDust[i].transform.position.x < xRef - cleanupBuffer)
            {
                Destroy(spawnedDust[i]);
                spawnedDust.RemoveAt(i);
            }
        }
    }
}
