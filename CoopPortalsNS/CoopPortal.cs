using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoopPortalsNS;

public class CoopPortal : CardData
{
	[ExtraData("_connectedFriend")]
	public string? _ConnectedFriend;

	[ExtraData("ConnectedPortalUId")]
	public string? ConnectedPortalUId;
	public int FailedPingsCount;

	public bool ConnectionActive = false;
	public bool LinkedToUnstable = false;

	protected bool FirstTimeActionsDone = false;

	protected float ConnectionPingTimer = 0f;

	public CSteamID ConnectedFriend
	{
		get
		{
			return (CSteamID)Convert.ToUInt64((!string.IsNullOrEmpty(_ConnectedFriend)) ? _ConnectedFriend : "0");
		}
		set
		{
			_ConnectedFriend = value.ToString();
		}
	}

	public bool HasConnectedFriend => !string.IsNullOrEmpty(_ConnectedFriend);

	public string ConnectedFriendName => HasConnectedFriend ? SteamFriends.GetFriendPersonaName(ConnectedFriend) : "";

	public override bool CanHaveCard(CardData otherCard)
	{
		return HasConnectedFriend && !MyGameCard.TimerRunning && (otherCard is not CoopPortal portal || portal.ConnectionActive || ConnectionActive);
	}

	public virtual void ReceivePacket(PortalPacket packet)
	{
		
		if (packet.FromPortalUId != ""
			&& ConnectedPortalUId != packet.FromPortalUId
			&& (ConnectedPortalUId == "" || LinkedToUnstable || !ConnectionActive))
		{
			ConnectedPortalUId = packet.FromPortalUId;
		}
		
		Activate();

		LinkedToUnstable = packet.IsFromUnstablePortal;

		if (packet.FromFriendId != _ConnectedFriend)
		{
			_ConnectedFriend = packet.FromFriendId;
			UpdateCardText();
		}
		//CoopPortalsPlugin.Log(UniqueId+" Received "+packet.Type.ToString()+" from "+packet.FromPortalUId+(packet.IsFromUnstablePortal?" is from unstable":""));
		switch (packet.Type)
		{
		case PortalPacket.MessageType.ConnectionInitiation:
			SendToFriend(PortalPacket.MessageType.Pong);
			break;
		case PortalPacket.MessageType.Ping:
			SendToFriend(PortalPacket.MessageType.Pong);
			break;
		case PortalPacket.MessageType.Pong:
			break;
		case PortalPacket.MessageType.PortalingCardsStart:
			ReceiveCards(packet.Cards);
			break;
		case PortalPacket.MessageType.PortalingCardsSuccess:
			break;
		case PortalPacket.MessageType.PortalingCardsFail:
			ReceiveCards(packet.Cards, packet.Age);
			break;
		case PortalPacket.MessageType.Notification:
			GameScreen.instance.AddNotification("From: "+this.ConnectedFriendName, packet.Message, delegate
			{
			});
			break;
		}
	}

	public virtual void Activate()
	{
		FailedPingsCount = 0;
		if (!ConnectionActive)
		{
			//CoopPortalsPlugin.Log("Activating");
			ConnectionActive = true;
			//IsFoil = true;
			WorldManager.instance.QueueCutscene(MessageCutscene("Portal opened to " + ConnectedFriendName));
		}
	}

	public virtual void Deactivate()
	{
		if (ConnectionActive)
		{
			//CoopPortalsPlugin.Log("Deactivate");
			ConnectionActive = false;
			//IsFoil = false;
		}
	}

	public override void UpdateCard()
	{
		if (!MyGameCard.IsDemoCard)
		{
			// if (CoopPortalsPlugin.VsMode.Value)
			// {
			// 	this.Value = -1;
			// }
			if (!FirstTimeActionsDone)
			{
				//CoopPortalsPlugin.Log("doin first time actions");
				FirstTimeActionsDone = true;
				FirstTimeActions();
			}
			if (!HasConnectedFriend && !GameCanvas.instance.ModalIsOpen)
			{
				SelectLinkedFriend();
			}
			else if (HasConnectedFriend && _ConnectedFriend != "0")
			{
				UpdateConnection();
			}
			base.UpdateCard();
		}
	}

