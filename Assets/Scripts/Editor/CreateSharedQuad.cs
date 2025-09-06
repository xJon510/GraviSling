using UnityEngine;
using UnityEditor;
using System.IO;

public static class CreateSharedQuad
{
    [MenuItem("Tools/Flipbook/Create Shared Quad Mesh Asset")]
    public static void Create()
    {
        var m = new Mesh();
        m.name = "SharedQuad";

        // +Z facing quad, 1x1, centered
        var verts = new Vector3[]
        {
            new(-0.5f,-0.5f,0f), new(0.5f,-0.5f,0f),
            new(0.5f, 0.5f,0f),  new(-0.5f, 0.5f,0f)
        };
        var uv = new Vector2[]
        {
            new(0f,0f), new(1f,0f), new(1f,1f), new(0f,1f)
        };
        var cols = new Color[] { Color.white, Color.white, Color.white, Color.white };
        var tris = new int[] { 0, 1, 2, 0, 2, 3 };

        m.SetVertices(verts);
        m.SetUVs(0, uv);
        m.SetColors(cols);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        m.RecalculateNormals(); // not strictly needed for Unlit

        var folder = "Assets/SharedMeshes";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "SharedMeshes");

        var path = Path.Combine(folder, "SharedQuad.asset");
        AssetDatabase.CreateAsset(m, path);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Shared Quad", $"Created: {path}", "OK");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }
}
