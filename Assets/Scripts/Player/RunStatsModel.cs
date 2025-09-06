using UnityEngine;

public class RunStatsModel : MonoBehaviour
{
    public static RunStatsModel I { get; private set; }

    [Header("Live")]
    public float currentDistance;     // km
    public float currentSpeed;        // km/s
    public float topSpeedThisRun;     // km/s
    public float furthestXThisRun;    // km

    [Header("All-time (persisted)")]
    public float bestDistanceAllTime; // km
    public float bestSpeedAllTime;    // km/s

    const string KeyBestDist = "BestDistanceKM";
    const string KeyBestSpeed = "BestSpeedKMS";

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;

        bestDistanceAllTime = PlayerPrefs.GetFloat(KeyBestDist, 0f);
        bestSpeedAllTime = PlayerPrefs.GetFloat(KeyBestSpeed, 0f);
    }

    public void ResetRun(float startX)
    {
        currentDistance = 0f;
        furthestXThisRun = startX;
        currentSpeed = 0f;
        topSpeedThisRun = 0f;
    }

    public void UpdateDistanceByX(float currentX)
    {
        if (currentX > furthestXThisRun)
        {
            furthestXThisRun = currentX;
            currentDistance = furthestXThisRun; // if 1 unit == 1 km in your world
        }
    }

    public void UpdateSpeed(float speed)
    {
        currentSpeed = speed;
        if (speed > topSpeedThisRun) topSpeedThisRun = speed;
    }

    public void EndRunAndPersist()
    {
        // Update all-time records if beaten
        if (furthestXThisRun > bestDistanceAllTime)
        {
            bestDistanceAllTime = furthestXThisRun;
            PlayerPrefs.SetFloat(KeyBestDist, bestDistanceAllTime);
        }
        if (topSpeedThisRun > bestSpeedAllTime)
        {
            bestSpeedAllTime = topSpeedThisRun;
            PlayerPrefs.SetFloat(KeyBestSpeed, bestSpeedAllTime);
        }
        PlayerPrefs.Save();
    }
}
