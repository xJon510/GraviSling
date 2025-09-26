using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopTabButtons : MonoBehaviour
{
    public enum Tab { Ship, Trail }

    [Header("Buttons")]
    [SerializeField] private Button shipTabButton;
    [SerializeField] private Button trailTabButton;

    [Header("Panels (RectTransforms)")]
    [SerializeField] private RectTransform shipPanel;
    [SerializeField] private RectTransform trailPanel;

    [Header("Slider (optional)")]
    [SerializeField] private RectTransform slider; // leave null if you’ll handle it elsewhere
    [SerializeField] private float sliderAbsX = 0f;

    [Header("Tab Text (TMP)")]
    [SerializeField] private TMP_Text shipTabText;
    [SerializeField] private TMP_Text trailTabText;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = Color.gray;

    [Header("Motion Settings")]
    [SerializeField] private float slideDistance = 1500f;
    [SerializeField] private float duration = 0.25f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Start State")]
    [SerializeField] private Tab startTab = Tab.Ship;
    [SerializeField] private ShopGroupsSwapper groupsSwapper;

    private Coroutine slideRoutine;
    private Tab currentTab;

    void Awake()
    {
        // Wire up clicks (or hook these in the inspector if you prefer).
        if (shipTabButton) shipTabButton.onClick.AddListener(() => GoTo(Tab.Ship));
        if (trailTabButton) trailTabButton.onClick.AddListener(() => GoTo(Tab.Trail));

        if (slider && Mathf.Approximately(sliderAbsX, 0f))
            sliderAbsX = Mathf.Abs(slider.anchoredPosition.x);
        if (sliderAbsX < 1f) sliderAbsX = 100f;

        // Initialize positions
        currentTab = startTab;
        if (currentTab == Tab.Ship)
        {
            SetPos(shipPanel, Vector2.zero);
            SetPos(trailPanel, new Vector2(+slideDistance, 0f));
            SetSliderX(-sliderAbsX);
        }
        else
        {
            SetPos(shipPanel, new Vector2(-slideDistance, 0f));
            SetPos(trailPanel, Vector2.zero);
            SetSliderX(+sliderAbsX);
        }

        if (groupsSwapper)
        {
            groupsSwapper.UpdateForTab(
                startTab == Tab.Ship ? ShopGroupsSwapper.Tab.Ship : ShopGroupsSwapper.Tab.Trail
            );
        }

        UpdateTabTextColors();
    }

    public void GoTo(Tab target)
    {
        if (target == currentTab) return;

        // prevent spamming while animating
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(SlideTo(target));
    }

    private IEnumerator SlideTo(Tab target)
    {
        // Disable inputs during slide
        SetButtonsInteractable(false);

        // Record start/end positions
        Vector2 shipStart = shipPanel.anchoredPosition;
        Vector2 trailStart = trailPanel.anchoredPosition;

        Vector2 shipEnd, trailEnd;
        float sliderStartX = GetSliderXFor(currentTab);
        float sliderEndX = GetSliderXFor(target);

        Color shipStartColor = (currentTab == Tab.Ship) ? activeColor : inactiveColor;
        Color shipEndColor = (target == Tab.Ship) ? activeColor : inactiveColor;
        Color trailStartColor = (currentTab == Tab.Trail) ? activeColor : inactiveColor;
        Color trailEndColor = (target == Tab.Trail) ? activeColor : inactiveColor;

        if (target == Tab.Trail) // ship -> trail
        {
            shipEnd = new Vector2(-slideDistance, 0f);
            trailEnd = Vector2.zero;                    // from +slideDistance to 0
            if (Mathf.Abs(trailStart.x) < 1f)           // if trail is already centered, bail
                trailStart = new Vector2(+slideDistance, 0f);
        }
        else // target == Ship (trail -> ship)
        {
            shipEnd = Vector2.zero;                    // from -slideDistance to 0
            trailEnd = new Vector2(+slideDistance, 0f);
            if (Mathf.Abs(shipStart.x) < 1f)
                shipStart = new Vector2(-slideDistance, 0f);
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;     // unscaled so it works in menus
            float k = ease.Evaluate(Mathf.Clamp01(t));

            SetPos(shipPanel, Vector2.LerpUnclamped(shipStart, shipEnd, k));
            SetPos(trailPanel, Vector2.LerpUnclamped(trailStart, trailEnd, k));

            if (slider)
            {
                Vector2 p = slider.anchoredPosition;
                p.x = Mathf.LerpUnclamped(sliderStartX, sliderEndX, k);
                slider.anchoredPosition = p;
            }

            if (shipTabText) shipTabText.color = Color.LerpUnclamped(shipStartColor, shipEndColor, k);
            if (trailTabText) trailTabText.color = Color.LerpUnclamped(trailStartColor, trailEndColor, k);

            yield return null;
        }

        currentTab = target;
        UpdateTabTextColors();

        if (groupsSwapper)
        {
            groupsSwapper.UpdateForTab(
                target == Tab.Ship ? ShopGroupsSwapper.Tab.Ship : ShopGroupsSwapper.Tab.Trail
            );
        }

        SetButtonsInteractable(true);
        slideRoutine = null;
    }

    private void UpdateTabTextColors()
    {
        if (shipTabText) shipTabText.color = (currentTab == Tab.Ship) ? activeColor : inactiveColor;
        if (trailTabText) trailTabText.color = (currentTab == Tab.Trail) ? activeColor : inactiveColor;
    }

    private void SetPos(RectTransform rt, Vector2 pos)
    {
        if (rt) rt.anchoredPosition = pos;
    }

    private float GetSliderXFor(Tab tab) => (tab == Tab.Ship) ? -sliderAbsX : +sliderAbsX;

    private void SetSliderX(float x)
    {
        if (!slider) return;
        Vector2 p = slider.anchoredPosition;
        p.x = x;
        slider.anchoredPosition = p;
    }

    private void SetButtonsInteractable(bool v)
    {
        if (shipTabButton) shipTabButton.interactable = v;
        if (trailTabButton) trailTabButton.interactable = v;
    }
}
