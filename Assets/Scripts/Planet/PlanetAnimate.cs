using UnityEngine;

public class PlanetAnimate : MonoBehaviour
{
    [Header("Folder name under Resources/Planets/")]
    public string planetFolder = "Moon";    // e.g. "Moon" -> will load Resources/Planets/Moon/*

    [Tooltip("Use every Nth frame (2 = every other frame)")]
    public int skip = 2;                    // 2 = 30 from 60   |   2 = 60 from 120

    public float framesPerSecond = 8f;

    private Sprite[] frames;
    private SpriteRenderer sr;
    private int frameCount = 0;
    private int currentFrame = 0;
    private float timer = 0f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Auto-load all sprites from Resources/Planets/<planetFolder>
        var loaded = Resources.LoadAll<Sprite>($"Planets/{planetFolder}");
        if (loaded == null || loaded.Length == 0)
        {
            Debug.LogError($"PlanetAnimate: No sprites found in Resources/Planets/{planetFolder}");
            return;
        }

        // Sort by name so 0,1,2,... loads in correct order
        System.Array.Sort(loaded, (a, b) =>
        {
            // extract leading numbers from the sprite name (before '_')
            int numA = int.Parse(a.name.Split('_')[0]);
            int numB = int.Parse(b.name.Split('_')[0]);
            return numA.CompareTo(numB);
        });

        // Apply skip logic
        var tempList = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; i < loaded.Length; i += skip)
        {
            tempList.Add(loaded[i]);
        }
        frames = tempList.ToArray();

        frameCount = frames.Length;
        Debug.Log($"Loaded {frameCount} frames for {planetFolder} (skip = {skip})");
    }

    private void Update()
    {
        if (frameCount == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / framesPerSecond)
        {
            timer -= 1f / framesPerSecond;
            currentFrame = (currentFrame + 1) % frameCount;
            sr.sprite = frames[currentFrame];
        }
    }
}
