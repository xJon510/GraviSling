using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TutorialWASDAnimator : MonoBehaviour
{
    [Header("W Key")]
    [SerializeField] private Image wKey;
    [SerializeField] private Sprite wUp;
    [SerializeField] private Sprite wDown;

    [Header("A Key")]
    [SerializeField] private Image aKey;
    [SerializeField] private Sprite aUp;
    [SerializeField] private Sprite aDown;

    [Header("S Key")]
    [SerializeField] private Image sKey;
    [SerializeField] private Sprite sUp;
    [SerializeField] private Sprite sDown;

    [Header("D Key")]
    [SerializeField] private Image dKey;
    [SerializeField] private Sprite dUp;
    [SerializeField] private Sprite dDown;

    [Header("Timing")]
    [Tooltip("How long each key stays pressed.")]
    [SerializeField] private float pressDuration = 0.3f;
    [Tooltip("Delay between keys being pressed.")]
    [SerializeField] private float stepDelay = 0.3f;
    [Tooltip("Pause before the loop restarts.")]
    [SerializeField] private float loopPause = 0.5f;

    private Coroutine animRoutine;

    void OnEnable()
    {
        ResetAllKeys();
        animRoutine = StartCoroutine(AnimateKeysLoop());
    }

    void OnDisable()
    {
        if (animRoutine != null)
            StopCoroutine(animRoutine);
        ResetAllKeys();
    }

    private IEnumerator AnimateKeysLoop()
    {
        while (true)
        {
            yield return StartCoroutine(PressAndRelease(wKey, wDown, wUp));
            yield return StartCoroutine(PressAndRelease(aKey, aDown, aUp));
            yield return StartCoroutine(PressAndRelease(sKey, sDown, sUp));
            yield return StartCoroutine(PressAndRelease(dKey, dDown, dUp));

            yield return new WaitForSeconds(loopPause);
        }
    }

    private IEnumerator PressAndRelease(Image key, Sprite down, Sprite up)
    {
        if (!key) yield break;

        key.sprite = down;
        yield return new WaitForSeconds(pressDuration);

        key.sprite = up;
        yield return new WaitForSeconds(stepDelay);
    }

    private void ResetAllKeys()
    {
        if (wKey) wKey.sprite = wUp;
        if (aKey) aKey.sprite = aUp;
        if (sKey) sKey.sprite = sUp;
        if (dKey) dKey.sprite = dUp;
    }
}
