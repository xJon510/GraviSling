using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelSwitcher : MonoBehaviour
{
    [Header("Panels (parents with all content)")]
    [Tooltip("The panel that starts centered/visible.")]
    public RectTransform settingsPanel;

    [Tooltip("The panel that starts off to the right (hidden).")]
    public RectTransform creditsPanel;

    [Header("Bottom-right buttons")]
    [Tooltip("The 'Credits' button that is visible while on the Settings panel.")]
    public RectTransform creditsButton;   // visible at +visibleButtonY, hidden at hiddenButtonY
    [Tooltip("The 'Settings' button that is visible while on the Credits panel.")]
    public RectTransform settingsButton;  // visible at +visibleButtonY, hidden at hiddenButtonY

    [Header("Button Y positions")]
    public float visibleButtonY = 100f;
    public float hiddenButtonY = -100f;

    [Header("Motion")]
    [Tooltip("If true, panels slide horizontally. If false, they slide vertically.")]
    public bool slideHorizontally = true;

    [Tooltip("Distance between panels (use 1500 per your layout).")]
    public float panelSeparation = 1500f;

    [Tooltip("Seconds for panel sweep.")]
    public float panelDuration = 0.25f;

    [Tooltip("Seconds for button pop/drop.")]
    public float buttonDuration = 0.18f;

    [Tooltip("Optional: CanvasGroups to block raycasts while animating.")]
    public CanvasGroup[] raycastBlocks;

    // State
    bool showingSettings = true;  // Settings starts on-screen
    bool busy;

    void Reset()
    {
        // Best-guess defaults if dropped on a GO
        panelSeparation = 1500f;
        panelDuration = 0.25f;
        buttonDuration = 0.18f;
        visibleButtonY = 100f;
        hiddenButtonY = -100f;
        slideHorizontally = true;
    }

    void OnEnable()
    {
        // Normalize starting layout: Settings centered (0), Credits to the right (+separation).
        if (settingsPanel) settingsPanel.anchoredPosition = Vector2.zero;

        if (creditsPanel)
        {
            var p = Vector2.zero;
            if (slideHorizontally) p.x = panelSeparation; else p.y = -panelSeparation;
            creditsPanel.anchoredPosition = p;
        }

        // Buttons: Credits visible (we're on Settings), Settings hidden.
        if (creditsButton) SetButtonY(creditsButton, visibleButtonY, instant: true);
        if (settingsButton) SetButtonY(settingsButton, hiddenButtonY, instant: true);

        UpdateButtonInteractable();
    }

    // Hook these to your UI Button onClick()s
    public void ShowCredits()
    {
        if (busy || !showingSettings) return;
        StartCoroutine(SwapRoutine(toCredits: true));
    }

    public void ShowSettings()
    {
        if (busy || showingSettings) return;
        StartCoroutine(SwapRoutine(toCredits: false));
    }

    IEnumerator SwapRoutine(bool toCredits)
    {
        busy = true;
        SetRaycastBlocks(false);

        // 1) Slide the bottom-right buttons (one down, the other up).
        RectTransform downBtn = toCredits ? creditsButton : settingsButton;
        RectTransform upBtn = toCredits ? settingsButton : creditsButton;

        IEnumerator b1 = SlideButtonY(downBtn, visibleButtonY, hiddenButtonY, buttonDuration);
        IEnumerator b2 = SlideButtonY(upBtn, hiddenButtonY, visibleButtonY, buttonDuration);

        // Run both button animations in parallel
        StartCoroutine(b1);
        yield return StartCoroutine(b2);

        // 2) Sweep panels under the mask.
        //    Settings moves left; Credits comes in from right (and vice-versa on return).
        Vector2 sFrom = settingsPanel.anchoredPosition;
        Vector2 cFrom = creditsPanel.anchoredPosition;

        Vector2 sTo = Vector2.zero;
        Vector2 cTo = Vector2.zero;

        if (slideHorizontally)
        {
            sTo.x = toCredits ? -panelSeparation : 0f;
            cTo.x = toCredits ? 0f : panelSeparation;
        }
        else
        {
            // vertical version if you ever want it
            sTo.y = toCredits ? panelSeparation : 0f;
            cTo.y = toCredits ? 0f : -panelSeparation;
        }

        float t = 0f;
        while (t < panelDuration)
        {
            t += Time.unscaledDeltaTime;
            float u = EaseOutCubic(Mathf.Clamp01(t / panelDuration));

            settingsPanel.anchoredPosition = Vector2.Lerp(sFrom, sTo, u);
            creditsPanel.anchoredPosition = Vector2.Lerp(cFrom, cTo, u);
            yield return null;
        }

        showingSettings = !toCredits ? true : false;
        UpdateButtonInteractable();

        SetRaycastBlocks(true);
        busy = false;
    }

    void UpdateButtonInteractable()
    {
        // Only the button for the opposite panel should be interactable.
        SetInteractable(creditsButton, showingSettings); // visible & clickable on Settings screen
        SetInteractable(settingsButton, !showingSettings);
    }

    void SetInteractable(RectTransform rt, bool on)
    {
        if (!rt) return;
        var btn = rt.GetComponent<Button>();
        if (btn) btn.interactable = on;
    }

    void SetRaycastBlocks(bool on)
    {
        if (raycastBlocks == null) return;
        foreach (var cg in raycastBlocks)
        {
            if (!cg) continue;
            cg.blocksRaycasts = on;
            cg.interactable = on;
        }
    }

    IEnumerator SlideButtonY(RectTransform rt, float fromY, float toY, float dur)
    {
        if (!rt) yield break;

        // Use anchoredPosition for pixel-perfect UI motion.
        Vector2 start = rt.anchoredPosition;
        start.y = fromY;
        Vector2 end = start; end.y = toY;

        // Snap to exact start in case designer moved it
        rt.anchoredPosition = start;

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = EaseOutCubic(Mathf.Clamp01(t / dur));
            rt.anchoredPosition = Vector2.Lerp(start, end, u);
            yield return null;
        }
        rt.anchoredPosition = end;
    }

    void SetButtonY(RectTransform rt, float y, bool instant)
    {
        if (!rt) return;
        var p = rt.anchoredPosition;
        p.y = y;
        rt.anchoredPosition = p;
    }

    // Nice snappy easing
    static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
}
