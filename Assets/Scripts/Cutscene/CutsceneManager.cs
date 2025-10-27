using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Linq;

public class CutsceneManager : MonoBehaviour
{
    public const string DontShowKey = "Cutscene_DontShowAgain";

    public static bool ShouldPlay()
    {
        // default is 0 (play), skip if == 1
        return PlayerPrefs.GetInt(DontShowKey, 0) == 0;
    }

    [Header("Global")]
    [Tooltip("If true, cutscene runs on Start(). Otherwise call StartCutscene() manually.")]
    public bool playOnStart = true;

    public GameObject playerShipObject;
    [Tooltip("Objects to re-enable when the cutscene fully ends.")]
    public List<GameObject> reactivateAfterCutscene = new List<GameObject>();

    [Header("Cover")]
    [Tooltip("CanvasGroup on the full-screen black Cover.")]
    public CanvasGroup coverCg;
    [Tooltip("Seconds to fade the Cover out at the beginning.")]
    public float coverFadeOutTime = 1.0f;

    [Header("Part 1 (parents / loading)")]
    public GameObject part1Root;         // inactive at start
    public CanvasGroup part1Cg;          // alpha = 0 at start
    public TMP_Text part1Text;           // already contains the FULL text in the inspector
    public float part1FadeInTime = 0.6f;

    [Header("Part 2 (reveal ship + text)")]
    public GameObject part2Root;         // inactive at start
    public CanvasGroup part2Cg;          // alpha = 0 at start
    public TMP_Text part2Text;           // already contains FULL Part 2 text
    public float part2FadeInTime = 0.6f;

    [Header("Part 3 (launch + optional extra text)")]
    [Tooltip("Optional extra line shown while/after launching. Leave null to skip.")]
    public TMP_Text part3Text;           // can be left unassigned if you don't want a 3rd caption
    [Tooltip("ParticleSystem on the player's ship (inactive at start).")]
    public ParticleSystem rocketSmoke;   // will SetActive(true) via .gameObject
    [Tooltip("Optional transform of the ship to nudge upward during launch.")]
    public Transform ship;
    [Tooltip("How far to move ship up during launch (units). Set 0 to disable movement.")]
    public Transform risePosition;
    private float launchRiseDistance;
    [Tooltip("Seconds for the launch rise movement.")]
    public float launchRiseTime = 1.2f;
    [Tooltip("Small delay after launch before we end the cutscene.")]
    public float postLaunchHold = 0.6f;

    [Header("Tutorial")]
    public GameObject tutorialManagerGO;

    [Header("Gameplay Hooks")]
    public PlayerEquippedManager equippedManager;

    [Header("Script Toggles")]
    [Tooltip("These components will be disabled at cutscene start, then re-enabled when it ends.")]
    public List<Behaviour> disableDuringCutscene = new List<Behaviour>();

    [Tooltip("These components will be enabled when the cutscene ends (no change at start).")]
    public List<Behaviour> enableAtEnd = new List<Behaviour>();

    [Header("Typewriter")]
    [Tooltip("Characters per second.")]
    public float typeSpeed = 40f;
    [Tooltip("Extra pause on punctuation (seconds).")]
    public float commaPause = 0.06f;
    public float periodPause = 0.12f;
    [Tooltip("Optional blip SFX while typing.")]
    public AudioSource typeSfx;

    [Header("Input")]
    [Tooltip("Keyboard keys to advance (Input System).")]
    public Key[] advanceKeys = new Key[] { Key.Space, Key.Enter, Key.Z, Key.X };

    // --- internals ---
    enum Step { Idle, CoverFade, Part1, Part2, Part3_Launch, Ending }
    Step step = Step.Idle;

    string p1Full, p2Full, p3Full;
    bool isTyping = false;
    bool lineCompleted = false;

    public float advanceCooldown = 0.12f;   // small debounce
    int _lastConsumedFrame = -1;
    float _nextInputTime = 0f;

    void Start()
    {
        CacheFullTexts();

        playOnStart = ShouldPlay();

        if (playOnStart == false)
        {
            FastForwardToGameplay();
            return;
        }

        launchRiseDistance = risePosition.position.y - ship.position.y;

        // Normal path
        PrepareInitialState();
        if (playOnStart) StartCutscene();
    }

    void CacheFullTexts()
    {
        p1Full = part1Text ? part1Text.text : "";
        p2Full = part2Text ? part2Text.text : "";
        p3Full = part3Text ? part3Text.text : "";
    }

