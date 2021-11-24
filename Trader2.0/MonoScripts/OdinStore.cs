using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class OdinStore : MonoBehaviour
{
    private static OdinStore m_instance;
    
    [SerializeField] private GameObject m_StorePanel;
    [SerializeField] private RectTransform ListRoot;
    [SerializeField] private Text SelectedItemDescription;
    [SerializeField] private Image ItemDropIcon;
    [SerializeField] private Text SelectedCost;
    [SerializeField] private Text StoreTitle;
    [SerializeField] private Button BuyButton;
    [SerializeField] private Text SelectedName;

    [SerializeField] internal Image Bkg1;
    [SerializeField] internal Image Bkg2;
    
    
    //ElementData
    [SerializeField] private GameObject ElementGO;

    [SerializeField] private NewTrader _trader;
    [SerializeField] internal Image ButtonImage;
    [SerializeField] internal Image Coins;
    
    //StoreInventoryListing
    internal Dictionary<ItemDrop, KeyValuePair<int, int>> _storeInventory = new Dictionary<ItemDrop, KeyValuePair<int,int>>();
    public static OdinStore instance => m_instance;
    internal static ElementFormat tempElement;
    internal static Material litpanel;
    internal List<GameObject> CurrentStoreList = new List<GameObject>();
    internal List<ElementFormat> _elements = new List<ElementFormat>();
    internal ItemDrop.ItemData? coins1 = null;
    internal ItemDrop.ItemData? coins2 = null;
    internal ItemDrop.ItemData? coins3 = null;
    internal ItemDrop.ItemData? coins4 = null;
    internal ItemDrop.ItemData? coins5 = null;
    internal ItemDrop.ItemData? coins6 = null;
    internal ItemDrop.ItemData? coins7 = null;
    internal ItemDrop.ItemData? coins8 = null;
    private void Awake() 
    {
        m_instance = this;
        m_StorePanel.SetActive(false);
        StoreTitle.text = "Odins Store";
        try
        {
            Bkg1.material = litpanel;
            Bkg2.material = litpanel;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
    }

    private void OnGUI()
    {
        if (Input.GetKey(KeyCode.Escape))
        {
            Hide();
        }
    }
    internal bool IsActive()
    {
        return m_StorePanel.activeSelf;
    }
    private void OnDestroy()
    {
        if (m_instance == this)
        {
            m_instance = null;
        }
    }

   private void  ClearStore()
    {
        if (CurrentStoreList.Count != _storeInventory.Count)
        {
            foreach (var GO in CurrentStoreList)
            {
                Destroy(GO);
            }
            
            CurrentStoreList.Clear();
            ReadItems();
            
        }
    }

    /// <summary>
    /// This method is invoked to add an item to the visual display of the store, it expects the ItemDrop.ItemData and the stack as arguments
    /// </summary>
    /// <param name="_drop"></param>
    /// <param name="stack"></param>
    public void AddItemToDisplayList(ItemDrop _drop, int stack, int cost)
    {
        ElementFormat newElement = new ElementFormat();
        newElement._drop = _drop;
        newElement.Icon = _drop.m_itemData.m_shared.m_icons.FirstOrDefault();
        newElement.Name = _drop.m_itemData.m_shared.m_name;
        newElement._drop.m_itemData.m_stack = stack;
        newElement.Element = ElementGO;

        newElement.Element.transform.Find("icon").GetComponent<Image>().sprite = newElement.Icon;
        var name = newElement.Element.transform.Find("name").GetComponent<Text>();
        name.text = newElement.Name;
        name.gameObject.AddComponent<Localize>();
        
        newElement.Element.transform.Find("price").GetComponent<Text>().text = cost.ToString();
        
        var elementthing = Instantiate(newElement.Element, ListRoot.transform, false);
            elementthing.GetComponent<Button>().onClick.AddListener(delegate { UpdateGenDescription(newElement); });;
        newElement.Element.transform.SetSiblingIndex(ListRoot.transform.GetSiblingIndex() - 1);
        _elements.Add(newElement);
        CurrentStoreList.Add(elementthing);
    }

    private void  ReadItems()
    {
        foreach (var itemData in _storeInventory)
        {
            //need to add some type of second level logic here to think about if items exist do not repopulate.....
            AddItemToDisplayList(itemData.Key,1, itemData.Value.Key);
        }
    }

    /// <summary>
    /// Invoke this method to instantiate an item from the storeInventory dictionary. This method expects an integer argument this integer should identify the index in the dictionary that the item lives at you wish to vend
    /// </summary>
    /// <param name="i"></param>
    public void SellItem(int i)
    {
        //spawn item on ground if no inventory room
        Vector3 vector = Random.insideUnitSphere * 0.5f;
        var transform1 = Player.m_localPlayer.transform;
        var itemDrop = (ItemDrop)Instantiate(_storeInventory.ElementAt(i).Key.gameObject,
            transform1.position + transform1.forward * 2f + Vector3.up + vector,
            Quaternion.identity).GetComponent(typeof(ItemDrop));
        if (itemDrop == null || itemDrop.m_itemData == null) return;
        
        itemDrop.m_itemData.m_stack = _storeInventory.ElementAt(i).Value.Value;
        itemDrop.m_itemData.m_durability = itemDrop.m_itemData.GetMaxDurability();

    }


    /// <summary>
    ///  Adds item to stores dictionary pass ItemDrop.ItemData and an integer for price
    /// </summary>
    /// <param name="itemDrop"></param>
    /// <param name="price"></param>
    public void AddItemToDict(ItemDrop itemDrop, int price, int stack)
    {
        _storeInventory.Add(itemDrop, new KeyValuePair<int, int>(price, stack) );
    }

    /// <summary>
    /// Pass this method an ItemDrop as an argument to drop it from the storeInventory dictionary.
    /// </summary>
    /// <param name="itemDrop"></param>
    /// <returns></returns>
    public bool RemoveItemFromDict(ItemDrop itemDrop)
    {
        return _storeInventory.Remove(itemDrop);
    }

    /// <summary>
    /// This methods invocation should return the index offset of the ItemDrop passed as an argument, this is for use with other functions that expect an index to be passed as an integer argument
    /// </summary>
    /// <param name="itemDrop"></param>
    /// <returns></returns>
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
        SelectedItemDescription.text = element._drop.m_itemData.m_shared.m_description;
        SelectedItemDescription.gameObject.AddComponent<Localize>();
        ItemDropIcon.sprite = element.Icon;
        tempElement = element;
    }

    public void UpdateCoins()
    {
        
        var inv = Player.m_localPlayer.m_inventory;

        foreach (var inventory in inv.m_inventory)
        {
            if (inventory.m_dropPrefab.name == "Coins")
            {
                SelectedCost.text = inventory.m_stack.ToString();
            }
        }
    }
    public void BuyButtonAction()
    {
        if (tempElement._drop is null) return;
        var i = FindIndex(tempElement._drop);
        if (!CanBuy(i)) return;
        SellItem(i);
        NewTrader.instance.OnSold();
    }

    private bool CanBuy(int i)
    {
        var inv = Player.m_localPlayer.m_inventory;

        
        int playerbank = 0;
        foreach (var item in inv.GetAllItems())
        {
            if (item.m_dropPrefab.name == "Coins")
            {
                playerbank += item.m_stack;
                if (item.m_stack < item.m_shared.m_maxStackSize) continue;
                if (coins1 == null)
                {
                    coins1 = item;
                }
                else if(coins2 == null)
                {
                    coins2 = item;
                }
                else if (coins3 == null)
                {
                    coins3 = item;
                }
                else if (coins4 == null)
                {
                    coins4 = item;
                }
                else if (coins5 == null)
                {
                    coins5 = item;
                }
                else if (coins6 == null)
                {
                    coins6 = item;
                }
                else if (coins7 == null)
                {
                    coins7 = item;
                }
            }
        }
        var cost = _storeInventory.ElementAt(i).Value.Key;

        if (playerbank < cost) return playerbank <= cost && false;
        if (coins1 != null && cost > coins1.m_stack)
        {
            if (coins2 != null)
            {
                var newstack = coins1.m_stack + coins2.m_stack;
                coins2.m_stack = 0;
                inv.RemoveOneItem(coins2);
                coins2 = null;
                newstack -= cost;
                coins1.m_stack = newstack;
            }
        }
        else if (coins1 != null && coins2 != null && cost > coins1.m_stack + coins2.m_stack)
        {
            if (coins3 != null)
            {
                var newstack2 = coins1.m_stack + coins2.m_stack + coins3.m_stack;
                coins2.m_stack = 0;
                inv.RemoveOneItem(coins2);
                coins2 = null;
                coins3.m_stack = 0;
                inv.RemoveOneItem(coins3);
                coins3 = null;
                newstack2 -= cost;
                coins1.m_stack = newstack2;
            }
        }
        else if (coins1 != null && coins3 != null && coins2 != null && cost > coins1.m_stack + coins2.m_stack + coins3.m_stack)
        {
            if (coins4 != null)
            {
                var newstack3 = coins1.m_stack + coins2.m_stack + coins3.m_stack + coins4.m_stack;
                coins2.m_stack = 0;
                inv.RemoveOneItem(coins2);
                coins2 = null;
                coins3.m_stack = 0;
                inv.RemoveOneItem(coins3);
                coins3 = null;
                coins4.m_stack = 0;
                inv.RemoveOneItem(coins4);
                coins4 = null;
                newstack3 -= cost;
                coins1.m_stack = newstack3;
            }
        }
        else if (coins4 != null && coins3 != null && coins2 != null && cost > coins1.m_stack + coins2.m_stack + coins3.m_stack + coins4.m_stack)
        {
            if (coins5 != null)
            {
                var newstack4 = coins1.m_stack + coins2.m_stack + coins3.m_stack + coins4.m_stack + coins5.m_stack;
                coins2.m_stack = 0;
                inv.RemoveOneItem(coins2);
                coins2 = null;
                coins3.m_stack = 0;
                inv.RemoveOneItem(coins3);
                coins3 = null;
                coins4.m_stack = 0;
                inv.RemoveOneItem(coins4);
                coins4 = null;
                coins5.m_stack = 0;
                inv.RemoveOneItem(coins5);
                coins5 = null;
                newstack4 -= cost;
                coins1.m_stack = newstack4;
            }
        }
        else if (coins5 != null && coins4 != null && coins3 != null && coins2 != null && cost > coins1.m_stack + coins2.m_stack + coins3.m_stack + coins4.m_stack+ coins5.m_stack)
        {
            if (coins6 != null)
            {
                var newstack5 = coins1.m_stack + coins2.m_stack + coins3.m_stack + coins4.m_stack + coins5.m_stack + coins6.m_stack;
                coins2.m_stack = 0;
                inv.RemoveOneItem(coins2);
                coins2 = null;
                coins3.m_stack = 0;
                inv.RemoveOneItem(coins3);
                coins3 = null;
                coins4.m_stack = 0;
                inv.RemoveOneItem(coins4);
                coins4 = null;
                coins5.m_stack = 0;
                inv.RemoveOneItem(coins5);
                coins5 = null;
                coins6.m_stack = 0;
                inv.RemoveOneItem(coins6);
                coins6 = null;
                newstack5 -= cost;
                coins1.m_stack = newstack5;
            }
        }
        else if (coins6 != null && coins5 != null && coins4 != null && coins3 != null && coins2 != null && coins1 != null && cost > coins1.m_stack + coins2.m_stack + coins3.m_stack + coins4.m_stack+ coins5.m_stack + coins6.m_stack)
        {
            if (coins7 != null)
            {
                var newstack6 = coins1.m_stack + coins2.m_stack + coins3.m_stack + coins4.m_stack + coins5.m_stack + coins6.m_stack + coins7.m_stack;
                coins2.m_stack = 0;
                inv.RemoveOneItem(coins2);
                coins2 = null;
                coins3.m_stack = 0;
                inv.RemoveOneItem(coins3);
                coins3 = null;
                coins4.m_stack = 0;
                inv.RemoveOneItem(coins4);
                coins4 = null;
                coins5.m_stack = 0;
                inv.RemoveOneItem(coins5);
                coins5 = null;
                coins6.m_stack = 0;
                inv.RemoveOneItem(coins6);
                coins6 = null;
                coins7.m_stack = 0;
                inv.RemoveOneItem(coins7);
                coins7 = null;
                newstack6 -= cost;
                coins1.m_stack = newstack6;
            }
        }
        return true;

    }

    /// <summary>
    /// Format of the Element GameObject that populates the for sale list.
    /// </summary>
    public class ElementFormat
    {
        internal GameObject? Element;
        internal Sprite? Icon;
        internal string? Name;
        internal int? Price;
        internal ItemDrop? _drop;
    }
    public void Hide()
    {
        m_StorePanel.SetActive(false);
    }

    public void Show()
    {
        m_StorePanel.SetActive(true);
        ClearStore();
        if(_elements.Count >=1)
        {
            UpdateGenDescription(_elements[0]);
        }
        UpdateCoins();
    }
}
