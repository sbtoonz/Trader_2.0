using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using AugaUnity;
using HarmonyLib;
using JetBrains.Annotations;
using JoshH.UI;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Trader20
{
    public class Patches
    {
        /// <summary>
        /// 
        /// </summary>
        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        public static class ZNetPatch
        {
            [UsedImplicitly]
            public static void Prefix(ZNetScene __instance)
            {
                if (__instance.m_prefabs.Count <= 0) return;
                Utilities.LoadAssets(Trader20.AssetBundle, __instance);
                Trader20.Knarr = __instance.GetPrefab("Knarr");

            }
        }

        /// <summary>
        /// 
        /// </summary>
        [HarmonyPatch(typeof(Game), nameof(Game.Start))]
        public static class RegisterRPCPatch
        {

            [HarmonyPostfix]
            [UsedImplicitly]
            public static void Postfix()
            {
                    
                ZRoutedRpc.instance.Register<bool>("RemoveKnarrDone", RPC_RemoveKnarrRespons);
                ZRoutedRpc.instance.Register<bool>("RequestRemoveKnarr", RPC_RemoveKnarrReq);
                ZRoutedRpc.instance.Register<bool>("FindKnarrDone", RPC_FindKnarrResponse);
                ZRoutedRpc.instance.Register<Vector3>("SetKnarrMapPin", RPC_SetKnarrMapIcon);
                ZRoutedRpc.instance.Register<string, int, bool>("SendItemInfoToServer", RPC_SendItemInfoToServer);
                ZRoutedRpc.instance.Register<string, int>("SendLogItemToServer", RPC_SendSaleLogInfoToServer);
            }

            
        }

        [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.SetupGui))]
        public static class TestPatch
        {
            [HarmonyPostfix]
            [UsedImplicitly]
            public static void Postfix()
            {
                Localization.instance.AddWord("BuyPage", Trader20.BuyPageLocalization?.Value);
                Localization.instance.AddWord("SellPage", Trader20.SellPageLocalization?.Value);
            }
        }

        private static void RPC_RemoveKnarrReq(long UID, bool s)
        {
            if (!Trader20._serverConfigLocked!.Value)
            {
                // ReSharper disable once HeapView.BoxingAllocation
                ZRoutedRpc.instance.InvokeRoutedRPC("RemoveKnarrDone", true);
            }
            else
            {
                // ReSharper disable once HeapView.BoxingAllocation
                ZRoutedRpc.instance.InvokeRoutedRPC("RemoveKnarrDone", false);
            }
        }
        private static List<ZDO> zdolist = new List<ZDO>();
        private static void RPC_RemoveKnarrRespons(long UID, bool s)
        {
            if (!ZNet.instance.IsServer()) return;
            if (s)
            {
                ZDOMan.instance.GetAllZDOsWithPrefab("Knarr", zdolist);
                if (zdolist.Count <= 0)
                {
                    ZLog.LogError("No instances of Knarr found");
                }

                foreach (var zdo in zdolist)
                {
                    ZDOMan.instance.DestroyZDO(zdo);
                }
            }
            else
            {
                ZLog.LogError("Non Admin invoking removal command");
            }

        }
        
        private static void RPC_FindKnarrResponse(long uid, bool s)
        {
            if (!ZNet.instance.IsServer()) return;
            if (s)
            {
                ZDOMan.instance.GetAllZDOsWithPrefab("Vendor_Knarr", zdolist);
                foreach (var KP in ZoneSystem.instance.m_locationInstances.Where(KP => KP.Value.m_location.m_prefabName == ZNetScene.instance.GetPrefab("Knarr").name))
                {
                    ZLog.LogWarning("Knarr Random Spawn location = " + KP.Value.m_position);
                    ZRoutedRpc.instance.InvokeRoutedRPC(uid, "SetKnarrMapPin", KP.Value.m_position);
                    Minimap.instance.AddPin(KP.Value.m_position, Minimap.PinType.Boss, "Knarr", true, false,
                        Game.instance.GetPlayerProfile().GetPlayerID());
                }
                if (zdolist.Count <= 0)
                {
                    ZLog.LogError("No instances of Knarr found marking potential spawn points");
                }
                foreach (var zdo in zdolist)
                {
                    ZLog.LogWarning("/Spawned Knarr instances at: " + zdo.m_position);
                }
            }
            else
            {
                ZLog.LogError("Non Admin invoking locator command");
            }
        }
        
        private static void RPC_SendItemInfoToServer(long uid, string drop, int stack, bool playerItem)
        {
            if(Utilities.GetConnectionState() != Utilities.ConnectionState.Server) return;
            var id = ZNetScene.instance.GetPrefab(drop).gameObject.GetComponent<ItemDrop>();
            id.m_itemData.m_dropPrefab = ZNetScene.instance.GetPrefab(drop);
            if (OdinStore.instance != null)
                OdinStore.instance.UpdateYmlFileFromSaleOrBuy(id.m_itemData, stack, playerItem);
        }

        private static void RPC_SendSaleLogInfoToServer(long uid, string dropName, int i)
        {
            if(Utilities.GetConnectionState() != Utilities.ConnectionState.Server) return;
            string playerID = Player.m_localPlayer.GetPlayerID().ToString();
            string? playerName = Player.m_localPlayer.GetPlayerName() ?? throw new ArgumentNullException(nameof(i));
            string cost = OdinStore.instance._storeInventory.ElementAt(i).Value.Cost.ToString();
            var envman = EnvMan.instance;
            var theTime = DateTime.Now;
            string concatinated = "";
            var drop = ZNetScene.instance.GetPrefab(dropName).gameObject.GetComponent<ItemDrop>();
            StoreInfo<int, int, int> outObj;
            if (OdinStore.instance._storeInventory.TryGetValue(drop, out outObj))
            {
                concatinated = "[" + theTime + "] "+ playerID + " - " + playerName + " Purchased: " + Localization.instance.Localize(drop.m_itemData.m_shared.m_name) + " For: "+ cost;
                Gogan.LogEvent("Game", "Knarr Sold Item",concatinated , 0);
                ZLog.Log("Knarr Sold Item " + concatinated);
                OdinStore.instance.LogSales(concatinated).ConfigureAwait(false);
            }
        }

        private static void RPC_InitialYMLWrite_OnClientConnect(long uid)
        {
            if(Utilities.GetConnectionState() == Utilities.ConnectionState.Server) return;
            // get the Trader20.TraderConfig write the contents to a temp var take the var and write those out to a yaml file 
        }

        private static void RPC_SetKnarrMapIcon(long uid, Vector3 position)
        {
            Minimap.instance.AddPin(position, Minimap.PinType.Boss, "Knarr", true, false,
                Game.instance.GetPlayerProfile().GetPlayerID());
        }
        
        
    
        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Awake))]
        [HarmonyAfter("randyknapp.mods.auga")]
        public static class ItemListPatch
        {
            internal static bool AuguaSetupRan = false;
            internal static List<ItemDrop.ItemData> m_wornItems = new List<ItemDrop.ItemData>();
            public static void Postfix(StoreGui __instance)
            {
                var newscreen = ZNetScene.instance.GetPrefab("CustomTrader");
                if (newscreen)
                {
                    if (Auga.API.IsLoaded())
                    {
                        if(AuguaSetupRan) return;
                        
                        GameObject augapanel = null!;
                        var typeAll = Resources.FindObjectsOfTypeAll<GameObject>();
                        foreach (var o in typeAll)
                        {
                            if (o.name == "AugaStoreScreen")
                            {
                                augapanel = o;
                            }
                        }
                        Trader20.CustomTraderScreen = GameObject.Instantiate(newscreen,
                            __instance.GetComponent<Localize>().transform, false);
                        Trader20.Coins = ZNetScene.instance.GetPrefab(Trader20.CurrencyPrefabName!.Value).GetComponent<ItemDrop>().m_itemData
                            .GetIcon();
                        Trader20.Coins = ZNetScene.instance.GetPrefab(Trader20.CurrencyPrefabName!.Value).GetComponent<ItemDrop>().m_itemData
                            .GetIcon();
                        var anchor = Trader20.CustomTraderScreen.transform as RectTransform;
                        anchor!.anchoredPosition= Trader20.StoreScreenPos!.Value;
                        
                        
                        OdinStore.instance!.BuyButtonImage!.sprite =
                            __instance!.transform.Find("Store/BuyButton/Image").GetComponent<Image>().sprite;
                        OdinStore.instance.SellButtonImage!.sprite = OdinStore.instance.BuyButtonImage!.sprite;
                        var bkg1 = __instance.transform.Find("Store/AugaPanelBase/Background").gameObject;
                        var test = bkg1.GetComponent<Image>();
                        OdinStore.instance.Bkg2!.sprite = test.sprite;
                        OdinStore.instance.Bkg2.material = test.material;
                        OdinStore.instance.Bkg2.type = Image.Type.Sliced;
                        OdinStore.instance.Bkg2.gameObject.AddComponent<UIGradient>();
                        var temp =OdinStore.instance.Bkg2.gameObject.GetComponent<UIGradient>();
                        temp.GradientType = UIGradient.UIGradientType.Corner;
                        Color topleft = new Color(0.1254902f, 0.1019608f, 0.08235294f,1);
                        Color topRight = new Color(0.282353f, 0.2352941f, 0.1882353f, 1);
                        Color lowerLeft = new Color(0.1803922f, 0.1529412f, 0.1254902f, 1);
                        Color lowerRight = new Color(0.3215686f, 0.2666667f, 0.2156863f, 1);
                        temp.CornerColorUpperLeft = topleft;
                        temp.CornerColorLowerLeft = lowerLeft;
                        temp.CornerColorLowerRight = lowerRight;
                        temp.CornerColorUpperRight = topRight;



                        var Bkg2 = __instance.transform.Find("Store/AugaPanelBase/Darken").GetComponent<Image>();
                        OdinStore.instance.Bkg1!.sprite =Bkg2 .sprite;
                        OdinStore.instance.Bkg1.material = Bkg2.material;
                        
                        OdinStore.instance.Coins!.sprite = Trader20.Coins;
                        OdinStore.instance.Coins.transform.localPosition = new Vector3(-174.054f, 308.3599f, 0);
                        OdinStore.instance!.SelectedCost_TMP!.transform.localPosition = new Vector3(-57.6711f, 324.26f, 0);
                        OdinStore.instance.InvCountPanel!.transform.localPosition = new Vector3(335.2804f, -355.26f, 0);
                        OdinStore.instance.BuyButtonImage!.sprite =
                            Object.Instantiate(__instance.transform.Find("Store/BuyButton/Image").GetComponent<Image>().sprite);
                        OdinStore.instance.SellButtonImage!.sprite = OdinStore.instance.BuyButtonImage!.sprite;
                        AuguaSetupRan = true;
                    }
                    else
                    {
                        var icon = ZNetScene.instance.GetPrefab(Trader20.CurrencyPrefabName!.Value).GetComponent<ItemDrop>().m_itemData
                            .GetIcon();
                        if (icon == null)
                        {
                            Debug.LogError("I cant locate your coin prefab please check the mod loading it or the spelling ");
                            return;
                        }

                        Trader20.Coins = icon;
                        Trader20.CustomTraderScreen = GameObject.Instantiate(newscreen,
                            __instance.GetComponentInParent<Localize>().transform, false);
                    
                        var anchor = Trader20.CustomTraderScreen.transform as RectTransform;
                        anchor!.anchoredPosition= __instance.m_listRoot.anchoredPosition;
                    
                        OdinStore.instance!.Coins!.sprite = Trader20.Coins;
                        OdinStore.instance.BuyButtonImage!.sprite =
                            Object.Instantiate(__instance.transform.Find("Store/BuyButton").GetComponent<Image>().sprite);
                        OdinStore.instance.SellButtonImage!.sprite = OdinStore.instance.BuyButtonImage!.sprite;
                        var bkg1 = Object.Instantiate(__instance.transform.Find("Store/bkg").GetComponent<Image>());
                        OdinStore.instance.Bkg1!.sprite = bkg1.sprite;
                        OdinStore.instance.Bkg1.material = bkg1.material;
                    
                        var Bkg2 = Object.Instantiate(__instance.transform.Find("Store/border (1)").GetComponent<Image>());
                        OdinStore.instance.Bkg2!.sprite =Bkg2 .sprite;
                        OdinStore.instance.Bkg2.material = Bkg2.material;

                        var RepairButton = Object.Instantiate(InventoryGui.instance.transform.Find("root/Crafting/RepairButton").gameObject);
                        var repairButtonButton = RepairButton.GetComponent<Button>();
                        var repairImage = RepairButton.transform.Find("Image").gameObject.GetComponent<Image>();
                        var RepairBKGpanel =
                            Object.Instantiate(InventoryGui.instance.transform.Find("root/Crafting/RepairSimple"))
                                .gameObject.GetComponent<Image>();

                        OdinStore.instance.RepairRect!.gameObject.GetComponent<Image>().sprite = RepairBKGpanel.sprite;
                        OdinStore.instance.RepairRect.gameObject.GetComponent<Image>().material = RepairBKGpanel.material;
                        OdinStore.instance.repairHammerImage!.sprite = repairImage.sprite;
                        OdinStore.instance.repairButton!.image.sprite = repairButtonButton.image.sprite;
                        OdinStore.instance.repairButton.onClick.AddListener(delegate
                        {
                            
                            if (Player.m_localPlayer == null)
                            {
                                return;
                            }
                            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, "Rolling dice");
                            var temp = OdinStore.instance.RollTheDice();
                            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Result is "+ temp);
                            if (temp != Trader20.LuckyNumber!.Value) return;
                            Player.m_localPlayer.GetInventory().GetWornItems(m_wornItems);
                            foreach (var itemData in m_wornItems.Where(itemData => itemData.m_durability < itemData.GetMaxDurability()))
                            {
                                itemData.m_durability = itemData.GetMaxDurability();
                                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                                    Localization.instance.Localize("$msg_repaired", itemData.m_shared.m_name));
                                break;
                            }
                        });
                        OdinStore.instance.repairButton.transition = Selectable.Transition.SpriteSwap;
                        OdinStore.instance.repairButton.spriteState = repairButtonButton.spriteState;

                    }
                    
                }


                //Fill CustomTrader store
                if (ZNetScene.instance.m_prefabs.Count <= 0 )return;
                Dictionary<string, ItemDataEntry> entry = new();
                List<Dictionary<string, ItemDataEntry>> listEntry = new();
                if (!File.ReadLines(Trader20.Paths + "/trader_config.yaml").Any()) return;
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
                            var id = drop.GetComponent<ItemDrop>();
                            if(id == null)
                            {
                                Trader20.knarrlogger.LogError("Failed to load ItemDrop for trader's item: " + variable.Key);
                                continue;
                            }
                            OdinStore.instance!.AddItemToDict(id, variable.Value.ItemCostInt,
                                variable.Value.ItemCount, variable.Value.Invcount);
                        }

                        if (!drop)
                        {
                            Trader20.knarrlogger.LogError("Failed to load trader's item: " + variable.Key);
                            Trader20.knarrlogger.LogError("Please Check your Prefab name "+ variable.Key);
                        }
                    }
                }

                OdinStore.gui = __instance.transform.parent.transform.Find("Inventory_screen").gameObject
                    .GetComponent<InventoryGui>();
                OdinStore.gui.m_splitPanel.gameObject.transform.Find("win_bkg").gameObject.AddComponent<DragHandler>();
                OdinStore.instance!.BuildKnarrSplitDialog();

            }
           
        }

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations))]
        public static class SpawnKnarr
        {
            private static GameObject? tempKnarr = new GameObject();

            [UsedImplicitly]
            private static void Prefix(ZoneSystem __instance)
            {
                if(Trader20.RandomlySpawnKnarr!.Value == false) return;
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
                        GameObject KnarrCopy = Object.Instantiate(ZNetScene.instance.GetPrefab("Knarr"), locationMisc, true);
                        KnarrCopy.name = ZNetScene.instance.GetPrefab("Knarr").name;
                        foreach (var VARIABLE in KnarrCopy.GetComponents<ZNetView>())
                        {
                            Object.DestroyImmediate(VARIABLE);
                        }
                        __instance.m_locations.Add(new ZoneSystem.ZoneLocation
                        {
                            m_randomRotation = true,
                            m_minAltitude = 10,
                            m_maxAltitude = 1000,
                            m_maxDistance = 10000,
                            m_quantity = 35,
                            m_biome = Heightmap.Biome.BlackForest,
                            m_prefab = ZNetScene.instance.GetPrefab("Knarr"),
                            m_prefabName = ZNetScene.instance.GetPrefab("Knarr").name,
                            m_enable = true,
                            m_minDistanceFromSimilar = 100,
                            m_prioritized = true,
                            m_forestTresholdMax = 5,
                            m_unique = true,
                            m_inForest = true,
                        });
                    }
                }
            }
        }


        /// <summary>
        /// RPC to remove Knarr
        /// </summary>
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.InputText))]
        public static class RemoveKnarrCommand
        {
            internal static List<ZDO> zdolist = new List<ZDO>();
            [UsedImplicitly]
            public static bool Prefix(Terminal __instance)
            {
                string lower = __instance.m_input.text.ToLower();
                if (lower.Equals("remove knarr"))
                {
                        ZRoutedRpc.instance.InvokeRoutedRPC("RequestRemoveKnarr", true);
                        return false;
                }

                if (lower.Equals("find knarr"))
                {
                    ZRoutedRpc.instance.InvokeRoutedRPC("FindKnarrDone", true);
                    return false;
                }

                return true;
            }

        }

    }
}
