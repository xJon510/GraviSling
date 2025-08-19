using UnityEngine;

public class ParallaxMover : MonoBehaviour
{
    public Transform cam;
    public float factor = 0.5f;
    private Vector3 lastCamPos;

    private void Start()
    {
        lastCamPos = cam.position;
    }

    private void LateUpdate()
    {
        // Direction cam moved (world space)
        Vector3 worldDelta = cam.position - lastCamPos;
        lastCamPos = cam.position;

        // Convert that delta into the local space of THIS holder
        Vector3 localDelta = transform.parent.InverseTransformVector(worldDelta);

        // Apply scaled parallax in local space (keeps coords small -> no stutter)
        transform.localPosition += localDelta * factor;
    }
}
