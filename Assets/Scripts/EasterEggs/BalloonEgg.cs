using UnityEngine;

public class BalloonEgg : MonoBehaviour, IEasterEgg
{
    [Header("Drift (randomized per balloon)")]
    public float upwardSpeedMin = 1f;
    public float upwardSpeedMax = 2f;
    public float rightwardSpeedMin = 0.3f;
    public float rightwardSpeedMax = 0.7f;

    [Header("Sway (natural)")]
    public float swayAngleMin = 6f;     // deg
    public float swayAngleMax = 12f;    // deg
    public float windFreqMin = 0.05f;   // Perlin sample speed (Hz)
    public float windFreqMax = 0.18f;
    public float damping = 0.25f;       // seconds to settle (lower = snappier)
    public float biasUpright = 0.2f;    // 0..1, how much to bias toward upright

    [Header("Optional: side bob")]
    public bool useSideBob = true;
    public float bobAmplitude = 0.15f;  // world units
    public float bobFactor = 0.35f;     // how much bob scales with |angle|

    string _id = "balloon";
    EasterEggManager _mgr;
    Camera _cam;
    float _lifeLeft;

    // rolled per instance
    float _upSpeed, _rightSpeed, _maxAngle, _windFreq, _noiseSeed;
    float _angle, _angleVel;            // current angle & angular velocity for SmoothDampAngle
    float _t;

    // local pivot pos used for side bob
    Vector3 _startLocalPos;

    public void Activate(EasterEggManager mgr, float lifespan, Camera cam)
    {
        _mgr = mgr;
        _cam = cam != null ? cam : Camera.main;
        _lifeLeft = lifespan;

        _upSpeed = Random.Range(upwardSpeedMin, upwardSpeedMax);
        _rightSpeed = Random.Range(rightwardSpeedMin, rightwardSpeedMax);
        _maxAngle = Random.Range(swayAngleMin, swayAngleMax);
        _windFreq = Random.Range(windFreqMin, windFreqMax);
        _noiseSeed = Random.value * 10f;

        _startLocalPos = transform.localPosition;
        _angle = 0f;
        _angleVel = 0f;
        _t = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _t += dt;

        // 1) Drift (world or parent-local — if these are on a parallax layer, local is usually best)
        transform.localPosition += new Vector3(_rightSpeed, _upSpeed, 0f) * dt;

        // 2) Compute a "wind angle target" from Perlin noise (smooth, natural)
        // Perlin returns 0..1. Remap to -1..+1, add upright bias toward 0.
        float n = Mathf.PerlinNoise(_noiseSeed, _t * _windFreq); // 0..1
        float signed = (n * 2f - 1f);                            // -1..1
        float target = signed * _maxAngle * (1f - biasUpright);  // reduce extremes
        target = Mathf.Lerp(target, 0f, biasUpright);            // nudge toward upright

        // 3) Laggy follow to target using SmoothDampAngle (feels like a damped spring)
        _angle = Mathf.SmoothDampAngle(_angle, target, ref _angleVel, Mathf.Max(0.01f, damping), Mathf.Infinity, dt);

        // 4) Apply rotation
        transform.localRotation = Quaternion.Euler(0f, 0f, _angle);

        // 5) Optional shallow side bob so the pivot traces an arc (very subtle)
        if (useSideBob)
        {
            // magnitude scales with angle; sign swaps with angle sign
            float bob = Mathf.Sin(_angle * Mathf.Deg2Rad) * bobAmplitude * bobFactor;
            // small lateral offset from start position
            var lp = _startLocalPos;
            lp.x += bob;
            transform.localPosition = new Vector3(lp.x, transform.localPosition.y, transform.localPosition.z);
        }

        // (Optional) lifetime cleanup if you're not using black-hole cleanup
        if (_lifeLeft > 0f)
        {
            _lifeLeft -= dt;
            if (_lifeLeft <= 0f)
            {
                if (_mgr != null) _mgr.Recycle(_id, gameObject);
                else gameObject.SetActive(false);
            }
        }
    }
}
