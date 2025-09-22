using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHoverSFX : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [Tooltip("Turn off on mobile automatically.")]
    public bool desktopOnly = true;

    [Tooltip("Minimum time between plays to avoid chatter.")]
    public float minInterval = 0.1f;

    [Tooltip("Optional: target Selectable to check. If none, auto-finds on this object or its parents.")]
    [SerializeField] private Selectable targetSelectable;

    private float _lastPlayTime;

    private void Awake()
    {
        if (!targetSelectable)
        {
            // Prefer a Selectable on this GameObject; fall back to parent if needed.
            targetSelectable = GetComponent<Selectable>();
            if (!targetSelectable) targetSelectable = GetComponentInParent<Selectable>();
        }
    }
    private bool IsInteractable()
    {
        // If we found a Selectable, use its IsInteractable() (respects CanvasGroup).
        // If none found, assume interactable so generic labels can still beep if desired.
        return targetSelectable == null || targetSelectable.IsInteractable();
    }

    bool CanPlay()
    {
        if (desktopOnly && Application.isMobilePlatform) return false;
        if (!IsInteractable()) return false;
        if (Time.unscaledTime - _lastPlayTime < minInterval) return false;
        _lastPlayTime = Time.unscaledTime;
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CanPlay()) SFXTitleManager.Instance?.PlayUIHover();
    }

    // Triggers when navigating with keyboard/controller focus
    public void OnSelect(BaseEventData eventData)
    {
        if (CanPlay()) SFXTitleManager.Instance?.PlayUIHover();
    }
}
