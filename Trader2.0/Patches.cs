using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;

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
                 
            }
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Awake))]
        public static class HudPatch
        {
            public static void Prefix(StoreGui __instance)
            {
                Trader20.CustomTraderScreen = ZNetScene.instance.GetPrefab("CustomTrader");
                
                GameObject.Instantiate(Trader20.CustomTraderScreen,
                    __instance.GetComponentInParent<Localize>().transform, false);
            }
        }

        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Awake))]
        public static class ItemListPatch
        {
            public static void Prefix()
            {
                if(ObjectDB.instance.m_items.Count <= 0) return;
                StoreEntry entry = new()
                {
                    _DataEntry = new List<ItemDataEntry>()
                };
                List<StoreEntry> listEntry = new();
                if (!File.Exists(Trader20.paths + "/trader_config.yaml"))
                {
                   var file = File.Create(Trader20.paths + "/trader_config.yaml");
                   file.Close();
                }
                if (File.ReadLines(Trader20.paths+"/trader_config.yaml").Count() != 0)
                {
                    var file = File.OpenText(Trader20.paths + "/trader_config.yaml");
                    var entry_ =YMLParser.ReadSerializedData(file.ReadToEnd());
                    List<StoreEntry> PopulatedList = new List<StoreEntry>();
                    PopulatedList.Add(entry_);
                    foreach (var store in PopulatedList)
                    {
                        foreach (var VARIABLE in store._DataEntry)
                        {
                            if (!VARIABLE.enabled) continue;
                            var drop = ObjectDB.instance.GetItemPrefab(VARIABLE.ItemNameString)
                                .GetComponent<ItemDrop>();
                            OdinStore.instance.AddItemToDict(drop, VARIABLE.ItemCostInt);
                        }
                    }
                }
                else
                {
                    var i = 0;
                    var items = ObjectDB.instance.m_items;
                    foreach (var go in items.Where(go => go.GetComponent<ItemDrop>().m_itemData.m_shared.m_icons.Length >= 1))
                    {
                        ItemDataEntry tempentry = new();
                        tempentry.enabled = false;
                        tempentry.ItemCostInt = 100;
                        tempentry.ItemNameString = go.name;
                        entry._DataEntry.Add(tempentry);
                        listEntry.Add(entry);
                    }

                    foreach (var VARIABLE in listEntry)
                    {
                        var s =YMLParser.Serializers(VARIABLE);
                        YMLParser.WriteSerializedData(s);
                    }
                }

                Trader20.Knarr = ZNetScene.instance.GetPrefab("Knarr");
            }
        }

        [HarmonyPriority(Priority.First)]
        [HarmonyPatch(typeof(Menu), nameof(Menu.IsVisible))]
        private static class MenuPatch
        {
            private static void Postfix(ref bool __result)
            {
                if (OdinStore.instance.IsActive())
                {
                    __result = OdinStore.instance.IsActive();
                }
            }
        }
    }
}