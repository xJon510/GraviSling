using UnityEngine;

public class CowSway : MonoBehaviour
{
    [Header("Swing Settings")]
    [Tooltip("Maximum swing angle in degrees (e.g. 8 -> swings -8° to +8°).")]
    public float swingAngle = 8f;

    [Tooltip("Swing cycles per second.")]
    public float swingFrequency = 0.5f;

    [Tooltip("Randomize the starting phase so multiple cows aren’t synced.")]
    public bool randomizePhase = true;

    private float _phaseOffset;

    void OnEnable()
    {
        _phaseOffset = randomizePhase ? Random.Range(0f, Mathf.PI * 2f) : 0f;
    }

    void Update()
    {
        float t = Time.time * swingFrequency * Mathf.PI * 2f + _phaseOffset;
        float angle = Mathf.Sin(t) * swingAngle;

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
