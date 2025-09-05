using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToMainMenu : MonoBehaviour
{
    public void GoToTitle()
    {
        // Only swap if we're not already on TitleScreen
        if (SceneManager.GetActiveScene().name != "TitleScreen")
        {
            SceneManager.LoadScene("TitleScreen");
        }
    }
}
