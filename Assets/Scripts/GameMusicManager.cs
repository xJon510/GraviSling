using UnityEngine;

public class GameMusicManager : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;        // assign in inspector
    public AudioClip[] songs;              // drag songs here

    private int lastIndex = 0;            // store last played index

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = false; // we'll handle looping ourselves
        }

        PlayNextSong();
    }

    void Update()
    {
        if (!audioSource.isPlaying)
        {
            PlayNextSong();
        }
    }

    private void PlayNextSong()
    {
        if (songs.Length == 0) return;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, songs.Length);
        } while (newIndex == lastIndex && songs.Length > 1);

        lastIndex = newIndex;
        audioSource.clip = songs[newIndex];
        audioSource.Play();
    }
}
