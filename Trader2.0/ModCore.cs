using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using ServerSync;

namespace Trader20
{
    /// <inheritdoc />
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Trader20 : BaseUnityPlugin
    {
        private const string ModName = "KnarrTheTrader";
        public const string ModVersion = "0.4.4";
        private const string ModGUID = "com.zarboz.KnarrTheTrader";
        internal static ConfigSync configSync = new(ModGUID)
        {
            DisplayName = ModName,
            CurrentVersion = ModVersion, 
            MinimumRequiredVersion = ModVersion,
            ModRequired = false
        };
        public static readonly CustomSyncedValue<Dictionary<string, ItemDataEntry>> TraderConfig 
            = new(configSync, "trader config", new Dictionary<string, ItemDataEntry>());
        internal static GameObject? Knarr;
        internal static GameObject? CustomTraderScreen;
        internal static Sprite? Coins;
        private static Dictionary<string, ItemDataEntry> entry_ { get; set; } = null!;
        internal static AssetBundle? AssetBundle { get; private set; }
        
        internal static readonly string Paths = BepInEx.Paths.ConfigPath;
        internal static readonly string Paths2 = BepInEx.Paths.BepInExAssemblyPath ;
        internal static ConfigEntry<bool>? _serverConfigLocked;
        internal static ConfigEntry<string>? CurrencyPrefabName;
        internal static ConfigEntry<Vector3>? StoreScreenPos;
        internal static ConfigEntry<bool>? RandomlySpawnKnarr;
        internal static ConfigEntry<bool>? LOGStoreSales;
        internal static ConfigEntry<bool>? OnlySellKnownItems;
        internal static ConfigEntry<int>? LuckyNumber;
        internal static ConfigEntry<bool>? ShowMatsWhenHidingRecipes;
        internal static ConfigEntry<bool>? ConfigWriteSalesBuysToYml;
        internal static ConfigEntry<string>? BuyPageLocalization;
        internal static ConfigEntry<string>? SellPageLocalization;
        internal static ConfigEntry<bool>? ConfigShowTabs;
        internal static ConfigEntry<bool>? ConfigShowRepair;
        internal static ConfigEntry<bool>? KnarrPlaySound;


        internal static ManualLogSource knarrlogger = new ManualLogSource(ModName);
        
        ConfigEntry<T> config<T>(string group, string configName, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, configName, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        ConfigEntry<T> config<T>(string group,
            string configName,
            T value,
            string description, bool synchronizedSetting = true) => config(group, configName, value, new ConfigDescription(description), synchronizedSetting);

        public void Awake()
        {
            knarrlogger = base.Logger;
            Assembly assembly = Assembly.GetExecutingAssembly();
            Harmony harmony = new(ModGUID);
            harmony.PatchAll(assembly);
            AssetBundle = Utilities.LoadAssetBundle("traderbundle")!;
            _serverConfigLocked = config("General", "Lock Configuration", false, "Lock Configuration");
            configSync.AddLockingConfigEntry(_serverConfigLocked);
            if (!File.Exists(Paths + Path.DirectorySeparatorChar + "trader_config.yaml"))
            {
                File.Create(Paths + Path.DirectorySeparatorChar + "trader_config.yaml").Close();
            }
            ReadYamlConfigFile(null!, null!);
            TraderConfig.ValueChanged += OnValChangUpdateStore;

            CurrencyPrefabName = config("General", "Config Prefab Name", "Coins",
                "This is the prefab name for the currency that Knarr uses in his trades");

            StoreScreenPos = config("General", "Position Of Trader Prefab On Screen", Vector3.zero,
                "This is the location on screen of the traders for sale screen", false);

            RandomlySpawnKnarr = config("General", "Should Knarr randomly spawn around your BRAND NEW world?", false,
                "Whether or not knarr should spawn using locationsystem on fresh worlds");

            LOGStoreSales = config("General", "Log what/when/to whom knarr sells things", false,
                "This is to log when a player buys an item from Knarr and in what volume");

            OnlySellKnownItems = config("General", "Only sell known recipes", false,
                "If set true Knarr will only vend a player recipes the player has discovered already");

            ShowMatsWhenHidingRecipes = config("General", "Sell Mats when hiding unknown recipes", false,
                "If set true Knarr will still vend materials");
            
            ConfigWriteSalesBuysToYml = config("General", "Change the YML file on sells/buys", true,
                "If set true Knarr will edit the values in the YML file when a player buys or sells items to him");

            LuckyNumber = config("General", "Lucky Number for repairs", 6,
                new ConfigDescription(
                    "This is the lucky number for your repair button if you roll this number your repairs will go through",
                    new AcceptableValueRange<int>(0, 6)));

            BuyPageLocalization = config("Localization", "Buy Page Tab", "Buy", "The Translation for the Buy Page Tab");
            SellPageLocalization = config("Localization", "Sell Page Tab", "Sell", "The Translation for the Sell Page Tab");

            ConfigShowTabs = config("General", "Show the Buy/Sell tabs", true, "This is the setting to enable/disable the selling interface");
            ConfigShowRepair = config("General", "Show the Repair Icon", true, "This is the setting to enable/disable showing of the repair icon");
            KnarrPlaySound = config("General", "Should Knarr play his audio?", true, "This is the setting to enable/disable the audio playback when walking up to knarr");
            
            
            SetupWatcher();

            if (LOGStoreSales.Value)
            {
                if (!File.Exists(Paths + Path.DirectorySeparatorChar+ "TraderSales.log"))
                {
                    File.Create(Paths + Path.DirectorySeparatorChar + "TraderSales.log");
                }
            }
            Game.isModded = true;
            
           
        }
        private static void OnValChangUpdateStore()
        {
            if (!ObjectDB.instance || ObjectDB.instance.m_items.Count <= 0 || ObjectDB.instance.GetItemPrefab("Wood") == null) return;
            OdinStore.instance!.DumpDict();
            foreach (var variable in TraderConfig.Value)
            {
                var drop = ObjectDB.instance.GetItemPrefab(variable.Key);
                ItemDrop id = null;
                if(drop)
                {
                    id = drop.GetComponent<ItemDrop>();
                    if(id == null)
                    {
                        Trader20.knarrlogger.LogError("Failed to load ItemDrop for trader's item: " + variable.Key);
                        continue;
                    }
                    OdinStore.instance.AddItemToDict(id, variable.Value.ItemCostInt,
                        variable.Value.ItemCount, variable.Value.Invcount);
                }

                if (!drop)
                {
                    knarrlogger.LogError("Failed to load trader's item: " + variable.Key);
                    knarrlogger.LogError("Please Check your Prefab name "+ variable.Key);
                   
                }
            }
            //if you are the server shoot the YML file to the client if you are the client write the  YML to local storage?
            if (OdinStore.instance != null)
            {
                OdinStore.instance.ClearStore();
            }
            
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
                var file = File.OpenText(Trader20.Paths + Path.DirectorySeparatorChar +"trader_config.yaml");
                entry_ = YMLParser.ReadSerializedData(file.ReadToEnd());
                file.Close();
                TraderConfig.AssignLocalValue(entry_);
            }
            catch
            {
                knarrlogger.LogError("There was an issue loading your trader_config.yaml");
                knarrlogger.LogError($"Please check your config entries for spelling and format!");
            }
            
        }
        internal void SaveConfig()
        {
            Config.Save();
        }
    }
}