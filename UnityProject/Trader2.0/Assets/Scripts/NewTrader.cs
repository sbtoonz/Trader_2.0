// Trader

using System;
using System.Collections.Generic;
using UnityEngine;

public class NewTrader : MonoBehaviour, Hoverable, Interactable
{
	private static NewTrader m_instance;
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

	private bool m_didGreet;

	private bool m_didGoodbye;

	private Animator m_animator;

	private LookAt m_lookAt;

	public static NewTrader instance => m_instance;

	[SerializeField] internal OdinStore _store;

	private void Start()
	{
		m_animator = GetComponentInChildren<Animator>();
		m_lookAt = GetComponentInChildren<LookAt>();
		InvokeRepeating("RandomTalk", m_randomTalkInterval, m_randomTalkInterval);
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
		Player closestPlayer = Player.GetClosestPlayer(base.transform.position, m_standRange);
		if ((bool)closestPlayer)
		{
			m_animator.SetBool("Stand", value: true);
			m_lookAt.SetLoockAtTarget(closestPlayer.GetHeadPoint());
			float num = Vector3.Distance(closestPlayer.transform.position, instance.transform.position);
			if (!m_didGreet && num < m_greetRange)
			{
				m_didGreet = true;
				Say(m_randomGreets, "Greet");
				m_randomGreetFX.Create(base.transform.position, Quaternion.identity);
			}
			if (m_didGreet && !m_didGoodbye && num > m_byeRange)
			{
				m_didGoodbye = true;
				Say(m_randomGoodbye, "Greet");
				m_randomGoodbyeFX.Create(base.transform.position, Quaternion.identity);
			}
		}
		else
		{
			m_animator.SetBool("Stand", value: false);
			m_lookAt.ResetTarget();
		}
	}

	private void RandomTalk()
	{
		if (m_animator.GetBool("Stand") && !StoreGui.IsVisible() && Player.IsPlayerInRange(base.transform.position, m_greetRange))
		{
			Say(m_randomTalk, "Talk");
			m_randomTalkFX.Create(base.transform.position, Quaternion.identity);
		}
	}

	public string GetHoverText()
	{
		return Localization.instance.Localize(m_name + "\n[<color=yellow><b>$KEY_Use</b></color>] $raven_interact");
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(m_name);
	}

	public bool Interact(Humanoid character, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		OdinStore.instance.Show();
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
			m_animator.SetTrigger(trigger);
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}

	public void OnSold()
	{
		Say(m_randomSell, "Sell");
		m_randomSellFX.Create(base.transform.position, Quaternion.identity);
	}
}