	public virtual void UpdateConnection()
	{
		if (!HasConnectedFriend)
		{
			Deactivate();
			return;
		}
		ConnectionPingTimer += Time.deltaTime;
		if (ConnectionPingTimer > 5f || (ConnectionActive && ConnectionPingTimer > 1f))
		{
			ConnectionPingTimer = 0f;
			ConnectionPing();
		}
		if (!ConnectionActive)
		{
			return;
		}

		if (MyGameCard.Child != null)
		{
			PortalChildren();
		}

		if (MyGameCard.Parent != null)
		{
			PortalParents();
		}

	}

	public virtual void FirstTimeActions()
	{
		//UpdateText();
		ConnectionPing();
	}

	public virtual void SelectLinkedFriend()
	{
		CoopPortalsPlugin.Log("attempting to show friend list");
		ModalScreen.instance.Clear();
		ModalScreen.instance.SetTexts("select friend to connect to", "");
		int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);

		CustomButton backButton = UnityEngine.Object.Instantiate(ModalScreen.instance.ButtonPrefab);
		backButton.transform.SetParent(ModalScreen.instance.ButtonParent);
		backButton.transform.localPosition = Vector3.zero;
		backButton.transform.localScale = Vector3.one;
		backButton.transform.localRotation = Quaternion.identity;
		backButton.TextMeshPro.text = SokLoc.Translate("label_back");
		backButton.Clicked += delegate
		{
			CoopPortalsPlugin.Log("Friend selection closed.");
			GameCanvas.instance.CloseModal();
			Subprint print = WorldManager.instance.GetBlueprintWithId("blueprint_cp_coop_portal").Subprints.FirstOrDefault();
			foreach(string cardId in print.GetAllCardsToRemove())
			{
				string firstOptionCardId = CardStringSplitter.me.Split(cardId)[0];
				CardData newCard = WorldManager.instance.CreateCard(this.transform.position, firstOptionCardId, checkAddToStack: false, playSound: false);
				newCard.MyGameCard.SendIt();
			}
			this._ConnectedFriend = "0";
			this.MyGameCard.DestroyCard(true, true);
		};

