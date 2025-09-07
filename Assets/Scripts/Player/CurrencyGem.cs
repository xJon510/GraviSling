using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CurrencyGem : MonoBehaviour
{
    public int value = 1;
    public float OffsetMultiplier = 1.5f;

    [Header("Animation")]
    public float flyDuration = 0.35f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.6f, 1, 1.1f);

    // injected
    private Transform player;
    private GemRecycleSimple manager;

    // state
    private bool collected = false;
    private float t = 0f;
    private Vector3 startPos;
    private Vector3 originalScale;
    private Rigidbody2D rb;
    private Collider2D col;
    private Vector3 flyOffset;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        originalScale = transform.localScale;
    }

    public void SetPlayer(Transform p) => player = p;       // manager injects
    public void SetManager(GemRecycleSimple m) => manager = m; // manager injects

    public void Collect()
    {
        if (collected) return;

        // If no player yet (e.g., cutscene), just bank immediately.
        if (player == null)
        {
            BankAndRecycle();
            return;
        }

        collected = true;
        if (rb) rb.simulated = false;
        if (col) col.enabled = false;

        startPos = transform.position;
        t = 0f;
        flyOffset = Random.insideUnitCircle.normalized * OffsetMultiplier;
    }

    void Update()
    {
        if (!collected) return;

        // If player becomes available mid-animation, continue; otherwise, finish instantly.
        if (player == null)
        {
            BankAndRecycle();
            return;
        }

        t += Time.deltaTime / flyDuration;
        if (t >= 1f) { BankAndRecycle(); return; }

        Vector3 target = Vector3.Lerp(startPos + (Vector3)flyOffset, player.position, t);
        transform.position = target;

        float s = scaleCurve.Evaluate(t);
        transform.localScale = originalScale * s;
    }

    void BankAndRecycle()
    {
        // update totals
        int lifetime = PlayerPrefs.GetInt("currency", 0);
        PlayerPrefs.SetInt("currency", lifetime + value);

        int run = PlayerPrefs.GetInt("gemsThisRun", 0);
        PlayerPrefs.SetInt("gemsThisRun", run + value);
        PlayerPrefs.Save();

        collected = false; // clear before recycle

        if (manager != null)
        {
            manager.Recycle(this);
        }
        else
        {
            // Fallback (shouldn't happen): just reset locally
            ResetForReuse();
        }
    }

    // Called by manager after re-positioning to make this gem active again
    public void ResetForReuse()
    {
        collected = false;
        t = 0f;
        transform.localScale = originalScale;
        if (col) { col.enabled = true; }
        if (rb) { rb.simulated = true; }
    }
}
