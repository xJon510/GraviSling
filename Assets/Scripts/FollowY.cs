using UnityEngine;

public class FollowY : MonoBehaviour
{
    public Transform target;  // the thing to follow

    void Update()
    {
        if (target == null) return;

        Vector3 pos = transform.position;
        pos.y = target.position.y;
        transform.position = pos;
    }
}
