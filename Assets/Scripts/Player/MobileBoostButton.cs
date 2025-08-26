using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to a full-screen (or right-side) UI object with a RaycastTarget
/// (e.g., Image) to act as a touch zone. Handles multi-touch safely.
/// </summary>
public class MobileBoostButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    // Track how many fingers are currently down within this zone
    private int activePointers = 0;

    public void OnPointerDown(PointerEventData eventData)
    {
        activePointers++;
        BoostInput.SetMobilePressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        activePointers = Mathf.Max(0, activePointers - 1);
        if (activePointers == 0)
            BoostInput.SetMobilePressed(false);
    }

    // If a finger slides off the zone, treat it as released for safety.
    public void OnPointerExit(PointerEventData eventData)
    {
        // Some platforms fire Exit without Up; guard with pointer count
        activePointers = Mathf.Max(0, activePointers - 1);
        if (activePointers == 0)
            BoostInput.SetMobilePressed(false);
    }

    private void OnDisable()
    {
        // Ensure no “stuck boost” when canvas/panel turns off
        activePointers = 0;
        BoostInput.ForceRelease();
    }
}
