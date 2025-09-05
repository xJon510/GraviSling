// Assets/Editor/FlipbookAtlasBaker.cs
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class FlipbookAtlasBaker : EditorWindow
{
    private DefaultAsset sourceFolder; // folder containing your frames (individual sprites)
    private int columns = 8;
    private int rows = 8;
    private string outputPath = "Assets/Flipbooks/MyPlanet_Atlas.png";
    private bool powerOfTwo = false;
    private bool disableMipmaps = true;
    private FilterMode filterMode = FilterMode.Point;

    [MenuItem("Tools/Flipbook Atlas Baker")]
    public static void Open() => GetWindow<FlipbookAtlasBaker>("Flipbook Atlas Baker");

    void OnGUI()
    {
        GUILayout.Label("Source (folder of sprites)", EditorStyles.boldLabel);
        sourceFolder = (DefaultAsset)EditorGUILayout.ObjectField("Frames Folder", sourceFolder, typeof(DefaultAsset), false);

        GUILayout.Space(6);
        GUILayout.Label("Grid", EditorStyles.boldLabel);
        columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", columns));
        rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", rows));

        GUILayout.Space(6);
        GUILayout.Label("Output", EditorStyles.boldLabel);
        outputPath = EditorGUILayout.TextField("PNG Path", outputPath);

        GUILayout.Space(6);
        GUILayout.Label("Import Options", EditorStyles.boldLabel);
        powerOfTwo = EditorGUILayout.Toggle("Force Power of Two", powerOfTwo);
        disableMipmaps = EditorGUILayout.Toggle("Disable Mipmaps", disableMipmaps);
        filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", filterMode);

        GUILayout.Space(10);
        if (GUILayout.Button("Bake Atlas"))
        {
            Bake();
        }
    }

    void Bake()
    {
        if (!sourceFolder)
        {
            EditorUtility.DisplayDialog("Flipbook Atlas Baker", "Please assign a frames folder.", "OK");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(sourceFolder);
        var spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        if (spriteGuids.Length == 0)
        {
            EditorUtility.DisplayDialog("Flipbook Atlas Baker", "No sprites found in the selected folder.", "OK");
            return;
        }

        // Load sprites and sort by name (assumes 00_, 01_, 02_...)
        var sprites = spriteGuids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Select(p => AssetDatabase.LoadAssetAtPath<Sprite>(p))
            .Where(s => s != null)
            .OrderBy(s => NumericKey(s.name))                       // primary: numeric
            .ThenBy(s => s.name, System.StringComparer.Ordinal)     // tie-breaker: name
            .ToList();

        int total = sprites.Count;
        if (columns * rows < total)
        {
            // auto-expand rows to fit if needed
            rows = Mathf.CeilToInt(total / (float)columns);
        }

        // All frames must be the same size
        int fw = Mathf.RoundToInt(sprites[0].rect.width);
        int fh = Mathf.RoundToInt(sprites[0].rect.height);

        int atlasW = fw * columns;
        int atlasH = fh * rows;

        if (powerOfTwo)
        {
            atlasW = NextPowerOfTwo(atlasW);
            atlasH = NextPowerOfTwo(atlasH);
        }

        var atlas = new Texture2D(atlasW, atlasH, TextureFormat.RGBA32, false);
        atlas.name = Path.GetFileNameWithoutExtension(outputPath);

        // Fill transparent
        var clear = new Color32(0, 0, 0, 0);
        var fill = Enumerable.Repeat(clear, atlasW * atlasH).ToArray();
        atlas.SetPixels32(fill);

        // Copy each sprite’s pixels into the atlas grid
        for (int i = 0; i < total; i++)
        {
            int col = i % columns;
            int row = i / columns;

            // Place row 0 at the top (common for flipbooks); flip Y if you prefer bottom origin.
            int x = col * fw;
            int y = (atlasH - (row + 1) * fh);

            var tex = GetReadableTexture(sprites[i]);
            var pixels = tex.GetPixels32();
            // If sprite had padding/trim, we assume frames were tightly packed, so pixels are exactly fw*fh.

            atlas.SetPixels32(x, y, fw, fh, pixels);
        }

        atlas.Apply();

        // Write PNG
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllBytes(outputPath, atlas.EncodeToPNG());
        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

        // Import settings as Sprite (Single)
        var importer = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = filterMode;
            importer.mipmapEnabled = !disableMipmaps ? true : false;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        EditorUtility.DisplayDialog("Flipbook Atlas Baker", $"Baked {total} frames to:\n{outputPath}\n\nGrid: {columns}x{rows}\nFrame size: {fw}x{fh}\nAtlas: {atlasW}x{atlasH}", "Nice!");
    }

    static Texture2D GetReadableTexture(Sprite s)
    {
        var path = AssetDatabase.GetAssetPath(s.texture);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        bool reimport = false;

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            reimport = true;
        }
        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            reimport = true;
        }

        if (reimport) importer.SaveAndReimport();

        var tex = s.texture;
        Rect r = s.rect;
        int x = Mathf.RoundToInt(r.x);
        int y = Mathf.RoundToInt(r.y);
        int w = Mathf.RoundToInt(r.width);
        int h = Mathf.RoundToInt(r.height);

        // Use GetPixels instead of GetPixels32 for rects
        var pix = tex.GetPixels(x, y, w, h, 0);
        var copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
        copy.SetPixels(pix);
        copy.Apply();
        return copy;
    }

    static int NumericKey(string name)
    {
        // Find the first integer anywhere in the name (e.g., "1", "001", "Moon_12")
        var m = System.Text.RegularExpressions.Regex.Match(name, @"\d+");
        if (m.Success && int.TryParse(m.Value, out int n)) return n;
        return int.MaxValue; // names without numbers go last
    }

    static int NextPowerOfTwo(int x)
    {
        int p = 1;
        while (p < x) p <<= 1;
        return p;
    }
}
