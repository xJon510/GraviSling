using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class TitleTextFloat : MonoBehaviour
{
    [Header("Bob (whole title)")]
    public bool bobEnabled = true;
    public float bobAmplitude = 8f;         // pixels/units (UI vs world)
    public float bobSpeed = 1.0f;           // cycles per second
    public float bobPhase = 0f;             // radians

    [Header("Wave (per-letter)")]
    public bool waveEnabled = true;
    public float waveInterval = 4f;         // seconds between wave triggers
    public float waveDuration = 0.9f;       // seconds wave remains active
    public float waveAmplitude = 8f;        // letter vertical offset
    public float waveSpeed = 8f;            // how fast the wave travels
    public float phasePerCharacter = 0.5f;  // radians shift per character
    public AnimationCurve waveEnvelope = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("General")]
    public bool reducedMotion = false;      // quick kill-switch for motion
    public bool useUnscaledTime = true;     // animate even if timescale changes

    [Header("TextMeshPro (optional)")]
    public TMP_Text tmp;                    // auto-found if null

    // --- internals ---
    Vector3 _baseLocalPos;
    float _waveTimer = 0f;
    float _waveActiveT = -1f;               // -1 => inactive, else [0, waveDuration]
    float _t => useUnscaledTime ? Time.unscaledTime : Time.time;
    float _dt => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

    struct MeshCache { public Vector3[] verts; }
    readonly List<MeshCache> _original = new();
    int _lastCharCount = -1;

    public RectTransform _rect;

    void Reset() { CacheBasePos(); }
    void OnEnable()
    {
        TryFindTMP();
        RebuildCacheIfNeeded(force: true);
        _rect = GetComponent<RectTransform>();
    }

    void Start()
    {
        if (Application.isPlaying)
            CacheBasePos();
    }

    void OnDisable()
    {
        // restore original mesh if we had modified it
        RestoreOriginalVerts();
    }

    void CacheBasePos()
    {
        if (_rect != null)
            _baseLocalPos = _rect.anchoredPosition;  // store as Vector2 in Vector3.y (x,y used)
        else
            _baseLocalPos = transform.localPosition;
    }

    void TryFindTMP()
    {
        if (!tmp) tmp = GetComponent<TMP_Text>();
    }

    void RebuildCacheIfNeeded(bool force = false)
    {
        if (!tmp) return;
        if (force) _lastCharCount = -1;

        tmp.ForceMeshUpdate();
        var ti = tmp.textInfo;
        if (!ti.meshInfo?.Length.Equals(ti.meshInfo.Length) ?? true) return;

        int charCount = ti.characterCount;
        if (_lastCharCount == charCount && _original.Count == ti.meshInfo.Length) return;

        _original.Clear();
        for (int i = 0; i < ti.meshInfo.Length; i++)
        {
            var src = ti.meshInfo[i].vertices;
            var copy = new Vector3[src.Length];
            System.Array.Copy(src, copy, src.Length);
            _original.Add(new MeshCache { verts = copy });
        }
        _lastCharCount = charCount;
    }

    void RestoreOriginalVerts()
    {
        if (!tmp) return;
        tmp.ForceMeshUpdate();
        var ti = tmp.textInfo;
        for (int i = 0; i < ti.meshInfo.Length && i < _original.Count; i++)
        {
            var mi = ti.meshInfo[i];
            if (_original[i].verts == null || mi.vertices == null) continue;
            System.Array.Copy(_original[i].verts, mi.vertices, mi.vertices.Length);
            var mesh = mi.mesh;
            mesh.vertices = mi.vertices;
            tmp.UpdateGeometry(mesh, i);
        }
    }

    void Update()
    {
        if (!Application.isPlaying)
        {
            // EDIT MODE: only maintain the text mesh (no bobbing, no transform writes)
            RebuildCacheIfNeeded();
            ApplyWave(0f);            // safe for mesh
            return;                   // <-- stop here; don't call ApplyBob()
        }

        if (reducedMotion) { ApplyBob(0f); DeactivateWave(); return; }

        ApplyBob(_t);
        HandleWaveTimers();
        RebuildCacheIfNeeded();
        ApplyWave(_t);
    }

    void ApplyBob(float timeNow)
    {
        if (!bobEnabled)
            return;

        float y = bobAmplitude * Mathf.Sin((Mathf.PI * 2f * bobSpeed) * timeNow + bobPhase);

        if (_rect != null)
        {
            var ap = (Vector2)_baseLocalPos;           // we stored anchoredPosition here
            ap.y += y;
            _rect.anchoredPosition = ap;
        }
        else
        {
            var lp = _baseLocalPos;
            lp.y += y;
            transform.localPosition = lp;
        }
    }

    void HandleWaveTimers()
    {
        if (!waveEnabled) { DeactivateWave(); return; }

        _waveTimer += _dt;

        if (_waveActiveT >= 0f)
        {
            _waveActiveT += _dt;
            if (_waveActiveT >= waveDuration)
                DeactivateWave();
        }
        else
        {
            if (_waveTimer >= Mathf.Max(0.01f, waveInterval))
            {
                _waveTimer = 0f;
                _waveActiveT = 0f; // activate!
            }
        }
    }

    void DeactivateWave()
    {
        _waveActiveT = -1f;
        // restore mesh to original when the wave ends
        RestoreOriginalVerts();
    }

    void ApplyWave(float timeNow)
    {
        if (!tmp) return;

        // If wave inactive, ensure original verts are displayed
        if (_waveActiveT < 0f || !waveEnabled)
        {
            RestoreOriginalVerts();
            return;
        }

        // Envelope 0..1 over waveDuration
        float t01 = Mathf.Clamp01(_waveActiveT / Mathf.Max(0.001f, waveDuration));
        float env = waveEnvelope != null ? waveEnvelope.Evaluate(t01) : (1f - t01);

        var ti = tmp.textInfo;

        // Start from original verts every frame to avoid drift
        for (int i = 0; i < ti.meshInfo.Length && i < _original.Count; i++)
        {
            var src = _original[i].verts;
            var dst = ti.meshInfo[i].vertices;
            if (src == null || dst == null || dst.Length != src.Length) continue;
            System.Array.Copy(src, dst, src.Length);
        }

        // Apply per-character Y offsets
        for (int c = 0; c < ti.characterCount; c++)
        {
            var ch = ti.characterInfo[c];
            if (!ch.isVisible) continue;

            int mIdx = ch.materialReferenceIndex;
            int vIdx = ch.vertexIndex;

            // wave offset for this character
            float phase = phasePerCharacter * c;
            float yOff = waveAmplitude * env * Mathf.Sin( (timeNow * waveSpeed) + phase );

            var verts = ti.meshInfo[mIdx].vertices;
            verts[vIdx + 0].y += yOff;
            verts[vIdx + 1].y += yOff;
            verts[vIdx + 2].y += yOff;
            verts[vIdx + 3].y += yOff;
        }

        // push updated verts back to meshes
        for (int i = 0; i < ti.meshInfo.Length; i++)
        {
            var mesh = ti.meshInfo[i].mesh;
            mesh.vertices = ti.meshInfo[i].vertices;
            tmp.UpdateGeometry(mesh, i);
        }
    }
}
