using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    // PlayerPrefs key for the "don't show again" choice
    public const string DontShowKey = "Tutorial_DontShowAgain";

    [Header("UI")]
    [Tooltip("Root panel for the tutorial UI (if null, this GameObject is used).")]
    public GameObject uiRoot;
    [Tooltip("Exit / Close button for the tutorial.")]
    public Button exitButton;
    [Tooltip("Toggle button for 'Don't show again'.")]
    public Button dontShowButton;
    [Tooltip("Checkmark shown when 'Don't show again' is active.")]
    public GameObject dontShowCheckIcon;

    [Header("Gameplay scripts to disable while tutorial is open")]
    public List<Behaviour> disableWhileOpen = new List<Behaviour>();
    [Header("GameObjects to disable while tutorial is open")]
    public List<GameObject> disableObjectsWhileOpen = new List<GameObject>();

    [Header("Optional")]
    [Tooltip("Also pause AudioListener while tutorial is open.")]
    public bool pauseAudio = false;

    bool dontShow;    // cached preference
    bool active;      // tutorial currently showing

    Dictionary<GameObject, bool> _prevObjectStates = new Dictionary<GameObject, bool>();

    // --- Static helper for other systems (e.g., CutsceneManager) ---
    public static bool ShouldShow()
    {
        return PlayerPrefs.GetInt(DontShowKey, 0) == 0;
    }

    void Awake()
    {
        // Wire buttons once
        if (exitButton) exitButton.onClick.AddListener(ExitTutorial);
        if (dontShowButton) dontShowButton.onClick.AddListener(ToggleDontShow);

        dontShow = PlayerPrefs.GetInt(DontShowKey, 0) == 1;
        ApplyToggleVisual();
    }

    void OnEnable()
    {
        // If user opted out, immediately hide ourselves (in case we were enabled by flow).
        if (!ShouldShow())
        {
            gameObject.SetActive(false);
            return;
        }

        // Show UI and disable gameplay scripts
        (uiRoot ? uiRoot : gameObject).SetActive(true);
        ToggleBehaviours(disableWhileOpen, false);
        StoreAndDeactivateObjects(disableObjectsWhileOpen);
        if (pauseAudio) AudioListener.pause = true;
        active = true;
    }

    void OnDisable()
    {
        // If someone disabled this externally while open, restore state safely.
        if (active)
        {
            ToggleBehaviours(disableWhileOpen, true);
            RestoreObjectsToSavedStates();
            if (pauseAudio) AudioListener.pause = false;
            active = false;
        }
    }

    // === UI Callbacks ===

    public void ExitTutorial()
    {
        // Re-enable gameplay
        ToggleBehaviours(disableWhileOpen, true);
        RestoreObjectsToSavedStates();

        if (pauseAudio) AudioListener.pause = false;

        // Hide UI & disable manager
        if (uiRoot) uiRoot.SetActive(false);
        gameObject.SetActive(false);
        active = false;
    }

    public void ToggleDontShow()
    {
        dontShow = !dontShow;
        PlayerPrefs.SetInt(DontShowKey, dontShow ? 1 : 0);
        PlayerPrefs.Save();
        ApplyToggleVisual();
    }

    void ApplyToggleVisual()
    {
        if (dontShowCheckIcon) dontShowCheckIcon.SetActive(dontShow);
    }

    // === Helpers ===

    void ToggleBehaviours(List<Behaviour> list, bool enable)
    {
        if (list == null) return;
        foreach (var b in list) if (b) b.enabled = enable;
    }

    void StoreAndDeactivateObjects(List<GameObject> list)
    {
        _prevObjectStates.Clear();
        if (list == null) return;
        foreach (var go in list)
        {
            if (!go) continue;
            _prevObjectStates[go] = go.activeSelf;
            go.SetActive(false);
        }
    }

    void RestoreObjectsToSavedStates()
    {
        foreach (var kv in _prevObjectStates)
        {
            if (kv.Key) kv.Key.SetActive(kv.Value);
        }
        _prevObjectStates.Clear();
    }
}
