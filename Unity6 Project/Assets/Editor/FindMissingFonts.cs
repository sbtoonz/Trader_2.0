using UnityEngine;
using UnityEditor;
using TMPro;
using System.Linq;

public class FindMissingTMPFonts : EditorWindow
{
    [MenuItem("Tools/Find Missing TMP Fonts")]
    static void FindMissing()
    {
        Debug.Log("=== Searching for TMP components with missing fonts ===");

        int totalFound = 0;
        int missingFonts = 0;

        // Find all prefabs in the project
        string[] allPrefabs = AssetDatabase.GetAllAssetPaths()
            .Where(path => path.EndsWith(".prefab"))
            .ToArray();

        Debug.Log($"Searching {allPrefabs.Length} prefabs...");

        foreach (string prefabPath in allPrefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            // Get all TMP components in this prefab
            var tmpComponents = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);

            foreach (var tmp in tmpComponents)
            {
                totalFound++;

                if (tmp.font == null)
                {
                    missingFonts++;
                    Debug.LogError($"[MISSING FONT] {prefabPath} → {GetFullPath(tmp.gameObject)}", tmp);
                }
            }
        }

        Debug.Log($"=== Search Complete ===");
        Debug.Log($"Total TMP_Text components found: {totalFound}");
        Debug.Log($"Missing fonts: {missingFonts}");

        if (missingFonts == 0 && totalFound > 0)
        {
            Debug.Log("✓ All TMP components have fonts assigned!");
        }
        else if (totalFound == 0)
        {
            Debug.LogWarning("No TMP_Text components found. Have you converted Text → TMP_Text yet?");
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
}
