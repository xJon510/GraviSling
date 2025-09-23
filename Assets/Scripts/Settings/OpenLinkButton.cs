using UnityEngine;

public class OpenLinkButton : MonoBehaviour
{
    [Tooltip("The URL to open when this button is clicked.")]
    public string url = "https://example.com";

    public void OpenLink()
    {
        if (!string.IsNullOrEmpty(url))
        {
            Application.OpenURL(url);
        }
        else
        {
            Debug.LogWarning("OpenLinkButton: No URL set!");
        }
    }
}
