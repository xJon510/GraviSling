using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableAreaLeft : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Assign")]
    public MobileJoystick mobileJoystick;

    private int activePointerId = -1;
    private float screenMidX;

    void Awake()
    {
        screenMidX = Screen.width * 0.5f;
        if (mobileJoystick) mobileJoystick.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != -1) return; // already tracking a finger

        // Left-half gate (optional—remove if this panel is already only on left)
        if (eventData.position.x > screenMidX) return;

        activePointerId = eventData.pointerId;
        mobileJoystick.ShowAtScreenPosition(eventData.position);
        mobileJoystick.UpdateDrag(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        mobileJoystick.UpdateDrag(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId) return;
        activePointerId = -1;
        mobileJoystick.Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Some platforms fire Exit without Up; treat as release if it’s our finger
        if (eventData.pointerId != activePointerId) return;
        activePointerId = -1;
        mobileJoystick.Release();
    }

    void OnDisable()
    {
        activePointerId = -1;
        if (mobileJoystick && mobileJoystick.gameObject.activeSelf)
            mobileJoystick.Release();
    }
}
