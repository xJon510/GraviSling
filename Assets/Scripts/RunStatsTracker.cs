using UnityEngine;
using TMPro;

public class RunStatsTracker : MonoBehaviour
{
    public static RunStatsTracker Instance { get; private set; }

    [Header("TMP References")]
    [SerializeField] private TMP_Text distanceTMP;
    [SerializeField] private TMP_Text speedTMP;

    [Header("Live Values")]
    public float currentDistance = 0f;
    public float currentTopSpeed = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (distanceTMP != null)
        {
            // Strip everything except digits and dots
            string cleaned = System.Text.RegularExpressions.Regex.Replace(distanceTMP.text, @"[^0-9\.]", "");
            float.TryParse(cleaned, out currentDistance);
        }

        if (speedTMP != null)
        {
            // Expects like "Speed: 42.3 km/s"
            string cleaned = System.Text.RegularExpressions.Regex.Replace(speedTMP.text, @"[^0-9\.]", "");
            float.TryParse(cleaned, out float s);
            if (s > currentTopSpeed) currentTopSpeed = s;
        }
    }
}
