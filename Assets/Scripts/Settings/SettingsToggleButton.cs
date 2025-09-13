using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Button))]
public class SettingsToggleButton : MonoBehaviour
{
    [System.Serializable]
    public class ToggleImage
    {
        public Image image;   // UI element to tint
        public Color onColor; // unmuted
        public Color offColor;// muted
    }

    [Header("Audio Control")]
    public AudioSource targetAudioSource;
    public bool startMuted = false;

    [Header("UI Tint Targets")]
    [Tooltip("Assign your 4 UI Images (each with its own ON/OFF color).")]
    public ToggleImage[] toggleImages;

    [Header("Knob Slide")]
    public RectTransform sliderKnob;          // the moving knob/handle
    public Vector2 knobOnPos;                 // anchoredPosition when UNMUTED (ON)
    public Vector2 knobOffPos;                // anchoredPosition when MUTED (OFF)
    [Range(0.01f, 2f)] public float slideDuration = 0.18f;
    public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Label Fades")]
    [Tooltip("CanvasGroup for the 'ON' label (fades in when unmuted).")]
    public CanvasGroup onLabel;
    [Tooltip("CanvasGroup for the 'OFF' label (fades in when muted).")]
    public CanvasGroup offLabel;
    [Range(0.01f, 1f)] public float labelFadeDuration = 0.12f;

    [Header("Tint Lerp")]
    [Tooltip("How long the color lerp takes.")]
    [Range(0.01f, 1f)] public float colorLerpDuration = 0.25f;

    private Button _button;
    private bool _isMuted;
    private Coroutine _colorCo, _slideCo, _fadeCo;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnToggleClicked);

        _isMuted = startMuted;
        if (targetAudioSource) targetAudioSource.mute = _isMuted;

        // Snap initial visuals
        ApplyColorsImmediate();
        ApplyKnobImmediate();
        ApplyLabelsImmediate();
    }

    void OnDestroy()
    {
        if (_button) _button.onClick.RemoveListener(OnToggleClicked);
    }

    void OnToggleClicked()
    {
        if (!targetAudioSource) return;

        // Flip state + apply to audio
        _isMuted = !_isMuted;
        targetAudioSource.mute = _isMuted;

        // Animate all three: colors, knob, labels
        if (_colorCo != null) StopCoroutine(_colorCo);
        _colorCo = StartCoroutine(LerpColors(_isMuted, colorLerpDuration));

        if (_slideCo != null) StopCoroutine(_slideCo);
        _slideCo = StartCoroutine(SlideKnob(_isMuted, slideDuration));

        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeLabels(_isMuted, labelFadeDuration));
    }

    // ----- Immediate apply for initial state -----
    void ApplyColorsImmediate()
    {
        foreach (var t in toggleImages)
        {
            if (!t.image) continue;
            t.image.color = _isMuted ? t.offColor : t.onColor;
        }
    }

    void ApplyKnobImmediate()
    {
        if (!sliderKnob) return;
        sliderKnob.anchoredPosition = _isMuted ? knobOffPos : knobOnPos;
    }

    void ApplyLabelsImmediate()
    {
        if (onLabel) onLabel.alpha = _isMuted ? 0f : 1f;
        if (offLabel) offLabel.alpha = _isMuted ? 1f : 0f;
    }

    // ----- Coroutines -----
    IEnumerator LerpColors(bool muted, float dur)
    {
        if (toggleImages == null || toggleImages.Length == 0) yield break;

        float t = 0f;
        var start = new Color[toggleImages.Length];
        var end = new Color[toggleImages.Length];

        for (int i = 0; i < toggleImages.Length; i++)
        {
            if (!toggleImages[i].image) continue;
            start[i] = toggleImages[i].image.color;
            end[i] = muted ? toggleImages[i].offColor : toggleImages[i].onColor;
        }

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            float k = Mathf.Clamp01(t);
            for (int i = 0; i < toggleImages.Length; i++)
            {
                if (!toggleImages[i].image) continue;
                toggleImages[i].image.color = Color.Lerp(start[i], end[i], k);
            }
            yield return null;
        }
    }

    IEnumerator SlideKnob(bool muted, float dur)
    {
        if (!sliderKnob) yield break;

        Vector2 a = sliderKnob.anchoredPosition;
        Vector2 b = muted ? knobOffPos : knobOnPos;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            float k = slideCurve.Evaluate(Mathf.Clamp01(t));
            sliderKnob.anchoredPosition = Vector2.LerpUnclamped(a, b, k);
            yield return null;
        }
        sliderKnob.anchoredPosition = b;
    }

    IEnumerator FadeLabels(bool muted, float dur)
    {
        if (!onLabel && !offLabel) yield break;

        float t = 0f;
        float onStart = onLabel ? onLabel.alpha : 0f;
        float offStart = offLabel ? offLabel.alpha : 0f;
        float onEnd = muted ? 0f : 1f;
        float offEnd = muted ? 1f : 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            float k = Mathf.Clamp01(t);
            if (onLabel) onLabel.alpha = Mathf.Lerp(onStart, onEnd, k);
            if (offLabel) offLabel.alpha = Mathf.Lerp(offStart, offEnd, k);
            yield return null;
        }
        if (onLabel) onLabel.alpha = onEnd;
        if (offLabel) offLabel.alpha = offEnd;
    }
}
