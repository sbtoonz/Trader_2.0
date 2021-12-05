using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using ServerSync;

namespace Trader20
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Trader20 : BaseUnityPlugin
    {
        private const string ModName = "KnarrTheTrader";
        public const string ModVersion = "0.0.9";
        private const string ModGUID = "com.zarboz.Trader20";
        private static ConfigSync configSync = new(ModGUID) { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion};
        public static readonly CustomSyncedValue<Dictionary<string, ItemDataEntry>> traderConfig = new(configSync, "trader config", new Dictionary<string, ItemDataEntry>());
        internal static GameObject? Knarr;
        internal static GameObject? CustomTraderScreen;
        internal static Sprite? Coins;
        private static Dictionary<string, ItemDataEntry> entry_ { get; set; }
        internal static AssetBundle? AssetBundle { get; set; }
        internal static readonly string Paths = BepInEx.Paths.ConfigPath;
        private static ConfigEntry<bool>? _serverConfigLocked;
        internal static ConfigEntry<string>? CurrencyPrefabName;

        ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group, string name, T value, string description, bool synchronizedSetting = true) => config(group, name, value, new ConfigDescription(description), synchronizedSetting);

        public void Awake()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            AssetBundle = Utilities.LoadAssetBundle("traderbundle")!;
            _serverConfigLocked = config("General", "Lock Configuration", false, "Lock Configuration");
            configSync.AddLockingConfigEntry(_serverConfigLocked);
            if (!File.Exists(Paths + "/trader_config.yaml"))
            {
                File.Create(Paths + "/trader_config.yaml").Close();
            }
            ReadYamlConfigFile(null!, null!);
            traderConfig.ValueChanged += OnValChangUpdateStore;

            CurrencyPrefabName = config("General", "Config Prefab Name", "Coins",
                "This is the prefab name for the currency that Knarr uses in his trades");
            
            SetupWatcher();
            
        }

       

        private static void OnValChangUpdateStore()
        {
            if (!ObjectDB.instance || ObjectDB.instance.m_items.Count <= 0 || ObjectDB.instance.GetItemPrefab("Wood") == null) return;
            OdinStore.instance.DumpDict();
            foreach (var variable in traderConfig.Value)
            {
                var drop = ObjectDB.instance.GetItemPrefab(variable.Key);
                if(drop)
                {
                    OdinStore.instance.AddItemToDict(drop.GetComponent<ItemDrop>(), variable.Value.ItemCostInt,
                        variable.Value.ItemCount, variable.Value.Invcount);
                }

                if (!drop)
                {
                    Debug.LogError("Failed to load trader's item: " + variable.Key);
                    Debug.LogError("Please Check your Prefab name "+ variable.Key);
                   
                }
            }
            OdinStore.instance.ForceClearStore();
        }
        

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths, "trader_config.yaml");
            watcher.Changed += ReadYamlConfigFile;
            watcher.Created += ReadYamlConfigFile;
            watcher.Renamed += ReadYamlConfigFile;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadYamlConfigFile(object sender, FileSystemEventArgs e)
        {
            try
            {
                var file = File.OpenText(Trader20.Paths + "/trader_config.yaml");
                entry_ = YMLParser.ReadSerializedData(file.ReadToEnd());
                file.Close();
                traderConfig.AssignLocalValue(entry_);
            }
            catch
            {
                Debug.LogError("There was an issue loading your trader_config.yaml");
                Debug.LogError($"Please check your config entries for spelling and format!");
            }
            
        }
    }
}