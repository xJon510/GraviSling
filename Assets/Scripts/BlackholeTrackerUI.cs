using UnityEngine;

public class BlackholeTrackerUI : MonoBehaviour
{
    [SerializeField] private Transform player;         // player world position
    [SerializeField] private Transform blackhole;      // blackhole world position

    [Header("UI Transform to move")]
    [SerializeField] private RectTransform trackerIcon;

    [Header("Clamp bounds (local anchored Y)")]
    [SerializeField] private float minY = -200f;        // how low the tracker can move
    [SerializeField] private float maxY = 200f;         // how high the tracker can move

    [Header("Tuning")]
    [SerializeField] private float followStrength = 1f; // how tightly it follows (0-1 for smoothing)

    private float t; // lerped position

    private void Update()
    {
        // world-space delta
        float heightDifference = blackhole.position.y - player.position.y;

        // normalize to something small (-1 to +1 ish)
        float normalized = Mathf.Clamp(heightDifference * 0.01f, -1f, 1f);

        // calculate target UI Y-position
        float targetY = Mathf.Lerp(minY, maxY, (normalized + 1f) * 0.5f);

        // optionally smooth the movement
        t = Mathf.Lerp(t, targetY, followStrength * Time.deltaTime);

        Vector2 anchored = trackerIcon.anchoredPosition;
        anchored.y = t;
        trackerIcon.anchoredPosition = anchored;
    }
}