		GameObject gameObject = UnityEngine.Object.Instantiate(GameScreen.instance.IdeasTab.gameObject, ModalScreen.instance.ButtonParent);
		foreach (RectTransform item in gameObject.transform)
		{
			if (item.name != "SearchParent" && item.name != "Scroll View")
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
		RectTransform cardTabReactTransform = gameObject.GetComponent<RectTransform>();
		if (cardTabReactTransform != null)
		{
			cardTabReactTransform.anchoredPosition = new Vector2(275f, -80f);
		}
		ContentSizeFitter component = GameCanvas.instance.Modal.Find("Modal").gameObject.GetComponent<ContentSizeFitter>();
		if (component != null)
		{
			component.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
		}
		RectTransform component2 = GameCanvas.instance.Modal.Find("Modal").gameObject.GetComponent<RectTransform>();
		if (component2 != null)
		{
			component2.sizeDelta = new Vector2(600f, 708f);
		}
		RectTransform component3 = gameObject.transform.Find("Scroll View").gameObject.GetComponent<RectTransform>();
		component3.sizeDelta = new Vector2(0f, 500f);
		component3.anchoredPosition = new Vector2(0f, -300f);
		gameObject.SetActive(value: true);
		Transform content = gameObject.transform.Find("Scroll View/Viewport/IdeaParent");
		foreach (RectTransform item in content)
		{
			UnityEngine.Object.Destroy(item.gameObject);
		}
		TMP_InputField component4 = gameObject.transform.GetComponentInChildren<TMP_InputField>();
		component4.text = "";
		component4.onValueChanged.AddListener(delegate(string value)
		{
			foreach (RectTransform item2 in content)
			{
				CustomButton component5 = item2.gameObject.GetComponent<CustomButton>();
				if (component5 != null)
				{
					component5.gameObject.SetActive(component5.TextMeshPro.text.ToLower().Contains(value.ToLower()));
				}
				// else
				// {
				// 	item2.gameObject.SetActive(value: false);
				// }
			}
		});
		ModalScreen.instance.ButtonParent.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
		

		bool flag = true;
		// Transform content = gameObject.transform.Find("Scroll View/Viewport");
		// foreach (RectTransform item in content)
		// {
		// 	UnityEngine.Object.Destroy(item.gameObject);
		// }
		// TMP_InputField component4 = gameObject.transform.GetComponentInChildren<TMP_InputField>();
		// component4.text = "";
		// component4.onValueChanged.AddListener(delegate(string value)
		// {
		// 	foreach (RectTransform item2 in content)
		// 	{
		// 		CustomButton component5 = item2.gameObject.GetComponent<CustomButton>();
		// 		if (component5 != null)
		// 		{
		// 			component5.gameObject.SetActive(component5.TextMeshPro.text.ToLower().Contains(value.ToLower()));
		// 		}
		// 		// else
		// 		// {
		// 		// 	item2.gameObject.SetActive(value: false);
		// 		// }
		// 	}
		// });
		// ModalScreen.instance.ButtonParent.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = true;
		

		// bool flag = true;
		// Adds themselves:
		if (WorldManager.instance.DebugScreenOpened)
		{
			CSteamID meId = SteamUser.GetSteamID();
			CustomButton customButton = UnityEngine.Object.Instantiate(ModalScreen.instance.ButtonPrefab);
			customButton.transform.SetParent(content);
			customButton.transform.localPosition = Vector3.zero;
			customButton.transform.localScale = Vector3.one;
			customButton.transform.localRotation = Quaternion.identity;
			customButton.TextMeshPro.text = SteamFriends.GetPersonaName();
			customButton.Clicked += delegate
			{
				GameCanvas.instance.CloseModal();
				component2.sizeDelta = new Vector2(600, 100);
				component.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
				ConnectedFriend = meId;
				ConnectionPing();
			};
		}

		for (int i = 0; i < friendCount; i++)
		{
			try
			{
				CSteamID friend = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
				CustomButton customButton2 = UnityEngine.Object.Instantiate(ModalScreen.instance.ButtonPrefab);
				customButton2.transform.SetParent(content);
				customButton2.transform.localPosition = Vector3.zero;
				customButton2.transform.localScale = Vector3.one;
				customButton2.transform.localRotation = Quaternion.identity;
				customButton2.TextMeshPro.text = SteamFriends.GetFriendPersonaName(friend);
				customButton2.Clicked += delegate
				{
					CoopPortalsPlugin.Log("friend chosen: " + SteamFriends.GetFriendPersonaName(friend));
					GameCanvas.instance.CloseModal();
					if (component2 != null) component2.sizeDelta = new Vector2(600, 100);
					if (component != null) component.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
					ConnectedFriend = friend;
					ConnectionPing();
				};
			}
			catch
			{
				flag = false;
			}
		}
		if (flag)
		{
			GameCanvas.instance.OpenModal();
		}
		//CoopPortalsPlugin.Log("doin stuff");
	}

	public virtual void PortalChildren()
	{
		//CoopPortalsPlugin.Log("PortalChildren");
		GameCard child = MyGameCard.Child;
		MyGameCard.RemoveFromStack();
		List<GameCard> list = new List<GameCard>();
		while (child != null)
		{
			list.Add(child);
			if (child.EquipmentChildren.Any())
			{
				list.AddRange(child.EquipmentChildren);
			}
			child = child.Child;
		}
		PortalCards(list);
	}
	public virtual void PortalParents()
	{
		//CoopPortalsPlugin.Log("PortalParents");
		GameCard parent = MyGameCard.GetRootCard();
		if (parent.CardData is CoopPortal portal && portal.ConnectionActive)
		{
			return;
		}
		CoopPortalsPlugin.Log("PortalParents1");
		MyGameCard.RemoveFromStack();
		List<GameCard> list = new List<GameCard>();
		while (parent != null && parent != this.MyGameCard && !(parent.CardData is CoopPortal p && p.ConnectionActive))
		{
			list.Add(parent);
			if (parent.EquipmentChildren.Any())
			{
				list.AddRange(parent.EquipmentChildren);
			}
			parent = parent.Child;
		}
		PortalCards(list);
	}

