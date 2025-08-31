using UnityEngine;

public class DelayClose : MonoBehaviour
{
    [Tooltip("How long to wait before disabling this GameObject.")]
    public float delay = 1f;

    private bool hasRun = false;

    void Awake()
    {
        // Only run once at startup
        Invoke(nameof(DisableSelf), delay);
        hasRun = true;
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}
