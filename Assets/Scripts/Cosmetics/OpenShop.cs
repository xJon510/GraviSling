using UnityEngine;
using TMPro;

public class OpenShop : MonoBehaviour
{
    [SerializeField] CanvasGroup shopGroup;   // assign in Inspector
    public GameObject shopUI;
    public TMP_Text TotalBalanceText;
    [SerializeField] CanvasGroup GameOverGroup;

    // Hook this to your Button OnClick()
    public void Open()
    {
        if (!shopGroup)
        {
            Debug.LogWarning("[OpenShop] No CanvasGroup assigned.");
            return;
        }

        shopGroup.alpha = 1f;
        shopGroup.interactable = true;
        shopGroup.blocksRaycasts = true;

        GameOverGroup.alpha = 0f;
        GameOverGroup.interactable = false;
        GameOverGroup.blocksRaycasts = false;

        shopUI.SetActive(true);

        TotalBalanceText.text = $"{PlayerPrefs.GetInt("currency", 0)}";
    }

    // Optional helper if you want to close it somewhere else
    public void Close()
    {
        if (!shopGroup) return;

        shopGroup.alpha = 0f;
        shopGroup.interactable = false;
        shopGroup.blocksRaycasts = false;

        GameOverGroup.alpha = 1f;
        GameOverGroup.interactable = true;
        GameOverGroup.blocksRaycasts = true;

        shopUI.SetActive(false);
    }

    //dopa down
}