	public virtual void PortalCards(List<GameCard> cards)
	{
		if (!cards.Any())
		{
			return;
		}

		if (ConnectedFriend == SteamUser.GetSteamID()
			&& cards.Any<GameCard>(card =>
				card.CardData is CoopPortal portal
				&& portal.ConnectedFriend == SteamUser.GetSteamID()
				&& portal.ConnectedPortalUId == UniqueId
				&& ConnectedPortalUId == portal.UniqueId))
		{
			CoopPortalsPlugin.Log("PortalIngPortalToSelfToSelf");
			foreach (GameCard item in cards)
			{
				item.DestroyCard(spawnSmoke: true, playSound: false);
			}
			WorldManager.instance.CreateCard(base.transform.position, "cp_void_portal", faceUp: true, checkAddToStack: false, playSound: false);
			AudioManager.me.PlaySound2D(CoopPortalsPlugin.MyAudioClips["portalingAway"], UnityEngine.Random.Range(0.8f, 1.2f), 1f);
			AudioManager.me.PlaySound2D(CoopPortalsPlugin.MyAudioClips["portalingAway"], UnityEngine.Random.Range(0.2f, 0.4f), 1f);
			AudioManager.me.PlaySound2D(CoopPortalsPlugin.MyAudioClips["portalOpening"], UnityEngine.Random.Range(0.2f, 0.4f), 1f);
			return;
		}
		
		int howManyVillagersPorting  = cards.Count((GameCard x) => x != null && x.CardData is BaseVillager);
		int howManyVillagersAre = WorldManager.instance.GetCardCount((CardData x) => x is BaseVillager);
		if (howManyVillagersPorting == howManyVillagersAre && !GameCanvas.instance.ModalIsOpen && howManyVillagersPorting != 0)
		{
			GameCard responcible = cards.FindLast((GameCard x) => x.CardData is BaseVillager);
			if (responcible != null)
			{
				responcible.RemoveFromStack();
				cards.Remove(responcible);
				GameCanvas.instance.OneVillagerNeedsToStayPrompt("label_taking_portal_title");
			}
		}

		CoopPortalsPlugin.Log("Stage 1 of attempting to send cards to " + ConnectedFriend);
		Steamworks.P2PSessionState_t connState;
		if (SteamNetworking.GetP2PSessionState(ConnectedFriend, out connState)
			&& connState.m_bConnectionActive != 0)
		{
			CoopPortalsPlugin.Log("Stage 2 of attempting to send cards to " + ConnectedFriend);
			AudioManager.me.PlaySound2D(CoopPortalsPlugin.MyAudioClips["portalingAway"], UnityEngine.Random.Range(0.8f, 1.2f), 1f);
			SendToFriend(cards);
			foreach (GameCard item in cards)
			{
				item.DestroyCard(spawnSmoke: true, playSound: false);
			}
		}
	}

