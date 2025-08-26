using UnityEngine;

public static class JoystickInput
{
    // 0..1 magnitude; normalized direction (0,0 when idle)
    private static Vector2 _vector = Vector2.zero;
    private static bool _pressed = false;

    public static Vector2 Direction => _vector.sqrMagnitude > 0f ? _vector.normalized : Vector2.zero;
    public static float Magnitude => Mathf.Clamp01(_vector.magnitude);
    public static Vector2 Vector => _vector; // already 0..1 radius space
    public static bool Pressed => _pressed;

    // Called by MobileJoystick
    public static void SetVector(Vector2 v01) => _vector = Vector2.ClampMagnitude(v01, 1f);
    public static void SetPressed(bool pressed) => _pressed = pressed;

    public static void Reset()
    {
        _vector = Vector2.zero;
        _pressed = false;
    }
}
