// ShipCard.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ShipCard : MonoBehaviour
{
    [Header("Data")]
    public ShipCosmetic data;

    [Header("UI")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text rarityText;
    public GameObject lockBadge;  // small lock icon or overlay
    public Button selectButton;   // clicking the whole card
    public Button fastEquipButton;        // only visible when owned

    [Header("Equipped State")]
    [Tooltip("PlayerPrefs key that stores the currently equipped ship")]
    public string equippedKeyPref = "Equipped_ShipKey";

    [Header("Preview")]
    public Animator previewAnimator;

    public event Action<ShipCosmetic> OnSelected;
    public event Action<ShipCosmetic> OnFastEquip;

    void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(() => OnSelected?.Invoke(data));

        if (fastEquipButton)
            fastEquipButton.onClick.AddListener(() => EquipThis());
    }

    void OnEnable() => Refresh();
    public void Refresh()
    {
        if (data == null) return;

        if (iconImage) iconImage.sprite = data.icon;
        if (nameText) nameText.text = data.displayName;
        if (rarityText) rarityText.text = data.rarity.ToString().ToUpperInvariant();

        bool owned = data.IsUnlocked();

        // Show cost only if locked
        if (lockBadge) lockBadge.SetActive(!owned);

        // Fast equip button only when owned
        if (fastEquipButton)
        {
            fastEquipButton.gameObject.SetActive(owned);

            // Disable if this ship is already equipped
            string equippedKey = PlayerPrefs.GetString(equippedKeyPref, "");
            bool isEquipped = owned && equippedKey == data.playerPrefKey;
            fastEquipButton.interactable = owned && !isEquipped;
        }

        // apply animator to preview
        if (previewAnimator && data.animatorController)
            previewAnimator.runtimeAnimatorController = data.animatorController;
    }

    private void EquipThis()
    {
        if (!data.IsUnlocked()) return;

        PlayerPrefs.SetString(equippedKeyPref, data.playerPrefKey);
        PlayerPrefs.Save();

        // Notify shop UI or other listeners
        OnFastEquip?.Invoke(data);

        Refresh(); // update button state
    }
}
