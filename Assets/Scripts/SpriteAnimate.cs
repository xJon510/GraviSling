using UnityEngine;
using System.Linq;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimate : MonoBehaviour
{
    [Header("Resource Path (under Resources/)")]
    [SerializeField] private string folderPath = "";
    [SerializeField] private float framesPerSecond = 10f;

    private SpriteRenderer sr;
    private Sprite[] frames;
    private int currentFrame;
    private float timer;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Load all sprites inside Resources/folderPath
        frames = Resources.LoadAll<Sprite>(folderPath)
                          .OrderBy(s => s.name)
                          .ToArray();

        if (frames == null || frames.Length == 0)
        {
            Debug.LogError($"SpriteAnimate: No sprites found at Resources/{folderPath}");
        }
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0)
            return;

        timer += Time.deltaTime;
        if (timer >= 1f / framesPerSecond)
        {
            timer -= 1f / framesPerSecond;

            currentFrame++;
            if (currentFrame >= frames.Length)
                currentFrame = 0;

            sr.sprite = frames[currentFrame];
        }
    }
}
