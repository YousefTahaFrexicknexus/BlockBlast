using UnityEditor;
using UnityEngine;

public static class MagicalWorldsArtQA
{
    [MenuItem("Tools/Magical Worlds/Verify Enchanted Garden Art")]
    private static void Verify()
    {
        string[] names = { "JourneyMap", "Gameplay", "Collections", "Dormant", "Awakening", "FullBloom" };
        const string folder = "Assets/MagicalWorlds/Art/EnchantedGarden/Backgrounds/";
        int passed = 0;

        foreach (string name in names)
        {
            string path = folder + "EnchantedGarden-" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite == null || sprite.rect.width != 1080f || sprite.rect.height != 1920f)
            {
                Debug.LogError("Magical Worlds art QA failed: " + path);
                continue;
            }

            passed++;
        }

        const string gameplayPath = "Assets/Resources/MagicalWorlds/Backgrounds/EnchantedGarden-Gameplay.png";
        AssetDatabase.ImportAsset(gameplayPath, ImportAssetOptions.ForceUpdate);
        Sprite gameplaySprite = AssetDatabase.LoadAssetAtPath<Sprite>(gameplayPath);

        if (gameplaySprite == null || gameplaySprite.rect.width != 1080f || gameplaySprite.rect.height != 1920f)
        {
            Debug.LogError("Magical Worlds runtime gameplay Sprite is missing or has incorrect dimensions.");
        }
        else
        {
            passed++;
        }

        Debug.Log("Magical Worlds Art QA: " + passed + "/7 sprites imported and verified at 1080x1920.");
    }
}
