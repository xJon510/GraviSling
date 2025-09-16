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
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject MobileUI;
    [SerializeField] private TMP_Text SpeedText;
    [SerializeField] private TMP_Text DistanceTextRuntime;
    [SerializeField] private TMP_Text GemsCollectedText;
    [SerializeField] private TMP_Text CurrencyTotalText;
    [SerializeField] private TMP_Text CurrencyTotalHelpText;
    [SerializeField] private GameObject levelManager;
    [SerializeField] private GameObject MiniMap;

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
    public void GameOver()
    {
        var stats = RunStatsModel.I;
        if (stats == null) return;

        // finalize run and update all-time records
        stats.EndRunAndPersist();

        // set values
        flavorText.text = randomFlavorLines[Random.Range(0, randomFlavorLines.Length)];

        distanceText.text = $"{stats.currentDistance:0} km";
        speedText.text = $"{stats.topSpeedThisRun:0.0} km/s";

        GemsCollectedText.text = PlayerPrefs.GetInt("gemsThisRun", 0).ToString();
        int currency = PlayerPrefs.GetInt("currency", 0);
        CurrencyTotalText.text = currency.ToString();
        CurrencyTotalHelpText.text = currency.ToString();

        // fade in
        rootCanvasGroup.alpha = 1f;
        rootCanvasGroup.interactable = true;
        rootCanvasGroup.blocksRaycasts = true;

        BHInfo.SetActive(false);
        player.SetActive(false);
        MobileUI.SetActive(false);
        levelManager.SetActive(false);
        SpeedText.gameObject.SetActive(false);
        DistanceTextRuntime.gameObject.SetActive(false);
        MiniMap.SetActive(false);
    }

    // for your restart button
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        PlayerPrefs.SetInt("gemsThisRun", 0);
        PlayerPrefs.Save();
    }
}
