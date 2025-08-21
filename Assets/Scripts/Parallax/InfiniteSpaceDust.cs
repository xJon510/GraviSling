using UnityEngine;

public class InfiniteSpaceDust : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;         // player/world target to compare X against

    [Header("Tiling")]
    public float tileSize = 10000f;  // width of a single panel in world units
    public float recycleThreshold = 1300f; // when player gets this far ahead of leftmost, recycle it

    [Header("Particle Refresh")]
    public bool refreshOnShift = true; // stop/clear/play when moved to avoid visible trails

    private Transform[] tiles;
    private ParticleSystem[] tilePS;   // optional, for refresh

    void Awake()
    {
        int count = transform.childCount;
        if (count == 0)
        {
            Debug.LogWarning("[InfiniteSpaceDust] No children found. Add 3 particle panels as children.");
            tiles = new Transform[0];
            tilePS = new ParticleSystem[0];
            return;
        }

        tiles = new Transform[count];
        tilePS = new ParticleSystem[count];

        for (int i = 0; i < count; i++)
        {
            var child = transform.GetChild(i);
            tiles[i] = child;

            // cache PS if present (recommended)
            tilePS[i] = child.GetComponent<ParticleSystem>();
        }
    }

    void LateUpdate()
    {
        if (tiles == null || tiles.Length == 0 || player == null)
            return;

        // find leftmost by world X
        int leftIdx = 0;
        float leftX = tiles[0].position.x;

        for (int i = 1; i < tiles.Length; i++)
        {
            float x = tiles[i].position.x;
            if (x < leftX)
            {
                leftX = x;
                leftIdx = i;
            }
        }

        // how far ahead the player is from the leftmost panel's world X
        float dist = player.position.x - leftX;

        if (dist > recycleThreshold)
        {
            // move the leftmost panel forward by total span (tileSize * panelCount)
            // use localPosition so it stays aligned under the holder
            var t = tiles[leftIdx];
            t.localPosition += new Vector3(tileSize * tiles.Length, 0f, 0f);

            if (refreshOnShift && tilePS[leftIdx] != null)
            {
                // refresh particle content so it doesn't look like it smeared during teleport
                var ps = tilePS[leftIdx];
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }
    }

#if UNITY_EDITOR
    // helper to auto-wire on inspector changes
    void OnValidate()
    {
        if (player == null)
        {
            // try to guess a player tagged "Player"
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }
    }
#endif
}
