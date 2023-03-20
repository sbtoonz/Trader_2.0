using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NewTrader : MonoBehaviour, Hoverable, Interactable
{
    private static NewTrader? m_instance;
    public string m_name = "Knarr";

    public float m_standRange = 15f;

    public float m_greetRange = 5f;

    public float m_byeRange = 5f;

    [Header("Dialog")]
    public float m_hideDialogDelay = 5f;

    public float m_randomTalkInterval = 30f;

    public List<string> m_randomTalk = new List<string>();

    public List<string> m_randomGreets = new List<string>();

    public List<string> m_randomGoodbye = new List<string>();

    public List<string> m_randomStartTrade = new List<string>();

    public List<string> m_randomBuy = new List<string>();

    public List<string> m_randomSell = new List<string>();

    public EffectList m_randomTalkFX = new EffectList();

    public EffectList m_randomGreetFX = new EffectList();

    public EffectList m_randomGoodbyeFX = new EffectList();

    public EffectList m_randomStartTradeFX = new EffectList();

    public EffectList m_randomSellFX = new EffectList();

    public SnapToGround? _snapper;

    private bool m_didGreet = false;

    private bool m_didGoodbye = false;

    private Animator? m_animator;

    private LookAt? m_lookAt;
    public static NewTrader? instance => m_instance;

    [SerializeField] internal OdinStore? _store;


    [SerializeField] internal float rotateAtPlayerSpeed;
    private static readonly int Stand = Animator.StringToHash("Stand");

    [SerializeField] private float nextTime { get; set; }
    [SerializeField] private float modifier { get; set; }

    [SerializeField] internal ZNetView? m_netview;

    private void Start()
    {
        m_animator = GetComponentInChildren<Animator>();
        m_lookAt = GetComponentInChildren<LookAt>();
        InvokeRepeating(nameof(RandomTalk), m_randomTalkInterval, m_randomTalkInterval);
        nextTime = 0.0f;
        SnapToGround.SnappAll();
        _snapper = GetComponent<SnapToGround>();
        if(_snapper)_snapper.Snap();
    }

    private void Awake()
    {
        m_instance = this;
    }

    private void OnDestroy()
    {
        if (m_instance == this)
        {
            m_instance = null;
        }
    }

    private void Update()
    {
        modifier = Random.Range(-0.08f, 40.0f);
    }

    private void LateUpdate()
    {
        Player closestPlayer = Player.GetClosestPlayer(base.transform.position, m_standRange);
        
        if ((bool)closestPlayer)
        {
            m_animator!.SetBool(Stand, value: true);
            m_lookAt!.SetLoockAtTarget(closestPlayer.GetHeadPoint());
            var position = closestPlayer.transform.position;
            float num = Vector3.Distance(position, instance!.transform.position);
			
			
            if (!m_didGreet && num <= m_greetRange)
            {
                m_didGreet = true;
                m_didGoodbye = false;
                Say(m_randomGreets, "Greet");
                if(Trader20.Trader20.KnarrPlaySound!.Value)
                {
                    GameObject FxGreet =
                        m_randomGreetFX.m_effectPrefabs[Random.Range(0, m_randomGreetFX.m_effectPrefabs.Length)].m_prefab;
                    FxGreet.GetComponent<AudioSource>().outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
                    Instantiate(FxGreet, transform.position, Quaternion.identity);
                }
            }

            if (m_didGreet && !m_didGoodbye && num >= m_byeRange)
            {
                m_didGoodbye = true;
                m_didGreet = false;
                Say(m_randomGoodbye, "Greet");
                if (Trader20.Trader20.KnarrPlaySound!.Value)
                {
                    GameObject FxBye =
                        m_randomGoodbyeFX.m_effectPrefabs[Random.Range(0, m_randomGoodbyeFX.m_effectPrefabs.Length)]
                            .m_prefab;
                    FxBye.GetComponent<AudioSource>().outputAudioMixerGroup = AudioMan.instance.m_ambientMixer;
                    Instantiate(FxBye, transform.position, Quaternion.identity);
                }
            }
        }
        else
        {
            m_animator!.SetBool(Stand, value: false);
            m_lookAt!.ResetTarget();
        }
    }

    private void RandomTalk()
    {
        if (m_animator!.GetBool(Stand) && !StoreGui.IsVisible() && Player.IsPlayerInRange(base.transform.position, m_greetRange))
        {
            Say(m_randomTalk, "Talk");
            if (Trader20.Trader20.KnarrPlaySound!.Value)
            {
                m_randomTalkFX.Create(base.transform.position, Quaternion.identity);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string GetHoverText()
    {
        return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string GetHoverName()
    {
        return Localization.instance.Localize(m_name);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="character"></param>
    /// <param name="hold"></param>
    /// <param name="alt"></param>
    /// <returns></returns>
    public bool Interact(Humanoid character, bool hold, bool alt)
    {
        if (hold)
        {
            return false;
        }
        if (OdinStore.instance != null) OdinStore.instance.Show();
        Say(m_randomStartTrade, "Talk");
        m_randomStartTradeFX.Create(base.transform.position, Quaternion.identity);
        return false;
    }
	

    private void Say(List<string> texts, string trigger)
    {
        Say(texts[UnityEngine.Random.Range(0, texts.Count)], trigger);
    }

    private void Say(string text, string trigger)
    {
        Chat.instance.SetNpcText(base.gameObject, Vector3.up * 1.5f, 20f, m_hideDialogDelay, "", text, large: false);
        if (trigger.Length > 0)
        {
            m_animator!.SetTrigger(trigger);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="user"></param>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    public void OnSold()
    {
        Say(m_randomSell, "Sell");
        var test = RandomSellFX(1);
        GameObject RandomSell = test[Random.Range(0, test.Length)]
            .m_prefab;
        Instantiate(RandomSell, transform.position, Quaternion.identity);
    }

    private EffectList.EffectData[] RandomSellFX(int numRequired)
    {
        EffectList.EffectData[] result = new EffectList.EffectData[numRequired];

        int numToChoose = numRequired;

        for (int numLeft = m_randomSellFX.m_effectPrefabs.Length; numLeft > 0; numLeft--) {

            float prob = (float)numToChoose/(float)numLeft;

            if (!(Random.value <= prob)) continue;
            numToChoose--;
            result[numToChoose] = m_randomSellFX.m_effectPrefabs[numLeft - 1];

            if (numToChoose == 0) {
                break;
            }
        }
        return result;
    }
}