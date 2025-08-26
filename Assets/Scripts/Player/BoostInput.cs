using UnityEngine;
using UnityEngine.InputSystem;

public static class BoostInput
{
    private static bool mobilePressed = false;
    private static bool releasedFlag = false; // consumed on read

    /// <summary>True while boost is being held (mobile or spacebar).</summary>
    public static bool Pressed
        => mobilePressed || (Keyboard.current != null && Keyboard.current.spaceKey.isPressed);

    /// <summary>True only on the frame boost was released (mobile or spacebar).</summary>
    public static bool WasReleasedThisFrame()
    {
        bool desktop = Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame;
        bool v = releasedFlag || desktop;
        releasedFlag = false; // consume
        return v;
    }

    /// <summary>Called by MobileBoostButton on pointer state changes.</summary>
    public static void SetMobilePressed(bool pressed)
    {
        // rising edge not needed; track falling edge to raise release flag
        if (mobilePressed && !pressed)
            releasedFlag = true;

        mobilePressed = pressed;
    }

    /// <summary>Emergency release (eg, object disabled).</summary>
    public static void ForceRelease()
    {
        if (mobilePressed)
            releasedFlag = true;
        mobilePressed = false;
    }
}
