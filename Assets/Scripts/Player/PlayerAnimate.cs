using UnityEngine;
using System.Linq;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimate : MonoBehaviour
{
    [Header("Resource Path: Resources/Player/Spaceship")]
    [SerializeField] private string folderPath = "Player/Spaceship";
    [SerializeField] private float framesPerSecond = 8f;

    [Range(0f, 1f)]
    public float thrustLevel = 0f;

    [Tooltip("Frames [0..lowThrustMaxFrame] loop for idle/low thrust.  Beyond that used for high thrust.")]
    [SerializeField] private int lowThrustMaxFrame = 2;

    private SpriteRenderer sr;
    private Sprite[] sprites;
    private int currentFrame;
    private float timer;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Load and alphabetically sort like your explosion script
        sprites = Resources.LoadAll<Sprite>(folderPath)
                           .OrderBy(s => s.name)
                           .ToArray();

        if (sprites == null || sprites.Length == 0)
            Debug.LogError($"PlayerAnimate: No frames found at Resources/{folderPath}");
    }

    private void Update()
    {
        if (sprites == null || sprites.Length == 0)
            return;

        timer += Time.deltaTime;
        if (timer >= 1f / framesPerSecond)
        {
            timer -= 1f / framesPerSecond;

            if (thrustLevel < 0.5f)
            {
                // Low thrust: loop 0..lowThrustMaxFrame
                currentFrame++;
                if (currentFrame > lowThrustMaxFrame)
                    currentFrame = 0;
            }
            else
            {
                // High thrust: loop ONLY frames 3 & 4
                if (currentFrame < 3 || currentFrame > 4)
                    currentFrame = 3;  // force start of loop

                currentFrame = (currentFrame == 3) ? 4 : 3;
            }

            sr.sprite = sprites[currentFrame];
        }
    }
}
