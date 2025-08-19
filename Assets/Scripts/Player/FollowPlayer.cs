using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform player;
    void LateUpdate()
    {
        transform.position = new Vector3(player.position.x, player.position.y, 0f);
    }
}
