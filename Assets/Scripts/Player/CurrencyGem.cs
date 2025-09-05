using UnityEngine;

public class CurrencyGem : MonoBehaviour
{
    public int value = 1;   // could be used later for rare gems
    public float OffsetMultiplier = 1.5f;
    private bool collected = false;

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Animation")]
    // Animation settings
    public float flyDuration = 0.35f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.6f, 1, 1.1f);
    private float t = 0f;
    private Vector3 startPos;
    private Vector3 originalScale;
    private Rigidbody2D rb;
    private Collider2D col;
    private Vector3 flyOffset;


    private void Awake()
    {
        originalScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;

        if (rb) rb.simulated = false;
        if (col) col.enabled = false;

        startPos = transform.position;
        t = 0f;

        // Give a random "push outwards" radius
        flyOffset = Random.insideUnitCircle.normalized * OffsetMultiplier;
    }

    private void Update()
    {
        if (!collected || player == null) return;

        t += Time.deltaTime / flyDuration;
        if (t >= 1f)
        {
            FinishCollect();
            return;
        }

        // Start position -> outward offset -> player
        Vector3 target = Vector3.Lerp(startPos + (Vector3)flyOffset, player.position, t);
        transform.position = target;

        float s = scaleCurve.Evaluate(t);
        transform.localScale = originalScale * s;
    }

    void FinishCollect()
    {
        int lifetime = PlayerPrefs.GetInt("currency", 0);
        PlayerPrefs.SetInt("currency", lifetime + value);

        int run = PlayerPrefs.GetInt("gemsThisRun", 0);
        PlayerPrefs.SetInt("gemsThisRun", run + value);

        PlayerPrefs.Save();
        Destroy(gameObject);
    }
}
