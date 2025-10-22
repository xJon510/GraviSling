using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[AddComponentMenu("Audio/Blackhole Warning SFX")]
public class BlackholeWarningSFX : MonoBehaviour
{
    [SerializeField] private Color nearColor = Color.red;
    [SerializeField] private Color farColor = Color.yellow;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private AudioClip warningBeep;

    [Tooltip("If not set, tries SFXTitleManager.Instance.audioSource, else adds a local AudioSource.")]
    [SerializeField] private AudioSource audioSource;

    [Header("Tutorial Reference")]
    [Tooltip("If assigned, the alarm will muffle while the tutorial is active.")]
    [SerializeField] private TutorialManager tutorialManager;

    [Header("Low-Pass Settings")]
    [Tooltip("Enable muffling while tutorial UI is open.")]
    [SerializeField] private bool enableLowPassWhileTutorial = true;
    [Tooltip("Low-pass cutoff frequency while muffled (Hz). Lower = more muffled.")]
    [SerializeField, Range(200f, 10000f)] private float muffledCutoff = 800f;
    [Tooltip("Normal cutoff frequency (default full-range).")]
    [SerializeField, Range(5000f, 22000f)] private float normalCutoff = 22000f;
    [Tooltip("Smoothing speed for cutoff transitions.")]
    [SerializeField] private float filterSmoothSpeed = 3f;

    [Header("Warning Flash")]
    [SerializeField] private Image warningIconA;
    [SerializeField] private Image warningIconB;
    [Tooltip("Seconds the icons stay visible per beep.")]
    [SerializeField] private float flashDuration = 0.2f;

    [Header("Warning Scale")]
    [SerializeField] private float scaleNear = 2.5f;  // at danger (red)
    [SerializeField] private float scaleFar = 1.0f;  // at cutoff (yellow)
    [SerializeField] private float scaleSmoothing = 8f; // higher = snappier

    [Header("Distance → Response")]
    [Tooltip("Inside this distance = max intensity (fastest beeps, loudest).")]
    [SerializeField] private float nearDistance = 12f;

    [Tooltip("Beyond this distance, the beep fades out and stops.")]
    [SerializeField] private float farDistance = 60f;
    private const float flashCutoffDistance = 1500f;

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
    private float _lastDist;
    private AudioLowPassFilter _lowPass;
    private bool _wasTutorialActive;

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

        if (enableLowPassWhileTutorial)
            _lowPass = audioSource.gameObject.GetComponent<AudioLowPassFilter>() ??
                       audioSource.gameObject.AddComponent<AudioLowPassFilter>();

        if (tutorialManager && tutorialManager.isActiveAndEnabled && _lowPass)
        {
            _lowPass.cutoffFrequency = muffledCutoff;
        }
        else if (_lowPass)
        {
            _lowPass.cutoffFrequency = normalCutoff;
        }
    }

    private void Update()
    {
        if (!player || !warningBeep || !audioSource) return;

        // --- Handle tutorial muffling ---
        if (enableLowPassWhileTutorial && _lowPass)
        {
            bool tutorialActive = tutorialManager && tutorialManager.isActiveAndEnabled;
            float targetCutoff = tutorialActive ? muffledCutoff : normalCutoff;

            // 👇 snap immediately when tutorial first activates
            if (tutorialActive && !_wasTutorialActive)
                _lowPass.cutoffFrequency = muffledCutoff;
            else
                _lowPass.cutoffFrequency = Mathf.Lerp(
                    _lowPass.cutoffFrequency, targetCutoff, Time.deltaTime * filterSmoothSpeed);

            _wasTutorialActive = tutorialActive;
        }

        _lastDist = Vector3.Distance(_self.position, player.position);

        if (warningIconA || warningIconB)
        {
            // Normalize distance between 0 (close) and 1 (flashCutoffDistance)
            float colorT = Mathf.InverseLerp(500f, flashCutoffDistance, _lastDist);
            Color lerped = Color.Lerp(nearColor, farColor, colorT);

            if (warningIconA) warningIconA.color = lerped;
            if (warningIconB) warningIconB.color = lerped;
        }

        // --- size lerp based on same distance curve as color ---
        if (warningIconA || warningIconB)
        {
            // 0 at close (<=500), 1 at far (flashCutoffDistance)
            float tColor = Mathf.InverseLerp(500f, flashCutoffDistance, _lastDist);

            // target scale: close -> big, far -> small
            float target = Mathf.Lerp(scaleNear, scaleFar, tColor);
            Vector3 targetScale = Vector3.one * target;

            // smooth the scale so it feels like a pulse/breath
            if (warningIconA)
            {
                var rt = warningIconA.rectTransform;
                rt.localScale = Vector3.Lerp(rt.localScale, targetScale, Time.deltaTime * scaleSmoothing);
            }
            if (warningIconB)
            {
                var rt = warningIconB.rectTransform;
                rt.localScale = Vector3.Lerp(rt.localScale, targetScale, Time.deltaTime * scaleSmoothing);
            }
        }

        float t = Mathf.InverseLerp(farDistance, nearDistance, _lastDist);

        float targetVol = volumeByProximity.Evaluate(t);

        _currentVol = Mathf.MoveTowards(_currentVol, targetVol, volumeSmoothing * Time.deltaTime);

        float rateT = rateByProximity.Evaluate(t);
        _lastInterval = Mathf.Lerp(maxInterval, minInterval, rateT);

        if (_currentVol <= 0.001f)
            return;

        if (Time.time >= _nextBeepTime && _lastDist < farDistance)
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

        if (_lastDist <= flashCutoffDistance)
            StartCoroutine(FlashIcons());
    }

    private IEnumerator FlashIcons()
    {
        if (warningIconA) warningIconA.enabled = true;
        if (warningIconB) warningIconB.enabled = true;

        yield return new WaitForSeconds(flashDuration);

        if (warningIconA) warningIconA.enabled = false;
        if (warningIconB) warningIconB.enabled = false;
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
