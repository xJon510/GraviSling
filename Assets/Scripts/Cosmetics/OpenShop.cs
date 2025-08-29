using UnityEngine;
using TMPro;

public class OpenShop : MonoBehaviour
{
    [SerializeField] CanvasGroup shopGroup;   // assign in Inspector
    public TMP_Text TotalBalanceText;
    public GameObject PreviewShipIcon;

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

        PreviewShipIcon.SetActive(true);

        TotalBalanceText.text = $"{PlayerPrefs.GetInt("currency", 0)}";
    }

    // Optional helper if you want to close it somewhere else
    public void Close()
    {
        if (!shopGroup) return;

        shopGroup.alpha = 0f;
        shopGroup.interactable = false;
        shopGroup.blocksRaycasts = false;

        PreviewShipIcon.SetActive(false);
    }

    //dopa down
}
