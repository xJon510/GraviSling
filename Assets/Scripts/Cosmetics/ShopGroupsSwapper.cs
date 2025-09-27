using Mono.Cecil.Cil;
using System;
using TMPro;
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

    [Header("Extra")]
    public GameObject RarityLabel;
    public TMP_Text ShipLabel;
    public TMP_Text ShipName;
    public TMP_Text TrailName;

    public void ShowShip()
    {
        SetVisible(unlockShipButton, ownedShipGroup);
        SetHidden(unlockTrailButton, ownedTrailGroup);
        RarityLabel.SetActive(true);
        ShipLabel.text = "Ship";

        ShipName.gameObject.SetActive(true);
        TrailName.gameObject.SetActive(false);

        string selected = PlayerPrefs.GetString("SelectedShip", "");
        if (string.IsNullOrEmpty(selected))
        {
            // fallback: read from Equipped_ShipKey and trim
            string equipped = PlayerPrefs.GetString("Equipped_ShipKey", "");
            selected = TrimPrefix(equipped, "Ship_");
        }

        ShipName.text = selected;
    }

    public void ShowTrail()
    {
        SetHidden(unlockShipButton, ownedShipGroup);
        SetVisible(unlockTrailButton, ownedTrailGroup);
        RarityLabel.SetActive(false);
        ShipLabel.text = "Trail";

        TrailName.gameObject.SetActive(true);
        ShipName.gameObject.SetActive(false);
        TrailName.text = TrimPrefix(PlayerPrefs.GetString("Equipped_TrailKey", ""), "Trail_").Replace("_", " ");
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
    private string TrimPrefix(string value, string prefix)
    {
        if (value.StartsWith(prefix))
            return value.Substring(prefix.Length);
        return value;
    }
}
