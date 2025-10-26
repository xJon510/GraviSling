// RainbowifyCard.cs
// Unity 6 compatible
// One driver per CARD: applies a single cycling hue to many assigned targets.
// Preserves each target's original Saturation/Value/Alpha; only the hue cycles.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class RainbowifyCard : MonoBehaviour
{
    public enum TargetType { Image, SpriteRenderer, TMP_UI_Text }

    [Serializable]
    public sealed class Target
    {
        public TargetType type;
        public Image image;
        public SpriteRenderer spriteRenderer;
        public TMP_Text tmpText;
    }

    [Header("Targets on this card (assign manually)")]
    [Tooltip("Add any UI Images, SpriteRenderers, or TMP Texts that should share the same rainbow hue.")]
    [SerializeField] private List<Target> targets = new List<Target>();

    [Header("Rainbow")]
    [Tooltip("Hue rotations per second (0 = static).")]
    [SerializeField, Min(0f)] private float rainbowSpeed = 0.25f;

    [Tooltip("Run the effect while not playing (editor preview).")]
    [SerializeField] private bool executeInEditMode = false;

    [Tooltip("Add a constant 0..1 hue offset to the whole card.")]
    [Range(0f, 1f)] public float startHueOffset = 0f;

    [Tooltip("Randomize start hue once in Awake() for this card.")]
    [SerializeField] private bool randomizeHueOnAwake = true;

    [Tooltip("Extra time offset (seconds) to desync this card vs others.")]
    [SerializeField] private float phaseOffsetSeconds = 0f;

    [Tooltip("Randomize phase in Awake within roughly one hue loop.")]
    [SerializeField] private bool randomizePhaseOnAwake = true;

    // Cache per-target original color and HSV (we keep S/V/A; cycle Hue globally)
    private readonly List<Color> _orig = new List<Color>();
    private readonly List<float> _sat = new List<float>();
    private readonly List<float> _val = new List<float>();
    private readonly List<float> _alpha = new List<float>();
    private bool _initialized;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep caches sized right for preview & to avoid index errors
        EnsureCaches();
        if (!Application.isPlaying && executeInEditMode)
        {
            CacheOriginals();       // recache in case you changed assignments
            ApplyAtTime(0f);        // preview
        }
    }
#endif

    private void Awake()
    {
        EnsureCaches();
        CacheOriginals();

        if (randomizeHueOnAwake)
            startHueOffset = UnityEngine.Random.value;

        if (randomizePhaseOnAwake && rainbowSpeed > 0f)
            phaseOffsetSeconds += UnityEngine.Random.value / Mathf.Max(0.0001f, rainbowSpeed);

        _initialized = true;
    }

    private void Update()
    {
        if (!Application.isPlaying && !executeInEditMode)
            return;
        if (!_initialized) return;

        float t;
        if (Application.isPlaying)
        {
            t = Time.time;
        }
        else
        {
#if UNITY_EDITOR
            t = (float)UnityEditor.EditorApplication.timeSinceStartup;
#else
            t = 0f;
#endif
        }

        ApplyAtTime(t + phaseOffsetSeconds);
    }

    // ---------- Helpers ----------

    private void EnsureCaches()
    {
        ResizeList(_orig, targets.Count);
        ResizeList(_sat, targets.Count);
        ResizeList(_val, targets.Count);
        ResizeList(_alpha, targets.Count);
    }

    private static void ResizeList<T>(List<T> list, int size)
    {
        if (list.Count < size) list.AddRange(new T[size - list.Count]);
        else if (list.Count > size) list.RemoveRange(size, list.Count - size);
    }

    private void CacheOriginals()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (!TryGetColor(t, out var c))
                c = Color.white;

            _orig[i] = c;
            _alpha[i] = c.a;

            Color.RGBToHSV(c, out var h, out var s, out var v);
            _sat[i] = s;
            _val[i] = v;
        }
    }

    private void ApplyAtTime(float t)
    {
        // One shared hue for the whole card (sync)
        float hue = Mathf.Repeat(startHueOffset + rainbowSpeed * t, 1f);

        for (int i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            Color c = Color.HSVToRGB(hue, _sat[i], _val[i]);
            c.a = _alpha[i];
            TrySetColor(target, c);
        }
    }

    private static bool TryGetColor(Target target, out Color c)
    {
        switch (target.type)
        {
            case TargetType.Image:
                if (target.image != null) { c = target.image.color; return true; }
                break;
            case TargetType.SpriteRenderer:
                if (target.spriteRenderer != null) { c = target.spriteRenderer.color; return true; }
                break;
            case TargetType.TMP_UI_Text:
                if (target.tmpText != null) { c = target.tmpText.color; return true; }
                break;
        }
        c = default;
        return false;
    }

    private static void TrySetColor(Target target, Color c)
    {
        switch (target.type)
        {
            case TargetType.Image:
                if (target.image != null) target.image.color = c;
                break;
            case TargetType.SpriteRenderer:
                if (target.spriteRenderer != null) target.spriteRenderer.color = c;
                break;
            case TargetType.TMP_UI_Text:
                if (target.tmpText != null) target.tmpText.color = c;
                break;
        }
    }

    // ---------- Public API (optional) ----------

    public void SetRainbowSpeed(float speed) => rainbowSpeed = Mathf.Max(0f, speed);

    public void RecacheFromCurrentColors()
    {
        EnsureCaches();
        CacheOriginals();
    }
}
