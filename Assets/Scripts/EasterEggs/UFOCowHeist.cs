using UnityEngine;

public class UFOCowHeist : MonoBehaviour, IEasterEgg
{
    [Header("IDs")]
    [SerializeField] private string id = "ufo_cow_heist";

    [Header("Cruise (local-space drift)")]
    public float forwardXSpeed = 0.0f;   // usually small; parallax layer provides main motion
    public float bobAmplitudeY = 0.8f;   // world units
    public float bobFrequencyY = 0.25f;  // Hz

    [Header("Sway Rotation")]
    public float swayAngle = 10f;        // deg peak
    public float swayFrequency = 0.3f;   // Hz

    [Header("Loops")]
    public bool enableLoops = true;
    [Tooltip("Min seconds between loops; randomized each time.")]
    public float loopCooldownMin = 4f;
    public float loopCooldownMax = 8f;

    [Tooltip("Chance per second to start a loop when off cooldown.")]
    [Range(0f, 1f)] public float loopStartChancePerSecond = 0.15f;

    [Tooltip("Radius of the loop in local space.")]
    public float loopRadius = 1.6f;

    [Tooltip("How long one loop takes (seconds).")]
    public float loopDuration = 1.1f;

    [Tooltip("Extra tilt applied during loop (deg).")]
    public float loopTilt = 15f;

    [Header("Lifetime (optional safety)")]
    public bool useLifespan = true;

    // --- runtime ---
    EasterEggManager _mgr;
    Camera _cam;
    float _lifeLeft;

    Vector3 _startLocalPos;   // base position to add bob offset on top
    float _phaseBob;          // randomized phase
    float _phaseSway;

    enum State { Cruise, Loop }
    State _state;
    float _stateT;            // time in state

    // loop state
    Vector3 _loopCenter;
    float _loopCooldownLeft;

    public void Activate(EasterEggManager mgr, float lifespan, Camera cam)
    {
        _mgr = mgr;
        _cam = cam != null ? cam : Camera.main;
        _lifeLeft = lifespan;

        _startLocalPos = transform.localPosition;
        _phaseBob = Random.value * Mathf.PI * 2f;
        _phaseSway = Random.value * Mathf.PI * 2f;

        _state = State.Cruise;
        _stateT = 0f;
        _loopCooldownLeft = Random.Range(loopCooldownMin, loopCooldownMax);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _stateT += dt;

        if (useLifespan)
        {
            _lifeLeft -= dt;
            if (_lifeLeft <= 0f)
            {
                if (_mgr != null) _mgr.Recycle(id, gameObject);
                else gameObject.SetActive(false);
                return;
            }
        }

        switch (_state)
        {
            case State.Cruise:
                TickCruise(dt);
                break;
            case State.Loop:
                TickLoop(dt);
                break;
        }
    }

    void TickCruise(float dt)
    {
        // Drift forward slightly in local X (parallax supplies most motion)
        var lp = transform.localPosition;
        lp.x += forwardXSpeed * dt;

        // Bob in local Y with a smooth sine
        float bob = Mathf.Sin((Time.time + _phaseBob) * bobFrequencyY * Mathf.PI * 2f) * bobAmplitudeY;
        lp.y = _startLocalPos.y + bob;

        transform.localPosition = lp;

        // Sway rotation
        float sway = Mathf.Sin((Time.time + _phaseSway) * swayFrequency * Mathf.PI * 2f) * swayAngle;
        transform.localRotation = Quaternion.Euler(0f, 0f, sway);

        // Loop trigger
        if (enableLoops)
        {
            _loopCooldownLeft -= dt;
            if (_loopCooldownLeft <= 0f)
            {
                // probabilistic start
                if (Random.value < loopStartChancePerSecond * dt)
                    BeginLoop();
            }
        }
    }

    void BeginLoop()
    {
        _state = State.Loop;
        _stateT = 0f;

        // Loop center slightly ahead of current position so it feels forward-moving
        _loopCenter = transform.localPosition + new Vector3(loopRadius * 0.5f, 0f, 0f);

        // reset cooldown for next time
        _loopCooldownLeft = Random.Range(loopCooldownMin, loopCooldownMax);
    }

    void TickLoop(float dt)
    {
        float t01 = Mathf.Clamp01(_stateT / Mathf.Max(0.001f, loopDuration));
        float angle = t01 * Mathf.PI * 2f;              // 0..2 over the loop
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        // Circular path around loopCenter
        Vector3 pos = _loopCenter + new Vector3(cos * loopRadius, sin * loopRadius, 0f);

        // Add a tiny forward drift so the circle progresses
        pos.x += (forwardXSpeed * 0.25f) * _stateT;

        transform.localPosition = pos;

        // Spin to match loop direction + a little tilt
        float spinDeg = -angle * Mathf.Rad2Deg;         // clockwise
        transform.localRotation = Quaternion.Euler(0f, 0f, spinDeg + loopTilt);

        // End loop
        if (_stateT >= loopDuration)
        {
            // Snap start reference for subsequent bobbing to current height
            _startLocalPos.y = transform.localPosition.y;
            _state = State.Cruise;
            _stateT = 0f;
        }
    }
}
