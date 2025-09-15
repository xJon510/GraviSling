using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
public class StartFull : MonoBehaviour
{
    [Range(0f, 1f)]
    public float target = 1f;

    [Tooltip("If true, also set the parent ScrollRect's normalized position.")]
    public bool alsoAffectScrollRect = true;

    [Tooltip("Optional. If not set, will try GetComponentInParent<ScrollRect>().")]
    public ScrollRect scrollRect;

    private Scrollbar scrollbar;

    void Reset()
    {
        scrollbar = GetComponent<Scrollbar>();
        if (!scrollRect) scrollRect = GetComponentInParent<ScrollRect>();
    }

    void OnEnable()
    {
        GrabRefs();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            // In editor (not playing), apply immediately so Inspector changes stick.
            ApplyImmediate();
            return;
        }
#endif
        // In play mode, defer a frame so layout/ScrollRect doesn't overwrite us.
        StartCoroutine(ApplyNextFrame());
    }

    void Start()
    {
        // Safety net if object was enabled before layout was ready.
        if (Application.isPlaying) StartCoroutine(ApplyNextFrame());
    }

    void OnValidate()
    {
        // Make it snap in the editor when you tweak values in Inspector.
        GrabRefs();
#if UNITY_EDITOR
        if (!Application.isPlaying) ApplyImmediate();
#endif
    }

    private void GrabRefs()
    {
        if (!scrollbar) scrollbar = GetComponent<Scrollbar>();
        if (!scrollRect) scrollRect = GetComponentInParent<ScrollRect>();
    }

    private IEnumerator ApplyNextFrame()
    {
        yield return null; // wait one frame
        Canvas.ForceUpdateCanvases(); // ensure layout done
        ApplyImmediate();
    }

    private void ApplyImmediate()
    {
        if (scrollbar)
        {
            float v = target;
            // Respect reversed directions so 1 still means “end” you intend.
            if (scrollbar.direction == Scrollbar.Direction.RightToLeft ||
                scrollbar.direction == Scrollbar.Direction.BottomToTop)
            {
                v = 1f - target;
            }

            // Avoid sending change callbacks if listeners reposition stuff.
            scrollbar.SetValueWithoutNotify(v);
        }

        if (alsoAffectScrollRect && scrollRect)
        {
            // Note: for vertical ScrollRects, 1 = top, 0 = bottom (Unity convention).
            if (scrollRect.vertical)
                scrollRect.verticalNormalizedPosition = target;

            if (scrollRect.horizontal)
                scrollRect.horizontalNormalizedPosition = target;
        }
    }
}
