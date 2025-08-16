using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BkRndAnimate : MonoBehaviour
{
    public List<Sprite> frames;      // assign your PNGs here
    public float framesPerSecond = 4f;

    private Image img;
    private int currentFrame = 0;
    private float timer = 0f;

    private void Awake()
    {
        img = GetComponent<Image>();
    }

    private void Update()
    {
        if (frames == null || frames.Count == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / framesPerSecond)
        {
            timer -= 1f / framesPerSecond;

            currentFrame = (currentFrame + 1) % frames.Count;
            img.sprite = frames[currentFrame];
        }
    }
}
