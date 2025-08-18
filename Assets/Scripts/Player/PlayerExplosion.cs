using UnityEngine;
using System.Collections;
using System.Linq;

public class PlayerExplosion : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float framesPerSecond = 15f;     // Adjust for slower/faster explosions
    [SerializeField] private string folderPath = "VFX/Explosion";

    private SpriteRenderer spriteRenderer;
    private Sprite[] sprites;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        var loadedSprites = Resources.LoadAll<Sprite>(folderPath);

        // only keep sprites whose name ends in exactly "0" or "_0"
        sprites = loadedSprites
            .Where(s => s.name.EndsWith("0"))
            .OrderBy(s => s.name)
            .ToArray();
    }

    private void OnEnable()
    {
        StartCoroutine(PlayExplosion());
    }

    private IEnumerator PlayExplosion()
    {
        float frameDuration = 1f / framesPerSecond;

        for (int i = 0; i < sprites.Length; i++)
        {
            spriteRenderer.sprite = sprites[i];
            yield return new WaitForSeconds(frameDuration);
        }

        Destroy(gameObject);   // explosion finished
    }
}
