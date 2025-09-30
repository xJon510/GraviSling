using UnityEngine;
using System.Linq;

public class TrailEquipHelper : MonoBehaviour
{
    [Header("Material to tint")]
    [Tooltip("Assign the material used by your trail renderer / UI trail.")]
    public Material trailMaterial;

    [Header("Shop reference")]
    [Tooltip("Reference to the shop UI, so we can grab its cards.")]
    public TrailShopUI shopUI;

    [Header("PlayerPrefs Key")]
    [Tooltip("Same key used in TrailShopUI for equipped trail.")]
    public string equippedKeyPref = "Equipped_TrailKey";

    void Start()
    {
        ApplyEquippedTrail();
    }

    public void ApplyEquippedTrail()
    {
        if (trailMaterial == null || shopUI == null || shopUI.cards == null)
            return;

        string equippedKey = PlayerPrefs.GetString(equippedKeyPref, "");

        if (string.IsNullOrEmpty(equippedKey))
            return; // nothing equipped yet

        // Find the matching card
        TrailCard equippedCard = shopUI.cards.FirstOrDefault(c =>
            c != null && string.Equals(c.trailKey, equippedKey, System.StringComparison.OrdinalIgnoreCase));

        if (equippedCard != null && equippedCard.IsUnlocked())
        {
            trailMaterial.color = equippedCard.GetColor();
        }
    }
}
