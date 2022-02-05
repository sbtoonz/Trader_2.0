using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Trader20;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class OdinStore : MonoBehaviour
{
    private static OdinStore m_instance;
    
    [SerializeField] private GameObject? m_StorePanel;
    [SerializeField] private RectTransform? ListRoot;
    [SerializeField] private Text? SelectedItemDescription;
    [SerializeField] private Image? ItemDropIcon;
    [SerializeField] private Text? SelectedCost;
    [SerializeField] private Text? StoreTitle;
    [SerializeField] private Button? BuyButton;
    [SerializeField] private Text? SelectedName;

    [SerializeField] private Text InventoryCount;
    [SerializeField] private GameObject InvCountPanel;
    
    [SerializeField] internal Image? Bkg1;
    [SerializeField] internal Image? Bkg2;
    
    
    //ElementData
    [SerializeField] private GameObject? ElementGO;

    [SerializeField] private NewTrader? _trader;
    [SerializeField] internal Image? ButtonImage;
    [SerializeField] internal Image? Coins;
    
    //StoreInventoryListing
    internal Dictionary<ItemDrop, StoreInfo<int, int, int>> _storeInventory = new();
    
    public static OdinStore instance => m_instance;
    internal static ElementFormat? tempElement;
    internal static Material? litpanel;
    internal List<GameObject> CurrentStoreList = new();
    internal List<ElementFormat> _elements = new();
    private void Awake() 
    {
        m_instance = this;
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

    private void  ClearStore()
   {
        if (CurrentStoreList.Count != _storeInventory.Count)
        {
            foreach (var go in CurrentStoreList)
            {
                Destroy(go);
            }
            
            CurrentStoreList.Clear();
           ReadAllItems();
        }
   }

    internal void ForceClearStore()
    {
        foreach (var go in CurrentStoreList)
        {
            Destroy(go);
        }
            
        CurrentStoreList.Clear();
        ReadAllItems();
    }

    /// <summary>
    /// This method is invoked to add an item to the visual display of the store, it expects the ItemDrop.ItemData and the stack as arguments
    /// </summary>
    /// <param name="drop"></param>
    /// <param name="stack"></param>
    /// <param name="cost"></param>
    /// <param name="invCount"></param>
    public void AddItemToDisplayList(ItemDrop drop, int stack, int cost, int invCount)
    {
        ElementFormat newElement = new();
        newElement.Drop = drop;
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
        var elementthing = Instantiate(newElement.Element, ListRoot!.transform, false);
        elementthing.GetComponent<Button>().onClick.AddListener(delegate
        {
            UpdateGenDescription(newElement);
            switch (invCount)
            {
                case -1:
                    InvCountPanel.SetActive(false);
                    break;
                case >= 1:
                    InventoryCount.text =
                        _storeInventory.ElementAt(FindIndex(newElement.Drop)).Value.InvCount.ToString();
                    break;
            }
        });
        elementthing.transform.SetSiblingIndex(ListRoot.transform.GetSiblingIndex() - 1);
        elementthing.transform.Find("coin_bkg/coin icon").GetComponent<Image>().sprite = Trader20.Trader20.Coins;
        _elements.Add(newElement);
        CurrentStoreList.Add(elementthing);
    }

    private async Task  ReadItems()
    {
        foreach (var itemData in _storeInventory)
        {
            //need to add some type of second level logic here to think about if items exist do not repopulate.....
            AddItemToDisplayList(itemData.Key,itemData.Value.Stack, itemData.Value.Cost, itemData.Value.InvCount);
        }

        await Task.Yield();
    }

    private async Task ReadAllItems()
    {
        await ReadItems();
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
        
        var stack = Mathf.Min(_storeInventory.ElementAt(i).Value.Stack, itemDrop.m_itemData.m_shared.m_maxStackSize);
        itemDrop.m_itemData.m_dropPrefab = ZNetScene.instance.GetPrefab(itemDrop.gameObject.name);
        itemDrop.m_itemData.m_stack = stack;
        itemDrop.m_itemData.m_durability = itemDrop.m_itemData.GetMaxDurability();
        
        if (inv.CanAddItem(itemDrop.m_itemData))
        {
            if (inv.AddItem(itemDrop.m_itemData, stack, inv.FindEmptySlot(false).x, inv.FindEmptySlot(false).y))
            {
                Player.m_localPlayer.ShowPickupMessage(itemDrop.m_itemData, stack);
                Gogan.LogEvent("Game", "BoughtItem", itemDrop.m_itemData.m_dropPrefab.name, 0L);
            }
        }
        else
        {
            //spawn item on ground if no inventory room
            var vector = Random.insideUnitSphere * 0.5f;
            var transform1 = Player.m_localPlayer.transform;
            Instantiate(_storeInventory.ElementAt(i).Key.gameObject,
                transform1.position + transform1.forward * 2f + Vector3.up + vector,
                Quaternion.identity);
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
                        return;
                    case < -1 when !RemoveItemFromDict(itemDrop):
                        return;
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
                return;
            }
            case <= -1:
                return;
        }
        
       


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
        if (tempElement!.Drop is null) return;
        var i = FindIndex(tempElement.Drop);
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
            .RemoveItem(ZNetScene.instance.GetPrefab(Trader20.Trader20.CurrencyPrefabName.Value).GetComponent<ItemDrop>().m_itemData.m_shared.m_name,
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
        UpdateCoins();
    }

    
    /// <summary>
    /// Returns the players coin count as int
    /// </summary>
    /// <returns>Player Coin Count as int</returns>
    private static int GetPlayerCoins()
    {
        return Player.m_localPlayer.GetInventory().CountItems(ZNetScene.instance.GetPrefab(Trader20.Trader20.CurrencyPrefabName!.Value).GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
    }

    /// <summary>
    /// Removes all items from traders for sale dictionary
    /// </summary>
    public void DumpDict()
    {
        _storeInventory.Clear();
    }

    public bool IsOnSale()
    {
        
        return false;
    }
    
}
public class StoreInfo<T, U, V> {
    public StoreInfo() {
    }

    public StoreInfo(T first, U second, V third) {
        Cost = first;
        Stack = second;
        InvCount = third;
    }

    public T Cost { get; set; } = default!;
    public U Stack { get; set; } = default!;
    public V InvCount { get; set; } = default!;
};
