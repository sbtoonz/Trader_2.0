using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Patches = Trader20.Patches;
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

    [SerializeField] internal Image? Bkg1;
    [SerializeField] internal Image? Bkg2;
    
    
    //ElementData
    [SerializeField] private GameObject? ElementGO;

    [SerializeField] private NewTrader? _trader;
    [SerializeField] internal Image? ButtonImage;
    [SerializeField] internal Image? Coins;
    
    //StoreInventoryListing
    internal Dictionary<ItemDrop, KeyValuePair<int, int>> _storeInventory = new Dictionary<ItemDrop, KeyValuePair<int,int>>();
    public static OdinStore instance => m_instance;
    internal static ElementFormat? tempElement;
    internal static Material? litpanel;
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
        m_StorePanel!.SetActive(false);
        StoreTitle!.text = "Odins Store";
        try
        {
            Bkg1!.material = litpanel;
            Bkg2!.material = litpanel;
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
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
    internal bool IsActive()
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

   internal void  ClearStore()
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
   /// <param name="drop"></param>
   /// <param name="stack"></param>
   /// <param name="cost"></param>
   public void AddItemToDisplayList(ItemDrop drop, int stack, int cost)
    {
        ElementFormat newElement = new ElementFormat();
        newElement._drop = drop;
        newElement.Icon = drop.m_itemData.m_shared.m_icons.FirstOrDefault();
        newElement.Name = drop.m_itemData.m_shared.m_name;
        newElement._drop.m_itemData.m_stack = stack;
        newElement.Element = ElementGO;

        newElement.Element!.transform.Find("icon").GetComponent<Image>().sprite = newElement.Icon;
        var name = newElement.Element.transform.Find("name").GetComponent<Text>();
        name.text = newElement.Name;
        name.gameObject.AddComponent<Localize>();
        
        newElement.Element.transform.Find("price").GetComponent<Text>().text = cost.ToString();
        
        var elementthing = Instantiate(newElement.Element, ListRoot!.transform, false);
            elementthing.GetComponent<Button>().onClick.AddListener(delegate { UpdateGenDescription(newElement); });;
            elementthing.transform.SetSiblingIndex(ListRoot.transform.GetSiblingIndex() - 1);
            elementthing.transform.Find("coin_bkg/coin icon").GetComponent<Image>().sprite = Trader20.Trader20.coins;
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
        SelectedItemDescription!.text = element._drop!.m_itemData.m_shared.m_description;
        SelectedItemDescription.gameObject.AddComponent<Localize>();
        ItemDropIcon!.sprite = element.Icon;
        tempElement = element;
    }

    public void UpdateCoins()
    {
        SelectedCost!.text = GetPlayerCoins().ToString();
    }
    public void BuyButtonAction()
    {
        if (tempElement!._drop is null) return;
        var i = FindIndex(tempElement._drop);
        if (!CanBuy(i)) return;
        SellItem(i);
        NewTrader.instance.OnSold();
        SelectedCost.text = GetPlayerCoins().ToString();
    }

    private bool CanBuy(int i)
    {
        int playerbank = GetPlayerCoins();
        var cost = _storeInventory.ElementAt(i).Value.Key;
        if (playerbank >= cost)
        {
            Player.m_localPlayer.GetInventory()
                .RemoveItem(ZNetScene.instance.GetPrefab(Trader20.Trader20.CurrencyPrefabName.Value).GetComponent<ItemDrop>().m_itemData.m_shared.m_name,
                    cost);
            return true;
        }
        return false;
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
        m_StorePanel!.SetActive(false);
    }

    public void Show()
    {
        m_StorePanel!.SetActive(true);
        ClearStore();
        if(_elements.Count >=1)
        {
            UpdateGenDescription(_elements[0]);
        }
        UpdateCoins();
    }

    private int GetPlayerCoins()
    {
        return Player.m_localPlayer.GetInventory().CountItems(ZNetScene.instance.GetPrefab(Trader20.Trader20.CurrencyPrefabName.Value).GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
    }

    public void DumpDict()
    {
        _storeInventory.Clear();
    }
}
