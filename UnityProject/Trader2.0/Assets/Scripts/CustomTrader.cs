using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
// ReSharper disable NotAccessedField.Global
// ReSharper disable NotAccessedField.Local
#nullable enable
public class CustomTrader : MonoBehaviour
{
    [Header("Store instance")]
    private static CustomTrader? m_instance;
    [SerializeField]  private GameObject? m_StorePanel;
    [SerializeField] private TextMeshProUGUI? StoreTitle_TMP;
    [SerializeField] internal Image? Bkg1;
    [SerializeField] internal Image? Bkg2;
    [SerializeField] internal RectTransform? TabRect;
    
    [Space]
    [Header("Items Panel")]
    [SerializeField] private RectTransform? KnarrsListPanel;
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
    
    [Space]
    [Header("Buy Panel")]
    [SerializeField] private Button? BuyButton;
    [SerializeField] internal Image? BuyButtonImage;
    [SerializeField] internal Image? Coins;
    
    [Space]
    [Header("Elements")]
    [SerializeField] private GameObject? ElementGO;
    
    
    [Space]
    [Header("Repair Tab")]
    [SerializeField] internal RectTransform? RepairRect;
    [SerializeField] internal Image? repairImage;
    [SerializeField] internal Button? repairButton;
    [SerializeField] internal Image? repairHammerImage;

    
    //ElementPool
    private GameObject ElementPoolGo;
    private Dictionary<int, NewElementFormat> ElementPool = new Dictionary<int, NewElementFormat>();
    private List<NewElementFormat> CleaningQueue = new List<NewElementFormat>();

    //Knarrs shit YML->Here
    internal Dictionary<ItemDrop, StoreInfoNew<int, int, int>> StoreInventory =
        new Dictionary<ItemDrop, StoreInfoNew<int, int, int>>();

    private Dictionary<NewElementFormat, StoreInfoNew<int, int, int>> _knarrsDisplayedElements =
        new Dictionary<NewElementFormat, StoreInfoNew<int, int, int>>();


    public static CustomTrader instance => m_instance;
    
    private void Awake()
    {
        if(!m_instance) m_instance = this;
        ElementPoolGo = new GameObject();
        ElementPoolGo.transform.SetParent(this.transform);
        DontDestroyOnLoad(ElementPoolGo);
        DeployElementPool();
        
        //ReadKnarrItems
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    private void OnDestroy()
    {
        
    }

    private async void OnGUI()
    {
        if (CleaningQueue.Count <= 0) return;
        var tasks = new Task[CleaningQueue.Count];
        for (int i = 0; i < tasks.Length; i++)
        {
            await CleanAndReturnElementObj(CleaningQueue[i]);
        }
        CleaningQueue.Clear();
    }

    internal void AddElementToCleaningQueue(NewElementFormat elementFormat)
    {
        CleaningQueue.Add(elementFormat);
    }

    private async Task CleanAndReturnElementObj(NewElementFormat element)
    {
        element.Element!.transform.SetParent(ElementPoolGo.transform, false);
        element.Element.SetActive(false);
        element.Drop = null;
        element.Name = null;
        element.Icon = null;
        element.InventoryCount = null;
        element.UITooltip = null;
        element.Price = null;
        await Task.Yield();
    }

    private Task<NewElementFormat> GetAndSetupElement(ItemDrop? drop, int price, int stack, int invCount)
    {
        var firstOrDefault = ElementPool.FirstOrDefault();
        var element = firstOrDefault.Value;
        element.Drop = drop;
        element.Drop!.m_itemData = drop!.m_itemData.Clone();
        element.Name = Localization.instance.Localize(element.Drop.m_itemData.m_shared.m_name);
        element.Icon = element.Drop.m_itemData.GetIcon();
        element.Price = price;
        element.Drop.m_itemData.m_stack = stack;
        element.InventoryCount = invCount;
        element.Element!.transform.Find("icon").GetComponent<Image>().sprite = element.Icon;
        var component = element.Element.transform.Find("name").GetComponent<Text>();
        component.text = element.Name;
        component.gameObject.AddComponent<Localize>();
        
        element.Element.transform.Find("price").GetComponent<Text>().text = price.ToString();
        
            
        
        // element.Element.transform.Find("stack").GetComponent<Text>().text = stack switch
        // {
        //     > 1 => "x" + stack,
        //     1 => "",
        //     _ => element.Element.transform.Find("stack").GetComponent<Text>().text
        // };
        return Task.FromResult(element);
    }

    private void DeployElementPool()
    {
        for (int i = 0; i < StoreInventory.Count+150; i++)
        {
            var newElement = Instantiate(ElementGO, ElementPoolGo.transform);
            newElement!.SetActive(false);
            NewElementFormat element = new NewElementFormat()
            {
                Element = newElement
            };
            ElementPool.Add(i, element);
        }
    }

    private void ReadKnarrItems()
    {
        var tasks = new Task<NewElementFormat>[StoreInventory.Count];
        for (int i = 0; i < tasks.Length; i++)
        {
            var store = StoreInventory.ElementAt(i).Value;
            tasks[i] = GetAndSetupElement(StoreInventory.ElementAt(i).Key, store.Cost, store.Stack, store.InvCount);
            tasks[i].Result.Element!.transform.SetParent(KnarrsListPanel, false);
        }
    }
    
}
[Serializable]
public class NewElementFormat
{
    internal GameObject? Element;
    internal Sprite? Icon;
    internal string? Name;
    internal int? Price;
    internal ItemDrop? Drop;
    internal int? InventoryCount;
    internal UITooltip? UITooltip;
}

[Serializable]
public class StoreInfoNew<TItemCost, TItemStack, TItemInventoryCount> {
    public StoreInfoNew(TItemCost cost, TItemStack stack, TItemInventoryCount count, ItemDrop drop) {
        Cost = cost;
        Stack = stack;
        InvCount = count;
        this.Drop = drop;
    }

    public TItemCost Cost { get; set; }
    public TItemStack Stack { get; set; }
    public TItemInventoryCount InvCount { get; set; }
    public ItemDrop Drop { get; set; }
};