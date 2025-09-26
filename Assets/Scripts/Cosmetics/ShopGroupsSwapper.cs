using UnityEngine;

public class ShopGroupsSwapper : MonoBehaviour
{
    public enum Tab { Ship, Trail }

    [Header("Ship Group")]
    [SerializeField] private RectTransform unlockShipButton;
    [SerializeField] private RectTransform ownedShipGroup;

    [Header("Trail Group")]
    [SerializeField] private RectTransform unlockTrailButton;
    [SerializeField] private RectTransform ownedTrailGroup;

    [Header("Positions")]
    [SerializeField] private Vector2 visiblePos = Vector2.zero;
    [SerializeField] private Vector2 hiddenPos = new Vector2(1000f, -1000f);

    public void ShowShip()
    {
        SetVisible(unlockShipButton, ownedShipGroup);
        SetHidden(unlockTrailButton, ownedTrailGroup);
    }

    public void ShowTrail()
    {
        SetHidden(unlockShipButton, ownedShipGroup);
        SetVisible(unlockTrailButton, ownedTrailGroup);
    }

    public void UpdateForTab(Tab tab)
    {
        if (tab == Tab.Ship) ShowShip();
        else ShowTrail();
    }

    void SetVisible(params RectTransform[] rts)
    {
        foreach (var rt in rts) if (rt) rt.anchoredPosition = visiblePos;
    }
    void SetHidden(params RectTransform[] rts)
    {
        foreach (var rt in rts) if (rt) rt.anchoredPosition = hiddenPos;
    }
}
