using UnityEngine;

public class CurrencyGem : MonoBehaviour
{
    public int value = 1;   // could be used later for rare gems
    private bool collected = false;
    private Transform player;

    // Animation settings
    public float flyDuration = 0.35f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.6f, 1, 1.1f);
    private float t = 0f;
    private Vector3 startPos;
    private Vector3 originalScale;

    private void Awake()
    {
        player = GameObject.FindWithTag("Player").transform;
        originalScale = transform.localScale;
    }

    public void Collect()
    {
        if (collected) return;
        collected = true;

        // Disable physics so nothing bumps it around
        var rb = GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false;

        // Disable collider so nothing else triggers
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        startPos = transform.position;
        t = 0f;
    }

    private void Update()
    {
        if (!collected) return;

        if (player == null)
        {
            FinishCollect();  // fallback
            return;
        }

        t += Time.deltaTime / flyDuration;
        if (t >= 1f)
        {
            FinishCollect();
            return;
        }

        // Move towards player
        transform.position = Vector3.Lerp(startPos, player.position, t);

        // Scale "pop"
        float s = scaleCurve.Evaluate(t);
        transform.localScale = originalScale * s;
    }


    void FinishCollect()
    {
        // Lifetime currency
        int lifetime = PlayerPrefs.GetInt("currency", 0);
        PlayerPrefs.SetInt("currency", lifetime + value);

        // This-run currency
        int run = PlayerPrefs.GetInt("gemsThisRun", 0);
        PlayerPrefs.SetInt("gemsThisRun", run + value);

        PlayerPrefs.Save();
        Destroy(gameObject);
    }

}
