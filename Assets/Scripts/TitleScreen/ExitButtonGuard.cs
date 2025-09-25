using UnityEngine;
using UnityEngine.UI;

public class ExitButtonGuard : MonoBehaviour
{
    [SerializeField] Button exitButton;

    void Awake()
    {
        exitButton.onClick.AddListener(() => Application.Quit());
    }
}
