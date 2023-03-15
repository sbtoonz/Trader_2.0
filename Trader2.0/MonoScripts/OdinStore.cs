using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using Trader20;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
// ReSharper disable NotAccessedField.Global
// ReSharper disable InconsistentNaming
public class OdinStore : MonoBehaviour
{
   
    [Header("Store instance")]
    private static OdinStore? m_instance;
    [SerializeField] private GameObject? m_StorePanel;
    [SerializeField] private TextMeshProUGUI? StoreTitle_TMP;
    [SerializeField] internal Image? Bkg1;
    [SerializeField] internal Image? Bkg2;
    [SerializeField] internal RectTransform? TabRect;
    
    [Space]
    [Header("Items Panel")]
    [SerializeField] private RectTransform? ListRoot;
    [SerializeField] private Image? ItemDropIcon;
    [SerializeField] internal TextMeshProUGUI? SelectedCost_TMP;
    [SerializeField] private TextMeshProUGUI? SelectedItemDescription_TMP;
    [SerializeField] private TextMeshProUGUI? SelectedName_TMP;
    
    [Space]
    [Header("Sell Panel")]
    [SerializeField] private Button? SellButton;
    [SerializeField] private RectTransform? SellListRoot;
    [SerializeField] private TextMeshProUGUI? InventoryCount_TMP;
    [SerializeField] internal TextMeshProUGUI? repairText_TMP;
    [SerializeField] internal GameObject? InvCountPanel;
    [SerializeField] internal Image? SellButtonImage;
    [SerializeField] internal bool SellPageActive;
    
    [Space]
    [Header("Buy Panel")]
    [SerializeField] private Button? BuyButton;
    [SerializeField] internal Image? BuyButtonImage;
    [SerializeField] internal Image? Coins;
    [SerializeField] internal bool BuyPageActive = true;
    
    [Space]
    [Header("Elements")]
    [SerializeField] private GameObject? ElementGO;
    
    
    [Space]
    [Header("Repair Tab")]
    [SerializeField] internal RectTransform? RepairRect;
    [SerializeField] internal Image? repairImage;
    [SerializeField] internal Button? repairButton;
    [SerializeField] internal Image? repairHammerImage;
    
    //Future: Objectpool for GO's holding sale/buy items
    internal List<GameObject> _forSaleObjects = new();
    internal List<GameObject> _forBuyObjects = new();
    

    //StoreInventoryListing
    internal Dictionary<ItemDrop, StoreInfo<int, int, int>> _storeInventory = new Dictionary<ItemDrop, StoreInfo<int, int, int>>();

    public static OdinStore? instance => m_instance;
    internal static ElementFormat? tempElement;
    internal static Material? litpanel;
    internal List<ElementFormat> _knarSellElements = new();
    internal List<ElementFormat> _playerSellElements = new();
    private List<ItemDrop.ItemData> m_tempItems = new List<ItemDrop.ItemData>();
    
    
    //ElementPool
    internal GameObject ElementPoolGO;
    internal List<ElementFormat> ElementPoolObjects = new();

    //gamepad
    internal int currentIdx = 0;
    private void Awake() 
    {
        m_instance = this;
        var rect = m_StorePanel!.transform as RectTransform;
        rect!.anchoredPosition = Trader20.Trader20.StoreScreenPos!.Value;
        StoreTitle_TMP!.SetText("Knarr's Shop");
        CreateAndFillElementPool();
        m_StorePanel!.SetActive(false);
    }

