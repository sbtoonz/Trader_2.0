using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class OdinStore : MonoBehaviour
{
    private static OdinStore m_instance;
    
    [SerializeField] private GameObject m_StorePanel;
    [SerializeField] private RectTransform ListRoot;
    [SerializeField] private RectTransform SellListRoot;
    [SerializeField] private Text SelectedItemDescription;
    [SerializeField] private Image ItemDropIcon;
    [SerializeField] internal Text SelectedCost;
    [SerializeField] private Text StoreTitle;
    [SerializeField] private Button BuyButton;
    [SerializeField] private Button SellButton;
    [SerializeField] private Text SelectedName;
    [SerializeField] private Text InventoryCount;
    [SerializeField] internal GameObject InvCountPanel;

    [SerializeField] internal Image Bkg1;
    [SerializeField] internal Image Bkg2;

    //ElementData
    [SerializeField] private GameObject ElementGO;
    
    [SerializeField] private NewTrader _trader; 
    [SerializeField] internal Image BuyButtonImage;
    [SerializeField] internal Image SellButtonImage;
    [SerializeField] internal Image Coins;
    
    [SerializeField] internal RectTransform RepairRect;
    [SerializeField] internal Image repairImage;
    [SerializeField] internal Text repairText;
    [SerializeField] internal Button repairButton;
    [SerializeField] internal Image repairHammerImage;
    
    //StoreInventoryListing
    internal Dictionary<ItemDrop, KeyValuePair<int, int>> _storeInventory = new Dictionary<ItemDrop, KeyValuePair<int,int>>();
    private List<ItemDrop.ItemData> m_tempItems = new List<ItemDrop.ItemData>();
    public static OdinStore instance => m_instance;
    internal static ElementFormat tempElement;
    internal static Material litpanel;
    internal List<GameObject> CurrentStoreList = new List<GameObject>();
    internal List<ElementFormat> _elements = new List<ElementFormat>();
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

    public void OnBuyItem()
    {
        
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
        
        // var inv = Player.m_localPlayer.m_inventory;
        //
        // foreach (var inventory in inv.m_inventory)
        // {
        //     if (inventory.m_dropPrefab.name == "Coins")
        //     {
        //         SelectedCost.text = inventory.m_stack.ToString();
        //     }
        // }
    }
    public void BuyButtonAction()
    {
       var i = FindIndex(tempElement._drop);
       if(CanBuy(i))
       {
           SellItem(i);
           NewTrader.instance.OnSold();
       }
    }
    
    public void FillPlayerSaleList()
    {
    }
    

    private bool CanBuy(int i)
    {
        // var inv = Player.m_localPlayer.m_inventory;
        int playerbank = 0;
        // foreach (var item in inv.GetAllItems())
        // {
        //     if (item.m_dropPrefab.name == "Coins")
        //     {
        //         playerbank += item.m_stack;
        //     }
        // }
        var cost = _storeInventory.ElementAt(i).Value.Key;

       if (playerbank >= cost)
       {
           playerbank -= cost;
           //Todo: Fix the trader taking coins for your stuff 
           
           return true;
       }

       return playerbank <= cost && false;
    }

    /// <summary>
    /// Format of the Element GameObject that populates the for sale list.
    /// </summary>
    public class ElementFormat
    {
        internal GameObject Element;
        internal Sprite Icon;
        internal string Name;
        internal int Price;
        internal ItemDrop _drop;
    }

    private void Hide()
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
