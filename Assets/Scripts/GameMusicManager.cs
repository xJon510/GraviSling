using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMusicManager : MonoBehaviour
{
    [Header("Scene Scope")]
    [SerializeField] private string mainGameSceneName = "MainGame";

    [Header("Audio Settings")]
    public List<AudioSource> musicSources = new List<AudioSource>(); // auto-filled in Awake
    public AudioClip[] songs;
    [Range(0f, 5f)] public float crossfadeSeconds = 1.0f;
    [Range(0.05f, 1.0f)] public float initialDelay = 0.1f;
    [Range(0f, 1f)] public float baseVolume = 0.2f;

    private static GameMusicManager _instance;
    private int lastIndex = -1;

    // Internal state
    private int currentSourceIndex = 0;
    private Coroutine playlistCo;
    private bool running;

    private void Awake()
    {
        // Handle singleton
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (SceneManager.GetActiveScene().name == mainGameSceneName)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }
    }

    private void Start()
    {
        if (_instance != this || songs == null || songs.Length == 0) return;
        running = true;
        playlistCo = StartCoroutine(Playlist());
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene prev, Scene next)
    {
        if (next.name != mainGameSceneName && _instance == this)
        {
            if (playlistCo != null) StopCoroutine(playlistCo);
            foreach (var s in musicSources) s.Stop();
            Destroy(gameObject);
            _instance = null;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }
    }

    private IEnumerator Playlist()
    {
        if (musicSources == null || musicSources.Count < 2) yield break;

        // Schedule the very first track a tiny bit in the future
        double dspStart = AudioSettings.dspTime + initialDelay;

        // Schedule on the "next" source at volume 0
        yield return StartNextScheduled(dspStart);

        // Promote the just-scheduled source to CURRENT and make it audible immediately
        currentSourceIndex = (currentSourceIndex + 1) % musicSources.Count;
        AudioSource cur = musicSources[currentSourceIndex];
        cur.volume = baseVolume;

        // The absolute DSP time when this current clip will end
        double clipEndDSP = dspStart + cur.clip.length;

        while (running)
        {
            // Wake a bit before end to warm/schedule the next clip
            double wakeAt = clipEndDSP - crossfadeSeconds - 0.1;
            while (AudioSettings.dspTime < wakeAt) yield return null;

            // Prepare & schedule next on the other source (kept at 0 volume)
            yield return StartNextScheduled(clipEndDSP);
            AudioSource nxt = musicSources[(currentSourceIndex + 1) % musicSources.Count];

            // Crossfade over the configured duration
            float t = 0f;
            while (t < crossfadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / crossfadeSeconds);
                nxt.volume = Mathf.Lerp(0f, baseVolume, k);
                cur.volume = Mathf.Lerp(baseVolume, 0f, k);
                yield return null;
            }

            // Swap roles
            cur.Stop();
            cur.volume = baseVolume;    // reset for next time it becomes 'nxt'
            currentSourceIndex = (currentSourceIndex + 1) % musicSources.Count;
            cur = musicSources[currentSourceIndex];

            // Advance absolute end time using the NEW current clip
            clipEndDSP += cur.clip.length;
        }
    }

    private IEnumerator StartNextScheduled(double dspWhen)
    {
        int idx;
        if (songs.Length == 1) idx = 0;
        else
        {
            do { idx = UnityEngine.Random.Range(0, songs.Length); } while (idx == lastIndex);
        }
        lastIndex = idx;

        var clip = songs[idx];
        if (!clip.preloadAudioData) clip.LoadAudioData();
        while (clip.loadState == AudioDataLoadState.Loading) yield return null;

        var nxt = musicSources[(currentSourceIndex + 1) % musicSources.Count];
        nxt.clip = clip;
        nxt.volume = 0f;
        nxt.PlayScheduled(dspWhen);
    }
}
