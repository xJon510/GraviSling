using UnityEngine;
using UnityEngine.UI;

public class SkipToggle : MonoBehaviour
{
    [Header("Cutscene Skip")]
    public Button skipCutsceneButton;
    public GameObject skipCutsceneCheckmark;
    private bool skipCutscene;

    [Header("Tutorial Skip")]
    public Button skipTutorialButton;
    public GameObject skipTutorialCheckmark;
    private bool skipTutorial;

    // PlayerPrefs keys (keep consistent with TutorialManager & CutsceneManager)
    const string TutorialKey = "Tutorial_DontShowAgain";
    const string CutsceneKey = "Cutscene_DontShowAgain";

    void Awake()
    {
        if (skipCutsceneButton) skipCutsceneButton.onClick.AddListener(ToggleCutscene);
        if (skipTutorialButton) skipTutorialButton.onClick.AddListener(ToggleTutorial);
    }

    void OnEnable()
    {
        // Load states from PlayerPrefs
        skipTutorial = PlayerPrefs.GetInt(TutorialKey, 0) == 1;
        skipCutscene = PlayerPrefs.GetInt(CutsceneKey, 0) == 1;

        ApplyVisuals();
    }

    void ToggleCutscene()
    {
        skipCutscene = !skipCutscene;
        PlayerPrefs.SetInt(CutsceneKey, skipCutscene ? 1 : 0);
        PlayerPrefs.Save();
        ApplyVisuals();
    }

    void ToggleTutorial()
    {
        skipTutorial = !skipTutorial;
        PlayerPrefs.SetInt(TutorialKey, skipTutorial ? 1 : 0);
        PlayerPrefs.Save();
        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        if (skipCutsceneCheckmark) skipCutsceneCheckmark.SetActive(!skipCutscene);
        if (skipTutorialCheckmark) skipTutorialCheckmark.SetActive(skipTutorial);
    }
}
