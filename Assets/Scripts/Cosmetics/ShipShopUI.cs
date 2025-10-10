// ShipShopUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class ShipShopUI : MonoBehaviour
{
    [Header("Cards in the grid")]
    public ShipCard[] cards;

    [Header("Preview panel")]
    public Image previewImage;
    public TMP_Text previewName;
    public TMP_Text previewRarity;
    public Animator previewAnimator;
    public TMP_Text previewCostText;

    [Header("Button groups")]
    public GameObject groupUnlock;   // contains ONLY the Unlock button
    public GameObject groupOwned;    // contains Equip & friends

    [Header("Buttons")]
    public Button buttonUnlock;
    public Button buttonEquip;
    public TMP_Text previewEquipText;

    [Header("Color Button Images")]
    public Image unlockButtonBorder;
    public Image equipButtonBorder;
    public Image randomizeButtonBorder;

    public Image unlockButtonBkRnd;
    public Image equipButtonBkRnd;
    public Image randomizeButtonBkRnd;

    public TMP_Text unlockName;
    public TMP_Text equipName;
    public TMP_Text randomizeName;

    public TrailShopUI TrailShopManager;

    [Header("Equipped state")]
    [Tooltip("PlayerPrefs key storing the currently equipped ship key")]
    public string equippedKeyPref = "Equipped_ShipKey";

    [Header("Rarity Colors")]
    public Color colorCommon = Color.white;
    public Color colorUncommon = Color.green;
    public Color colorRare = Color.blue;
    public Color colorEpic = new Color(0.6f, 0f, 1f);   // purple-ish
    public Color colorLegendary = new Color(1f, 0.65f, 0f); // orange-gold

    [Header("Rarity BkRnd Colors")]
    public Color colorCommonBkRnd = Color.white;
    public Color colorUncommonBkRnd = Color.green;
    public Color colorRareBkRnd = Color.blue;
    public Color colorEpicBkRnd = new Color(0.6f, 0f, 1f);   // purple-ish
    public Color colorLegendaryBkRnd = new Color(1f, 0.65f, 0f); // orange-gold

    private ShipCosmetic _selected;

    void Awake()
    {
        // subscribe to card clicks
        foreach (var c in cards.Where(c => c != null))
        {
            c.OnSelected += OnCardSelected;
            c.OnFastEquip += OnCardFastEquip;   // <- listen to card-level fast equip
            c.Refresh();
        }

        if (buttonUnlock) buttonUnlock.onClick.AddListener(TryUnlockSelected);
        if (buttonEquip) buttonEquip.onClick.AddListener(EquipSelected);
    }

    void Start()
    {
        // Try to select whatever is currently equipped
        string equippedKey = GetEquippedKey(); // reads PlayerPrefs "Equipped_ShipKey"
        ShipCard equippedCard = FindCardByEquippedKey(equippedKey);

        if (equippedCard != null)
            OnCardSelected(equippedCard.data);
        else if (_selected == null && cards.Length > 0 && cards[0] != null)
            OnCardSelected(cards[0].data);

        RefreshAllCards();
        UpdateButtons();
        UpdatePreview();
    }

    void OnDestroy()
    {
        foreach (var c in cards.Where(c => c != null))
        {
            c.OnSelected -= OnCardSelected;
            c.OnFastEquip -= OnCardFastEquip;
        }
    }
    private void RefreshAllCards()
    {
        foreach (var c in cards) if (c) c.Refresh();
    }

    // --- Selection & UI ---
    private void OnCardSelected(ShipCosmetic data)
    {
        _selected = data;

        if (_selected != null)
        {
            string trimmedName = TrimShipPrefix(_selected.playerPrefKey);
            PlayerPrefs.SetString("SelectedShip", trimmedName);
            PlayerPrefs.Save();
        }

        UpdatePreview();
        UpdateButtons();
    }
    private void OnCardFastEquip(ShipCosmetic data)
    {
        if (data == null || !data.IsUnlocked()) return;
        SetEquippedKey(data.playerPrefKey);
        RefreshAllCards();
        UpdateButtons();
    }
    private ShipCard FindCardByEquippedKey(string equippedKey)
    {
        if (string.IsNullOrEmpty(equippedKey)) return null;
        foreach (var c in cards)
            if (c && c.data && string.Equals(c.data.playerPrefKey, equippedKey, System.StringComparison.OrdinalIgnoreCase))
                return c;
        return null;
    }

    private void UpdatePreview()
    {
        if (_selected == null) return;

        if (previewImage) previewImage.sprite = _selected.icon;
        if (previewName) previewName.text = _selected.displayName;
        if (previewRarity) previewRarity.text = _selected.rarity.ToString().ToUpperInvariant();

        // apply rarity colors
        Color rarityColor = GetColorForRarity(_selected.rarity);
        if (previewName) previewName.color = rarityColor;
        if (previewRarity) previewRarity.color = rarityColor;
        if (unlockButtonBorder) unlockButtonBorder.color = rarityColor;
        if (equipButtonBorder) equipButtonBorder.color = rarityColor;
        if (randomizeButtonBorder) randomizeButtonBorder.color = rarityColor;

        if (unlockName) unlockName.color = rarityColor;
        if (equipName) equipName.color = rarityColor;
        if (randomizeName) randomizeName.color = rarityColor;


        Color rarityBkRndColor = GetColorForRarityBkRnd(_selected.rarity);
        if (unlockButtonBkRnd) unlockButtonBkRnd.color = rarityBkRndColor;
        if (equipButtonBkRnd) equipButtonBkRnd.color = rarityBkRndColor;
        if (randomizeButtonBkRnd) randomizeButtonBkRnd.color = rarityBkRndColor;

        bool owned = _selected.IsUnlocked();
        if (previewCostText) previewCostText.text = owned ? "" : $"Unlock: {_selected.unlockCost}";

        ApplyPreviewAnimator(_selected);
    }
    private void ApplyPreviewAnimator(ShipCosmetic data)
    {
        if (!previewAnimator) return;

        previewAnimator.runtimeAnimatorController = data.animatorController;
    }

    private void UpdateButtons()
    {
        if (_selected == null) return;

        bool owned = _selected.IsUnlocked();
        string equippedKey = GetEquippedKey();
        bool isEquipped = owned && equippedKey == _selected.playerPrefKey;

        if (groupUnlock) groupUnlock.SetActive(!owned);
        if (groupOwned) groupOwned.SetActive(owned);

        // If showing the Unlock button, make it interactable only if affordable
        if (buttonUnlock) buttonUnlock.interactable = !owned && CurrencyBank.CanAfford(_selected.unlockCost);

        // Equip button grays out if already equipped
        if (buttonEquip) buttonEquip.interactable = owned && GetEquippedKey() != _selected.playerPrefKey;

        if (previewEquipText)
            previewEquipText.text = isEquipped ? "Equipped" : "Equip";

    }

    // --- Actions ---
    private void TryUnlockSelected()
    {
        if (_selected == null) return;

        if (!_selected.IsUnlocked() && CurrencyBank.TrySpend(_selected.unlockCost))
        {
            _selected.Unlock();

            // refresh cards + UI
            RefreshAllCards();
            UpdateButtons();
            UpdatePreview();
        }
        else
        {
            // TODO: play error sfx / flash currency
            Debug.Log("Not enough currency or already owned.");
        }
    }

    public void EquipSelected()
    {
        if (_selected == null || !_selected.IsUnlocked()) return;

        if (!PairEquipSync.Active)
        {
            PairEquipSync.Active = true;
            try
            {
                // 1) Equip the selected ship locally
                SetEquippedKey(_selected.playerPrefKey);
                RefreshAllCards();
                UpdateButtons();

                // 2) Also equip the currently selected trail (if any & different)
                if (TrailShopManager != null)
                {
                    string trailSelectedKey = TrailShopManager.GetSelectedKey();
                    string trailEquippedKey = TrailShopManager.GetEquippedKeyPublic();

                    bool changedTrail = false;
                    if (!string.IsNullOrEmpty(trailSelectedKey)
                        && trailSelectedKey != trailEquippedKey
                        && TrailShopManager.IsUnlocked(trailSelectedKey))
                    {
                        // Safe attempt; won't fall back to default if somehow disallowed
                        changedTrail = TrailShopManager.TrySetEquippedKeyPublic(trailSelectedKey);
                    }

                    if (!changedTrail)
                    {
                        // Do NOT alter trail; just refresh visuals against the real equipped key
                        TrailShopManager.RefreshUIWithEquippedKey();
                    }
                }
            }
            finally
            {
                PairEquipSync.Active = false;
            }
        }
        else
        {
            // Quiet equip when we're already inside a paired transaction
            SetEquippedKey(_selected.playerPrefKey);
            RefreshAllCards();
            UpdateButtons();
        }
    }


    // --- Equipped persistence ---
    private string GetEquippedKey() =>
        PlayerPrefs.GetString(equippedKeyPref, "");

    private void SetEquippedKey(string key)
    {
        PlayerPrefs.SetString(equippedKeyPref, key);
        PlayerPrefs.Save();
    }
    private Color GetColorForRarity(ShipRarity rarity)
    {
        switch (rarity)
        {
            case ShipRarity.Common: return colorCommon;
            case ShipRarity.Uncommon: return colorUncommon;
            case ShipRarity.Rare: return colorRare;
            case ShipRarity.Epic: return colorEpic;
            case ShipRarity.Legendary: return colorLegendary;
            default: return Color.white;
        }
    }

    private Color GetColorForRarityBkRnd(ShipRarity rarity)
    {
        switch (rarity)
        {
            case ShipRarity.Common: return colorCommonBkRnd;
            case ShipRarity.Uncommon: return colorUncommonBkRnd;
            case ShipRarity.Rare: return colorRareBkRnd;
            case ShipRarity.Epic: return colorEpicBkRnd;
            case ShipRarity.Legendary: return colorLegendaryBkRnd;
            default: return Color.white;
        }
    }
    private string TrimShipPrefix(string key)
    {
        const string prefix = "Ship_";
        if (!string.IsNullOrEmpty(key) && key.StartsWith(prefix))
            return key.Substring(prefix.Length);
        return key;
    }

    // Expose currently selected ship key (null if none)
    public string GetSelectedKey() => _selected ? _selected.playerPrefKey : null;

    // Expose currently equipped key
    public string GetEquippedKeyPublic() => GetEquippedKey();

    // Public setter that also refreshes local UI
    public void SetEquippedKeyPublic(string key)
    {
        SetEquippedKey(key);
        RefreshAllCards();
        UpdateButtons();
        UpdatePreview();
    }

    // Quick UI refresh helper (when other side changed something)
    public void RefreshUIOnly()
    {
        RefreshAllCards();
        UpdateButtons();
        UpdatePreview();
    }

    // Is a given ship key unlocked?
    public bool IsUnlocked(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        var card = cards.FirstOrDefault(c => c && c.data &&
            string.Equals(c.data.playerPrefKey, key, System.StringComparison.OrdinalIgnoreCase));
        return card != null && card.data.IsUnlocked();
    }

    // Safe public equip: NO-OP if locked
    public bool TrySetEquippedKeyPublic(string key)
    {
        if (!IsUnlocked(key)) return false;
        SetEquippedKey(key);
        RefreshAllCards();
        UpdateButtons();
        UpdatePreview();
        return true;
    }

    // Force a visual refresh using the actually equipped key
    public void RefreshUIWithEquippedKey()
    {
        RefreshAllCards();
        UpdateButtons();
        UpdatePreview();
    }
}
