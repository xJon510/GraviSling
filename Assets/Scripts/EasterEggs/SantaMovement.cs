using UnityEngine;

public class SantaMovement : MonoBehaviour, IEasterEgg
{
    [Header("IDs")]
    [SerializeField] private string id = "santa_sleigh";

    [Header("Flight")]
    public float speed = 4f;           // units per second (local X)
    public bool leftToRight = true;    // flip travel direction

    [Header("Bob Motion")]
    public float bobAmplitude = 0.5f;  // world units up/down
    public float bobFrequency = 0.5f;  // Hz

    [Header("Lifetime")]
    public bool useLifespan = true;

    // --- runtime ---
    EasterEggManager _mgr;
    Camera _cam;
    float _lifeLeft;
    float _phaseOffset;
    Vector3 _startLocalPos;

    public void Activate(EasterEggManager mgr, float lifespan, Camera cam)
    {
        _mgr = mgr;
        _cam = cam != null ? cam : Camera.main;
        _lifeLeft = lifespan;
        _startLocalPos = transform.localPosition;
        _phaseOffset = Random.value * Mathf.PI * 2f; // offset so multiple Santas aren't identical
    }

    void Update()
    {
        float dt = Time.deltaTime;
        _lifeLeft -= dt;

        // Horizontal drift
        float dir = leftToRight ? 1f : -1f;
        transform.localPosition += new Vector3(speed * dir * dt, 0f, 0f);

        // Bobbing sine motion
        float bob = Mathf.Sin((Time.time + _phaseOffset) * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        var lp = transform.localPosition;
        lp.y = _startLocalPos.y + bob;
        transform.localPosition = lp;

        // Lifetime recycle
        if (useLifespan && _lifeLeft <= 0f)
        {
            if (_mgr != null) _mgr.Recycle(id, gameObject);
            else gameObject.SetActive(false);
        }
    }
}
