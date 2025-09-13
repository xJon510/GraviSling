using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SFXTitleManager : MonoBehaviour
{
    public static SFXTitleManager Instance { get; private set; }

    [Header("Wiring")]
    public AudioSource audioSource;

    [Header("Clips")]
    public AudioClip machRingExplosionClip;
    public AudioClip buttonSFX;
    public AudioClip uiHoverClip;

    [Header("Button Hookup")]
    [Tooltip("Assign any UI Buttons you want to auto-wire for click SFX.")]
    public List<Button> buttons = new List<Button>();
    [Range(0f, 1f)] public float buttonVolume = 0.7f;
    [Tooltip("Random pitch range for button clicks (min..max).")]
    [Range(0.1f, 3f)] public float buttonPitchMin = 0.9f;
    [Range(0.1f, 3f)] public float buttonPitchMax = 1.0f;

    [Header("OnHover Settings")]
    [Range(0f, 1f)] public float hoverVolume = 0.4f;
    [Range(0.9f, 1.1f)] public float hoverPitchMin = 0.98f;
    [Range(0.9f, 1.1f)] public float hoverPitchMax = 1.02f;

    [Header("Launch Pitch Mapping")]
    public float minLaunchSpeed = 6f;   // set to your slowest title launch
    public float maxLaunchSpeed = 18f;  // set to your fastest title launch
    public AnimationCurve powerToPitch = AnimationCurve.Linear(0f, 1f, 1f, 1.45f);
    [Range(0f, 0.5f)] public float randomJitter = 0.05f; // ±jitter around final pitch
    [Range(0.5f, 1.5f)] public float basePitch = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    void OnEnable()
    {
        // Wire buttons (avoid duplicates by clearing first)
        foreach (var b in buttons)
        {
            if (!b) continue;
            b.onClick.RemoveListener(PlayButtonClick); // safety
            b.onClick.AddListener(PlayButtonClick);
        }
    }
    void OnDisable()
    {
        foreach (var b in buttons)
        {
            if (!b) continue;
            b.onClick.RemoveListener(PlayButtonClick);
        }
    }

    // ---------- Public API ----------

    public void PlayMachRingExplosionBySpeed(float launchSpeed, float volume = 1f)
    {
        if (!audioSource || !machRingExplosionClip) return;

        // Normalize speed -> [0..1]
        float t = Mathf.InverseLerp(minLaunchSpeed, maxLaunchSpeed, launchSpeed);

        // Map power -> pitch multiplier
        float pitchMul = powerToPitch.Evaluate(t);

        // Apply jitter
        float oldPitch = audioSource.pitch;
        audioSource.pitch = basePitch * pitchMul * (1f + Random.Range(-randomJitter, randomJitter));
        float volumeMul = Mathf.Lerp(0.8f, 1.2f, t);
        audioSource.PlayOneShot(machRingExplosionClip, volume * volumeMul);
        audioSource.pitch = oldPitch;
    }
    public void PlayButtonClick()
    {
        if (!audioSource || !buttonSFX) return;

        float oldPitch = audioSource.pitch;
        audioSource.pitch = Random.Range(buttonPitchMin, buttonPitchMax);
        audioSource.PlayOneShot(buttonSFX, buttonVolume);
        audioSource.pitch = oldPitch;
    }
    public void PlayUIHover()
    {
        if (!audioSource || !uiHoverClip) return;
        float oldPitch = audioSource.pitch;
        audioSource.pitch = Random.Range(hoverPitchMin, hoverPitchMax);
        audioSource.PlayOneShot(uiHoverClip, hoverVolume);
        audioSource.pitch = oldPitch;
    }
}
