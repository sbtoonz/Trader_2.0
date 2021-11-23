using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
                 
            }
        }

        [HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Awake))]
        public static class ItemListPatch
        {
            public static void Prefix(StoreGui __instance)
            {
                Trader20.CustomTraderScreen = ZNetScene.instance.GetPrefab("CustomTrader");
                var odinStore = Trader20.CustomTraderScreen.GetComponent<OdinStore>();
                GameObject.Instantiate(Trader20.CustomTraderScreen,
                    __instance.GetComponentInParent<Localize>().transform, false);
                odinStore.Bkg1.sprite = __instance.transform.Find("Store").transform.Find("bkg").GetComponent<Image>().sprite;
                odinStore.Bkg2.sprite = __instance.transform.Find("Store").transform.Find("border (1)").GetComponent<Image>().sprite;
                odinStore.Coins.sprite = __instance.transform.Find("Store").transform.Find("coins").GetComponent<Image>().sprite;
                odinStore.ButtonImage.sprite = __instance.transform.Find("Store").transform.Find("BuyButton").GetComponent<Image>().sprite;
                
                
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