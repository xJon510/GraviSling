using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class TrailCard : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Unique key for this trail (used for PlayerPrefs and equip key).")]
    public string trailKey = "Trail_Default";

    [Tooltip("Display name shown in preview.")]
    public string displayName = "Default Trail";

    [Header("Color Source")]
    [Tooltip("Color is read from this Image component (no manual color entry needed).")]
    public Image colorImage;

    [Header("UI")]
    public GameObject lockBadge;   // shown when locked
    public Button selectButton;    // click to select this card
    public Image checkmarkImage;        // the check icon
    public GameObject selectedHighlight;

    [Header("Checkmark Colors")]
    [Tooltip("Color for unlocked but NOT equipped trails.")]
    public Color checkUnlockedColor = Color.white;
    [Tooltip("Color for the equipped trail.")]
    public Color checkEquippedColor = Color.green; // (0,255,0)

    [Header("Defaults")]
    [Tooltip("If true, this card unlocks itself ONCE on first run.")]
    public bool unlockByDefault = false;

    public event Action<TrailCard> OnSelected;

    [Header("Equipped State")]
    [Tooltip("PlayerPrefs key that stores the currently equipped trail")]
    public string equippedKeyPref = "Equipped_TrailKey";

    // --- Unlock state helpers ---
    public string UnlockPrefKey => $"TrailUnlocked_{trailKey}";
    private const string kTrailsInitFlag = "Trails_DefaultInitialized";

    public bool IsUnlocked()
    {
        // 1 = unlocked, 0 = locked (default)
        return PlayerPrefs.GetInt(UnlockPrefKey, 0) == 1;
    }

    public void Unlock()
    {
        PlayerPrefs.SetInt(UnlockPrefKey, 1);
        PlayerPrefs.Save();
        Refresh();
    }

    public string GetName() => displayName;
    public Color GetColor() => colorImage ? colorImage.color : Color.white;

    void Awake()
    {
        if (selectButton)
        {
            selectButton.onClick.AddListener(() =>
            {
                OnSelected?.Invoke(this);

                // Push color to the emitter singleton
                if (UIPipEmitter.Instance)
                {
                    UIPipEmitter.Instance.SetStartColor(GetColor(), retintAlive: true);
                }
            });
        }

        if (selectedHighlight) selectedHighlight.SetActive(false);

        // One-time default unlock
        if (unlockByDefault && PlayerPrefs.GetInt(kTrailsInitFlag, 0) == 0 && !IsUnlocked())
        {
            Unlock();
            PlayerPrefs.SetInt(kTrailsInitFlag, 1);
            PlayerPrefs.Save();
        }
    }

    void OnEnable() => Refresh();

    public void Refresh()
    {
        string equippedKey = PlayerPrefs.GetString(equippedKeyPref, "");
        Refresh(equippedKey);
    }
    public void Refresh(string equippedKey)
    {
        bool unlocked = IsUnlocked();

        if (lockBadge) lockBadge.SetActive(!unlocked);

        if (checkmarkImage)
        {
            checkmarkImage.gameObject.SetActive(unlocked);

            if (unlocked)
            {
                bool isEquipped = !string.IsNullOrEmpty(equippedKey) &&
                                  string.Equals(trailKey, equippedKey, StringComparison.OrdinalIgnoreCase);
                checkmarkImage.color = isEquipped ? checkEquippedColor : checkUnlockedColor;
            }
        }
    }
    public void SetSelectedVisual(bool isSelected)
    {
        if (selectedHighlight) selectedHighlight.SetActive(isSelected);
    }
}
