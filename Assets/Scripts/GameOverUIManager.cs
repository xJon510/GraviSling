using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverUIManager : MonoBehaviour
{
    public static GameOverUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] public CanvasGroup rootCanvasGroup;         // The whole panel to enable/disable
    [SerializeField] private TMP_Text flavorText;
    [SerializeField] private TMP_Text distanceText;
    [SerializeField] private TMP_Text speedText;

    [Header("Hide UI References")]
    [SerializeField] private GameObject BHInfo;          // The whole panel to enable/disable
    [SerializeField] private TMP_Text SpeedText;
    [SerializeField] private TMP_Text DistanceTextRuntime;
    [SerializeField] private TMP_Text GemsCollectedText;
    [SerializeField] private TMP_Text CurrencyTotalText;
    [SerializeField] private GameObject levelManager;

    [Header("Flavor Lines")]
    [SerializeField] private string[] randomFlavorLines;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // hide on start
        rootCanvasGroup.alpha = 0f;
        rootCanvasGroup.interactable = false;
        rootCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Call this to trigger the game over screen
    /// </summary>
    public void GameOver(float distanceTravelled, float topSpeed)
    {
        // set values
        flavorText.text = randomFlavorLines[Random.Range(0, randomFlavorLines.Length)];
        distanceText.text = $"Distance Travelled: {distanceTravelled:F0} m";
        speedText.text = $"Top Speed: {topSpeed:F1} km/s";
        GemsCollectedText.text = $"Gems Collected: {PlayerPrefs.GetInt("gemsThisRun", 0)}";
        CurrencyTotalText.text = $"Gems Total: {PlayerPrefs.GetInt("currency", 0)}";

        // fade in
        rootCanvasGroup.alpha = 1f;
        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;

        BHInfo.SetActive(false);
        levelManager.SetActive(false);
        SpeedText.gameObject.SetActive(false);
        DistanceTextRuntime.gameObject.SetActive(false);
    }

    // for your restart button
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        PlayerPrefs.SetInt("gemsThisRun", 0);
        PlayerPrefs.Save();
    }
}
