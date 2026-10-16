using UnityEditor;
using UnityEngine;

public sealed class MagicalWorldsBackgroundImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/MagicalWorlds/Art/EnchantedGarden/Backgrounds/") &&
            !assetPath.StartsWith("Assets/Resources/MagicalWorlds/Backgrounds/"))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.sRGBTexture = true;
        importer.alphaIsTransparency = false;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
    }
}
