using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class TextToTMPConverter : EditorWindow
{
    private static int convertedCount = 0;
    private static int errorCount = 0;
    private static TMP_FontAsset defaultFont;

    [MenuItem("Tools/Convert Text to TMP_Text")]
    static void ConvertAll()
    {
        convertedCount = 0;
        errorCount = 0;

        // Get default TMP font
        defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont == null)
        {
            Debug.LogError("Could not find default TMP font 'LiberationSans SDF'. Import TMP Essentials first!");
            return;
        }

        Debug.Log("=== Starting Text → TMP_Text Conversion ===");
        Debug.Log($"Using default font: {defaultFont.name}");

        // Find all prefabs
        string[] allPrefabs = AssetDatabase.GetAllAssetPaths()
            .Where(path => path.EndsWith(".prefab"))
            .ToArray();

        Debug.Log($"Scanning {allPrefabs.Length} prefabs...\n");

        foreach (string prefabPath in allPrefabs)
        {
            ConvertPrefab(prefabPath);
        }

        Debug.Log("\n=== Conversion Complete ===");
        Debug.Log($"✓ Converted: {convertedCount} Text components");
        Debug.Log($"✗ Errors: {errorCount}");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (convertedCount > 0)
        {
            Debug.Log("\n<color=green>SUCCESS! Rebuild your AssetBundle now.</color>");
        }
    }

    static void ConvertPrefab(string prefabPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;

        // Find all Text components (including inactive ones)
        Text[] textComponents = prefab.GetComponentsInChildren<Text>(true);
        if (textComponents.Length == 0) return;

        Debug.Log($"[{prefabPath}] Found {textComponents.Length} Text component(s)");

        // Instantiate prefab for editing
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            Debug.LogError($"Failed to instantiate prefab: {prefabPath}");
            errorCount++;
            return;
        }

        bool modified = false;

        // Re-find components in instance (not original prefab)
        Text[] instanceTextComponents = instance.GetComponentsInChildren<Text>(true);

        foreach (Text oldText in instanceTextComponents)
        {
            if (ConvertTextComponent(oldText))
            {
                modified = true;
                convertedCount++;
            }
        }

        if (modified)
        {
            // Save changes back to prefab
            PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            Debug.Log($"  ✓ Saved: {prefabPath}");
        }

        // Cleanup instance
        DestroyImmediate(instance);
    }

    static bool ConvertTextComponent(Text oldText)
    {
        GameObject go = oldText.gameObject;
        string objPath = GetFullPath(go);

        try
        {
            // Store old values
            string text = oldText.text;
            Color color = oldText.color;
            int fontSize = oldText.fontSize;
            FontStyle fontStyle = oldText.fontStyle;
            TextAnchor alignment = oldText.alignment;
            bool raycastTarget = oldText.raycastTarget;
            bool enabled = oldText.enabled;

            // Remove old Text component
            DestroyImmediate(oldText);

            // Add TMP_Text component
            TextMeshProUGUI tmpText = go.AddComponent<TextMeshProUGUI>();

            // Copy properties
            tmpText.text = text;
            tmpText.color = color;
            tmpText.fontSize = fontSize;
            tmpText.fontStyle = ConvertFontStyle(fontStyle);
            tmpText.alignment = ConvertAlignment(alignment);
            tmpText.raycastTarget = raycastTarget;
            tmpText.enabled = enabled;

            // Assign default font
            tmpText.font = defaultFont;

            // Set recommended settings
            tmpText.enableWordWrapping = true;
            tmpText.overflowMode = TextOverflowModes.Overflow;

            Debug.Log($"    → Converted: {objPath}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"    ✗ Failed to convert {objPath}: {e.Message}");
            errorCount++;
            return false;
        }
    }

    static FontStyles ConvertFontStyle(FontStyle style)
    {
        switch (style)
        {
            case FontStyle.Bold:
                return FontStyles.Bold;
            case FontStyle.Italic:
                return FontStyles.Italic;
            case FontStyle.BoldAndItalic:
                return FontStyles.Bold | FontStyles.Italic;
            default:
                return FontStyles.Normal;
        }
    }

    static TextAlignmentOptions ConvertAlignment(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                return TextAlignmentOptions.TopLeft;
            case TextAnchor.UpperCenter:
                return TextAlignmentOptions.Top;
            case TextAnchor.UpperRight:
                return TextAlignmentOptions.TopRight;
            case TextAnchor.MiddleLeft:
                return TextAlignmentOptions.Left;
            case TextAnchor.MiddleCenter:
                return TextAlignmentOptions.Center;
            case TextAnchor.MiddleRight:
                return TextAlignmentOptions.Right;
            case TextAnchor.LowerLeft:
                return TextAlignmentOptions.BottomLeft;
            case TextAnchor.LowerCenter:
                return TextAlignmentOptions.Bottom;
            case TextAnchor.LowerRight:
                return TextAlignmentOptions.BottomRight;
            default:
                return TextAlignmentOptions.Center;
        }
    }

    static string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }

    [MenuItem("Tools/Count Old Text Components")]
    static void CountOldText()
    {
        Debug.Log("=== Counting old Text components ===");

        int totalText = 0;
        int totalTMP = 0;

        string[] allPrefabs = AssetDatabase.GetAllAssetPaths()
            .Where(path => path.EndsWith(".prefab"))
            .ToArray();

        foreach (string prefabPath in allPrefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            int textCount = prefab.GetComponentsInChildren<Text>(true).Length;
            int tmpCount = prefab.GetComponentsInChildren<TextMeshProUGUI>(true).Length;

            if (textCount > 0)
            {
                Debug.Log($"[OLD TEXT] {prefabPath}: {textCount} Text components");
                totalText += textCount;
            }

            totalTMP += tmpCount;
        }

        Debug.Log("\n=== Summary ===");
        Debug.Log($"Old Text components: {totalText}");
        Debug.Log($"TMP_Text components: {totalTMP}");

        if (totalText > 0)
        {
            Debug.LogWarning($"⚠ Found {totalText} old Text components that need conversion!");
        }
        else
        {
            Debug.Log("✓ No old Text components found!");
        }
    }
}
