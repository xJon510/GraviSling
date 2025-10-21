using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class PlayerExplosionImage : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float framesPerSecond = 15f;   // Adjust for slower/faster explosions
    [SerializeField] private string folderPath = "VFX/Explosion";

    private Image image;
    private Sprite[] sprites;

    private void Awake()
    {
        image = GetComponent<Image>();

        var loadedSprites = Resources.LoadAll<Sprite>(folderPath);

        // Only keep sprites whose name ends with "0" or "_0" for consistency
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
        if (sprites == null || sprites.Length == 0)
            yield break;

        float frameDuration = 1f / framesPerSecond;

        for (int i = 0; i < sprites.Length; i++)
        {
            image.sprite = sprites[i];
            yield return new WaitForSeconds(frameDuration);
        }

        Destroy(gameObject); // Remove explosion when finished
    }
}
