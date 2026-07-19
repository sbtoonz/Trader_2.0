using UnityEditor;
using System.IO;

public class BuildAssetBundles
{
    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        string outputPath = "Assets/AssetBundles";
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);
        
        BuildPipeline.BuildAssetBundles(outputPath, 
            BuildAssetBundleOptions.None, 
            BuildTarget.StandaloneWindows64);
        
        UnityEngine.Debug.Log("[BuildAssetBundles] Build complete! Output: " + outputPath);
        AssetDatabase.Refresh();
    }
}
