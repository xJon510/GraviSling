using UnityEngine;
using UnityEngine.UI;

public class TutorialSpacebar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject targetObject;   // The object to check active state of
    [SerializeField] private Image sourceImage;         // The Image component on this object
    [SerializeField] private Sprite activeSprite;       // Sprite when target is active
    [SerializeField] private Sprite inactiveSprite;     // Sprite when target is inactive

    private void Reset()
    {
        sourceImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (!targetObject || !sourceImage) return;

        if (targetObject.activeSelf)
            sourceImage.sprite = activeSprite;
        else
            sourceImage.sprite = inactiveSprite;
    }
}
