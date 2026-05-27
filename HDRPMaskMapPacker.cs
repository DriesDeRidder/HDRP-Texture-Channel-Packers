#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public class HDRPMaskMapPacker : EditorWindow
{
    private enum SmoothnessSource
    {
        Smoothness,
        Roughness
    }

    private Texture2D metallicTexture;
    private Texture2D aoTexture;
    private Texture2D detailMaskTexture;
    private Texture2D smoothnessOrRoughnessTexture;
    private SmoothnessSource smoothnessSource = SmoothnessSource.Smoothness;

    private float defaultMetallic = 0f;
    private float defaultAO = 1f;
    private float defaultDetailMask = 0f;
    private float defaultSmoothness = 0.5f;

    private string outputName = "";
    private string lastAutoName = "";
    private Texture2D anchorTexture;

    private static readonly string[] RoleSuffixes = new[]
    {
        "ambientocclusion", "ambient_occlusion", "ambient-occlusion",
        "occlusion", "ao",
        "metallic", "metalness", "metal",
        "roughness", "rough",
        "smoothness", "smooth", "gloss", "glossiness",
        "detailmask", "detail_mask", "detail-mask", "detail",
        "maskmap", "mask_map", "mask-map", "mask",
    };

    [MenuItem("Tools/HDRP Mask Map Packer")]
    private static void ShowWindow()
    {
        var window = GetWindow<HDRPMaskMapPacker>();
        window.titleContent = new GUIContent("HDRP Mask Map Packer");
        window.minSize = new Vector2(400, 420);
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("Pack HDRP Lit Mask Map", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "HDRP Lit mask map layout:\n" +
            "  R: Metallic\n" +
            "  G: Ambient Occlusion\n" +
            "  B: Detail Mask\n" +
            "  A: Smoothness\n\n" +
            "Leave any slot empty to use the default value below.",
            MessageType.Info);

        EditorGUILayout.Space();
        metallicTexture = (Texture2D)EditorGUILayout.ObjectField("Metallic (R)", metallicTexture, typeof(Texture2D), false);
        if (metallicTexture == null)
            defaultMetallic = EditorGUILayout.Slider("Default Metallic", defaultMetallic, 0f, 1f);

        aoTexture = (Texture2D)EditorGUILayout.ObjectField("Ambient Occlusion (G)", aoTexture, typeof(Texture2D), false);
        if (aoTexture == null)
            defaultAO = EditorGUILayout.Slider("Default AO", defaultAO, 0f, 1f);

        detailMaskTexture = (Texture2D)EditorGUILayout.ObjectField("Detail Mask (B)", detailMaskTexture, typeof(Texture2D), false);
        if (detailMaskTexture == null)
            defaultDetailMask = EditorGUILayout.Slider("Default Detail Mask", defaultDetailMask, 0f, 1f);

        EditorGUILayout.Space();
        smoothnessSource = (SmoothnessSource)EditorGUILayout.EnumPopup("Smoothness Source", smoothnessSource);
        string smoothnessLabel = smoothnessSource == SmoothnessSource.Smoothness ? "Smoothness (A)" : "Roughness (inverted to A)";
        smoothnessOrRoughnessTexture = (Texture2D)EditorGUILayout.ObjectField(smoothnessLabel, smoothnessOrRoughnessTexture, typeof(Texture2D), false);
        if (smoothnessOrRoughnessTexture == null)
            defaultSmoothness = EditorGUILayout.Slider("Default Smoothness", defaultSmoothness, 0f, 1f);

        EditorGUILayout.Space();
        bool hasAny = metallicTexture || aoTexture || detailMaskTexture || smoothnessOrRoughnessTexture;

        UpdateAnchor();

        string outputDir = anchorTexture ? System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(anchorTexture)) : "Assets";
        outputDir = outputDir.Replace('\\', '/');

        string autoName = anchorTexture
            ? StripRoleSuffix(System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(anchorTexture))) + "_MaskMap"
            : "MaskMap";
        if (string.IsNullOrEmpty(outputName) || outputName == lastAutoName)
        {
            outputName = autoName;
        }
        lastAutoName = autoName;

        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            outputName = EditorGUILayout.TextField("File Name", outputName);
            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                outputName = autoName;
                GUI.FocusControl(null);
            }
        }
        EditorGUILayout.LabelField("Path", $"{outputDir}/{outputName}.png", EditorStyles.miniLabel);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(!hasAny || string.IsNullOrWhiteSpace(outputName)))
        {
            if (GUILayout.Button("Pack Mask Map"))
            {
                PackMaskMap(outputDir, outputName);
            }
        }
    }

    private void UpdateAnchor()
    {
        bool stillReferenced = anchorTexture != null && (
            anchorTexture == metallicTexture
            || anchorTexture == aoTexture
            || anchorTexture == detailMaskTexture
            || anchorTexture == smoothnessOrRoughnessTexture);

        if (!stillReferenced)
        {
            anchorTexture = metallicTexture ?? aoTexture ?? detailMaskTexture ?? smoothnessOrRoughnessTexture;
        }
    }

    private static string StripRoleSuffix(string name)
    {
        string result = name;
        bool stripped;
        do
        {
            stripped = false;
            foreach (string suffix in RoleSuffixes)
            {
                foreach (char sep in new[] { '_', '-', ' ', '.' })
                {
                    string candidate = sep + suffix;
                    if (result.Length > candidate.Length && result.EndsWith(candidate, System.StringComparison.OrdinalIgnoreCase))
                    {
                        result = result.Substring(0, result.Length - candidate.Length);
                        stripped = true;
                        break;
                    }
                }
                if (stripped) break;
            }
        } while (stripped);

        return string.IsNullOrEmpty(result) ? name : result;
    }

    private void PackMaskMap(string outputDir, string fileName)
    {
        Texture2D reference = metallicTexture ?? aoTexture ?? detailMaskTexture ?? smoothnessOrRoughnessTexture;
        int width = reference.width;
        int height = reference.height;

        if (!ValidateTexture(metallicTexture, "Metallic", width, height)) return;
        if (!ValidateTexture(aoTexture, "Ambient Occlusion", width, height)) return;
        if (!ValidateTexture(detailMaskTexture, "Detail Mask", width, height)) return;
        if (!ValidateTexture(smoothnessOrRoughnessTexture, smoothnessSource == SmoothnessSource.Smoothness ? "Smoothness" : "Roughness", width, height)) return;

        Color[] metallicPixels = metallicTexture ? metallicTexture.GetPixels() : null;
        Color[] aoPixels = aoTexture ? aoTexture.GetPixels() : null;
        Color[] detailPixels = detailMaskTexture ? detailMaskTexture.GetPixels() : null;
        Color[] smoothPixels = smoothnessOrRoughnessTexture ? smoothnessOrRoughnessTexture.GetPixels() : null;

        int count = width * height;
        Color[] packed = new Color[count];

        for (int i = 0; i < count; i++)
        {
            float r = metallicPixels != null ? metallicPixels[i].r : defaultMetallic;
            float g = aoPixels != null ? aoPixels[i].r : defaultAO;
            float b = detailPixels != null ? detailPixels[i].r : defaultDetailMask;

            float a;
            if (smoothPixels != null)
            {
                float sample = smoothPixels[i].r;
                a = smoothnessSource == SmoothnessSource.Roughness ? 1f - sample : sample;
            }
            else
            {
                a = defaultSmoothness;
            }

            packed[i] = new Color(r, g, b, a);
        }

        Texture2D packedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        packedTexture.SetPixels(packed);
        packedTexture.Apply();

        byte[] pngData = packedTexture.EncodeToPNG();
        if (pngData != null)
        {
            string maskPath = AssetDatabase.GenerateUniqueAssetPath($"{outputDir}/{fileName}.png");
            System.IO.File.WriteAllBytes(maskPath, pngData);
            AssetDatabase.Refresh();

            TextureImporter maskImporter = AssetImporter.GetAtPath(maskPath) as TextureImporter;
            if (maskImporter != null)
            {
                maskImporter.sRGBTexture = false;
                AssetDatabase.ImportAsset(maskPath, ImportAssetOptions.ForceUpdate);
            }

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath));
        }

        DestroyImmediate(packedTexture);
    }

    private bool ValidateTexture(Texture2D texture, string label, int width, int height)
    {
        if (texture == null) return true;

        if (texture.width != width || texture.height != height)
        {
            EditorUtility.DisplayDialog("Error", $"{label} texture dimensions must match ({width}x{height})", "OK");
            return false;
        }

        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            EditorUtility.DisplayDialog("Error", $"{label} texture must be set to readable in import settings", "OK");
            return false;
        }

        return true;
    }
}
#endif
