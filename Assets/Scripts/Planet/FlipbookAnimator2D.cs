using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FlipbookAnimator2D : MonoBehaviour
{
    [Header("Atlas grid")]
    public int columns = 10;
    public int rows = 6;
    public int totalFrames = 60;

    [Header("Playback")]
    public float fps = 4f;
    public bool randomStartPhase = true;

    [Header("Visibility")]
    [Tooltip("Extra viewport margin around the screen where we keep animating. " +
             "0 = only animate when actually on screen. 0.1 = start/stop ~10% before/after.")]
    [Range(0f, 0.5f)] public float viewportPadding = 0.08f;
    public Camera targetCamera; // leave null to use Camera.main every frame

    Renderer rend;
    MaterialPropertyBlock mpb;
    int lastFrame = -1;
    float phase;

    static readonly int ID_Frame = Shader.PropertyToID("_Frame");
    static readonly int ID_Cols = Shader.PropertyToID("_Cols");
    static readonly int ID_Rows = Shader.PropertyToID("_Rows");

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();

        rend.GetPropertyBlock(mpb);
        mpb.SetFloat(ID_Cols, columns);
        mpb.SetFloat(ID_Rows, rows);
        rend.SetPropertyBlock(mpb);

        phase = randomStartPhase ? Random.value : 0f;
    }

    void Update()
    {
        if (totalFrames <= 1 || fps <= 0f) return;
        var cam = targetCamera ? targetCamera : Camera.main;
        if (!cam) return;

        // Visibility with padding
        if (!VisibleWithPadding(cam, rend.bounds, viewportPadding))
            return; // pause: don’t advance frames or touch MPB

        // Compute deterministic frame; only touch MPB on change
        int frame = Mathf.FloorToInt((Time.time + phase) * fps) % totalFrames;
        if (frame == lastFrame) return;

        lastFrame = frame;
        rend.GetPropertyBlock(mpb);
        mpb.SetFloat(ID_Frame, frame);
        rend.SetPropertyBlock(mpb);
    }

    // Checks if any part of bounds is inside viewport rectangle expanded by padding.
    static bool VisibleWithPadding(Camera cam, Bounds b, float pad)
    {
        // Quick reject by depth
        Vector3 center = cam.WorldToViewportPoint(b.center);
        if (center.z < 0f) return false;

        // Project 8 corners to capture large bounds near edges
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
