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

    [Header("Default trail key")]
    [Tooltip("Fallback if nothing equipped yet.")]
    public string defaultTrailKey = "Trail_Blue_Sky";

    [Tooltip("Direct reference to the default TrailCard (recommended).")]
    public TrailCard defaultCard;

    [Tooltip("If true, ensures the default is unlocked once on first boot.")]
    public bool unlockDefaultIfNeeded = true;

    void Awake()
    {
        // Make sure this runs before anything tries to read the equipped trail
        EnsureEquippedExists();
    }

    void OnEnable()
    {
        // In case of scene reloads / addressables, re-assert state
        ApplyEquippedTrail();
    }

    public void ApplyEquippedTrail()
    {
        if (shopUI == null || shopUI.cards == null || shopUI.cards.Length == 0)
            return;

        var equippedCard = GetEquippedCardOrNull();
        if (equippedCard == null) return;

        // Tint material (guarded for WebGL)
        if (trailMaterial != null)
            trailMaterial.color = equippedCard.GetColor();

        // Push to pips if present
        if (UIPipEmitter.Instance != null)
            UIPipEmitter.Instance.SetStartColor(equippedCard.GetColor(), retintAlive: true);

        // Refresh card checkmarks/highlights across the grid
        RefreshAllCards(equippedCard.trailKey);
    }

    // ---------- Internals ----------

    void EnsureEquippedExists()
    {
        // Already equipped? cool.
        string equippedKey = PlayerPrefs.GetString(equippedKeyPref, "");
        if (!string.IsNullOrEmpty(equippedKey))
            return;

        // Prefer explicit default card if assigned
        TrailCard cardToEquip = defaultCard;

        // If not wired, try to find by key among cards
        if (cardToEquip == null && shopUI != null && shopUI.cards != null)
        {
            cardToEquip = shopUI.cards.FirstOrDefault(c =>
                c != null && string.Equals(c.trailKey, defaultTrailKey, System.StringComparison.OrdinalIgnoreCase));
        }

        // Still nothing? last resort: first unlocked card, or just first non-null card
        if (cardToEquip == null && shopUI != null && shopUI.cards != null)
        {
            cardToEquip = shopUI.cards.FirstOrDefault(c => c != null && c.IsUnlocked())
                       ?? shopUI.cards.FirstOrDefault(c => c != null);
        }

        if (cardToEquip == null) return;

        // Optionally unlock default (so other logic like IsUnlocked() passes)
        if (unlockDefaultIfNeeded && !cardToEquip.IsUnlocked())
            cardToEquip.Unlock();

        // Persist as equipped and save
        PlayerPrefs.SetString(equippedKeyPref, cardToEquip.trailKey);
        PlayerPrefs.Save();
    }

    TrailCard GetEquippedCardOrNull()
    {
        string equippedKey = PlayerPrefs.GetString(equippedKeyPref, "");
        if (string.IsNullOrEmpty(equippedKey)) return null;

        if (shopUI == null || shopUI.cards == null) return null;

        var card = shopUI.cards.FirstOrDefault(c =>
            c != null && string.Equals(c.trailKey, equippedKey, System.StringComparison.OrdinalIgnoreCase));

        // If prefs point to a missing/locked card, fall back to default and fix the pref
        if (card == null || !card.IsUnlocked())
        {
            // Try defaultCard first
            var fallback = defaultCard;
            if (fallback == null)
            {
                fallback = shopUI.cards.FirstOrDefault(c =>
                    c != null && string.Equals(c.trailKey, defaultTrailKey, System.StringComparison.OrdinalIgnoreCase));
            }
            if (fallback == null)
            {
                fallback = shopUI.cards.FirstOrDefault(c => c != null && c.IsUnlocked())
                       ?? shopUI.cards.FirstOrDefault(c => c != null);
            }
            if (fallback == null) return null;

            if (unlockDefaultIfNeeded && !fallback.IsUnlocked())
                fallback.Unlock();

            PlayerPrefs.SetString(equippedKeyPref, fallback.trailKey);
            PlayerPrefs.Save();

            card = fallback;
        }

        return card;
    }

    void RefreshAllCards(string equippedKey)
    {
        if (shopUI == null || shopUI.cards == null) return;
        foreach (var c in shopUI.cards)
        {
            if (c == null) continue;
            c.Refresh(equippedKey);
            c.SetSelectedVisual(string.Equals(c.trailKey, equippedKey, System.StringComparison.OrdinalIgnoreCase));
        }
    }

}
