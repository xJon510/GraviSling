using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Scrollbar))]
public class StartFull : MonoBehaviour
{
    private Scrollbar scrollbar;

    void Awake()
    {
        scrollbar = GetComponent<Scrollbar>();
        if (scrollbar != null)
        {
            scrollbar.value = 1f;
        }
    }
}