	public virtual void ReceiveCards(List<SavedCard> cardsToSpawn, short age = 10)
	{
		try
		{
			//CoopPortalsPlugin.Log("ReceiveCards");
			foreach (SavedCard item in cardsToSpawn)
			{
				string newItemId = item.UniqueId;
				item.UniqueId = Guid.NewGuid().ToString().Substring(0, 12);
				
				if (item.CardPrefabId == "cp_coop_portal" || item.CardPrefabId == "cp_unstable_coop_portal")
				{
					foreach (CoopPortal card in WorldManager.instance.GetCards<CoopPortal>())
					{
						if (card.ConnectedPortalUId == newItemId && card._ConnectedFriend == this._ConnectedFriend)
						{
							card.ConnectedPortalUId = item.UniqueId;
							card.ConnectedFriend = SteamUser.GetSteamID();
							foreach (var extraData in item.ExtraCardData)
							{
								switch (extraData.AttributeId)
								{
								case "_connectedFriend":
									extraData.StringValue = SteamUser.GetSteamID().ToString();
									break;
								case "ConnectedPortalUId":
									extraData.StringValue = card.UniqueId;
									break;
								default:
									break;
								}
							}
						}
					}
				}
					
				foreach (SavedCard item2 in cardsToSpawn)
				{
					foreach (ExtraCardData extraData in item2.ExtraCardData)
					{
						if (extraData.StringValue == newItemId)
						{
							extraData.StringValue = item.UniqueId;
						}
					}
					if (item2.ParentUniqueId == newItemId)
					{
						item2.ParentUniqueId = item.UniqueId;
					}
					if (item2.EquipmentHolderUniqueId == newItemId)
					{
						item2.EquipmentHolderUniqueId = item.UniqueId;
					}
				}
				
				if (item.ParentUniqueId == ConnectedPortalUId
					|| item.ParentUniqueId == UniqueId)
				{
					item.ParentUniqueId = "";
				}

			}

			bool hostileCreated = false;

			foreach (SavedCard item2 in cardsToSpawn)
			{
				if (CoopPortalsPlugin.VsMode.Value)
				{
					switch (item2.CardPrefabId)
					{
						case Cards.archer:
							item2.CardPrefabId = "cp_evil_archer";
							break;
						case Cards.kid:
							item2.CardPrefabId = "cp_evil_baby";
							break;
						case Cards.builder:
							item2.CardPrefabId = "cp_evil_builder";
							break;
						case Cards.cat:
							item2.CardPrefabId = "cp_evil_cat";
							break;
						case Cards.dog:
							item2.CardPrefabId = "cp_evil_dog";
							break;
						case Cards.explorer:
							item2.CardPrefabId = "cp_evil_explorer";
							break;
						case Cards.fisher:
							item2.CardPrefabId = "cp_evil_fisher";
							break;
						case Cards.friendly_pirate:
							item2.CardPrefabId = "cp_evil_friendly_pirate";
							break;
						case Cards.jester:
							item2.CardPrefabId = "cp_evil_jester";
							break;
						case Cards.kitten:
							item2.CardPrefabId = "cp_evil_kitten";
							break;
						case Cards.lumberjack:
							item2.CardPrefabId = "cp_evil_lumberjack";
							break;
						case Cards.mage:
							item2.CardPrefabId = "cp_evil_mage";
							break;
						case Cards.militia:
							item2.CardPrefabId = "cp_evil_militia";
							break;
						case Cards.miner:
							item2.CardPrefabId = "cp_evil_miner";
							break;
						case Cards.ninja:
							item2.CardPrefabId = "cp_evil_ninja";
							break;
						case Cards.old_cat:
							item2.CardPrefabId = "cp_evil_old_cat";
							break;
						case Cards.old_dog:
							item2.CardPrefabId = "cp_evil_old_dog";
							break;
						case Cards.old_villager:
							item2.CardPrefabId = "cp_evil_old_villager";
							break;
						case Cards.puppy:
							item2.CardPrefabId = "cp_evil_puppy";
							break;
						case Cards.swordsman:
							item2.CardPrefabId = "cp_evil_swordsman";
							break;
						case Cards.trained_monkey:
							item2.CardPrefabId = "cp_evil_trained_monkey";
							break;
						case Cards.villager:
							item2.CardPrefabId = "cp_evil_villager";
							break;
						case Cards.warrior:
							item2.CardPrefabId = "cp_evil_warrior";
							break;
						case Cards.wizard:
							item2.CardPrefabId = "cp_evil_wizard";
							break;
						case Cards.teenage_villager:
							item2.CardPrefabId = "cp_evil_young_villager";
							break;
					}
				}
				CardData cardData = WorldManager.instance.CreateCard(base.transform.position, item2.CardPrefabId, item2.FaceUp, checkAddToStack: false, playSound: false);
				hostileCreated = hostileCreated || cardData is Enemy;
				CoopPortalsPlugin.Log("Spawned from the portal " + this.UniqueId + " a card: " + cardData.Id);
				// cardData.MyGameCard.SendIt();
				if (cardData == null)
				{
					continue;
				}
				cardData.UniqueId = item2.UniqueId;
				cardData.ParentUniqueId = item2.ParentUniqueId;
				cardData.EquipmentHolderUniqueId = item2.EquipmentHolderUniqueId;
				cardData.SetExtraCardData(item2.ExtraCardData);
				
				if (item2.IsFoil)
				{
					cardData.SetFoil();
				}

				if (item2.StatusEffects != null && item2.StatusEffects.Count > 0)
				{
					List<StatusEffect> list = item2.StatusEffects.Select((SavedStatusEffect x) => StatusEffect.FromSavedStatusEffect(x)).ToList();
					list.RemoveAll((StatusEffect x) => x == null);
					foreach (StatusEffect item3 in list)
					{
						item3.ParentCard = cardData;
					}
					cardData.StatusEffects = list;
				}
				else
				{
					cardData.StatusEffects = new List<StatusEffect>();
				}

				if (item2.TimerRunning)
				{
					TimerAction delegateForActionId = cardData.GetDelegateForActionId(item2.TimerActionId);
					if (delegateForActionId != null)
					{
						cardData.MyGameCard.StartTimer(item2.TargetTimerTime, delegateForActionId, item2.Status, item2.TimerActionId);
						cardData.MyGameCard.CurrentTimerTime = item2.CurrentTimerTime;
						cardData.MyGameCard.TimerBlueprintId = item2.TimerBlueprintId;
						cardData.MyGameCard.TimerSubprintIndex = item2.SubprintIndex;
					}
				}
				if (cardData is CoopPortal cp)
				{
					cp.ConnectionPing();
				}

			}

			foreach (SavedCard item in cardsToSpawn)
			{
				if (!string.IsNullOrEmpty(item.ParentUniqueId))
				{
					GameCard parent = WorldManager.instance.GetCardWithUniqueId(item.ParentUniqueId);
					GameCard child = WorldManager.instance.GetCardWithUniqueId(item.UniqueId);
					if (parent != null && child != null)
					{
						child.SetParent(parent);
					}
				}
			}
			
			foreach (SavedCard item in cardsToSpawn)
			{
				if (!string.IsNullOrEmpty(item.EquipmentHolderUniqueId))
				{
					GameCard parent = WorldManager.instance.GetCardWithUniqueId(item.EquipmentHolderUniqueId);
					GameCard child = WorldManager.instance.GetCardWithUniqueId(item.UniqueId);
					if (parent != null)
					{
						parent.EquipmentChildren.Add(child);
						child.EquipmentHolder = parent;
					}
				}
			}
			AudioManager.me.PlaySound2D(CoopPortalsPlugin.MyAudioClips["portalingGetting"], UnityEngine.Random.Range(0.8f, 1.2f), Math.Min((float)cardsToSpawn.Count, 3f) * 0.5f);
			
			if (hostileCreated)
			{
				AudioManager.me.PlaySound2D(CoopPortalsPlugin.MyAudioClips["portalingGetting"], 0.6f, 0.5f);
				AudioManager.me.PlaySound2D(CoopPortalsPlugin.MyAudioClips["portalingGetting"], 1.4f, 0.5f);
			}
			if (CoopPortalsPlugin.VsMode.Value)
			{
				WorldManager.instance.GetCardWithUniqueId(cardsToSpawn[0].UniqueId).GetRootCard().SendIt();
			}
			else
			{
				WorldManager.instance.StackSend(WorldManager.instance.GetCardWithUniqueId(cardsToSpawn[0].UniqueId).GetRootCard(), MyGameCard);
			}
			SendToFriend(PortalPacket.MessageType.PortalingCardsSuccess);
		}
		catch
		{
			SendToFriend(cardsToSpawn, PortalPacket.MessageType.PortalingCardsFail, --age);
		}
	}

