using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIPipEmitter : MonoBehaviour
{
    public static UIPipEmitter Instance { get; private set; }
    void OnEnable()
    {
        Instance = this;
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    [Header("UI")]
    [SerializeField] RectTransform container;   // parent under Overlay canvas
    [SerializeField] Sprite pipSprite;
    [SerializeField] int poolSize = 64;

    [Header("Emit")]
    [SerializeField] float emitRate = 30f;      // pips/sec
    [SerializeField] Vector2 startSize = new Vector2(8f, 14f);
    [SerializeField] float lifetime = 0.6f;
    [SerializeField] Vector2 startSpeed = new Vector2(80f, 140f);
    [SerializeField] Vector2 dirJitter = new Vector2(-15f, 15f); // degrees
    [SerializeField] Color startColor = new Color(1, 1, 1, 0.9f);
    //[SerializeField] Color endColor = new Color(1, 1, 1, 0.0f);

    private struct Pip
    {
        public RectTransform rt;
        public Vector2 v;
        public float t;
        public float T;
        public Color a, b;
        public Vector2 s0, s1;
        public bool alive;
    }

    private List<Pip> pool;
    private float emitAcc;

    void Awake()
    {
        if (!container) container = (RectTransform)transform;

        pool = new List<Pip>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject("Pip", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(container, false);

            var img = go.GetComponent<Image>();
            img.sprite = pipSprite;
            img.raycastTarget = false;

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.one * startSize.x; // initial size; will be overridden per pip
            go.SetActive(false);

            Pip p = new Pip { rt = rt, alive = false };
            pool.Add(p);
        }
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Emit
        emitAcc += emitRate * dt;
        while (emitAcc >= 1f)
        {
            EmitOne();
            emitAcc -= 1f;
        }

        // Update
        for (int i = 0; i < pool.Count; i++)
        {
            Pip p = pool[i];
            if (!p.alive) continue;

            p.t += dt;
            float k = Mathf.Clamp01(p.t / p.T);

            // move
            Vector2 pos = p.rt.anchoredPosition + p.v * dt;
            p.rt.anchoredPosition = pos;

            // color & size
            var img = p.rt.GetComponent<Image>();
            img.color = Color.LerpUnclamped(p.a, p.b, k);
            p.rt.sizeDelta = Vector2.LerpUnclamped(p.s0, p.s1, k);

            // kill
            if (p.t >= p.T)
            {
                p.alive = false;
                p.rt.gameObject.SetActive(false);
            }

            pool[i] = p;
        }
    }

    private void EmitOne()
    {
        int idx = -1;
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].alive) { idx = i; break; }
        }
        if (idx < 0) return;

        Pip p = pool[idx];
        p.alive = true;
        p.t = 0f;
        p.T = lifetime;

        // spawn at this GameObject's anchoredPosition (same canvas space)
        var selfRT = (RectTransform)transform;
        p.rt.anchoredPosition = selfRT.anchoredPosition;

        // random dir/speed
        float angDeg = Random.Range(dirJitter.x, dirJitter.y);
        float ang = angDeg * Mathf.Deg2Rad;
        float spd = Random.Range(startSpeed.x, startSpeed.y);
        Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
        p.v = dir * spd;

        // color/size over life
        p.a = startColor;
        p.b = startColor;
        float s0 = Random.Range(startSize.x, startSize.y);
        p.s0 = Vector2.one * s0;
        p.s1 = Vector2.one * (s0 * 0.5f);

        p.rt.gameObject.SetActive(true);
        pool[idx] = p;
    }

    // --- Public API: set the color from a TrailCard selection ---
    public void SetStartColor(Color c, bool retintAlive = true)
    {
        startColor = c;

        if (!retintAlive) return;

        // Optionally recolor currently alive pips immediately so the change feels instant.
        for (int i = 0; i < pool.Count; i++)
        {
            var p = pool[i];
            if (!p.alive) continue;

            // keep the same alpha the pip already had at its current progress
            var img = p.rt.GetComponent<Image>();
            float currentAlpha = img.color.a;

            p.a = new Color(c.r, c.g, c.b, p.a.a);
            p.b = new Color(c.r, c.g, c.b, p.b.a);

            img.color = new Color(c.r, c.g, c.b, currentAlpha);

            pool[i] = p;
        }
    }
}
