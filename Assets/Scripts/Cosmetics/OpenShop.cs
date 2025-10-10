using System.Linq;
using TMPro;
using UnityEngine;

public class OpenShop : MonoBehaviour
{
    [SerializeField] CanvasGroup shopGroup;   // assign in Inspector
    public GameObject shopUI;
    public TMP_Text TotalBalanceText;
    [SerializeField] CanvasGroup GameOverGroup;
    public TMP_Text ShipName;
    public TMP_Text TrailName;

    [Header("Trail")]
    [SerializeField] private TrailShopUI trailShop; // assign in Inspector
    [SerializeField] private string equippedTrailPrefKey = "Equipped_TrailKey";

    // Hook this to your Button OnClick()
    public void Open()
    {
        if (!shopGroup)
        {
            Debug.LogWarning("[OpenShop] No CanvasGroup assigned.");
            return;
        }

        shopGroup.alpha = 1f;
        shopGroup.interactable = true;
        shopGroup.blocksRaycasts = true;

        GameOverGroup.alpha = 0f;
        GameOverGroup.interactable = false;
        GameOverGroup.blocksRaycasts = false;

        shopUI.SetActive(true);

        string selectedShip = PlayerPrefs.GetString("SelectedShip", "");
        if (string.IsNullOrEmpty(selectedShip))
        {
            // fallback: read from Equipped_ShipKey and trim
            string equipped = PlayerPrefs.GetString("Equipped_ShipKey", "");
            selectedShip = TrimPrefix(equipped, "Ship_");
        }

        ShipName.text = selectedShip;

        int currency = PlayerPrefs.GetInt("currency", 0);

        TotalBalanceText.text = currency.ToString("N0");

        string selectedTrail = PlayerPrefs.GetString("SelectedTrail", "");
        if (string.IsNullOrEmpty(selectedTrail))
        {
            // fallback: read from Equipped_ShipKey and trim
            string equipped = PlayerPrefs.GetString("Equipped_TrailKey", "");
            selectedTrail = TrimPrefix(equipped, "Ship_");
            ApplyEquippedTrailColorToPips();
        }
        else
        {
            ApplySelectedTrailColorToPips();
        }

        TrailName.text = selectedTrail.Replace("_", " ");
    }
    private void ApplyEquippedTrailColorToPips()
    {
        if (UIPipEmitter.Instance == null) return;
        if (trailShop == null || trailShop.cards == null || trailShop.cards.Length == 0) return;

        string equippedTrailKey = PlayerPrefs.GetString(equippedTrailPrefKey, "");
        if (string.IsNullOrEmpty(equippedTrailKey)) return;

        var card = trailShop.cards.FirstOrDefault(c =>
            c != null && string.Equals(c.trailKey, equippedTrailKey, System.StringComparison.OrdinalIgnoreCase));

        if (card != null && card.IsUnlocked())
        {
            UIPipEmitter.Instance.SetStartColor(card.GetColor(), retintAlive: true);
        }
    }
    private void ApplySelectedTrailColorToPips()
    {
        if (UIPipEmitter.Instance == null) return;
        if (trailShop == null || trailShop.cards == null || trailShop.cards.Length == 0) return;

        string selectedTrailKey = PlayerPrefs.GetString("SelectedTrail", "");
        if (string.IsNullOrEmpty(selectedTrailKey)) return;

        var card = trailShop.cards.FirstOrDefault(c =>
            c != null && string.Equals(c.trailKey, selectedTrailKey, System.StringComparison.OrdinalIgnoreCase));

        if (card != null)
        {
            UIPipEmitter.Instance.SetStartColor(card.GetColor(), retintAlive: true);
        }
    }

    // Optional helper if you want to close it somewhere else
    public void Close()
    {
        if (!shopGroup) return;

        shopGroup.alpha = 0f;
        shopGroup.interactable = false;
        shopGroup.blocksRaycasts = false;

        GameOverGroup.alpha = 1f;
        GameOverGroup.interactable = true;
        GameOverGroup.blocksRaycasts = true;

        shopUI.SetActive(false);
    }
    private string TrimPrefix(string value, string prefix)
    {
        if (value.StartsWith(prefix))
            return value.Substring(prefix.Length);
        return value;
    }

    //dopa down
}