	public virtual void SendToFriend(PortalPacket.MessageType type, string message = "")
	{
		SendToFriend(new List<SavedCard>(), type, 10, message);
	}

	public virtual void SendToFriend(List<GameCard> cards, PortalPacket.MessageType type = PortalPacket.MessageType.PortalingCardsStart, short age = 10, string message = "")
	{
		List<SavedCard> savedCards = new List<SavedCard>();
		QuestManager.instance.SpecialActionComplete("cp_teleport_to_friend");
		foreach (GameCard card in cards)
		{
			SavedCard item = card.ToSavedCard();
			if (card.CardData is Enemy) QuestManager.instance.SpecialActionComplete("cp_teleport_hostile_to_friend");
			savedCards.Add(item);
		}
		SendToFriend(savedCards, type, age, message);
	}

	public virtual void SendToFriend(List<SavedCard> cards, PortalPacket.MessageType type = PortalPacket.MessageType.PortalingCardsStart, short age = 10, string message = "")
	{
		//CoopPortalsPlugin.Log(UniqueId+": "+type.ToString()+" to "+ConnectedPortalUId);
		if (!HasConnectedFriend || age <= 0 || (type == PortalPacket.MessageType.PortalingCardsStart && !cards.Any()))
		{
			return;
		}
		byte[]? array = MakeAPack(cards, type, age, message)?.ToBytes();
		if (array != null)
		{
			CoopPortalsPlugin.SendPortalPacket(ConnectedFriend, array, (uint)array.Length, EP2PSend.k_EP2PSendReliable);
		}
	}
	public virtual PortalPacket? MakeAPack(List<SavedCard> cards, PortalPacket.MessageType type = PortalPacket.MessageType.PortalingCardsStart, short age = 10, string message = "")
	{
		PortalPacket portalPacket = new PortalPacket(type, UniqueId, ConnectedPortalUId == null ? "" : ConnectedPortalUId, age);
		portalPacket.Message = message;
		if (cards.Any())
		{
			portalPacket.Cards.AddRange(cards);
		}
		return portalPacket;
	}
	public virtual void ConnectionPing()
	{
		//CoopPortalsPlugin.Log("ConnectionPing");
		if (HasConnectedFriend)
		{
			SendToFriend(PortalPacket.MessageType.Ping);
			//CoopPortalsPlugin.Log("Ping sent");
			FailedPingsCount++;
			int neededPings = ConnectionActive ? 5 : 2;
			if (FailedPingsCount > neededPings)
			{
				//CoopPortalsPlugin.Log("FailedPingsCount > "+neededPings);
				Deactivate();
			}
		}
		else
		{
			CoopPortalsPlugin.Log("Handshake called with no friend");
			Deactivate();
		}
		UpdateText();
	}

