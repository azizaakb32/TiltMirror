using UnityEditor;
using UnityEngine;

namespace TiltMirror.EditorTools
{
    // Gives every PNG in Assets/Resources/TiltMirror the right import settings
    // automatically (Sprite, no mipmaps, correct pixels-per-unit, 9-slice for the rounded rect).
    // This works even if the project is a 3D template where textures import as "Default".
    public sealed class TiltMirrorAssetImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("/Resources/TiltMirror/")) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;

            // Game sprites are drawn at 256 px per world unit, UI images at 100.
            importer.spritePixelsPerUnit = path.Contains("/Sprites/") ? 256f : 100f;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);

            if (path.EndsWith("ui_round_rect.png"))
                importer.spriteBorder = new Vector4(48f, 48f, 48f, 48f);
        }
    }
}