    private void Start()
    {
        if (Trader20.Trader20.ConfigShowRepair?.Value == false)
        {
            if (RepairRect != null) RepairRect.gameObject.SetActive(false);
        }
        if (Trader20.Trader20.ConfigShowTabs?.Value == false)
        {
            TabRect!.gameObject.SetActive(false);
        }
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
        if (Vector3.Distance(NewTrader.instance!.transform.position, player.transform.position) > 15)
        {
            Hide();
        }
        if ( Input.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyButtonB"))
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
        UpdateRecipeGamepadInput();
    }

    
    private void OnDestroy()
    {
        if (m_instance == this)
        {
            m_instance = null!;
        }
    }

    private ElementFormat GetPooledElement()
    {
        return ElementPoolObjects.FirstOrDefault(t => !t.Element!.activeInHierarchy)!;
    }

    private void ReturnPooledElement(ElementFormat element)
    {
        element.Element!.transform.SetParent(ElementPoolGO.transform);
        element.Element.SetActive(false);
        element._uiTooltip = null;
        element.Drop = null;
        element.Icon = null;
        element.Price = null;
        element.InventoryCount = null;
        element.ItemName = null;
    }
    private void CreateAndFillElementPool()
    {
        ElementPoolGO = new GameObject("ElementPool");
        ElementPoolGO!.transform.SetParent(this.transform);
        ElementPoolGO.transform.SetSiblingIndex(-1);
        ElementPoolGO.SetActive(false);
        for (int i = 0; i < ObjectDB.instance.m_items.Count; i++)
        {
            GameObject obj = (GameObject)Instantiate(ElementGO, ElementPoolGO!.transform, false)!;
            obj.SetActive(false);
            ElementFormat element = new ElementFormat()
            {
                Element = obj
            };
            ElementPoolObjects.Add(element);
        }
    }


    private bool IsActive()
    {
        return m_StorePanel!.activeSelf;
    }
    private async void  ClearStore()
    {
        if (BuyPageActive)
        {
            if (_knarSellElements.Count != _storeInventory.Count)
            {
                foreach (var go in _knarSellElements)
                {
                    ReturnPooledElement(go);
                }

                _knarSellElements.Clear();
                await ReadAllStoreItems().ConfigureAwait(false);
            }
        }

        if (SellPageActive)
        {
            foreach (var playerSellElement in _playerSellElements)
            {
                ReturnPooledElement(playerSellElement);
            }
            FillPlayerItemListVoid();
        }
    }
    

    /// <summary>
    /// This method is invoked to add an item to the visual display of the store, it expects the ItemDrop.ItemData and the stack as arguments
    /// </summary>
    /// <param name="drop"></param>
    /// <param name="stack"></param>
    /// <param name="cost"></param>
    /// <param name="invCount"></param>
    /// <param name="rectForElements"></param>
    /// <param name="isPlayerItem"></param>
    public void AddItemToDisplayList(ItemDrop drop, int stack, int cost, int invCount, RectTransform rectForElements, bool isPlayerItem)
    {
        ElementFormat newElement = GetPooledElement();
        newElement.Drop = drop;
        newElement.Drop.m_itemData = drop.m_itemData.Clone();
        newElement.Icon = drop.m_itemData.m_shared.m_icons.FirstOrDefault();
        newElement.ItemName = drop.m_itemData.m_shared.m_name;
        newElement.Drop.m_itemData.m_stack = stack;
        newElement.Element = ElementGO;
        newElement._uiTooltip = ElementGO.GetComponent<UITooltip>();
        newElement._uiTooltip.m_text = Localization.instance.Localize(newElement.Drop.m_itemData.m_shared.m_name);
        newElement._uiTooltip.m_topic = Localization.instance.Localize(newElement.Drop.m_itemData.GetTooltip());

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
        var instantiated_element = Instantiate(newElement.Element, rectForElements!.transform, false);
        instantiated_element.GetComponent<Button>().onClick.AddListener(delegate
        {
            UpdateGenDescription(newElement);
            switch (invCount)
            {
                case -1:
                    InvCountPanel!.SetActive(false);
                    break;
                case >= 1:
                    InvCountPanel!.SetActive(true);
                    InventoryCount_TMP!.SetText(_storeInventory.ElementAt(FindIndex(newElement.Drop)).Value.InvCount.ToString());
                    break;
            }
        });
        instantiated_element.transform.SetSiblingIndex(rectForElements.transform.GetSiblingIndex() - 1);
        instantiated_element.transform.Find("coin_bkg/coin icon").GetComponent<Image>().sprite = Trader20.Trader20.Coins;
        if (isPlayerItem)
        {
            _playerSellElements.Add(newElement);
        }
        else
        {
            _knarSellElements.Add(newElement);
        }
        newElement.Element.SetActive(true);
    }

    /// <summary>
    /// Async task that reads all items in store inventory and then adds them to display list
    /// </summary>
    ///
    private async Task  ReadStoreItems()
    {
        try
        {
            if (_knarSellElements.Count >= 1)
            {
                _knarSellElements.Clear();
            }
            foreach (var itemData in _storeInventory)
            {
                if (Trader20.Trader20.OnlySellKnownItems is { Value: true })
                {
                    if(itemData.Value.InvCount == 0) continue;
                    if (Player.m_localPlayer.m_knownRecipes.Contains(itemData.Key.m_itemData.m_shared.m_name))
                    {
                        AddItemToDisplayList(itemData.Key, itemData.Value.Stack, itemData.Value.Cost, itemData.Value.InvCount, ListRoot!, false);
                    }
                    else if (itemData.Key.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material && Trader20.Trader20.ShowMatsWhenHidingRecipes!.Value == true)
                    {
                        AddItemToDisplayList(itemData.Key, itemData.Value.Stack, itemData.Value.Cost, itemData.Value.InvCount, ListRoot!, false);
                    }
                }
                else
                {
                    if(itemData.Value.InvCount == 0) continue;
                    AddItemToDisplayList(itemData.Key, itemData.Value.Stack, itemData.Value.Cost, itemData.Value.InvCount, ListRoot!, false);
                }

            }
        }
        catch (Exception ex)
        {
            Trader20.Trader20.knarrlogger.LogDebug(ex);
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
                _storeInventory.ElementAt(i).Value.InvCount -= _storeInventory.ElementAt(i).Value.Stack;
                InventoryCount_TMP!.SetText(_storeInventory.ElementAt(i).Value.InvCount.ToString());
                int? temp = _storeInventory.ElementAt(i).Value.InvCount;
                UpdateYmlFileFromSaleOrBuy(_storeInventory.ElementAt(i).Key.m_itemData,(int)temp, false);
                if (temp <= 0)
                {
                    try
                    {
                        UpdateGenDescription(_knarSellElements[0]);
                        InventoryCount_TMP!.SetText(_knarSellElements[0].InventoryCount.ToString());
                        tempElement = null;
                        UpdateYmlFileFromSaleOrBuy(_storeInventory.ElementAt(i).Key.m_itemData, (int)temp, false);
                        if(RemoveItemFromDict(itemDrop))ClearStore();
                    }
                    catch (Exception e)
                    {
                        Trader20.Trader20.knarrlogger.LogDebug(e);
                    }
                }
                break;
            case <= -1:
                break;
        }

        if (!Trader20.Trader20.LOGStoreSales!.Value) return;
        string playerID = Player.m_localPlayer.GetPlayerID().ToString();
        string? playerName = Player.m_localPlayer.GetPlayerName() ?? throw new ArgumentNullException(nameof(i));
        string cost = _storeInventory.ElementAt(i).Value.Cost.ToString();
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
        var concatinated = "[" + theTime + "] "+ playerID + " - " + playerName + " Purchased: " + Localization.instance.Localize(itemDrop.m_itemData.m_shared.m_name) + " For: "+ cost;
        Gogan.LogEvent("Game", "Knarr Sold Item",concatinated , 0);
        ZLog.Log("Knarr Sold Item " + concatinated);
        LogSales(concatinated).ConfigureAwait(false);
    }

    private static void UpdateYmlFileFromSaleOrBuy(ItemDrop.ItemData sellableItem, int newInvCount, bool isPlayerItem)
    {
        
        if(Trader20.Trader20.ConfigWriteSalesBuysToYml?.Value != true) return;
        var file = File.OpenText(Trader20.Trader20.Paths + "/trader_config.yaml");
        var currentList = YMLParser.ReadSerializedData(file.ReadToEnd());
        file.Close();
        if(YMLParser.CheckForEntry(currentList, sellableItem.m_dropPrefab.name))
        {
            if (!currentList.TryGetValue(sellableItem.m_dropPrefab.name, out ItemDataEntry test)) return;
            if (isPlayerItem) test.Invcount += sellableItem.m_stack;
            else test.Invcount = newInvCount;
            currentList[sellableItem.m_dropPrefab.name] = test;
            var tempdict = YMLParser.Serializers(currentList);
            File.WriteAllText(Trader20.Trader20.Paths + "/trader_config.yaml", tempdict);
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
    }

    private async Task LogSales(string saleinfo)
    {
        await WriteSales(saleinfo).ConfigureAwait(false);
    }

    private static async Task WriteSales(string saleInfo)
    {
        var encoding = new UnicodeEncoding();
        string filename = Trader20.Trader20.Paths + "/TraderSales.log";

        byte[] result = encoding.GetBytes(saleInfo);

        using var sourceStream = File.Open(filename, FileMode.OpenOrCreate);
        sourceStream.Seek(0, SeekOrigin.End);
        await sourceStream.WriteAsync(result, 0, result.Length).ConfigureAwait(false);
        await sourceStream.WriteAsync(encoding.GetBytes(Environment.NewLine),0, Environment.NewLine.Length).ConfigureAwait(false);
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
        SelectedItemDescription_TMP!.gameObject.SetActive(true);
        ItemDropIcon!.gameObject.SetActive(true);
        SelectedItemDescription_TMP!.SetText(element.Drop!.m_itemData.m_shared.m_description);
        SelectedItemDescription_TMP.gameObject.AddComponent<Localize>();
        ItemDropIcon!.sprite = element.Icon;
        tempElement = element;
    }

    /// <summary>
    /// Call this method to hide the description icon and text
    /// </summary>
    public void DisableGenDescription()
    {
        SelectedItemDescription_TMP.gameObject.SetActive(false);
        ItemDropIcon.gameObject.SetActive(false);
        tempElement = null;
    }

    /// <summary>
    /// Call this method to update the coins shown in UI with coins in player inventory
    /// </summary>
    public void UpdateCoins()
    {
        SelectedCost_TMP!.SetText(GetPlayerCoins().ToString());
    }
    
    /// <summary>
    /// Call this method upon attempting to buy something (this is tied to an onclick event)
    /// </summary>
    public void BuyButtonAction()
    {
        try {
            if (tempElement?.Drop == null) return;
            int i = FindIndex(tempElement.Drop!);
            if (!CanBuy(i)) return;
            SellItem(i);
            NewTrader.instance!.OnSold();
            UpdateCoins(); 
        }
        catch (Exception e)
        {
            Trader20.Trader20.knarrlogger.LogDebug(e);
        }
        
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
            .RemoveItem(ObjectDB.instance.GetItemPrefab(Trader20.Trader20.CurrencyPrefabName?.Value).GetComponent<ItemDrop>().m_itemData.m_shared.m_name,
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
        internal UITooltip? _uiTooltip;
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
        if(_knarSellElements.Count >=1)
        {
            UpdateGenDescription(_knarSellElements[0]);
            switch (_knarSellElements[0].InventoryCount)
            {
                case >= 1:
                    InventoryCount_TMP!.SetText(_storeInventory.ElementAt(0).Value.InvCount.ToString());
                    break;
                case <=0:
                    InvCountPanel!.SetActive(false);
                    break;
            }
        }
        FillPlayerItemListVoid();
        UpdateCoins();
    }

    /// <summary>
    /// Sets the boolean for the viewpage for gamepad shit
    /// </summary>
    public void SetBuyBool()
    {
        if (SellPageActive)
        {
            SellPageActive = false;
        }

        BuyPageActive = true;
    }

    /// <summary>
    /// Sets the boolean for the viewpage for gamepad shit
    /// </summary>
    public void SetSellBool()
    {
        if (BuyPageActive)
        {
            BuyPageActive = false;
        }

        SellPageActive = true;
    }
    
    /// <summary>
    /// This is called OnTabSelect() for the Buy tab so that the Description and icon for the item in the description panel updates
    /// </summary>
    public void SelectKnarrFirstItemForDisplay()
    {
        if(_knarSellElements.Count>0) UpdateGenDescription(_knarSellElements[0]);
    }

    /// <summary>
    /// This is called OnTabSelect() for the Sell tab so that the Description and icon for the item in the description panel updates
    /// </summary>
    public void SelectPlayerFirstItemForDisplay()
    {
        if(_playerSellElements.Count <= 0) return;
        switch (_playerSellElements.Count)
        {
            case 0:
                DisableGenDescription();
                break;
            case >= 1:
                UpdateGenDescription(_playerSellElements[0]);
                break;
        }
    }
    /// <summary>
    /// This is called to show the panel holding the inventory text
    /// </summary>
    public void ShowInvCount()
    {
        InvCountPanel!.SetActive(true);
    }

    /// <summary>
    /// This is called to hide the panel holding the inventory text
    /// </summary>
    public void HideInvCount()
    {
        InvCountPanel!.SetActive(false);
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

    /// <summary>
    ///  This calls the async task which populates what knarr can buy from the player
    /// </summary>
    public async void FillPlayerItemListVoid()
    {
        await FillPlayerSaleList();
    }
    private async Task FillPlayerSaleList()
    {
        await Task.WhenAny(SetupPlayerItemListTask());
    }

    private async Task SetupPlayerItemListTask()
    {
        if (!SellListRoot!.gameObject.activeSelf)
        {
            await Task.Yield();
        }
        _playerSellElements.Clear();
        var playerInv = Player.m_localPlayer.GetInventory();
        var playerItems = playerInv.GetAllItems();
        
        m_tempItems = playerItems;
        
        if (SellListRoot.transform.childCount >= 1)
        {
            foreach (Transform transform in SellListRoot.transform)
            {
                Destroy(transform.gameObject);
            }
        }
        foreach (var itemData in m_tempItems.Where(itemData => YMLContainsKey(itemData.m_dropPrefab.name)).Where(itemData => ReturnYMLPlayerPurchaseValue(itemData.m_dropPrefab.name) != 0))
        {
            AddItemToDisplayList(itemData.m_dropPrefab.GetComponent<ItemDrop>(), itemData.m_stack, ReturnYMLPlayerPurchaseValue(itemData.m_dropPrefab.name),  itemData.m_stack, SellListRoot, true);
        }
        await Task.Yield();
    }

    /// <summary>
    /// This is called when Knarr buys an item from the player
    /// </summary>
    public void OnBuyItem() 
    {
        // ReSharper disable once Unity.NoNullPropagation
        var sellableItem = Player.m_localPlayer.GetInventory().GetItem(tempElement?.Drop?.m_itemData?.m_shared.m_name);
        if (sellableItem == null) return;
        int stack = ReturnYMLPlayerPurchaseValue(sellableItem.m_dropPrefab.name) * sellableItem.m_stack;
        Player.m_localPlayer.GetInventory().RemoveItem(sellableItem);
        
        Player.m_localPlayer.GetInventory().AddItem(
            CurrentCurrency().name, 
            stack, 
            CurrentCurrency().GetComponent<ItemDrop>().m_itemData.m_quality, 
            CurrentCurrency().GetComponent<ItemDrop>().m_itemData.m_variant, 
            0L, 
            "");
        
        string text = "";
        text = ((sellableItem.m_stack <= 1) ? sellableItem.m_shared.m_name : (sellableItem.m_stack + "x" + sellableItem.m_shared.m_name)); 
        Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("$msg_sold", text, stack.ToString()), stack, sellableItem.m_shared.m_icons[0]);
        Gogan.LogEvent("Game", "SoldItem", text, 0L);
        //Check for existing entry
        UpdateYmlFileFromSaleOrBuy(sellableItem, sellableItem.m_stack, true);
        
        if (SellListRoot!.transform.childCount >= 1)
        {
            foreach (Transform transform in SellListRoot.transform)
            {
                Destroy(transform.gameObject);
            }
        }
        foreach (var itemData in m_tempItems.Where(itemData => YMLContainsKey(itemData.m_dropPrefab.name)).Where(itemData => ReturnYMLPlayerPurchaseValue(itemData.m_dropPrefab.name) != 0))
        {
            AddItemToDisplayList(itemData.m_dropPrefab.GetComponent<ItemDrop>(), itemData.m_stack, ReturnYMLPlayerPurchaseValue(sellableItem.m_dropPrefab.name),  itemData.m_stack, SellListRoot, true);
        }
        switch (m_tempItems.Count)
        {
            case > 0:
                UpdateGenDescription(_playerSellElements[0]);
                break;
            case 0:
                DisableGenDescription();
                break;
        }
        
    }

    private protected int ReturnYMLPlayerPurchaseValue(string s)
    {
        var file = File.OpenText(Trader20.Trader20.Paths + "/trader_config.yaml");
        var currentList = YMLParser.ReadSerializedData(file.ReadToEnd());
        file.Close();
        return currentList[s].PurchaseFromPlayerCost;
    }

    private protected bool YMLContainsKey(string s)
    {
        var file = File.OpenText(Trader20.Trader20.Paths + "/trader_config.yaml");
        var currentList = YMLParser.ReadSerializedData(file.ReadToEnd());
        file.Close();
        return currentList.ContainsKey(s);
    }

    private void SetActiveSelection()
    {
        int si = 0;
        foreach (var VARIABLE in _knarSellElements)
        {
            si++;
            if (si == currentIdx)
            {
                VARIABLE.Element!.gameObject.transform.Find("selected").gameObject.SetActive(true);
            }
            VARIABLE.Element!.gameObject.transform.Find("selected").gameObject.SetActive(false);
        }

        si = 0;
    }
    private void UpdateRecipeGamepadInput()
    {
     
        if(BuyPageActive)
        {
            if (ZInput.GetButtonDown("JoyLStickDown"))
            {
                currentIdx += 1;
                if (currentIdx >= _knarSellElements.Count)
                {
                    currentIdx = _knarSellElements.Count - 1;
                }

                if (_knarSellElements.Count >= 1)
                {
                    UpdateGenDescription(_knarSellElements[currentIdx]);
                    SetActiveSelection();
                }
            }

            if (ZInput.GetButtonDown("JoyLStickUp"))
            {
                currentIdx -= 1;
                if (currentIdx <= 0)
                {
                    currentIdx = 0;
                }
                if (_knarSellElements.Count >= 1)
                {
                    UpdateGenDescription(_knarSellElements[currentIdx]);
                    SetActiveSelection();
                }
            }
            if (ZInput.GetButtonDown("JoyTabRight"))
            {
                SetSellBool();
                SelectPlayerFirstItemForDisplay();
            }
        }
        else if (SellPageActive)
        {
            if (ZInput.GetButtonDown("JoyLStickDown"))
            {
                currentIdx += 1;
                if (currentIdx >= _playerSellElements.Count)
                {
                    currentIdx = _playerSellElements.Count - 1;
                }

                if(_playerSellElements.Count >=1)
                {
                    UpdateGenDescription(_playerSellElements[currentIdx]);
                    SetActiveSelection();
                }
            }

            if (ZInput.GetButtonDown("JoyLStickUp"))
            {
                currentIdx -= 1;
                if (currentIdx <= 0)
                {
                    currentIdx = 0;
                }
                if(_playerSellElements.Count >=1)
                {
                    UpdateGenDescription(_playerSellElements[currentIdx]);
                    SetActiveSelection();
                }
            }
            if (ZInput.GetButtonDown("JoyTabLeft"))
            {
                SetBuyBool();
                SelectKnarrFirstItemForDisplay();
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