using UnityEngine;
using TMPro;

public class BlackholeUIUpdater : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text speedText;

    [Header("Display Tweaks")]
    [SerializeField] private string distanceSuffix = "m";
    [SerializeField] private string speedSuffix = "m/s";

    public BlackHoleDrift drift;   // your movement script

    private void Update()
    {
        UpdateDistanceUI();
        UpdateSpeedUI();
    }

    private void UpdateDistanceUI()
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.position);
        distanceText.text = $"{dist:F0}{distanceSuffix}";
    }

    private void UpdateSpeedUI()
    {
        if (drift == null) return;
        float speed = drift.currentSpeed;  // pull from your script
        speedText.text = $"{speed:F1}{speedSuffix}";
    }
}
