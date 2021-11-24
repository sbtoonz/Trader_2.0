using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Trader20
{
    public class Patches
    {
        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        public static class ZNetPatch
        {
            public static void Prefix(ZNetScene __instance)
            {
                if (__instance.m_prefabs.Count <= 0) return;
                 Utilities.LoadAssets(Trader20.assetBundle, __instance);
                 Trader20.Knarr = __instance.GetPrefab("Knarr");
            }
        }

        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Awake))]
        public static class ItemListPatch
        {
            public static void Prefix(StoreGui __instance)
            {
                var newscreen = ZNetScene.instance.GetPrefab("CustomTrader");
                if (newscreen)
                {
                    Trader20.coins = ZNetScene.instance.GetPrefab("Coins").GetComponent<ItemDrop>().m_itemData.GetIcon();
                    Trader20.CustomTraderScreen = GameObject.Instantiate(newscreen, __instance.GetComponentInParent<Localize>().transform, false);
                    OdinStore.instance.Bkg1.sprite = __instance.transform.Find("Store/bkg").GetComponent<Image>().sprite;
                    OdinStore.instance.Bkg2.sprite = Object.Instantiate(__instance.transform.Find("Store/border (1)").GetComponent<Image>().sprite);
                    OdinStore.instance.Coins.sprite = Trader20.coins;
                    OdinStore.instance.ButtonImage.sprite = Object.Instantiate(__instance.transform.Find("Store/BuyButton").GetComponent<Image>().sprite);
 
                }
                
               
                //Fill CustomTrader store
                if(ObjectDB.instance.m_items.Count <= 0) return;
                Dictionary<string, ItemDataEntry> entry = new();
                List<Dictionary<string, ItemDataEntry>> listEntry = new();
                if (!File.Exists(Trader20.paths + "/trader_config.yaml"))
                {
                   var file = File.Create(Trader20.paths + "/trader_config.yaml");
                   file.Close();
                }
                if (File.ReadLines(Trader20.paths+"/trader_config.yaml").Count() != 0)
                {
                    var file = File.OpenText(Trader20.paths + "/trader_config.yaml");
                    var entry_ = YMLParser.ReadSerializedData(file.ReadToEnd());
                    List<Dictionary<string, ItemDataEntry>> PopulatedList = new List<Dictionary<string, ItemDataEntry>>();
                    PopulatedList.Add(entry_);
                    foreach (var store in PopulatedList)
                    {
                        foreach (KeyValuePair<string, ItemDataEntry> VARIABLE in store)
                        {
                            var drop = ObjectDB.instance.GetItemPrefab(VARIABLE.Key)
                                .GetComponent<ItemDrop>();
                            OdinStore.instance.AddItemToDict(drop, VARIABLE.Value.ItemCostInt, VARIABLE.Value.ItemCount);
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Menu), nameof(Menu.Update))]
        public class PreventMainMenu
        {
            public static bool AllowMainMenu = true;
            private static bool Prefix() => !OdinStore.instance.IsActive() && AllowMainMenu;
        }

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations))]
        public static class SpawnKnarr
        {
            private static void Prefix(ZoneSystem __instance)
            {
                Location knarrLocation = new Location();
                knarrLocation = ZNetScene.instance.GetPrefab("Knarr").GetComponent<Location>();
                knarrLocation.m_clearArea = true;
                knarrLocation.m_exteriorRadius = 10;
                knarrLocation.m_hasInterior = false;
                knarrLocation.m_noBuild = true;
                
                foreach (GameObject gameObject in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (gameObject.name == "_Locations" && gameObject.transform.Find("Misc") is Transform locationMisc)
                    {
                        GameObject altarCopy = Object.Instantiate(ZNetScene.instance.GetPrefab("Knarr"), locationMisc, true);
                        altarCopy.name = ZNetScene.instance.GetPrefab("Knarr").name;
                        __instance.m_locations.Add(new ZoneSystem.ZoneLocation
                        {
                            m_randomRotation = true,
                            m_minAltitude = 10,
                            m_maxAltitude = 1000,
                            m_maxDistance = 1500,
                            m_quantity = 1,
                            m_biome = Heightmap.Biome.Meadows,
                            m_prefabName = ZNetScene.instance.GetPrefab("Knarr").name,
                            m_enable = true,
                            m_minDistanceFromSimilar = 100,
                            m_prioritized = true,
                            m_forestTresholdMax = 5,
                            m_unique = true
                        });
                    }
                }
            }
        }
        
    }
}