using System.Runtime.Remoting.Messaging;
using HarmonyLib;
using UnityEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

        [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        public static class ObjectDBPatch
        {
            public static void Prefix(ObjectDB __instance)
            {
                
            }
        }
    }
}