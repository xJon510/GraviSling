using UnityEngine;
using UnityEngine.SceneManagement;

public class Title2Game : MonoBehaviour
{
    [Header("Target Scene")]
    public string sceneName = "MainGame";

    // Call this from your UI Button's OnClick event
    public void LoadGame()
    {
        SceneManager.LoadScene(sceneName);
    }
}
