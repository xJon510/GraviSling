using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverSFX : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [Tooltip("Turn off on mobile automatically.")]
    public bool desktopOnly = true;

    [Tooltip("Minimum time between plays to avoid chatter.")]
    public float minInterval = 0.06f;

    private float _lastPlayTime;

    bool CanPlay()
    {
        if (desktopOnly && Application.isMobilePlatform) return false;
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
