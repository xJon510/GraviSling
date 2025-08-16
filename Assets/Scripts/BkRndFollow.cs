using UnityEngine;

public class BkRndFollow : MonoBehaviour
{
    public Transform player;
    public float parallaxMultiplier = 0.1f;
    public float smooth = 5f;  // how "laggy" the background is

    private Vector3 lastPlayerPos;

    private void Start()
    {
        if (player != null)
            lastPlayerPos = player.position;
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector3 deltaMove = player.position - lastPlayerPos;
        Vector3 targetPos = transform.position + deltaMove * parallaxMultiplier;
        transform.position = Vector3.Lerp(transform.position, targetPos, smooth * Time.deltaTime);

        lastPlayerPos = player.position;
    }
}
