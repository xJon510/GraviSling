using UnityEngine;

public class AstronautWave : MonoBehaviour
{
    [Header("Wave Settings")]
    [Tooltip("Maximum rotation from center in degrees (e.g. 45 -> swings -45° to +45°).")]
    public float waveArc = 45f;

    [Tooltip("Min/max waves per second. Noise blends between them.")]
    public float waveSpeedMin = 0.2f;   // slower side
    public float waveSpeedMax = 0.8f;   // faster side

    [Tooltip("How fast the noise evolves over time.")]
    public float noiseFrequency = 0.2f;

    [Tooltip("Optional random delay before starting wave (sec).")]
    public float startDelay = 0.5f;

    [Tooltip("Smooth the motion so it's not rigid.")]
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private float _t;
    private float _delayLeft;
    private float _noiseSeed;

    void OnEnable()
    {
        _t = Random.value * Mathf.PI * 2f;    // randomize initial phase
        _delayLeft = startDelay;
        _noiseSeed = Random.value * 100f;     // per-instance noise seed
    }

    void Update()
    {
        if (_delayLeft > 0f)
        {
            _delayLeft -= Time.deltaTime;
            return;
        }

        // Perlin noise -> 0..1, remap to [waveSpeedMin, waveSpeedMax]
        float n = Mathf.PerlinNoise(_noiseSeed, Time.time * noiseFrequency);
        float waveSpeed = Mathf.Lerp(waveSpeedMin, waveSpeedMax, n);

        // Advance the phase
        _t += Time.deltaTime * waveSpeed * Mathf.PI * 2f;

        // Raw sine gives -1..+1
        float s = Mathf.Sin(_t);

        // Ease curve makes it less stiff
        float eased = easeCurve.Evaluate((s + 1f) * 0.5f) * 2f - 1f;

        float angle = eased * waveArc;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}
