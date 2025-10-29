using System.Collections.Generic;
using UnityEngine;

public class AudioPrefsApplier : MonoBehaviour
{
    public enum AudioChannel { Music, SFX }

    [Header("Target(s)")]
    public List<AudioSource> targetAudioSources = new List<AudioSource>();

    [Header("Which setting to read")]
    public AudioChannel channel = AudioChannel.Music;

    [Tooltip("Optional override. Leave empty to use GS_MUTE_<CHANNEL>.")]
    public string customPrefsKey = "";

    [Tooltip("Only used if no PlayerPrefs value exists yet.")]
    public bool defaultMutedIfUnset = false;

    [Tooltip("Apply in Awake (before anything might play). If you have your own init order, you can turn this off and call Apply() yourself.")]
    public bool applyInAwake = true;

    string PrefsKey =>
        !string.IsNullOrWhiteSpace(customPrefsKey)
            ? customPrefsKey
            : (channel == AudioChannel.Music ? "GS_MUTE_MUSIC" : "GS_MUTE_SFX");

    void Awake()
    {
        int defaultVal = defaultMutedIfUnset ? 1 : 0;
        bool muted = PlayerPrefs.GetInt(PrefsKey, defaultVal) == 1;

        if (applyInAwake) ApplyToTargets(muted);
    }

    void OnEnable()
    {
        int defaultVal = defaultMutedIfUnset ? 1 : 0;
        bool muted = PlayerPrefs.GetInt(PrefsKey, defaultVal) == 1;
        // Safety net in case this component or the AudioSource is toggled active later
        if (!applyInAwake) ApplyToTargets(muted);
    }

    /// <summary>
    /// Reads PlayerPrefs and applies mute to the target source.
    /// </summary>
    public void Apply()
    {
        if (targetAudioSources == null || targetAudioSources.Count == 0)
            return;

        int defaultVal = defaultMutedIfUnset ? 1 : 0;
        bool muted = PlayerPrefs.GetInt(PrefsKey, defaultVal) == 1;

        foreach (var source in targetAudioSources)
        {
            if (source)
                source.mute = muted;
        }
    }

    void HandleMuteChanged(string changedKey, bool muted)
    {
        // Only react to our own key
        if (changedKey != PrefsKey) return;
        ApplyToTargets(muted);
    }

    void ApplyToTargets(bool muted)
    {
        if (targetAudioSources == null) return;
        foreach (var src in targetAudioSources)
            if (src) src.mute = muted;
    }
}
