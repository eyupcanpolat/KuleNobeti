using System;
using UnityEditor;
using UnityEngine;

namespace KuleNobeti.Editor
{
    // Import rules are confined to the nine CC0 maps included with this room.
    public sealed class ServiceRoomTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Art/ServiceRoom/Textures/", StringComparison.Ordinal)) return;
            Configure((TextureImporter)assetImporter, assetPath);
        }

        internal static void Configure(TextureImporter importer, string path)
        {
            bool normal = path.Contains("_nor_gl_");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal && !path.Contains("_arm_");
            importer.convertToNormalmap = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.isReadable = false;
        }
    }

    [InitializeOnLoad]
    public static class ServiceRoomAssets
    {
        private const string Textures = "Assets/Art/ServiceRoom/Textures/";
        private const string Materials = "Assets/Resources/ServiceRoom/";
        private static readonly string[] Sets = { "concrete_floor_worn_001", "concrete_wall_003", "green_metal_rust" };
        private static bool pending = true;
        private static double nextAttempt;

        static ServiceRoomAssets() => EditorApplication.update += PrepareWhenIdle;

        private static void PrepareWhenIdle()
        {
            if (!pending || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.timeSinceStartup < nextAttempt) return;
            nextAttempt = EditorApplication.timeSinceStartup + 2;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/ServiceRoom/ServiceSurface.shader");
            if (shader == null) return;
            foreach (string set in Sets)
                foreach (string map in new[] { "diff", "nor_gl", "arm" })
                    if (AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + set + "_" + map + "_1k.jpg") == null) return;
            pending = false;
            PrepareMaterials();
        }

        [MenuItem("YazLab/7 - Servis Odası Malzemelerini Hazırla")]
        public static void PrepareMaterials()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/ServiceRoom/ServiceSurface.shader");
            if (shader == null || ShaderUtil.ShaderHasError(shader))
                throw new InvalidOperationException("Servis odası yüzey shader'ı derlenemedi.");
            foreach (string set in Sets)
                foreach (string map in new[] { "diff", "nor_gl", "arm" })
                {
                    string path = Textures + set + "_" + map + "_1k.jpg";
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) throw new InvalidOperationException("Eksik servis odası dokusu: " + path);
                    bool normal = map == "nor_gl";
                    if (importer.textureType != (normal ? TextureImporterType.NormalMap : TextureImporterType.Default) ||
                        importer.sRGBTexture != (map == "diff") || importer.anisoLevel != 8)
                    {
                        ServiceRoomTextureImporter.Configure(importer, path);
                        importer.SaveAndReimport();
                    }
                }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/ServiceRoom"))
                AssetDatabase.CreateFolder("Assets/Resources", "ServiceRoom");
            Create("FloorConcrete", Sets[0], shader, new Color(.86f, .87f, .86f), 1.05f, 1, .85f, .75f, 0);
            Create("WallConcrete", Sets[1], shader, new Color(.75f, .77f, .75f), 1, .75f, .65f, .55f, 0);
            Create("PaintedMetal", Sets[2], shader, new Color(.80f, .77f, .66f), 2.1f, .08f, .55f, .85f, 0);
            Create("DarkMetal", Sets[2], shader, new Color(.19f, .23f, .24f), 1.7f, .05f, .50f, .80f, .15f);
            Create("SafetyYellow", Sets[2], shader, new Color(.88f, .65f, .20f), 2.7f, .05f, .50f, .70f, 0);
            Create("BareSteel", Sets[2], shader, new Color(.60f, .64f, .65f), 1.5f, 0, .35f, .75f, .75f);
        }

        private static void Create(string name, string set, Shader shader, Color color, float gain,
            float saturation, float normal, float smoothness, float metallic)
        {
            string path = Materials + name + ".mat";
            // Existing materials may have been tuned by the user in the Inspector.
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var material = new Material(shader) { name = name };
            material.SetTexture("_MainTex", Load(set, "diff"));
            material.SetTexture("_BumpMap", Load(set, "nor_gl"));
            material.SetTexture("_ArmMap", Load(set, "arm"));
            material.SetColor("_Color", color);
            material.SetFloat("_AlbedoGain", gain);
            material.SetFloat("_Saturation", saturation);
            material.SetFloat("_NormalStrength", normal);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssetIfDirty(material);
        }

        private static Texture2D Load(string set, string map)
            => AssetDatabase.LoadAssetAtPath<Texture2D>(Textures + set + "_" + map + "_1k.jpg");
    }
}
