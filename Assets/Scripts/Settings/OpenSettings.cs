using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpenSettings : MonoBehaviour
{
    [Header("Settings UI")]
    [Tooltip("Root GameObject of the Settings UI panel. Can be initially inactive.")]
    public GameObject settingsRoot;

    [Tooltip("CanvasGroup on the Settings panel (optional but recommended for fade). " +
             "If null, will try GetComponent<CanvasGroup>() on settingsRoot.")]
    public CanvasGroup settingsCg;

    [Header("Buttons (optional)")]
    [Tooltip("If assigned, clicking will open/toggle Settings.")]
    public Button openButton;
    [Tooltip("If assigned, clicking will close Settings.")]
    public Button closeButton;

    [Header("Behavior While Open")]
    [Tooltip("Components to disable while the Settings UI is open.")]
    public List<Behaviour> disableBehavioursWhileOpen = new List<Behaviour>();
    [Tooltip("GameObjects to disable while the Settings UI is open.")]
    public List<GameObject> disableObjectsWhileOpen = new List<GameObject>();

    [Header("Optional")]
    [Tooltip("Pause AudioListener while Settings is open.")]
    public bool pauseAudio = false;
    [Tooltip("Set Time.timeScale = 0 while open, restore on close.")]
    public bool pauseTime = false;

    [Header("Fade")]
    public bool useFade = true;
    public float fadeInTime = 0.15f;
    public float fadeOutTime = 0.15f;

    bool _isOpen = false;
    float _prevTimeScale = 1f;
    readonly Dictionary<GameObject, bool> _prevObjectActive = new Dictionary<GameObject, bool>();
    PlayerShipController _psc;
    Coroutine _fadeCo;

    [SerializeField] private GameObject player;   // optional: assign in inspector
    [SerializeField] private string playerTag = "Player";

    Rigidbody2D _rb2d;
    Rigidbody _rb3d;

    void Awake()
    {
        if (openButton) openButton.onClick.AddListener(Toggle);
        if (closeButton) closeButton.onClick.AddListener(Close);

        if (!settingsCg && settingsRoot)
            settingsCg = settingsRoot.GetComponent<CanvasGroup>();

        // Ensure UI starts hidden (safe if already inactive)
        if (settingsRoot && settingsRoot.activeSelf)
        {
            // If it’s active in the scene, start with alpha 0 so it doesn’t pop.
            if (settingsCg) settingsCg.alpha = 0f;
            settingsRoot.SetActive(false);
        }
    }

    // Public entry points (wire these from UnityEvents if you prefer)
    public void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (_isOpen) return;
        if (!settingsRoot) { Debug.LogWarning("[OpenSettings] settingsRoot is not assigned."); return; }

        // Pause options
        if (pauseAudio) AudioListener.pause = true;
        if (pauseTime)
        {
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        // Disable gameplay stuff
        ToggleBehaviours(disableBehavioursWhileOpen, false);
        StoreAndDeactivateObjects(disableObjectsWhileOpen);

        // Pause player drift
        ResolvePlayerRefsIfNeeded();
        PausePlayerPhysics();

        // Show UI (activate first if inactive)
        settingsRoot.SetActive(true);
        if (settingsCg && useFade)
        {
            settingsCg.alpha = 0f;
            StartFade(1f, fadeInTime);
            settingsCg.blocksRaycasts = true;
            settingsCg.interactable = true;
        }

        _isOpen = true;
    }

    public void Close()
    {
        if (!_isOpen) return;

        // Restore gameplay stuff
        ToggleBehaviours(disableBehavioursWhileOpen, true);
        RestoreObjectsToSavedStates();

        // Unpause options
        if (pauseAudio) AudioListener.pause = false;
        if (pauseTime) Time.timeScale = _prevTimeScale;

        // Resume player drift
        ResumePlayerPhysics();

        // Hide UI
        if (settingsRoot)
        {
            if (settingsCg && useFade)
            {
                settingsCg.blocksRaycasts = false;
                settingsCg.interactable = false;
                StartFade(0f, fadeOutTime, deactivateOnComplete: true);
            }
            else
            {
                settingsRoot.SetActive(false);
            }
        }

        _isOpen = false;
    }

    // --- helpers ---

    void ToggleBehaviours(List<Behaviour> list, bool enable)
    {
        if (list == null) return;
        foreach (var b in list)
            if (b) b.enabled = enable;
    }

    void StoreAndDeactivateObjects(List<GameObject> list)
    {
        _prevObjectActive.Clear();
        if (list == null) return;
        foreach (var go in list)
        {
            if (!go) continue;
            _prevObjectActive[go] = go.activeSelf;
            go.SetActive(false);
        }
    }

    void RestoreObjectsToSavedStates()
    {
        foreach (var kv in _prevObjectActive)
            if (kv.Key) kv.Key.SetActive(kv.Value);
        _prevObjectActive.Clear();
    }

    void StartFade(float target, float time, bool deactivateOnComplete = false)
    {
        if (!settingsCg) return;
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeRoutine(target, time, deactivateOnComplete));
    }

    System.Collections.IEnumerator FadeRoutine(float target, float time, bool deactivateOnComplete)
    {
        float start = settingsCg.alpha;
        float t = 0f;
        while (t < time)
        {
            t += (pauseTime ? Time.unscaledDeltaTime : Time.deltaTime);
            settingsCg.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(t / time));
            yield return null;
        }
        settingsCg.alpha = target;

        if (deactivateOnComplete && settingsRoot)
            settingsRoot.SetActive(false);

        _fadeCo = null;
    }

    void ResolvePlayerRefsIfNeeded()
    {
        if (!player)
        {
            var found = GameObject.FindGameObjectWithTag(playerTag);
            if (found) player = found;
        }

        _psc = null;
        _rb2d = null; _rb3d = null;

        if (player)
        {
            _psc = player.GetComponent<PlayerShipController>();
            if (!_psc)
            {
                _rb2d = player.GetComponent<Rigidbody2D>();
                if (!_rb2d) _rb3d = player.GetComponent<Rigidbody>();
            }
        }
    }

    void PausePlayerPhysics()
    {
        if (_psc)
        {
            _psc.PauseDrift();
            return;
        }

        // Fallback if no PlayerShipController
        if (_rb2d)
        {
            _rb2d.simulated = false;
            return;
        }
        if (_rb3d)
        {
            _rb3d.isKinematic = true;
            _rb3d.useGravity = false;
        }
    }

    void ResumePlayerPhysics()
    {
        if (_psc)
        {
            _psc.ResumeDrift();
            return;
        }

        // Fallback if no PlayerShipController
        if (_rb2d)
        {
            _rb2d.simulated = true;
            return;
        }
        if (_rb3d)
        {
            _rb3d.isKinematic = false;
            _rb3d.useGravity = true;
        }
    }
}
