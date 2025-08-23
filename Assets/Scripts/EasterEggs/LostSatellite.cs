using UnityEngine;

public class LostSatellite : MonoBehaviour, IEasterEgg
{
    [Header("Motion (relative to parent)")]
    public float wobbleAmp = 0.15f;     // local Y wobble
    public float wobbleFreq = 0.6f;     // Hz
    public float spinDegPerSec = 8f;

    [Range(0f, 0.5f)] public float offscreenMargin = 0.1f;

    string _id = "lost_satellite";
    EasterEggManager _mgr;
    Camera _cam;
    float _t;
    float _lifeLeft;
    bool _enteredView;

    float _startLocalY;

    public void Activate(EasterEggManager mgr, float lifespan, Camera cam)
    {
        _mgr = mgr;
        _cam = cam;
        _lifeLeft = lifespan;
        _t = UnityEngine.Random.value * 10f;
        _enteredView = false;

        _startLocalY = transform.localPosition.y;   // wobble relative to parent
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _t += dt;
        _lifeLeft -= dt;

        // Wobble in LOCAL space so we don't fight parallax translation
        var lp = transform.localPosition;
        lp.y = _startLocalY + Mathf.Sin(_t * (Mathf.PI * 2f) * wobbleFreq) * wobbleAmp;
        transform.localPosition = lp;

        // Spin
        transform.Rotate(0f, 0f, spinDegPerSec * dt, Space.Self);
    }
}
