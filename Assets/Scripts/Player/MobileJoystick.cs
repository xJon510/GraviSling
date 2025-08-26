using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MobileJoystick : MonoBehaviour
{
    [Header("Refs (assign)")]
    public RectTransform rootCanvasRect;   // Usually the top-level ScreenSpace canvas
    public RectTransform holder;           // Big background circle (this object or child)
    public RectTransform knob;             // "SmallCircle" child
    public CanvasGroup canvasGroup;        // On the same holder (for fade)

    [Header("Tuning")]
    public float maxRadius = 120f;         // px radius for full input (knob clamp)
    public float deadZone = 0.08f;         // in 0..1 space
    public float fadeOutTime = 0.12f;

    Camera uiCam;
    Vector2 holderCenterLocal; // local point in rootCanvasRect space
    RectTransform parentRect;
    Canvas canvas;
    bool isWorldSpace;

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (!canvas) canvas = rootCanvasRect ? rootCanvasRect.GetComponentInParent<Canvas>() : null;

        if (!rootCanvasRect && canvas) rootCanvasRect = canvas.GetComponent<RectTransform>();
        if (!holder) holder = (RectTransform)transform;
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();

        isWorldSpace = canvas && canvas.renderMode == RenderMode.WorldSpace;
        uiCam = null;
        if (canvas)
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) uiCam = null;
            else uiCam = canvas.worldCamera; // ScreenSpace-Camera or WorldSpace
        }

        parentRect = holder.parent as RectTransform;
    }

    public void ShowAtScreenPosition(Vector2 screenPos)
    {
        // Reset input & visuals first
        knob.anchoredPosition = Vector2.zero;
        JoystickInput.SetVector(Vector2.zero);
        JoystickInput.SetPressed(true);

        // Position the holder at the finger position, in the *parent* space
        if (!isWorldSpace)
        {
            // Overlay or ScreenSpace-Camera -> anchoredPosition in parent rect space
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, uiCam, out var local))
            {
                holder.anchoredPosition = local;
                holderCenterLocal = local; // store center in same space for drags
            }
        }
        else
        {
            // World Space Canvas -> set world position on the holder plane
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screenPos, uiCam, out var world))
            {
                holder.position = world;
                // Convert that world pos back to parent local for drag math
                holderCenterLocal = parentRect.InverseTransformPoint(world);
            }
        }

        // Show instantly
        StopAllCoroutines();
        if (canvasGroup) canvasGroup.alpha = 1f;

        gameObject.SetActive(true);
    }

    public void UpdateDrag(Vector2 screenPos)
    {
        Vector2 nowLocal;

        if (!isWorldSpace)
        {
            // Overlay / ScreenSpace-Camera
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, uiCam, out nowLocal))
                return;
        }
        else
        {
            // World Space -> convert to world, then to parent local
            if (!RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screenPos, uiCam, out var world))
                return;
            nowLocal = parentRect.InverseTransformPoint(world);
        }

        // Delta in the *same local space* as holder/knob
        Vector2 delta = nowLocal - holderCenterLocal;

        // Clamp to max radius
        Vector2 clamped = Vector2.ClampMagnitude(delta, maxRadius);
        knob.anchoredPosition = clamped;

        // 0..1 vector
        Vector2 v01 = clamped / Mathf.Max(1f, maxRadius);
        if (v01.magnitude < deadZone) v01 = Vector2.zero;

        JoystickInput.SetVector(v01);
        JoystickInput.SetPressed(v01 != Vector2.zero);
    }

    public void Release()
    {
        // Reset input & fade away
        StartCoroutine(FadeOutAndHide());
    }

    IEnumerator FadeOutAndHide()
    {
        Vector2 startKnob = knob.anchoredPosition;
        float t = 0f;

        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(t / fadeOutTime);
            if (canvasGroup) canvasGroup.alpha = a;

            // Optional: spring knob back to center while fading
            knob.anchoredPosition = Vector2.Lerp(startKnob, Vector2.zero, Mathf.SmoothStep(0f, 1f, t / fadeOutTime));
            yield return null;
        }

        knob.anchoredPosition = Vector2.zero;
        if (canvasGroup) canvasGroup.alpha = 0f;

        JoystickInput.Reset();
        gameObject.SetActive(false);
    }

    void OnDisable()
    {
        JoystickInput.Reset();
        if (canvasGroup) canvasGroup.alpha = 0f;
        if (knob) knob.anchoredPosition = Vector2.zero;
    }
}
