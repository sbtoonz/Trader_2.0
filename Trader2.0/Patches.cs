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
                var tmp = ObjectDB.instance.m_items;
                StoreEntry _entry = new();
                ItemDataEntry _itemData = new();
                if (File.ReadLines("config.yaml").Count() != 0)
                {
                    YMLParser.ReadSerializedData();
                    YMLParser.ParseSerializedData();
                }
                else
                {
                    foreach (ItemDrop drop in from GO in tmp where GO.GetComponent<ItemDrop>() != null select GO.GetComponent<ItemDrop>() into drop where drop.m_itemData.m_shared.m_icons.Length > 0 select drop)
                    {
                        _itemData.ItemNameString = Localization.instance.Localize(drop.m_itemData.m_shared.m_name);
                        _itemData.ItemCostInt = 0;
                        _itemData.enabled = false;
                        _entry._DataEntry = _itemData;
                        var lineentry= YMLParser.Serializers(_entry);
                        YMLParser.WriteSerializedData(lineentry);
                    }  
                }
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