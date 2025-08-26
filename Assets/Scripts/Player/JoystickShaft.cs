using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws/positions a UI "shaft" (thin Image) between the LargeHolder center
/// and the SmallCircle (knob). Works for Overlay / Camera / World Space canvases.
/// Place "Shaft" above LargeHolder and below SmallCircle in hierarchy.
/// </summary>
public class JoystickShaft : MonoBehaviour
{
    [Header("Assign")]
    public RectTransform largeHolder;   // big circle (center)
    public RectTransform smallCircle;   // knob
    public RectTransform shaft;         // thin Image to stretch/rotate
    public RectTransform coordSpace;    // usually largeHolder.parent (shared parent space)

    [Header("Look")]
    public float thickness = 16f;       // shaft width (px)
    public float centerInset = 8f;      // how far to pull back from center
    public float knobInset = 14f;       // how far to pull back from knob (so it tucks under)

    [Header("Behavior")]
    public bool hideWhenCentered = true; // hide when knob is near center
    public float minVisibleLength = 6f;

    Canvas canvas;
    Camera uiCam;

    void Awake()
    {
        if (!coordSpace) coordSpace = largeHolder ? largeHolder.parent as RectTransform : null;
        canvas = GetComponentInParent<Canvas>();
        uiCam = (canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        // Nice default: round caps sprite, semi-transparent
        var img = shaft ? shaft.GetComponent<Image>() : null;
        if (img && img.sprite == null) img.sprite = UnityEngine.Resources.GetBuiltinResource<Sprite>("UISprite.psd");
        if (img) img.raycastTarget = false;
    }

    void LateUpdate()
    {
        if (!largeHolder || !smallCircle || !shaft || !coordSpace) return;

        // Get both endpoints in coordSpace local coordinates
        Vector3 centerWorld = largeHolder.TransformPoint(Vector3.zero);
        Vector3 knobWorld = smallCircle.TransformPoint(smallCircle.rect.center);

        Vector2 centerLocal = coordSpace.InverseTransformPoint(centerWorld);
        Vector2 knobLocal = coordSpace.InverseTransformPoint(knobWorld);

        Vector2 dir = knobLocal - centerLocal;
        float len = dir.magnitude;

        if (hideWhenCentered && len <= minVisibleLength)
        {
            if (shaft.gameObject.activeSelf) shaft.gameObject.SetActive(false);
            return;
        }
        if (!shaft.gameObject.activeSelf) shaft.gameObject.SetActive(true);

        // Inset from both ends so it tucks under the circles
        float trimmed = Mathf.Max(0f, len - (centerInset + knobInset));
        Vector2 dirN = (len > 0.0001f) ? (dir / len) : Vector2.right;

        Vector2 start = centerLocal + dirN * centerInset;
        Vector2 end = knobLocal - dirN * knobInset;
        Vector2 mid = (start + end) * 0.5f;
        float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;

        // Size & place the shaft (pivot at center for easy rotation)
        shaft.pivot = new Vector2(0.5f, 0.5f);
        shaft.sizeDelta = new Vector2(Mathf.Max(trimmed, 0.0001f), thickness);
        shaft.anchoredPosition = mid;
        shaft.localRotation = Quaternion.Euler(0, 0, angle);
    }

    // Optional: call from your joystick show/hide to keep visual in sync
    public void ForceHide()
    {
        if (shaft) shaft.gameObject.SetActive(false);
    }
}