    void PrepareInitialState()
    {
        // Ensure cover is fully opaque if provided
        if (coverCg)
        {
            coverCg.alpha = 1f;
            coverCg.gameObject.SetActive(true);
        }

        // Ensure parts are hidden/inactive at boot
        if (part1Root) part1Root.SetActive(false);
        if (part1Cg) part1Cg.alpha = 0f;

        if (part2Root) part2Root.SetActive(false);
        if (part2Cg) part2Cg.alpha = 0f;

        // Clear texts (typewriter will refill)
        if (part1Text) part1Text.text = "";
        if (part2Text) part2Text.text = "";
        if (part3Text) part3Text.text = "";

        ToggleBehaviours(disableDuringCutscene, false);
    }

    public void StartCutscene()
    {
        if (!ShouldPlay())
        {
            FastForwardToGameplay();
            return;
        }

        StopAllCoroutines();
        StartCoroutine(RunCutscene());
    }

    IEnumerator RunCutscene()
    {
        step = Step.CoverFade;

        foreach (var go in reactivateAfterCutscene)
            if (go) go.SetActive(false);

        CameraFollow.Instance?.SetPaused(true);

        // Activate Part 1 before fading cover so it’s ready underneath
        if (part1Root) part1Root.SetActive(true);
        yield return StartCoroutine(FadeCanvas(part1Cg, 0f, 1f, part1FadeInTime, setActiveOnStart: null));

        // Fade out cover
        if (coverCg) yield return StartCoroutine(FadeCanvas(coverCg, 1f, 0f, coverFadeOutTime));
        if (coverCg) coverCg.gameObject.SetActive(false);

        // PART 1 — type text, tap-to-complete, tap-to-advance
        step = Step.Part1;
        yield return StartCoroutine(TypeLine(part1Text, p1Full));
        yield return StartCoroutine(WaitForAdvanceTap());

        // Transition to PART 2
        if (part1Root) part1Root.SetActive(false);
        if (part2Root) part2Root.SetActive(true);
        yield return StartCoroutine(FadeCanvas(part2Cg, 1f, 0f, part2FadeInTime));

        step = Step.Part2;
        yield return StartCoroutine(TypeLine(part2Text, p2Full));
        yield return StartCoroutine(WaitForAdvanceTap());

        // PART 3 — clear Part2 text, start Part3 typewriter, and launch in parallel
        step = Step.Part3_Launch;

        // Clear the previous caption that shares the same spot
        if (part2Text) part2Text.text = "";

        // Ensure Part3 TMP is visible & reset before typing
        if (part3Text)
        {
            part3Text.gameObject.SetActive(true);
            part3Text.text = "";
        }

        Coroutine typingCo = null;
        if (part3Text && !string.IsNullOrEmpty(p3Full))
            typingCo = StartCoroutine(TypeLine(part3Text, p3Full)); // start typing first

        // Launch (particles + rise) while typing continues
        yield return StartCoroutine(LaunchSequence());

        // Make sure typing finished (player may have skipped to complete instantly)
        if (typingCo != null) yield return typingCo;

        // Optional brief hold or allow tap here
        yield return StartCoroutine(WaitForAdvanceTap());

        // END — Reactivate gameplay objects
        step = Step.Ending;
        foreach (var go in reactivateAfterCutscene)
            if (go) go.SetActive(true);

        // Give one frame so SetActive propagates before enabling behaviours (safer for OnEnable order)
        yield return null;

        // Re-enable anything we turned off at start
        ToggleBehaviours(disableDuringCutscene, true);

        // Enable any extras you only want on at the end
        ToggleBehaviours(enableAtEnd, true);

        // Hide Part2 UI nicely (optional)
        if (part2Cg) yield return StartCoroutine(FadeCanvas(part2Cg, part2Cg.alpha, 0f, 0.4f));
        if (part2Root) part2Root.SetActive(false);

        yield return null;

        if (tutorialManagerGO  && TutorialManager.ShouldShow())
        {
            tutorialManagerGO.SetActive(true);
        }

        CameraFollow.Instance?.SetPaused(false, snapToTargetOnResume: true);

        if (equippedManager) equippedManager.ApplyEquippedFromPrefs();

        PlayerPrefs.SetInt(DontShowKey, 1);
        PlayerPrefs.Save();

        // Disable manager if you want
        gameObject.SetActive(false);
    }

    // ---------- helpers ----------

