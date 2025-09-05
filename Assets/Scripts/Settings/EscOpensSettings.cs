using UnityEngine;
using UnityEngine.InputSystem; // new Input System

public class EscOpensSettings : MonoBehaviour
{
    [SerializeField] private OpenSettings settings;

    // We'll create a tiny action at runtime so you don't need to edit your input asset
    private InputAction _toggleAction;

    void OnEnable()
    {
        if (settings == null) return;

        // Create action if needed
        if (_toggleAction == null)
        {
            _toggleAction = new InputAction("ToggleSettings");

            // Keyboard / Gamepad / (VR menu) bindings — add/remove as you like
            _toggleAction.AddBinding("<Keyboard>/escape");
            //_toggleAction.AddBinding("<Gamepad>/start");
            //_toggleAction.AddBinding("<Gamepad>/select");               // options/menu on some pads
            //_toggleAction.AddBinding("<XRController>{LeftHand}/menuButton>");
            //_toggleAction.AddBinding("<XRController>{RightHand}/menuButton>");
        }

        _toggleAction.performed += OnToggle;
        _toggleAction.Enable();
    }

    void OnDisable()
    {
        if (_toggleAction == null) return;
        _toggleAction.performed -= OnToggle;
        _toggleAction.Disable();
    }

    private void OnToggle(InputAction.CallbackContext ctx)
    {
        if (settings != null) settings.Toggle();
    }
}