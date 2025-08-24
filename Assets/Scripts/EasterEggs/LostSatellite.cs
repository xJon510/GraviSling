using UnityEngine;

public class LostSatellite : MonoBehaviour, IEasterEgg
{
    [Header("Motion (relative to parent)")]
    public float wobbleAmp = 0.15f;     // local Y wobble
    public float wobbleFreq = 0.6f;     // Hz
    public float spinDegPerSec = 8f;

    [Header("Drift")]
    public float driftSpeedMin = 0.2f;  // world units per sec
    public float driftSpeedMax = 1.0f;

    string _id = "lost_satellite";
    EasterEggManager _mgr;
    Camera _cam;
    float _t;
    float _lifeLeft;
    bool _enteredView;

    float _startLocalY;
    Vector3 _driftLocal;

    public void Activate(EasterEggManager mgr, float lifespan, Camera cam)
    {
        _mgr = mgr;
        _cam = cam;
        _lifeLeft = lifespan;
        _t = UnityEngine.Random.value * 10f;

        _startLocalY = transform.localPosition.y;

        // Pick random direction + speed in LOCAL space
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float speed = UnityEngine.Random.Range(driftSpeedMin, driftSpeedMax);
        _driftLocal = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _t += dt;
        _lifeLeft -= dt;

        // Apply random drift in LOCAL space
        transform.localPosition += _driftLocal * dt;

        // Add wobble (local Y only)
        var lp = transform.localPosition;
        lp.y = _startLocalY + Mathf.Sin(_t * (Mathf.PI * 2f) * wobbleFreq) * wobbleAmp;
        transform.localPosition = lp;

        // Spin
        transform.Rotate(0f, 0f, spinDegPerSec * dt, Space.Self);
    }
}
