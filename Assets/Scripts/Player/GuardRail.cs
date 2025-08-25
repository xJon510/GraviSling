using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Two-row "guard rail" of asteroids that brackets the player's X position.
/// Asteroids are static in world space (no following/lerp). When the player
/// crosses a cell boundary, we recycle the trailing edge columns to the
/// leading edge. No runtime Instantiate/Destroy after Start.
/// </summary>
public class GuardRail : MonoBehaviour
{
    [Header("Follow Target")]
    public Transform player;

    [Header("Rail Layout")]
    public float railWidth = 60f;      // total width we keep populated
    [Range(1, 4)] public int rows = 2; // 2 rows
    public int columns = 24;           // columns across the width
    public float baseY = 0f;           // center Y of the band
    public float rowSpacing = 12f;     // distance between row centers

    [Header("Cell Padding / Jitter")]
    public float cellPadding = 0.4f;
    [Range(0f, 1f)] public float xJitterFrac = 0.3f;
    public float yJitter = 0.5f;

    [Header("Row Staggering")]
    [Tooltip("Enable to horizontally offset selected rows by a fraction of a cell width.")]
    public bool enableStagger = true;

    [Tooltip("Fraction of a cell (e.g., 0.5 = half-cell).")]
    [Range(-1f, 1f)] public float staggerCells = 0.5f;

    [Tooltip("Offset odd rows (rowIndex % 2 == 1). If false, offsets even rows instead.")]
    public bool staggerOddRows = true;

    [Header("Asteroid Appearance")]
    public GameObject[] asteroidPrefabs;
    public float minScale = 0.8f;
    public float maxScale = 1.3f;
    public float globalRotationOffset = 0f;

    [Header("Seeding (stable per world column)")]
    public int seed = 1337;

    [Header("Optional idle rotation drift")]
    public float minAngularSpeed = -8f; // deg/sec
    public float maxAngularSpeed = 8f; // deg/sec

    [Header("MiniMap")]
    public MiniMapManager minimap;

    // --- internals ---
    private float _cellWidth;
    private int _currentBaseWorldColumn;         // world-column index of the LEFT edge
    private bool _initialized;

    private class Slot
    {
        public Transform t;
        public int row;               // 0..rows-1
        public float angularSpeed;    // deg/sec
        public bool registered;
    }

    // One deque (left->right visual order) per row
    private readonly List<LinkedList<Slot>> _rowSlots = new();
    private System.Random _rng;

    void Awake()
    {
        if (columns < 1) columns = 1;
        if (rows < 1) rows = 1;
        _cellWidth = railWidth / Mathf.Max(1, columns);
        _rng = new System.Random(seed);
        _rowSlots.Clear();
        for (int r = 0; r < rows; r++)
            _rowSlots.Add(new LinkedList<Slot>());
    }

