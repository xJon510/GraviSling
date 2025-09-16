// PlayerEquippedManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerEquippedManager : MonoBehaviour
{
    [Header("PlayerPrefs")]
    [Tooltip("Key used by ShipCard to store the currently equipped ship key, e.g., 'Ship_Comet'.")]
    public string equippedKeyPref = "Equipped_ShipKey";

    [Tooltip("If no equipped key is found, fall back to this key.")]
    public string defaultShipKey = "Ship_Comet";

    [Header("Targets")]
    [Tooltip("Object you want to scale based on the equipped ship.")]
    public GameObject targetShipObject;
    public SpriteRenderer targetSprite;      // Sprite driven by animator

    [Tooltip("Animator that should use the ship's specific RuntimeAnimatorController.")]
    public Animator targetAnimator;

    [Tooltip("Optional: reset collider when animator changes")]
    public PolygonCollider2D targetCollider;

    [Header("Animator Sampling")]
    [Tooltip("Play this state at time 0 before rebuilding the collider (e.g., 'Idle'). Leave empty to skip.")]
    public string idleStateName = "Idle";

    [Header("Scale Presets")]
    [Tooltip("Scale to apply when the equipped key matches Default Ship Key (e.g., Ship_Comet).")]
    public Vector3 defaultShipScale = Vector3.one;

    [Tooltip("Scale to apply for all other ships (unless overridden per binding below).")]
    public Vector3 otherShipsScale = Vector3.one;

    [Header("Animator/Scale Mapping")]
    [Tooltip("Map ship keys (or key suffixes) to animator controllers and optional scale overrides.")]
    public List<ShipAnimatorBinding> bindings = new();

    [Header("Collider Refresh")]
    public RefreshMode refreshMode = RefreshMode.RecreateComponent;
    public enum RefreshMode { RecreateComponent, RebuildFromSpriteShape }

    [Serializable]
    public class ShipAnimatorBinding
    {
        [Tooltip("Full key (e.g., 'Ship_Comet') or just the ending (e.g., 'Comet'). Matching is: exact OR EndsWith.")]
        public string shipKeyOrSuffix;

        [Tooltip("Animator controller to use for this ship.")]
        public RuntimeAnimatorController controller;

        [Tooltip("Tick if you want this ship to override the default/other scale.")]
        public bool overrideScale = false;

        [Tooltip("Scale used when overrideScale is ticked.")]
        public Vector3 scale = Vector3.one;
    }

    void Start()
    {
        ApplyEquippedFromPrefs();
    }

    /// <summary>
    /// Reads PlayerPrefs and applies animator + scale.
    /// </summary>
    public void ApplyEquippedFromPrefs()
    {
        string equippedKey = PlayerPrefs.GetString(equippedKeyPref, defaultShipKey);
        ApplyForKey(equippedKey);
    }

    /// <summary>
    /// Directly apply for a provided key (useful when your Shop UI equips on the fly).
    /// </summary>
    public void ApplyForKey(string shipKey)
    {
        if (string.IsNullOrEmpty(shipKey)) shipKey = defaultShipKey;

        // 1) Choose scale
        Vector3 chosenScale = shipKey.Equals(defaultShipKey, StringComparison.OrdinalIgnoreCase)
            ? defaultShipScale : otherShipsScale;

        var bind = FindBinding(shipKey);
        if (bind != null && bind.overrideScale) chosenScale = bind.scale;

        // 2) Swap animator controller (if any)
        if (targetAnimator && bind != null && bind.controller)
            targetAnimator.runtimeAnimatorController = bind.controller;

        // 3) Apply final visual scale now
        if (targetShipObject) targetShipObject.transform.localScale = chosenScale;

        // 4) Force animator to a deterministic sprite instantly (no frame delay)
        if (targetAnimator && !string.IsNullOrEmpty(idleStateName))
        {
            targetAnimator.Play(idleStateName, 0, 0f);
            targetAnimator.Update(0f); // forces SpriteRenderer to this frame immediately
        }

        // 5) Rebuild collider to match the now-updated sprite at the final scale
        if (targetCollider)
        {
            switch (refreshMode)
            {
                case RefreshMode.RecreateComponent:
                    RecreatePolygonCollider();
                    break;
            }
        }
    }

    ShipAnimatorBinding FindBinding(string shipKey)
    {
        foreach (var b in bindings)
        {
            if (b == null || string.IsNullOrEmpty(b.shipKeyOrSuffix)) continue;
            if (shipKey.Equals(b.shipKeyOrSuffix, StringComparison.OrdinalIgnoreCase) ||
                shipKey.EndsWith(b.shipKeyOrSuffix, StringComparison.OrdinalIgnoreCase))
                return b;
        }
        return null;
    }
    System.Collections.IEnumerator ResetColliderNextFrame()
    {
        yield return null; // wait 1 frame
        if (targetCollider != null)
        {
            targetCollider.enabled = false;
            targetCollider.enabled = true; // forces it to recalc bounds from the current sprite
        }
    }
    void RecreatePolygonCollider()
    {
        if (!targetCollider) return;

        var t = targetCollider.transform;
        bool trigger = targetCollider.isTrigger;
        var offset = targetCollider.offset;
        var usedByComposite = targetCollider.usedByComposite;
#if UNITY_EDITOR
        DestroyImmediate(targetCollider);
#else
        Destroy(targetCollider);
#endif
        targetCollider = t.gameObject.AddComponent<PolygonCollider2D>();
        targetCollider.isTrigger = trigger;
        targetCollider.usedByComposite = usedByComposite;
        targetCollider.offset = offset;
        // Unity will regen the shape from current Sprite (via Sprite outline) at current scale.
    }

    // Handy for testing in the editor without re-entering play mode
    [ContextMenu("Apply Equipped From Prefs")]
    void ContextApply() => ApplyEquippedFromPrefs();

    // Optional: call this from your Shop when a ship is equipped (e.g., ShipCard invokes OnFastEquip)
    public void HandleEquippedKeyChanged(string newEquippedKey)
    {
        // Persist (mirrors what ShipCard does, but safe to call)
        PlayerPrefs.SetString(equippedKeyPref, newEquippedKey);
        PlayerPrefs.Save();
        ApplyForKey(newEquippedKey);
    }
}
