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

    [SerializeField] private Image Bkg1;
    [SerializeField] private Image Bkg2;
    
    
    //ElementData
    [SerializeField] private GameObject ElementGO;
    
    //StoreInventoryListing
    internal Dictionary<ItemDrop, int> _storeInventory = new Dictionary<ItemDrop, int>();
    public static OdinStore instance => m_instance;
    internal static ElementFormat tempElement;
    internal static Material litpanel;
    internal List<GameObject> CurrentStoreList;
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

    private void OnDestroy()
    {
        if (m_instance == this)
        {
            m_instance = null;
        }
    }

    /// <summary>
    /// This method is invoked to add an item to the visual display of the store, it expects the ItemDrop.ItemData and the stack as arguments
    /// </summary>
    /// <param name="_drop"></param>
    /// <param name="stack"></param>
    public void AddItemToDisplayList(ItemDrop _drop, int stack, int cost)
    {
        if (CurrentStoreList.Count >= 1)
        {
            foreach (var GO in CurrentStoreList)
            {
                Destroy(GO);
            }
            CurrentStoreList.Clear();
        }
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
        
        var tmp = Instantiate(newElement.Element, ListRoot.transform, false);
        tmp.GetComponent<Button>().onClick.AddListener(delegate
        {
            UpdateGenDescription(newElement);
        });
        CurrentStoreList.Add(tmp);
        newElement.Element.transform.SetSiblingIndex(ListRoot.transform.GetSiblingIndex() - 1);
    }

    public void ReadItems()
    {
        foreach (var itemData in _storeInventory)
        {
            //need to add some type of second level logic here to think about if items exist do not repopulate.....
            AddItemToDisplayList(itemData.Key,itemData.Key.m_itemData.m_stack, itemData.Value);
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
        
        itemDrop.m_itemData.m_stack = _storeInventory.ElementAt(i).Key.m_itemData.m_stack;
        itemDrop.m_itemData.m_durability = itemDrop.m_itemData.GetMaxDurability();

    }


    /// <summary>
    ///  Adds item to stores dictionary pass ItemDrop.ItemData and an integer for price
    /// </summary>
    /// <param name="itemDrop"></param>
    /// <param name="price"></param>
    public void AddItemToDict(ItemDrop itemDrop, int price)
    {
        _storeInventory.Add(itemDrop, price);
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
    public int FindIndex(ItemDrop itemDrop)
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
        SelectedCost.text = element.Price.ToString();
        SelectedItemDescription.text = element._drop.m_itemData.m_shared.m_description;
        SelectedItemDescription.gameObject.AddComponent<Localize>();
        SelectedCost.gameObject.AddComponent<Localize>();
        ItemDropIcon.sprite = element.Icon;
        tempElement = element;
    }

    public void BuyButtonAction()
    {
       var i = FindIndex(tempElement._drop);
       SellItem(i);
    }

    public bool CanBuy(int i)
    {
        var inv =  Player.m_localPlayer.GetInventory();
        int playerbank = 0;
        foreach (var item in inv.m_inventory)
        {
            if (item.m_shared.m_name == "Coins")
            {
                playerbank = item.m_stack;
            }
        }
        var cost = _storeInventory.ElementAt(i).Value;

       if (playerbank >= cost)
       {
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
    public void Hide()
    {
        m_StorePanel.SetActive(false);
    }

    public void Show()
    {
        m_StorePanel.SetActive(true);
        ReadItems();
    }
}