    IEnumerator FadeCanvas(CanvasGroup cg, float from, float to, float time, bool? setActiveOnStart = null)
    {
        if (!cg)
        {
            yield break;
        }

        if (setActiveOnStart.HasValue && cg.gameObject != null)
            cg.gameObject.SetActive(setActiveOnStart.Value);

        float t = 0f;
        cg.alpha = from;
        while (t < time)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / time));
            yield return null;
        }
        cg.alpha = to;
    }

    IEnumerator TypeLine(TMP_Text target, string full)
    {
        if (!target) yield break;

        isTyping = true;
        lineCompleted = false;

        target.text = "";

        float cps = Mathf.Max(1f, typeSpeed);
        for (int i = 0; i < full.Length; i++)
        {
            target.text = full.Substring(0, i + 1);

            // tick SFX (optional)
            if (typeSfx) typeSfx.Play();

            // punctuation pacing
            char c = full[i];
            float delay = 1f / cps;
            if (c == ',' || c == ';') delay += commaPause;
            if (c == '.' || c == '!' || c == '?' || c == '—') delay += periodPause;

            // allow skip to complete
            float elapsed = 0f;
            while (elapsed < delay)
            {
                if (TryConsumeAdvancePress())
                {
                    // finish instantly
                    target.text = full;
                    i = full.Length; // break outer loop
                    break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        isTyping = false;
        lineCompleted = true;
    }

    IEnumerator WaitForAdvanceTap()
    {
        // If still typing, first tap will complete; second tap advances.
        while (true)
        {
            if (TryConsumeAdvancePress())
            {
                if (isTyping)
                {
                    // ignore here; TypeLine handles instant complete
                }
                else if (lineCompleted)
                {
                    yield break; // advance
                }
            }
            yield return null;
        }
    }

    IEnumerator WaitForAdvanceTapOrTimeout(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            if (TryConsumeAdvancePress()) break;
            t += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator Rise(Transform t, float distance, float time)
    {
        Vector3 start = t.position;
        Vector3 end = start + Vector3.up * distance;
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / time));
            t.position = Vector3.LerpUnclamped(start, end, u);
            yield return null;
        }
        t.position = end;
    }

    void ToggleBehaviours(IEnumerable<Behaviour> list, bool enable)
    {
        if (list == null) return;
        foreach (var b in list.Where(b => b != null))
        {
            // If the object is inactive, this still flips the 'enabled' flag;
            // it will start updating once the GameObject is active again.
            b.enabled = enable;
        }
    }

    IEnumerator LaunchSequence()
    {
        // Activate plume
        if (rocketSmoke)
        {
            if (!rocketSmoke.gameObject.activeSelf)
                rocketSmoke.gameObject.SetActive(true);

            if (!rocketSmoke.isPlaying)
                rocketSmoke.Play(true);
        }

        // Rise the ship (0 disables motion)
        if (ship && launchRiseDistance != 0f && launchRiseTime > 0f)
            yield return StartCoroutine(Rise(ship, launchRiseDistance, launchRiseTime));
        else
            yield return null;
    }

    bool IsAdvancePressedRaw()
    {
        // Mouse
        if (Mouse.current?.leftButton.wasPressedThisFrame == true) return true;
        // Touch
        if (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true) return true;
        // Gamepad (A/Cross)
        if (Gamepad.current?.buttonSouth.wasPressedThisFrame == true) return true;

        // Keyboard
        var kb = Keyboard.current;
        if (kb != null && advanceKeys != null)
        {
            foreach (var key in advanceKeys)
            {
                if (key == Key.None) continue;
                var ctl = kb[key];
                if (ctl != null && ctl.wasPressedThisFrame) return true;
            }
        }
        return false;
    }

    bool TryConsumeAdvancePress()
    {
        if (Time.time < _nextInputTime) return false;               // debounce
        if (!IsAdvancePressedRaw()) return false;                   // no press
        if (_lastConsumedFrame == Time.frameCount) return false;    // already consumed this frame

        _lastConsumedFrame = Time.frameCount;                       // consume
        _nextInputTime = Time.time + advanceCooldown;
        return true;
    }

    void FastForwardToGameplay()
    {
        // Kill any cutscene UI if it was left active in the scene/prefab
        if (coverCg) { coverCg.gameObject.SetActive(false); }
        if (part1Root) { part1Root.SetActive(false); }
        if (part2Root) { part2Root.SetActive(false); }
        if (part3Text) { part3Text.gameObject.SetActive(false); }
        if (rocketSmoke) { rocketSmoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); rocketSmoke.gameObject.SetActive(false); }

        // Reactivate gameplay objects
        foreach (var go in reactivateAfterCutscene)
            if (go) go.SetActive(true);

        // Re-enable anything we disabled for the cutscene
        ToggleBehaviours(disableDuringCutscene, true);
        ToggleBehaviours(enableAtEnd, true);

        // Show tutorial if needed
        if (tutorialManagerGO && TutorialManager.ShouldShow())
            tutorialManagerGO.SetActive(true);
        
        // Unpause camera/game
        CameraFollow.Instance?.SetPaused(false, snapToTargetOnResume: true);

        playerShipObject.SetActive(true);

        // We’re done with the manager
        gameObject.SetActive(false);
    }
}
