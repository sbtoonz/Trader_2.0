using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trader20;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class OdinStore : MonoBehaviour
{
    private static OdinStore? m_instance;
    
    [SerializeField] private GameObject? m_StorePanel;
    [SerializeField] private RectTransform? ListRoot;
    [SerializeField] private RectTransform? SellListRoot;
    [SerializeField] private Text? SelectedItemDescription;
    [SerializeField] private Image? ItemDropIcon;
    [SerializeField] internal Text? SelectedCost;
    [SerializeField] private Text? StoreTitle;
    [SerializeField] private Button? BuyButton;
    [SerializeField] private Button? SellButton;
    [SerializeField] private Text? SelectedName;

    [SerializeField] private Text? InventoryCount;
    [SerializeField] internal GameObject? InvCountPanel;
    
    [SerializeField] internal Image? Bkg1;
    [SerializeField] internal Image? Bkg2;
    
    
    //ElementData
    [SerializeField] private GameObject? ElementGO;

    [SerializeField] private NewTrader? _trader; 
    [SerializeField] internal Image? BuyButtonImage;
    [SerializeField] internal Image? SellButtonImage;
    [SerializeField] internal Image? Coins;

    [SerializeField] internal RectTransform? RepairRect;
    [SerializeField] internal Image? repairImage;
    [SerializeField] internal Text? repairText;
    [SerializeField] internal Button? repairButton;
    [SerializeField] internal Image? repairHammerImage;
    
    //StoreInventoryListing
    internal Dictionary<ItemDrop, StoreInfo<int, int, int>> _storeInventory = new Dictionary<ItemDrop, StoreInfo<int, int, int>>();

    public static OdinStore? instance => m_instance;
    internal static ElementFormat? tempElement;
    internal static Material? litpanel;
    internal List<GameObject> CurrentStoreList = new();
    internal List<ElementFormat> _elements = new();
    private List<ItemDrop.ItemData> m_tempItems = new List<ItemDrop.ItemData>();
    private void Awake() 
    {
        m_instance = this;
        var rect = m_StorePanel?.transform as RectTransform;
        rect!.anchoredPosition = Trader20.Trader20.StoreScreenPos!.Value;
        m_StorePanel!.SetActive(false);
        StoreTitle!.text = "Knarr's Shop";
    }
    

    private void Update()
    {
        if (!IsActive()) return;
        if (IsActive())
        {
            StoreGui.instance.m_hiddenFrames = 0;
        }
        
        if (Player.m_localPlayer is not Player player)
        {
            return;
        }
        if (Vector3.Distance(NewTrader.instance.transform.position, Player.m_localPlayer.transform.position) > 15)
        {
            Hide();
        }
        if ( Input.GetKeyDown(KeyCode.Escape))
        {
            ZInput.ResetButtonStatus("JoyButtonB");
            Hide();
        }
        if (InventoryGui.IsVisible() || Minimap.IsOpen())
        {
            Hide();
        }
        if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead() || Player.m_localPlayer.InCutscene())
        {
            Hide();
        }
    }

    

    private bool IsActive()
    {
        return m_StorePanel!.activeSelf;
    }
    private void OnDestroy()
    {
        if (m_instance == this)
        {
            m_instance = null!;
        }
    }

    private async void  ClearStore()
   {
        if (CurrentStoreList.Count != _storeInventory.Count)
        {
            foreach (var go in CurrentStoreList)
            {
                Destroy(go);
            }
            
            CurrentStoreList.Clear();
          await ReadAllStoreItems();
        }
   }
    
    internal async void ForceClearStore()
    {
        foreach (var go in CurrentStoreList)
        {
            Destroy(go);
        }
            
        CurrentStoreList.Clear();
       await ReadAllStoreItems();
    }

    /// <summary>
    /// This method is invoked to add an item to the visual display of the store, it expects the ItemDrop.ItemData and the stack as arguments
    /// </summary>
    /// <param name="drop"></param>
    /// <param name="stack"></param>
    /// <param name="cost"></param>
    /// <param name="invCount"></param>
    public void AddItemToDisplayList(ItemDrop drop, int stack, int cost, int invCount, RectTransform rectForElements)
    {
        ElementFormat newElement = new();
        newElement.Drop = drop;
        newElement.Drop.m_itemData = drop.m_itemData.Clone();
        newElement.Icon = drop.m_itemData.m_shared.m_icons.FirstOrDefault();
        newElement.ItemName = drop.m_itemData.m_shared.m_name;
        newElement.Drop.m_itemData.m_stack = stack;
        newElement.Element = ElementGO;

        newElement.InventoryCount = invCount;
        
        newElement.Element!.transform.Find("icon").GetComponent<Image>().sprite = newElement.Icon;
        var component = newElement.Element.transform.Find("name").GetComponent<Text>();
        component.text = newElement.ItemName;
        component.gameObject.AddComponent<Localize>();
        
        newElement.Element.transform.Find("price").GetComponent<Text>().text = cost.ToString();
        newElement.Element.transform.Find("stack").GetComponent<Text>().text = stack switch
        {
            > 1 => "x" + stack,
            1 => "",
            _ => newElement.Element.transform.Find("stack").GetComponent<Text>().text
        };
        var elementthing = Instantiate(newElement.Element, rectForElements!.transform, false);
        elementthing.GetComponent<Button>().onClick.AddListener(delegate
        {
            UpdateGenDescription(newElement);
            switch (invCount)
            {
                case -1:
                    InvCountPanel?.SetActive(false);
                    break;
                case >= 1:
                    InventoryCount!.text =
                        _storeInventory.ElementAt(FindIndex(newElement.Drop)).Value.InvCount.ToString();
                    break;
            }
        });
        elementthing.transform.SetSiblingIndex(rectForElements.transform.GetSiblingIndex() - 1);
        elementthing.transform.Find("coin_bkg/coin icon").GetComponent<Image>().sprite = Trader20.Trader20.Coins;
        _elements.Add(newElement);
        CurrentStoreList.Add(elementthing);
    }

    /// <summary>
    /// Async task that reads all items in store inventory and then adds them to display list
    /// </summary>
    ///
    private async Task  ReadStoreItems()
    {
        try
        {
            foreach (var itemData in _storeInventory)
            {
                if (Trader20.Trader20.OnlySellKnownItems is { Value: true })
                {
                    if (Player.m_localPlayer.m_knownRecipes.Contains(itemData.Key.m_itemData.m_shared.m_name))
                    {
                        AddItemToDisplayList(itemData.Key, itemData.Value.Stack, itemData.Value.Cost, itemData.Value.InvCount, ListRoot!);
                    }
                    else if (itemData.Key.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material)
                    {
                        AddItemToDisplayList(itemData.Key, itemData.Value.Stack, itemData.Value.Cost, itemData.Value.InvCount, ListRoot!);
                    }
                }
                else
                {
                    AddItemToDisplayList(itemData.Key, itemData.Value.Stack, itemData.Value.Cost, itemData.Value.InvCount, ListRoot!);
                }

            }
        }
        catch
        {
            // ignored
        }
        finally
        {
            await Task.Yield(); 
        }
        

        
    }

    private async Task ReadAllStoreItems()
    {
        await Task.WhenAny(ReadStoreItems());
    }

    /// <summary>
    /// Invoke this method to instantiate an item from the storeInventory dictionary. This method expects an integer argument this integer should identify the index in the dictionary that the item lives at you wish to vend
    /// </summary>
    /// <param name="i"></param>
    public void SellItem(int i)
    {
        var inv = Player.m_localPlayer.GetInventory();
        var itemDrop = _storeInventory.ElementAt(i).Key;

        if (itemDrop == null || itemDrop.m_itemData == null) return;
        
        int stack = Mathf.Min(_storeInventory.ElementAt(i).Value.Stack, itemDrop.m_itemData.m_shared.m_maxStackSize);
        int quality = itemDrop.m_itemData.m_quality;
        int variant = itemDrop.m_itemData.m_variant;
        itemDrop.m_itemData.m_dropPrefab = ObjectDB.instance.GetItemPrefab(itemDrop.gameObject.name);
        itemDrop.m_itemData.m_durability = itemDrop.m_itemData.GetMaxDurability();
        if (inv.AddItem(itemDrop.name, stack, quality, variant, 0L, "") != null)
        {
            Player.m_localPlayer.ShowPickupMessage(itemDrop.m_itemData, stack);
            Gogan.LogEvent("Game", "BoughtItem", itemDrop.m_itemData.m_dropPrefab.name, 0L);
        }
        else
        {
            //spawn item on ground if no inventory room
            var vector = Random.insideUnitSphere * 0.5f;
            var transform1 = Player.m_localPlayer.transform;
            var go =Instantiate(_storeInventory.ElementAt(i).Key.gameObject,
                transform1.position + transform1.forward * 2f + Vector3.up + vector,
                Quaternion.identity);
            go.GetComponent<ItemDrop>().m_itemData.m_stack = stack;
        }
        switch (_storeInventory.ElementAt(i).Value.InvCount)
        {
            case >= 1:
            {
                _storeInventory.ElementAt(i).Value.InvCount -= _storeInventory.ElementAt(i).Value.Stack;
                InventoryCount.text = _storeInventory.ElementAt(i).Value.InvCount.ToString();
                switch (_storeInventory.ElementAt(i).Value.InvCount)
                {
                    case >= 1:
                        InventoryCount.text = _storeInventory.ElementAt(i).Value.InvCount.ToString();
                        break;
                    case < -1 when !RemoveItemFromDict(itemDrop):
                        break;
                    case < -1:
                        ForceClearStore();
                        UpdateGenDescription(_elements[0]);
                        break;
                }


                switch (_elements[0].InventoryCount)
                {
                    case >= 1:
                        InventoryCount.text = _storeInventory.ElementAt(0).Value.InvCount.ToString();
                        break;
                    case -1:
                        InvCountPanel.SetActive(false);
                        break;
                }
                break;
            }
            case <= -1:
                break;
        }

        if (!Trader20.Trader20.LOGStoreSales.Value) return;
        var PlayerID = Player.m_localPlayer.GetPlayerID().ToString();
        var PlayerName = Player.m_localPlayer.GetPlayerName();
        var cost = _storeInventory.ElementAt(i).Value.Cost.ToString();
        var envman = EnvMan.instance;
        var theTime = DateTime.Now;
        if(envman)
        {
            float fraction = envman.m_smoothDayFraction;
            int hour = (int)(fraction * 24);
            int minute = (int)((fraction * 24 - hour) * 60);
            int second = (int)((((fraction * 24 - hour) * 60) - minute) * 60);
            DateTime now = DateTime.Now;
            theTime = new DateTime(now.Year, now.Month, now.Day, hour, minute, second);
            int days = EnvMan.instance.GetCurrentDay();
            
            
        }
        var concatinated = "[" + theTime + "] "+ PlayerID + " - " + PlayerName + " Purchased: " + Localization.instance.Localize(itemDrop.m_itemData.m_shared.m_name) + " For: "+ cost;
        Gogan.LogEvent("Game", "Knarr Sold Item",concatinated , 0);
        ZLog.Log("Knarr Sold Item " + concatinated);
        LogSales(concatinated).ConfigureAwait(false);
    }

    private async Task LogSales(string Saleinfo)
    {
        await WriteSales(Saleinfo).ConfigureAwait(false);
    }

    private static async Task WriteSales(string SaleInfo)
    {
        UnicodeEncoding uniencoding = new UnicodeEncoding();
        var filename = Trader20.Trader20.Paths + "/TraderSales.log";

        var result = uniencoding.GetBytes(SaleInfo);

        using FileStream SourceStream = File.Open(filename, FileMode.OpenOrCreate);
        SourceStream.Seek(0, SeekOrigin.End);
        await SourceStream.WriteAsync(result, 0, result.Length).ConfigureAwait(false);
        await SourceStream.WriteAsync(uniencoding.GetBytes(Environment.NewLine),0, Environment.NewLine.Length).ConfigureAwait(false);
    }


    /// <summary>
    ///  Adds item to stores dictionary pass ItemDrop.ItemData and an integer for price
    /// </summary>
    /// <param name="itemDrop"></param>
    /// <param name="price"></param>
    /// <param name="stack"></param>
    /// <param name="invCount"></param>
    public void AddItemToDict(ItemDrop itemDrop, int price, int stack, int invCount)
    {
        _storeInventory.Add(itemDrop, new StoreInfo<int, int, int>(price, stack, invCount) );
    }

    /// <summary>
    /// Pass this method an ItemDrop as an argument to drop it from the storeInventory dictionary.
    /// </summary>
    /// <param name="itemDrop"></param>
    /// <returns>returns true if specific item is removed from trader inventory. Use this in tandem with inventory management</returns>
    private bool RemoveItemFromDict(ItemDrop itemDrop)
    {
        Dictionary<string, ItemDataEntry> list = new();
        foreach (var pair in _storeInventory)
        {
            ItemDataEntry dataEntry = new();
            dataEntry.Invcount = pair.Value.InvCount;
            dataEntry.ItemCount = pair.Value.Stack;
            dataEntry.ItemCostInt = pair.Value.Cost;
                        
            list.Add(pair.Key.name, dataEntry);
                        
        }
        Trader20.Trader20.TraderConfig.Value = list;
        return _storeInventory.Remove(itemDrop);
    }

    /// <summary>
    /// This methods invocation should return the index offset of the ItemDrop passed as an argument, this is for use with other functions that expect an index to be passed as an integer argument
    /// </summary>
    /// <param name="itemDrop"></param>
    /// <returns>returns index of item within trader inventory</returns>
    private int FindIndex(ItemDrop itemDrop)
    {
        var templist = _storeInventory.Keys.ToList();
        var index = templist.IndexOf(itemDrop);

        return index;

    }
    /// <summary>
    /// This method will update the general description of the store page pass it an ElementFormat as argument
    /// </summary>
    /// <param name="element"></param>
    public void UpdateGenDescription(ElementFormat element)
    {
        SelectedItemDescription!.text = element.Drop!.m_itemData.m_shared.m_description;
        SelectedItemDescription.gameObject.AddComponent<Localize>();
        ItemDropIcon!.sprite = element.Icon;
        tempElement = element;
    }

    /// <summary>
    /// Call this method to update the coins shown in UI with coins in player inventory
    /// </summary>
    public void UpdateCoins()
    {
        SelectedCost!.text = GetPlayerCoins().ToString();
    }
    
    /// <summary>
    /// Call this method upon attempting to buy something (this is tied to an onclick event)
    /// </summary>
    public void BuyButtonAction()
    {
        if (tempElement?.Drop == null) return;
        var i = FindIndex(tempElement.Drop!);
        if (!CanBuy(i)) return;
        SellItem(i);
        NewTrader.instance.OnSold();
        UpdateCoins();
    }

    /// <summary>
    /// give this bool the index of your item within the traders inventory and it will return true/false based on players bank
    /// </summary>
    /// <param name="i"></param>
    /// <returns>return true/false based on players bank</returns>
    private bool CanBuy(int i)
    {
        var playerbank = GetPlayerCoins();
        var cost = _storeInventory.ElementAt(i).Value.Cost;
        if (playerbank < cost) return false;
        Player.m_localPlayer.GetInventory()
            .RemoveItem(ObjectDB.instance.GetItemPrefab(Trader20.Trader20.CurrencyPrefabName.Value).GetComponent<ItemDrop>().m_itemData.m_shared.m_name,
                cost);
        return true;
    }

    /// <summary>
    /// Format of the Element GameObject that populates the for sale list.
    /// </summary>
    public class ElementFormat
    {
        internal GameObject? Element;
        internal Sprite? Icon;
        internal string? ItemName;
        internal int? Price;
        internal ItemDrop? Drop;
        internal int? InventoryCount;
    }
    
    /// <summary>
    /// Called to Hide the UI
    /// </summary>
    public void Hide()
    {
        m_StorePanel!.SetActive(false);
    }

    /// <summary>
    /// Called to show the UI
    /// </summary>
    public void Show()
    {
        if (_storeInventory.Count <= 0)
        {
            Trader20.Trader20.knarrlogger.LogWarning("Store is empty not showing UI");
            return;
        }
        m_StorePanel!.SetActive(true);
        ClearStore();
        if(_elements.Count >=1)
        {
            UpdateGenDescription(_elements[0]);
            switch (_elements[0].InventoryCount)
            {
                case >= 1:
                    InventoryCount.text = _storeInventory.ElementAt(0).Value.InvCount.ToString();
                    break;
                case -1:
                    InvCountPanel.SetActive(false);
                    break;
            }
        }
        FillPlayerSaleList();
        UpdateCoins();
    }

    private static GameObject CurrentCurrency()
    {
        return ZNetScene.instance.GetPrefab(Trader20.Trader20.CurrencyPrefabName!.Value);
    }
    /// <summary>
    /// Returns the players coin count as int
    /// </summary>
    /// <returns>Player Coin Count as int</returns>
    private static int GetPlayerCoins()
    {
        return Player.m_localPlayer.GetInventory().CountItems(CurrentCurrency().GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
    }

    /// <summary>
    /// Removes all items from traders for sale dictionary
    /// </summary>
    public void DumpDict()
    {
        _storeInventory.Clear();
    }
    
    /// <summary>
    /// Returns a random integer between 1 and 6
    /// </summary>
    /// <returns></returns>
    
    internal int RollTheDice()
    {
        
        int randomDiceSide = 0;

        int finalSide = 0;

        for (int i = 0; i <= 20; i++)
        {
            randomDiceSide = Random.Range(0, 6);
        }

        finalSide = randomDiceSide + 1;

        
        return finalSide;
    }

    public bool IsOnSale()
    {
        
        return false;
    }

    public void FillPlayerSaleList()
    {
        StartCoroutine(SetupPlayerItemList());
        if (SellListRoot.transform.childCount >= 1)
        {
            foreach (Transform transform in SellListRoot.transform)
            {
                Destroy(transform.gameObject);
            }
        }
        foreach (var itemData in m_tempItems)
        {
              AddItemToDisplayList(itemData.m_dropPrefab.GetComponent<ItemDrop>(), itemData.m_stack, 0,  0, SellListRoot);            
        }
    }

    private IEnumerator SetupPlayerItemList()
    {
        m_tempItems.Clear();
        var playerInv = Player.m_localPlayer.GetInventory();
        var playerItems = playerInv.GetAllItems();
        foreach (var item in playerItems)
        {
            m_tempItems.Add(item);
        }
        yield break;
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnBuyItem()
    {
        ItemDrop.ItemData sellableItem = Player.m_localPlayer.GetInventory().GetItem(tempElement?.Drop?.m_itemData?.m_shared.m_name); 
        if (sellableItem != null)
        {
            int stack = sellableItem.m_shared.m_value * sellableItem.m_stack;
            Player.m_localPlayer.GetInventory().RemoveItem(sellableItem);
            Player.m_localPlayer.GetInventory().AddItem(CurrentCurrency().name, stack, CurrentCurrency().GetComponent<ItemDrop>().m_itemData.m_quality, CurrentCurrency().GetComponent<ItemDrop>().m_itemData.m_variant, 0L, "");
            string text = "";
            text = ((sellableItem.m_stack <= 1) ? sellableItem.m_shared.m_name : (sellableItem.m_stack + "x" + sellableItem.m_shared.m_name)); 
            Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_sold", text, stack.ToString()), 0, sellableItem.m_shared.m_icons[0]);
            Gogan.LogEvent("Game", "SoldItem", text, 0L);
            
            //Check for existing entry
            var file = File.OpenText(Trader20.Trader20.Paths + "/trader_config.yaml");
            var currentList = YMLParser.ReadSerializedData(file.ReadToEnd());
            file.Close();
            if(YMLParser.CheckForEntry(currentList, sellableItem.m_dropPrefab.name))
            {
                //var itemDataEntry = currentList[sellableItem.m_dropPrefab.name];
                //itemDataEntry.Invcount += sellableItem.m_stack;
                if (currentList.TryGetValue(sellableItem.m_dropPrefab.name, out ItemDataEntry test))
                {
                    
                    test.Invcount += sellableItem.m_stack;
                    currentList[sellableItem.m_dropPrefab.name] = test;
                    var tempdict = YMLParser.Serializers(currentList);
                    File.WriteAllText(Trader20.Trader20.Paths + "/trader_config.yaml", tempdict);
                    
                }
            }else
            {
                //Setup the data entry for the YML file 
                var entry = new ItemDataEntry();
                entry.Invcount += sellableItem.m_stack;
                entry.ItemCount += sellableItem.m_stack;
                //if none found make an entry
                Dictionary<string, ItemDataEntry> itemDataEntries = new Dictionary<string, ItemDataEntry>();
                itemDataEntries.Add(sellableItem.m_dropPrefab.name, entry);
                var serializeddata = YMLParser.Serializers(itemDataEntries);
                YMLParser.AppendYmLfile(serializeddata);
            }
            m_tempItems.Clear();
            var playerInv = Player.m_localPlayer.GetInventory();
            var playerItems = playerInv.GetAllItems();
            foreach (var item in playerItems)
            {
                m_tempItems.Add(item);
            }
            foreach (var itemData in m_tempItems)
            {
                AddItemToDisplayList(itemData.m_dropPrefab.GetComponent<ItemDrop>(), itemData.m_stack, 0,  0, SellListRoot);            
            }
        }
    }
}

[Serializable]
public class StoreInfo<ItemCost, ItemStack, ItemInventoryCount> {
    public StoreInfo(ItemCost cost, ItemStack stack, ItemInventoryCount count) {
        Cost = cost;
        Stack = stack;
        InvCount = count;
    }

    public ItemCost Cost { get; set; }
    public ItemStack Stack { get; set; }
    public ItemInventoryCount InvCount { get; set; }
};
