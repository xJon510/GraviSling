using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Title2Game : MonoBehaviour
{
    [Header("Target Scene")]
    [SerializeField] private string sceneName = "MainGame";

    [Header("Fade")]
    [Tooltip("Full-screen black Image with a CanvasGroup. Can be inactive initially.")]
    [SerializeField] private CanvasGroup coverCg;
    [SerializeField] private float fadeTime = 0.25f;
    [SerializeField] private bool blockDuringFade = true;

    [SerializeField] private Button SettingsButton;

    bool _isLoading;

    // Call this from your Title button OnClick
    public void LoadGame()
    {
        if (_isLoading) return;

        if (SettingsButton) SettingsButton.interactable = false;

        // If no cover assigned, just hard load
        if (!coverCg)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        StartCoroutine(FadeThenLoad());
    }

    System.Collections.IEnumerator FadeThenLoad()
    {
        _isLoading = true;

        // Make sure the cover is active & ready
        if (!coverCg.gameObject.activeSelf) coverCg.gameObject.SetActive(true);
        if (blockDuringFade) coverCg.blocksRaycasts = true;
        coverCg.interactable = false;

        // Start from current alpha (expect 0)
        float start = coverCg.alpha;
        float t = 0f;

        // Fade using unscaled time so it still works if timescale changes later
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            coverCg.alpha = Mathf.Lerp(start, 1f, k);
            yield return null;
        }
        coverCg.alpha = 1f;

        // Load once fully black
        SceneManager.LoadScene(sceneName);
    }
}
