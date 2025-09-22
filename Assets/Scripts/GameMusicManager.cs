using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMusicManager : MonoBehaviour
{
    [Header("Scene Scope")]
    [SerializeField] private string mainGameSceneName = "MainGame";

    [Header("Audio Settings")]
    public AudioSource audioSource;        // assign in inspector (optional)
    public AudioClip[] songs;              // drag songs here

    private static GameMusicManager _instance; // scene-scoped singleton
    private int lastIndex = -1;

    // ---- LIFECYCLE ----
    private void Awake()
    {
        // If another instance already persists, this one is a duplicate from the scene reload—kill it.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Only persist if we are *currently* in the MainGame scene.
        if (SceneManager.GetActiveScene().name == mainGameSceneName)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }
        // If we get spawned in a non-MainGame scene (e.g., accidentally placed on TitleScreen), do not persist.
    }

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = false; // we'll handle track changes
            audioSource.playOnAwake = false;
        }

        // Only start music if we are the live instance
        if (_instance == this)
        {
            // If nothing is playing (first boot), start a song
            if (!audioSource.isPlaying)
                PlayNextSong();
        }
    }

    private void Update()
    {
        if (_instance != this) return; // ignore duplicates
        if (!audioSource.isPlaying)
        {
            PlayNextSong();
        }
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    // ---- SCENE HANDOFF ----
    private void OnActiveSceneChanged(Scene prev, Scene next)
    {
        // If we left MainGame, kill the manager so it doesn't bleed into TitleScreen
        if (next.name != mainGameSceneName && _instance == this)
        {
            // Optional: fade out here if you like, then Destroy
            Destroy(gameObject);
            _instance = null;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }
        // If we reloaded MainGame, this instance stays. Any newly-instantiated copy in the scene will destroy itself in Awake.
    }

    // ---- MUSIC LOGIC ----
    private void PlayNextSong()
    {
        if (songs == null || songs.Length == 0 || audioSource == null) return;

        int newIndex;
        if (songs.Length == 1)
        {
            newIndex = 0;
        }
        else
        {
            do { newIndex = Random.Range(0, songs.Length); }
            while (newIndex == lastIndex);
        }

        lastIndex = newIndex;
        audioSource.clip = songs[newIndex];
        audioSource.Play();
    }
}
