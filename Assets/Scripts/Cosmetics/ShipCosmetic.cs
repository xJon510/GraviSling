// ShipCosmetic.cs
using UnityEngine;

public enum ShipRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }

[CreateAssetMenu(fileName = "Ship_", menuName = "GraviSling/Ship Cosmetic", order = 0)]
public class ShipCosmetic : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Comet";
    public ShipRarity rarity = ShipRarity.Common;
    public Sprite icon;               // your PNG as Sprite
    [Tooltip("Unique PlayerPrefs key, e.g. Ship_Comet")]
    public string playerPrefKey = "Ship_Comet";

    [Header("Unlocking")]
    public int unlockCost = 0;
    [Tooltip("Starter ships, etc.")]
    public bool unlockedByDefault = false;

    [Header("Animation")]
    public RuntimeAnimatorController animatorController;

    // --- PlayerPrefs helpers ---
    public bool IsUnlocked() =>
        PlayerPrefs.GetInt(playerPrefKey, unlockedByDefault ? 1 : 0) == 1;

    public void Unlock()
    {
        PlayerPrefs.SetInt(playerPrefKey, 1);
        PlayerPrefs.Save();
    }
}
