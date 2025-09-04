using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public Texture2D cursorTex;
    public Vector2 hotspot;               // pixels from top-left (e.g., center: new Vector2(tex.width/2, tex.height/2))
    public CursorMode mode = CursorMode.Auto;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);    // keep across scenes
        Apply();
    }

    void OnApplicationFocus(bool focus)   // some platforms reset on focus change
    {
        if (focus) Apply();
    }

    public void Apply()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.SetCursor(cursorTex, hotspot, mode);
    }

    public void ClearToSystemDefault()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
