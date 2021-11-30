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
                 Utilities.LoadAssets(Trader20.AssetBundle, __instance);
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
                    Trader20.Coins = ZNetScene.instance.GetPrefab(Trader20.CurrencyPrefabName.Value).GetComponent<ItemDrop>().m_itemData
                        .GetIcon();
                    Trader20.CustomTraderScreen = GameObject.Instantiate(newscreen,
                        __instance.GetComponentInParent<Localize>().transform, false);
                    
                    var bkg1 = Object.Instantiate(__instance.transform.Find("Store/bkg").GetComponent<Image>());
                    OdinStore.instance.Bkg1!.sprite = bkg1.sprite;
                    OdinStore.instance.Bkg1.material = bkg1.material;
                    
                    var Bkg2 = Object.Instantiate(__instance.transform.Find("Store/border (1)").GetComponent<Image>());
                    OdinStore.instance.Bkg2!.sprite =Bkg2 .sprite;
                    OdinStore.instance.Bkg2.material = Bkg2.material;
                        
                    OdinStore.instance.Coins!.sprite = Trader20.Coins;
                    OdinStore.instance.ButtonImage!.sprite =
                        Object.Instantiate(__instance.transform.Find("Store/BuyButton").GetComponent<Image>().sprite);

                }


                //Fill CustomTrader store
                if (ObjectDB.instance.m_items.Count <= 0 || ObjectDB.instance.GetItemPrefab("Wood") == null) return;
                Dictionary<string, ItemDataEntry> entry = new();
                List<Dictionary<string, ItemDataEntry>> listEntry = new();
                if (File.ReadLines(Trader20.Paths + "/trader_config.yaml").Count() == 0) return;
                var file = File.OpenText(Trader20.Paths + "/trader_config.yaml");
                var entry_ = YMLParser.ReadSerializedData(file.ReadToEnd());
                List<Dictionary<string, ItemDataEntry>> PopulatedList =
                    new();
                PopulatedList.Add(entry_);
                foreach (var store in PopulatedList)
                {
                    foreach (KeyValuePair<string, ItemDataEntry> variable in store)
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
                }
            }
        }
        

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations))]
        public static class SpawnKnarr
        {
            private static void Prefix(ZoneSystem __instance)
            {
                Location knarrLocation = new();
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
                            m_quantity = 5,
                            m_biome = Heightmap.Biome.BlackForest,
                            m_prefabName = ZNetScene.instance.GetPrefab("Knarr").name,
                            m_enable = true,
                            m_minDistanceFromSimilar = 100,
                            m_prioritized = true,
                            m_forestTresholdMax = 5,
                            m_unique = true,
                            m_iconPlaced = true,
                            m_chanceToSpawn = 100,
                            m_inForest = true,
                            m_iconAlways = true
                        });
                    }
                }
            }
        }
        
    }
}