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

    private int _trailShowCountThisEnable = 0;

    void OnEnable()
    {
        _trailShowCountThisEnable = 0;
    }

    void OnDisable()
    {
        // Reset so a fresh open treats next Show* as "first time"
        _trailShowCountThisEnable = 0;
    }

    public void ShowShip()
    {
        SetVisible(unlockShipButton, ownedShipGroup);
        SetHidden(unlockTrailButton, ownedTrailGroup);
        RarityLabel.SetActive(true);
        ShipLabel.text = "Ship";

        ShipName.gameObject.SetActive(true);
        TrailName.gameObject.SetActive(false);

        string selectedShip = PlayerPrefs.GetString("SelectedShip", "");
        if (string.IsNullOrEmpty(selectedShip))
        {
            // fallback: read from Equipped_ShipKey and trim
            string equipped = PlayerPrefs.GetString("Equipped_ShipKey", "");
            selectedShip = TrimPrefix(equipped, "Ship_");
        }

        ShipName.text = selectedShip;
    }

    public void ShowTrail()
    {
        SetHidden(unlockShipButton, ownedShipGroup);
        SetVisible(unlockTrailButton, ownedTrailGroup);
        RarityLabel.SetActive(false);
        ShipLabel.text = "Trail";

        TrailName.gameObject.SetActive(true);
        ShipName.gameObject.SetActive(false);

        string displayTrail;

        if (_trailShowCountThisEnable == 0)
        {
            // First time this UI is enabled & Trail tab shown -> use EQUIPPED
            string equippedTrail = PlayerPrefs.GetString("Equipped_TrailKey", "");
            displayTrail = TrimPrefix(equippedTrail, "Trail_");
        }
        else
        {
            // Subsequent shows while enabled -> use SELECTED (sticky within session)
            string selectedTrail = PlayerPrefs.GetString("SelectedTrail", "");
            if (string.IsNullOrEmpty(selectedTrail))
            {
                // safety fallback if nothing was selected yet
                string equippedTrail = PlayerPrefs.GetString("Equipped_TrailKey", "");
                selectedTrail = TrimPrefix(equippedTrail, "Trail_");
            }
            displayTrail = selectedTrail;
        }

        TrailName.text = (displayTrail ?? string.Empty).Replace("_", " ");
        _trailShowCountThisEnable++;
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
