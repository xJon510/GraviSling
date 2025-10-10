using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class PairEquipSync
{
    public static bool Active = false;
}

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

    public ShipShopUI ShipShopManager;
    private static string s_SelectedKeyThisScene = null;
    private static int s_SceneToken = -1;

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
        EnsureSceneToken();
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
            s_SelectedKeyThisScene = _selected.trailKey;

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

    public void EquipSelected()
    {
        if (_selected == null || !_selected.IsUnlocked()) return;

        // Prevent ping-pong while we equip both sides at once
        if (!PairEquipSync.Active)
        {
            PairEquipSync.Active = true;
            try
            {
                // 1) Equip the selected trail locally
                SetEquippedKey(_selected.trailKey);
                RefreshAllCards();
                UpdateButtons();

                // 2) Also equip the currently selected ship (if any & different AND unlocked)
                if (ShipShopManager != null)
                {
                    string shipSelectedKey = ShipShopManager.GetSelectedKey();
                    string shipEquippedKey = ShipShopManager.GetEquippedKeyPublic();

                    bool changedShip = false;
                    if (!string.IsNullOrEmpty(shipSelectedKey)
                        && shipSelectedKey != shipEquippedKey
                        && ShipShopManager.IsUnlocked(shipSelectedKey))
                    {
                        changedShip = ShipShopManager.TrySetEquippedKeyPublic(shipSelectedKey);
                    }

                    if (!changedShip)
                    {
                        // Do NOT alter ship; just refresh visuals against the real equipped ship
                        ShipShopManager.RefreshUIWithEquippedKey();
                    }
                }

                // 3) Apply live material color (your existing behavior)
                if (trailMaterial != null)
                {
                    trailMaterial.color = _selected.GetColor();
                }
            }
            finally
            {
                PairEquipSync.Active = false;
            }
        }
        else
        {
            // We're being called as part of the pair sync: just equip self quietly
            SetEquippedKey(_selected.trailKey);
            RefreshAllCards();
            UpdateButtons();
            if (trailMaterial != null)
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
        // 1) If player already picked a card in THIS scene, honor that
        TrailCard card = null;
        if (!string.IsNullOrEmpty(s_SelectedKeyThisScene))
        {
            card = cards.FirstOrDefault(c =>
                c && string.Equals(c.trailKey, s_SelectedKeyThisScene, System.StringComparison.OrdinalIgnoreCase));
        }

        // 2) Otherwise, default to EQUIPPED key on first open after scene load
        if (card == null)
        {
            string eq = GetEquippedKey();
            if (!string.IsNullOrEmpty(eq))
            {
                card = cards.FirstOrDefault(c =>
                    c && string.Equals(c.trailKey, eq, System.StringComparison.OrdinalIgnoreCase));
            }
        }

        // 3) Final fallback: first valid card
        if (card == null)
            card = cards.FirstOrDefault(c => c != null);

        if (card != null)
        {
            if (_lastSelectedForHighlight && _lastSelectedForHighlight != card)
                _lastSelectedForHighlight.SetSelectedVisual(false);

            _selected = card;
            _selected.SetSelectedVisual(true);
            _lastSelectedForHighlight = _selected;

            UIPipEmitter.Instance.SetStartColor(card.GetColor());

            UpdatePreview();
            UpdateButtons();
        }
    }


    // Expose currently selected trailKey (null if none)
    public string GetSelectedKey() => _selected ? _selected.trailKey : null;

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

    // Is a given key unlocked?
    public bool IsUnlocked(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        var card = cards.FirstOrDefault(c => c && string.Equals(c.trailKey, key, StringComparison.OrdinalIgnoreCase));
        return card != null && card.IsUnlocked();
    }

    // Is the current selection unlocked?
    public bool IsSelectedUnlocked() => IsUnlocked(GetSelectedKey());

    // Safe public equip: NO-OP if locked (prevents falling back to default)
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
        var eq = GetEquippedKey();
        foreach (var c in cards) if (c) c.Refresh(eq);
        UpdateButtons();
        UpdatePreview();
    }

    private void EnsureSceneToken()
    {
        int token = SceneManager.GetActiveScene().buildIndex;
        s_SelectedKeyThisScene = null;
    }
}