	public virtual void UpdateText()
	{
		//CoopPortalsPlugin.Log("updating text");
		if (HasConnectedFriend)
		{
			descriptionOverride = SokLoc.Translate("cp_coop_portal_linked_description", LocParam.Create("ConnectedFriendName", ConnectedFriendName));
			nameOverride = SokLoc.Translate("cp_coop_portal_linked_name", LocParam.Create("ConnectedFriendName", ConnectedFriendName));
		}
	}

	public virtual IEnumerator MessageCutscene(string message)
	{
		//CoopPortalsPlugin.Log("MessageCutscene");
		GameCanvas.instance.SetScreen<CutsceneScreen>();
		WorldManager.instance.CutsceneTitle = message;
		WorldManager.instance.CutsceneText = "";
		try {
			GameCamera.instance.TargetPositionOverride = transform.position;
		}
		catch(Exception ex)
		{
			CoopPortalsPlugin.Log("message cutscene error:" + ex.Message);
		}
		AudioManager.me.PlaySound2D(CoopPortalsPlugin.MyAudioClips["portalOpening"], UnityEngine.Random.Range(0.8f, 1.2f), 1f);
		
		yield return Cutscenes.WaitForContinueClicked("okay");
		GameCamera.instance.TargetPositionOverride = null;
		GameCanvas.instance.SetScreen<GameScreen>();
		WorldManager.instance.currentAnimation = null;
		
	}

    public override void StoppedDragging()
    {
		if (ConnectionActive && !CoopPortalsPlugin.VsMode.Value)
		{
			
			var overlaping = MyGameCard.GetOverlappingCards();
			List<GameCard> sending = new List<GameCard>();
			foreach (GameCard card in overlaping)
			{
				GameCard root = card.GetRootCard();
				if (!sending.Any<GameCard>(c => c == root))
				{
					while (root != null && !(root.CardData is CoopPortal portal && portal.ConnectionActive))
					{
						sending.Add(root);
						if (root.EquipmentChildren.Any())
						{
							sending.AddRange(root.EquipmentChildren);
						}
						root = root.Child;
					}
				}
			}
			if (sending.Any())
			{
				PortalCards(sending);
			}
		}

        base.StoppedDragging();
    }
}
