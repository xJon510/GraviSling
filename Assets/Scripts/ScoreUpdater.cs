using UnityEngine;
using TMPro;

public class ScoreUpdater : MonoBehaviour
{
    public Transform player;
    public TMP_Text scoreText;

    private float lastShown = float.MinValue;

    void Start()
    {
        if (player) RunStatsModel.I?.ResetRun(player.position.x);
    }

    void Update()
    {
        if (player == null || scoreText == null) return;

        RunStatsModel.I?.UpdateDistanceByX(player.position.x);

        float d = RunStatsModel.I.currentDistance;
        // Only push text if the displayed value would change
        float rounded = d < 100f ? Mathf.Round(d * 10f) * 0.1f : Mathf.Round(d);
        if (!Mathf.Approximately(rounded, lastShown))
        {
            scoreText.text = FormatDistance(rounded, d < 100f);
            lastShown = rounded;
        }
    }

    string FormatDistance(float shown, bool small)
    {
        if (small) return $"{shown:F1} km";
        return $"{shown:0} km";
    }
}
