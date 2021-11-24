using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using ServerSync;


namespace Trader20
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Trader20 : BaseUnityPlugin
    {
        private const string ModName = "Trader2.0";
        private const string ModVersion = "0.0.1";
        private const string ModGUID = "com.zarboz.Trader20";
        internal static GameObject Knarr;
        internal static GameObject CustomTraderScreen;
        internal static AssetBundle assetBundle { get; set; }
        internal static string paths = Paths.ConfigPath;
    
        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            assetBundle = Utilities.LoadAssetBundle("traderbundle");

            
        }

        
    }
}