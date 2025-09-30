using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class TrailShopUI : MonoBehaviour
{
    [Header("Cards (24 total)")]
    public TrailCard[] cards;

    [Header("Preview Panel")]
    [Tooltip("Reuses your existing ShipName text object.")]
    public TMP_Text previewName;
    public TMP_Text previewCostText;

    [Tooltip("Optional swatch to preview the trail color (can be left null).")]
    public Image previewColorSwatch;

    [Header("Button Groups")]
    public GameObject groupUnlock;   // contains ONLY Unlock button
    public GameObject groupOwned;    // contains Equip button, etc.

    [Header("Buttons")]
    public Button buttonUnlock;
    public Button buttonEquip;
    public TMP_Text previewEquipText; // shows "Equip" or "Equipped"

    [Header("Economy")]
    [Tooltip("Uniform unlock cost for ALL trails.")]
    public int trailUnlockCost = 100;

    [Header("Equipped State")]
    [Tooltip("PlayerPrefs key storing the currently equipped trail key.")]
    public string equippedKeyPref = "Equipped_TrailKey";

    [Header("Live Trail Material")]
    [Tooltip("The material used by the in-game trail (UI/Default or custom).")]
    public Material trailMaterial;

    private TrailCard _selected;
    private TrailCard _lastSelectedForHighlight;

    void Awake()
    {
        foreach (var c in cards.Where(c => c != null))
        {
            c.OnSelected += OnCardSelected;
            c.Refresh();
        }

        if (buttonUnlock) buttonUnlock.onClick.AddListener(TryUnlockSelected);
        if (buttonEquip) buttonEquip.onClick.AddListener(EquipSelected);
    }

    void Start()
    {
        RefreshAllCards();
        RestoreInitialSelectionHighlight();
        UpdateButtons();
        UpdatePreview();
    }

    void OnDestroy()
    {
        foreach (var c in cards.Where(c => c != null))
            c.OnSelected -= OnCardSelected;
    }

    private void RefreshAllCards()
    {
        string equippedKey = GetEquippedKey();
        foreach (var c in cards)
        {
            if (!c) continue;
            c.Refresh(equippedKey);
        }
    }

    // --- Selection & UI ---
    private void OnCardSelected(TrailCard card)
    {
        if (_lastSelectedForHighlight)
            _lastSelectedForHighlight.SetSelectedVisual(false);

        _selected = card;

        if (_selected != null)
        {
            _selected.SetSelectedVisual(true);
            _lastSelectedForHighlight = _selected;

            string trimmedName = TrimTrailPrefix(_selected.UnlockPrefKey);
            PlayerPrefs.SetString("SelectedTrail", trimmedName);
            PlayerPrefs.Save();
        }

        UpdatePreview();
        UpdateButtons();
    }

    private void UpdatePreview()
    {
        if (_selected == null) return;

        // Name
        if (previewName) previewName.text = _selected.GetName();

        // Optional color swatch
        if (previewColorSwatch) previewColorSwatch.color = _selected.GetColor();

        // Cost text only when locked
        bool owned = _selected.IsUnlocked();
        if (previewCostText) previewCostText.text = owned ? "" : $"Unlock: {trailUnlockCost}";
    }

    private void UpdateButtons()
    {
        if (_selected == null) return;

        bool owned = _selected.IsUnlocked();
        string equippedKey = GetEquippedKey();
        bool isEquipped = owned && equippedKey == _selected.trailKey;

        if (groupUnlock) groupUnlock.SetActive(!owned);
        if (groupOwned) groupOwned.SetActive(owned);

        // Unlock interactable only if affordable (and not owned)
        if (buttonUnlock)
            buttonUnlock.interactable = !owned && CurrencyBank.CanAfford(trailUnlockCost);

        // Equip interactable only if owned and not already equipped
        if (buttonEquip)
            buttonEquip.interactable = owned && !isEquipped;

        if (previewEquipText)
            previewEquipText.text = isEquipped ? "Equipped" : "Equip";
    }

    // --- Actions ---
    private void TryUnlockSelected()
    {
        if (_selected == null) return;

        if (!_selected.IsUnlocked() && CurrencyBank.TrySpend(trailUnlockCost))
        {
            _selected.Unlock();
            RefreshAllCards();
            UpdateButtons();
            UpdatePreview();
        }
        else
        {
            Debug.Log("Not enough currency or already owned.");
        }
    }

    private void EquipSelected()
    {
        if (_selected == null || !_selected.IsUnlocked()) return;

        SetEquippedKey(_selected.trailKey);
        RefreshAllCards();
        UpdateButtons();

        // --- Apply to live trail material ---
        if (trailMaterial != null)
        {
            trailMaterial.color = _selected.GetColor();
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
    private string TrimTrailPrefix(string key)
    {
        const string prefix = "TrailUnlocked_Trail_";
        if (!string.IsNullOrEmpty(key) && key.StartsWith(prefix))
            return key.Substring(prefix.Length);
        return key;
    }

    // --- Initial highlight on open/start ---
    private void RestoreInitialSelectionHighlight()
    {
        // Prefer last explicit selection if present
        string selectedKey = PlayerPrefs.GetString("SelectedTrailKey", "");

        TrailCard card = null;

        if (!string.IsNullOrEmpty(selectedKey))
        {
            card = cards.FirstOrDefault(c =>
                c && string.Equals(c.trailKey, selectedKey, System.StringComparison.OrdinalIgnoreCase));
        }

        // Fallback to EQUIPPED trail if no saved selection
        if (card == null)
        {
            string eq = GetEquippedKey();
            if (!string.IsNullOrEmpty(eq))
            {
                card = cards.FirstOrDefault(c =>
                    c && string.Equals(c.trailKey, eq, System.StringComparison.OrdinalIgnoreCase));
            }
        }

        // Final fallback: first existing card
        if (card == null)
            card = cards.FirstOrDefault(c => c != null);

        if (card != null)
        {
            // Set internal state + visuals so everything stays consistent
            _selected = card;
            _selected.SetSelectedVisual(true);
            _lastSelectedForHighlight = _selected;

            // Keep preview/buttons in sync
            UpdatePreview();
            UpdateButtons();
        }
    }
}
