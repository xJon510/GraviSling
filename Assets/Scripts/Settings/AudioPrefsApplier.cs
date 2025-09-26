using UnityEngine;

public class AudioPrefsApplier : MonoBehaviour
{
    public enum AudioChannel { Music, SFX }

    [Header("Target")]
    public AudioSource targetAudioSource;

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
        if (applyInAwake) Apply();
    }

    void OnEnable()
    {
        // Safety net in case this component or the AudioSource is toggled active later
        if (!applyInAwake) Apply();
    }

    /// <summary>
    /// Reads PlayerPrefs and applies mute to the target source.
    /// </summary>
    public void Apply()
    {
        if (!targetAudioSource) return;

        int defaultVal = defaultMutedIfUnset ? 1 : 0;
        bool muted = PlayerPrefs.GetInt(PrefsKey, defaultVal) == 1;
        targetAudioSource.mute = muted;
    }
}