    void Start()
    {
        if (player == null)
            Debug.LogWarning("[GuardRail] No player assigned; rails will still populate at world X=0.");

        if (asteroidPrefabs == null || asteroidPrefabs.Length == 0)
        {
            Debug.LogError("[GuardRail] Assign at least one asteroid prefab.");
            enabled = false;
            return;
        }

        // Pre-create exactly rows*columns asteroids, left -> right per row
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                int pf = _rng.Next(asteroidPrefabs.Length);
                var go = Instantiate(asteroidPrefabs[pf], transform);
                go.name = $"Asteroid r{r} slot{c} pf{pf}";
                var slot = new Slot
                {
                    t = go.transform,
                    row = r,
                    angularSpeed = Mathf.Lerp(minAngularSpeed, maxAngularSpeed, (float)_rng.NextDouble())
                };
                _rowSlots[r].AddLast(slot);
            }
        }

        // Initial placement aligned to the player's current cell
        int baseCol = GetBaseWorldColumn(CurrentCenterX());
        _currentBaseWorldColumn = baseCol;
        PlaceAllForBaseColumn(baseCol);
        _initialized = true;
    }

    void Update()
    {
        // Tiny rotation drift only (purely cosmetic)
        foreach (var row in _rowSlots)
            foreach (var s in row)
                s.t.rotation = Quaternion.Euler(0f, 0f, s.t.rotation.eulerAngles.z + s.angularSpeed * Time.deltaTime + globalRotationOffset);

        int newBase = GetBaseWorldColumn(CurrentCenterX());
        if (!_initialized || newBase == _currentBaseWorldColumn) return;

        int shift = newBase - _currentBaseWorldColumn;
        _currentBaseWorldColumn = newBase;

        // If the player jumped far, just re-place everything
        if (Mathf.Abs(shift) >= columns)
        {
            PlaceAllForBaseColumn(newBase);
            return;
        }

        // Recycle by columns
        if (shift > 0)
        {
            // player moved right: take 'shift' columns from left and move to right
            for (int step = 0; step < shift; step++)
            {
                for (int r = 0; r < rows; r++)
                {
                    var rowList = _rowSlots[r];
                    var leftSlot = rowList.First.Value;
                    rowList.RemoveFirst();
                    rowList.AddLast(leftSlot);

                    int newWorldCol = newBase + (columns - 1); // rightmost column index
                    PlaceOne(leftSlot, r, newWorldCol);
                }
                newBase++; // we’ve effectively advanced one column
            }
        }
        else if (shift < 0)
        {
            // player moved left: take '-shift' columns from right and move to left
            for (int step = 0; step < -shift; step++)
            {
                for (int r = 0; r < rows; r++)
                {
                    var rowList = _rowSlots[r];
                    var rightSlot = rowList.Last.Value;
                    rowList.RemoveLast();
                    rowList.AddFirst(rightSlot);

                    int newWorldCol = newBase; // leftmost column index
                    PlaceOne(rightSlot, r, newWorldCol);
                }
                // newBase stays the same here; we’re filling leftmost repeatedly
            }
        }
    }

    // --- helpers ---

    private float CurrentCenterX() => player ? player.position.x : 0f;

    // Convert the left edge (centerX - width/2) to an integer world column
    private int GetBaseWorldColumn(float centerX)
    {
        float left = centerX - railWidth * 0.5f;
        return Mathf.FloorToInt(left / _cellWidth);
    }

    private void PlaceAllForBaseColumn(int baseWorldColumn)
    {
        for (int r = 0; r < rows; r++)
        {
            int worldCol = baseWorldColumn;
            foreach (var slot in _rowSlots[r])
            {
                PlaceOne(slot, r, worldCol);
                worldCol++;
            }
        }
    }

    private void PlaceOne(Slot slot, int rowIndex, int worldCol)
    {
        // Stable hash for this worldCol,row pair so the same column always looks the same.
        uint h = Hash2D(worldCol, rowIndex, seed);

        // Base cell center (no stagger)
        float cellCenterX = (worldCol + 0.5f) * _cellWidth;

        // Per-row stagger: shift selected rows by a fraction of a cell width.
        // (Assumes you've added: enableStagger, staggerCells, staggerOddRows fields)
        float rowXOffset = 0f;
        bool isOdd = (rowIndex % 2) == 1;
        if (enableStagger && ((staggerOddRows && isOdd) || (!staggerOddRows && !isOdd)))
            rowXOffset = staggerCells * _cellWidth;

        // Vertical placement for this row
        float rowCenterY = baseY + (rowIndex - (rows - 1) * 0.5f) * rowSpacing;

        // Compute jitter safely so we don't spill outside our cell even after stagger.
        float half = _cellWidth * 0.5f;
        float maxXInsideCell = Mathf.Max(0f, half - cellPadding - Mathf.Abs(rowXOffset));
        float xJit = (Frac(h * 1664525u + 1013904223u) * 2f - 1f) * maxXInsideCell * xJitterFrac;

        float yJit = (Frac(h * 22695477u + 1u) * 2f - 1f) * yJitter;

        // Scale & rotation (stable per cell)
        float tScale = Mathf.Lerp(minScale, maxScale, Frac(h * 747796405u + 2891336453u));
        float rot = (Frac(h * 4294957665u + 40499u) * 360f) + globalRotationOffset;

        // Apply transform
        slot.t.position = new Vector3(cellCenterX + rowXOffset + xJit, rowCenterY + yJit, 0f);
        slot.t.localScale = Vector3.one * tScale;
        slot.t.rotation = Quaternion.Euler(0f, 0f, rot);

        // Register to minimap once
        if (!slot.registered && minimap != null)
        {
            minimap.RegisterAsteroid(slot.t);
            slot.registered = true;
        }
    }


    private static float Frac(uint x) => (x & 0xFFFFFFu) / 16777216f; // [0,1)

    private static uint Hash2D(int x, int y, int seed)
    {
        unchecked
        {
            uint h = 2166136261u;
            h = (h ^ (uint)x) * 16777619u;
            h = (h ^ (uint)y) * 16777619u;
            h = (h ^ (uint)seed) * 16777619u;
            h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
            return h;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.2f);
        float cx = (player != null) ? player.position.x : transform.position.x;
        Gizmos.DrawCube(
            new Vector3(cx, baseY, 0f),
            new Vector3(railWidth, Mathf.Max(0.2f, rowSpacing * (rows + 0.25f)), 0.1f)
        );

        // draw faint column lines
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.1f);
        float cw = (columns > 0) ? (railWidth / columns) : railWidth;
        float left = cx - railWidth * 0.5f;
        for (int c = 0; c <= columns; c++)
        {
            float x = left + c * cw;
            Gizmos.DrawLine(new Vector3(x, baseY - rowSpacing, 0f),
                            new Vector3(x, baseY + rowSpacing, 0f));
        }
    }
}
