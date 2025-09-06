using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class MeshAnimator : MonoBehaviour
{
    [Header("Atlas grid (material uses these)")]
    public int columns = 10;
    public int rows = 6;
    public int totalFrames = 60; // usually columns * rows

    [Header("Playback")]
    public float fps = 4f;
    public bool randomStartPhase = true;

    [Header("Visibility")]
    [Tooltip("Extra viewport margin around the screen where we keep animating. " +
             "0 = only animate when actually on screen. 0.1 = start/stop ~10% before/after.")]
    [Range(0f, 0.5f)] public float viewportPadding = 0.08f;
    public Camera targetCamera; // leave null to use Camera.main

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;
    private int _lastFrame = -1;
    private float _phase;

    // Shader property IDs (must match instanced props in shader)
    private static readonly int ID_IFrame = Shader.PropertyToID("_IFrame");
    private static readonly int ID_ITint = Shader.PropertyToID("_ITint");

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();

        // Ensure a visible default tint
        _mpb.SetColor(ID_ITint, Color.white);
        _renderer.SetPropertyBlock(_mpb);

        if (totalFrames <= 0)
            totalFrames = Mathf.Max(1, columns * rows);

        _phase = randomStartPhase ? Random.value : 0f;
    }

    void Update()
    {
        if (totalFrames <= 1 || fps <= 0f) return;

        var cam = targetCamera ? targetCamera : Camera.main;
        if (!cam) return;

        if (!VisibleWithPadding(cam, _renderer.bounds, viewportPadding))
            return;

        int frame = Mathf.FloorToInt((Time.time + _phase) * fps) % totalFrames;
        if (frame == _lastFrame) return;

        _lastFrame = frame;

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(ID_IFrame, frame);  // update instanced frame
        _renderer.SetPropertyBlock(_mpb);
    }

    static bool VisibleWithPadding(Camera cam, Bounds b, float pad)
    {
        Vector3 center = cam.WorldToViewportPoint(b.center);
        if (center.z < 0f) return false;

        Vector3 ext = b.extents;
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 p = b.center + Vector3.Scale(ext, new Vector3(x, y, z));
                    Vector3 v = cam.WorldToViewportPoint(p);
                    if (v.z > 0f &&
                        v.x >= -pad && v.x <= 1f + pad &&
                        v.y >= -pad && v.y <= 1f + pad)
                        return true;
                }
        return false;
    }
}
