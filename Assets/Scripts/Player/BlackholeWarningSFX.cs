using UnityEngine;

[AddComponentMenu("Audio/Blackhole Warning SFX")]
public class BlackholeWarningSFX : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private AudioClip warningBeep;

    [Tooltip("Optional. If not set, tries SFXTitleManager.Instance.audioSource, else adds a local AudioSource.")]
    [SerializeField] private AudioSource audioSource;

    [Header("Distance → Response")]
    [Tooltip("Inside this distance = max intensity (fastest beeps, loudest).")]
    [SerializeField] private float nearDistance = 12f;

    [Tooltip("Beyond this distance, the beep fades out and stops.")]
    [SerializeField] private float farDistance = 60f;

    [Header("Cadence (closer = faster)")]
    [Tooltip("Fastest interval (seconds) when very close.")]
    [SerializeField] private float minInterval = 0.18f;

    [Tooltip("Slowest interval (seconds) when at the edge of the warning range).")]
    [SerializeField] private float maxInterval = 1.2f;

    [Header("Volume / Pitch")]
    [Tooltip("Maps proximity [0..1] to volume multiplier. Default: linear 0→1.")]
    [SerializeField] private AnimationCurve volumeByProximity = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("Maps proximity [0..1] to cadence curve. Default: linear 0→1.")]
    [SerializeField] private AnimationCurve rateByProximity = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("How quickly the perceived volume follows distance changes (bigger = snappier).")]
    [SerializeField] private float volumeSmoothing = 6f;

    [Tooltip("Base pitch applied before jitter.")]
    [SerializeField] private float basePitch = 1f;

    [Tooltip("± pitch jitter each beep to avoid ear fatigue.")]
    [Range(0f, 0.5f)][SerializeField] private float pitchJitter = 0.03f;

    [Header("3D Settings (optional)")]
    [SerializeField] private bool spatialize3D = true;
    [SerializeField] private float spatialMinDistance = 5f;
    [SerializeField] private float spatialMaxDistance = 50f;

    // runtime
    private float _currentVol;   
    private float _nextBeepTime; 
    private float _lastInterval;     
    private Transform _self;

    private void Awake()
    {
        _self = transform;

        if (!audioSource)
        {
            if (SFXTitleManager.Instance && SFXTitleManager.Instance.audioSource)
            {
                audioSource = SFXTitleManager.Instance.audioSource;
            }
            else
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.loop = false;
            }
        }

        if (spatialize3D && audioSource)
        {
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = spatialMinDistance;
            audioSource.maxDistance = spatialMaxDistance;
        }
    }

    private void Update()
    {
        if (!player || !warningBeep || !audioSource) return;

        float dist = Vector3.Distance(_self.position, player.position);
        float t = Mathf.InverseLerp(farDistance, nearDistance, dist);

        float targetVol = volumeByProximity.Evaluate(t);

        _currentVol = Mathf.MoveTowards(_currentVol, targetVol, volumeSmoothing * Time.deltaTime);

        float rateT = rateByProximity.Evaluate(t);
        _lastInterval = Mathf.Lerp(maxInterval, minInterval, rateT);

        if (_currentVol <= 0.001f)
            return;

        if (Time.time >= _nextBeepTime)
        {
            Beep(_currentVol);
            _nextBeepTime = Time.time + Mathf.Max(0.02f, _lastInterval); 
        }
    }

    private void Beep(float vol)
    {
        if (!audioSource || !warningBeep) return;

        float oldPitch = audioSource.pitch;
        audioSource.pitch = basePitch * (1f + Random.Range(-pitchJitter, pitchJitter));

        audioSource.PlayOneShot(warningBeep, vol);

        audioSource.pitch = oldPitch;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, nearDistance);
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, farDistance);
    }
#endif
}
