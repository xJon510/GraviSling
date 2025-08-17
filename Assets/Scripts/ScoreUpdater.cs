using UnityEngine;
using TMPro;

public class ScoreUpdater : MonoBehaviour
{
    public Transform player;
    public TMP_Text scoreText;

    private float furthestX = 0f;

    // 1 AU 150,000,000 km
    private const float AU = 150_000f;

    void Update()
    {
        if (player == null || scoreText == null) return;

        float currentX = player.position.x;

        if (currentX > furthestX)
        {
            furthestX = currentX;
            scoreText.text = FormatDistance(furthestX);
        }
    }

    string FormatDistance(float d)
    {
        // d is in km (your label is km)
        if (d < 100f)
        {
            return $"{d:F1} km";           // 57.3 km
        }
        else if (d < 15000f)
        {
            return $"{d:0} km";            // 6,432 km
        }
        else
        {
            float auVal = d / AU;
            return $"{auVal:F2} AU";       // 0.01 AU
        }
    }
}
