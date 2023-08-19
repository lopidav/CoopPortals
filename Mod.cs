using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Linq;

namespace CoopPortalsNS;

public class CoopPortalsPlugin : Mod
{
	public const int NetworkReservedChannel = 998915;
	public const int QuestReservedGroupId = 2498646;
	public static ModLogger? L;
	public static ConfigEntry<bool> AllowDimentionalSlits;


	//public static float voidQuestCountdown = 0;

	protected Callback<P2PSessionRequest_t>? _p2PSessionRequestCallback;
	protected static List<CSteamID> SessionsList = new List<CSteamID>();
	public static Dictionary<string, AudioClip> MyAudioClips = new Dictionary<string, AudioClip>();
	void LoadAudio(string path, string key)
	{
		Log("Attempted to load sound: "+path+ " "+key);
		this.StartCoroutine(
			ResourceHelper.LoadAudioClipFromPath(
				path, AudioType.MPEG, 
				(AudioClip ac) => { MyAudioClips.Add(key, ac);Log("loaded sound: "+key);},
				()=>Log("Failed to load the sound")));
	}
	private void Awake()
	{
		L = ((CoopPortalsPlugin)this).Logger;


		if (SteamManager.Initialized)
		{
			string personaName = SteamFriends.GetPersonaName();
			_p2PSessionRequestCallback = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
			//Log(personaName);
		}
		else
		{
			throw new Exception("Steam manager not initialized");
		}
		
		try
		{
			// Harmony = new Harmony("CoopPortalsPlugin");
			Harmony.PatchAll(typeof(CoopPortalsPlugin));
		}
		catch (Exception ex3)
		{
			Log("Patching failed: " + ex3.Message);
		}
		AllowDimentionalSlits = Config.GetEntry<bool>("Allow Dimentional Slits", true, new ConfigUI {
			Tooltip = "Allow people connecting to you even when you haven't opened a portal to them",
		});

	}

    public override void Ready()
    {
		//WorldManager.instance.GameDataLoader.AddCardToSetCardBag(SetCardBagType.BasicBuildingIdea, "blueprint_cp_coop_portal", 1);
		
		LoadAudio(System.IO.Path.Combine(Path, "Sounds","portaling14.mp3"), "portalingAway");
		LoadAudio(System.IO.Path.Combine(Path, "Sounds","portaling17.mp3"), "portalingGetting");
		LoadAudio(System.IO.Path.Combine(Path, "Sounds","portaling4.mp3"), "portalOpening");
		LoadAudio(System.IO.Path.Combine(Path, "Sounds","VoidPortalPickup2.mp3"), "voidPickup");
        base.Ready();
    }

    

	public static CoopPortal? GetLinkedPortal(CSteamID friendId, string ToPortalUId = "", string FromPortalUId = "")
	{
		CoopPortal? connectedUnstable = null;

		if(ToPortalUId != "")
		{
			var card = WorldManager.instance.GetCardWithUniqueId(ToPortalUId);
			if (card != null && card.MyBoard.IsCurrent && card.CardData is CoopPortal p)
			{
				if (p is not UnstableCoopPortal)
				{
					return p;
				}
				else if (AllowDimentionalSlits.Value)
				{
					connectedUnstable = p;
				}
			}
		}

		CoopPortal? portalToLinkTo = null;
		CoopPortal? portalToLinkTo2 = null;
		CoopPortal? portalToLinkTo3 = null;
		foreach (GameCard allCard in WorldManager.instance.AllCards)
		{
			if (allCard.MyBoard.IsCurrent && allCard.CardData is CoopPortal p && p.ConnectedFriend == friendId)
			{
				if (p.ConnectedPortalUId == FromPortalUId && p is not UnstableCoopPortal)
				{
					return p;
				}
				else if (connectedUnstable == null && p.ConnectedPortalUId == FromPortalUId && p is UnstableCoopPortal && AllowDimentionalSlits.Value)
				{
					connectedUnstable = p;
				}
				else if (p is not UnstableCoopPortal && (p.ConnectedPortalUId == "" || p.ConnectedPortalUId == p.UniqueId))
				{
					portalToLinkTo = p;
				}
				else if (p is not UnstableCoopPortal && p.LinkedToUnstable)
				{
					portalToLinkTo2 = p;
				}
				else if (p is UnstableCoopPortal && AllowDimentionalSlits.Value && (p.ConnectedPortalUId == "" || p.ConnectedPortalUId == p.UniqueId || !p.ConnectionActive))
				{
					portalToLinkTo3 = p;
				}
			}
		}

		return portalToLinkTo ?? portalToLinkTo2 ?? connectedUnstable ?? portalToLinkTo3;
	}

	public void OnP2PSessionRequest(P2PSessionRequest_t request)
	{
		//Log("OnP2PSessionRequest");
		CSteamID steamIDRemote = request.m_steamIDRemote;
		if (WorldManager.instance.CurrentGameState != WorldManager.GameState.InMenu && SteamFriends.HasFriend(steamIDRemote, EFriendFlags.k_EFriendFlagImmediate) && (WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing || WorldManager.instance.CurrentGameState == WorldManager.GameState.Paused))
		{
			SteamNetworking.AcceptP2PSessionWithUser(steamIDRemote);
			/*SessionsList.Add(steamIDRemote);
			CoopPortal coopPortal = GetLinkedPortal(steamIDRemote);
			if (coopPortal == null)
			{
				Log("Unstable portal created");
				Vector3 randomSpawnPosition = WorldManager.instance.GetRandomSpawnPosition();
				CoopPortal coopPortal2 = WorldManager.instance.CreateCard(randomSpawnPosition, "cp_unstable_coop_portal", faceUp: true, checkAddToStack: false) as CoopPortal;
				coopPortal2.ConnectedFriend = steamIDRemote;
				coopPortal = coopPortal2;
			}
			coopPortal.Activate();*/
			//coopPortal.ReceivePacket(new PortalPacket(PortalPacket.MessageType.ConnectionInitiation));
		}
	}

	public static void Log(string s)
	{
		L?.Log(s);
	}

	[HarmonyPatch(typeof(GameScreen), "UpdateIdeasLog")]
	[HarmonyPrefix]
	public static void GameScreen_UpdateIdeasLog_Prefix()
	{
		if (SaveManager.instance.CurrentSave != null && !SaveManager.instance.CurrentSave.FoundCardIds.Contains("blueprint_cp_coop_portal"))
		{
			SaveManager.instance.CurrentSave.NewKnowledgeIds.Add("blueprint_cp_coop_portal");
			SaveManager.instance.CurrentSave.NewCardopediaIds.Add("blueprint_cp_coop_portal");
			SaveManager.instance.CurrentSave.FoundCardIds.Add("blueprint_cp_coop_portal");
		}
	}
	[HarmonyPatch(typeof(WorldManager), "Update")]
	[HarmonyPrefix]
	private static void WorldManager_Update_Prefix(ref WorldManager __instance)
	{
		uint pcubMsgSize;
		while (SteamNetworking.IsP2PPacketAvailable(out pcubMsgSize, NetworkReservedChannel))
		{
			byte[] array = new byte[pcubMsgSize];
			if (SteamNetworking.ReadP2PPacket(array, pcubMsgSize, out var _, out var psteamIDRemote, NetworkReservedChannel))
			{
				if (!SessionsList.Contains(psteamIDRemote))
				{
					SessionsList.Add(psteamIDRemote);
				}
				try{
					var packet = PortalPacket.FromBytes(array);
					if (WorldManager.instance.CurrentGameState != WorldManager.GameState.InMenu
						//&& SteamFriends.HasFriend(psteamIDRemote, EFriendFlags.k_EFriendFlagImmediate)
						&& (WorldManager.instance.CurrentGameState == WorldManager.GameState.Playing || WorldManager.instance.CurrentGameState == WorldManager.GameState.Paused))
					{
						CoopPortal? coopPortal = GetLinkedPortal(psteamIDRemote, packet.ToPortalUId, packet.FromPortalUId);
						if (coopPortal == null && 
							(packet.Type == PortalPacket.MessageType.Ping
								|| packet.Type == PortalPacket.MessageType.PortalingCardsStart 
								|| packet.Type == PortalPacket.MessageType.PortalingCardsFail ))
						{
							if (AllowDimentionalSlits.Value)
							{
								Log("Unstable portal created");
								Vector3 randomSpawnPosition = WorldManager.instance.GetRandomSpawnPosition();
								CoopPortal? coopPortal2 = WorldManager.instance.CreateCard(randomSpawnPosition, "cp_unstable_coop_portal", faceUp: true, checkAddToStack: false) as CoopPortal;
								if (coopPortal2 != null) coopPortal2.ConnectedFriend = psteamIDRemote;
								coopPortal = coopPortal2;
							}
						}
						if (coopPortal != null)
						{
							//Log("got a massage for "+packet.ToPortalUId+" from "+packet.FromPortalUId+" directing it to "+coopPortal.UniqueId);
							coopPortal.ReceivePacket(packet);
							
							//if (coopPortal is UnstableCoopPortal && packet.IsFromUnstablePortal)
							//{
							//	coopPortal.Deactivate();
							//}
						}
					}
				}
				catch (Exception ex)
				{
					Log(ex.Message);
				}
			}
		}
	}

	
	[HarmonyPatch(typeof(WorldManager), "OnApplicationQuit")]
	[HarmonyPrefix]
	static private void WorldManager_OnApplicationQuit_Prefix()
	{
		CloseAllSessions();
	}

	static public void CloseAllSessions()
	{
		foreach (var friend in SessionsList)
		{
			SteamNetworking.CloseP2PSessionWithUser(friend);
		}
	}

	[HarmonyPatch(typeof(GameCard), "SetColors")]
	[HarmonyPostfix]
	static private void GameCard_SetColors_Postfix(ref GameCard __instance)
	{
		if (__instance.CardData is CoopPortal || __instance.CardData is VoidPortal)
		{
			Color color;
			Color value;
			Color color2;
			if (__instance.CardData is CoopPortal portal)
			{
				if (portal.ConnectionActive)
				{
					color = new Color(0f, 0.4f, 0.4f, 1f);
					value = new Color(0f, 0.55f, 0.55f, 1f);
					color2 = Color.white;
				}
				else
				{
					color = new Color(0.7f, 0.75f, 0.75f, 1f);
					value = new Color(0.8f, 0.85f, 0.85f, 1f);
					color2 = new Color(0.4f, 0.4f, 0.4f, 1f);
				}
			}
			else if (__instance.CardData is VoidPortal voidPortal)
			{
				color = new Color(0f, 00f, 00f, 1f);
				value = new Color(0.1f, 0.1f, 0.1f, 1f);
				color2 = new Color(1f, 1f, 1f, 1f);
			}
			else
			{
				color = new Color(0f, 00f, 00f, 1f);
				value = new Color(0.1f, 0.1f, 0.1f, 1f);
				color2 = new Color(1f, 1f, 1f, 1f);
			}

			MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
			__instance.CardRenderer.GetPropertyBlock(propBlock, 2);
			propBlock.SetColor("_Color", color);
			propBlock.SetColor("_Color2", value);
			propBlock.SetColor("_IconColor", color2);
			__instance.CardRenderer.SetPropertyBlock(propBlock, 2);
			__instance.SpecialText.color = color;
			__instance.SpecialIcon.color = color2;
			__instance.IconRenderer.color = color2;
			__instance.CoinText.color = color;
			__instance.CoinIcon.color = color2;
			__instance.EquipmentButton.Color = color;
			__instance.CardNameText.color = color2;
		}
	}

	[HarmonyPatch(typeof(GameCard), "Update")]
	[HarmonyPostfix]
	static private void GameCard_Update_Postfix(ref GameCard __instance)
	{
		if (__instance.CardData is CoopPortal portal && portal.ConnectionActive)
		{
			ParticleSystem.EmissionModule emission = __instance.FoilParticles.emission;
			emission.enabled = true;
		}
	}

	[HarmonyPatch(typeof(CardData), "CanHaveCardOnTop")]
	[HarmonyPostfix]
	static private void CardData_CanHaveCardOnTop_Postfix(CardData otherCard, CardData __instance, ref bool __result)
	{
		if (otherCard is CoopPortal portal && __instance is not CoopPortal)
		{
			__result = false;
		}
	}
	[HarmonyPatch(typeof(CardData), "UpdateCard")]
	[HarmonyPostfix]
	static private void CardData_UpdateCard_Postfix(CardData __instance)
	{
		if (WorldManager.instance.DraggingCard
			&& WorldManager.instance.DraggingCard != __instance.MyGameCard
			&& !__instance.MyGameCard.HighlightActive
			&& !__instance.MyGameCard.HasChild
			&& !__instance.MyGameCard.IsChildOf(WorldManager.instance.DraggingCard))
		{
			if (WorldManager.instance.DraggingCard.CardData is CoopPortal portal)
			{
				__instance.MyGameCard.HighlightActive = portal.ConnectionActive;
			}
			if (WorldManager.instance.DraggingCard.CardData is VoidPortal)
			{
				__instance.MyGameCard.HighlightActive = false;
			}
		}
	}
	/*
	[HarmonyPatch(typeof(CardData), "CanHaveCardsWhileHasStatus")]
	[HarmonyPostfix]
	static private void CardData_CanHaveCardsWhileHasStatus_Postfix(ref bool __result)
	{
		if (otherCard is CoopPortal portal)
		{
			__result = __result || portal.ConnectionActive;
		}
	}
	*/
	[HarmonyPatch(typeof(WorldManager), "CheckAllVillagersDead")]
	[HarmonyPostfix]
	static private void WorldManager_CheckAllVillagersDead_Postfix(ref bool __result, WorldManager __instance)
	{
		if (__result)
		{
			__result = __instance.GetCardCount<VoidPortal>() <= 0;
		}
	}

	

	[HarmonyPatch(typeof(QuestManager), "GetAllQuests")]
	[HarmonyPostfix]
	public static void QuestManager_GetAllQuests_Postfix(ref List<Quest> __result)
	{
		__result.Add(new Quest("cp_build_coop_portal")
			{
				OnCardCreate = (CardData card) => card.Id == "cp_coop_portal",
				QuestGroup = (QuestGroup)QuestReservedGroupId,
				DefaultVisible = true
			});
		__result.Add(new Quest("cp_teleport_to_friend")
			{
				OnSpecialAction = (string action) => action == "cp_teleport_to_friend",
				QuestGroup = (QuestGroup)QuestReservedGroupId
			});
		__result.Add(new Quest("cp_teleport_hostile_to_friend")
			{
				OnSpecialAction = (string action) => action == "cp_teleport_hostile_to_friend",
				QuestGroup = (QuestGroup)QuestReservedGroupId,
				PossibleInPeacefulMode = false
			});
		__result.Add(new Quest("cp_void_portal_invisible")
			{
				OnCardCreate = (CardData card) => card.Id == "cp_void_portal",
				QuestGroup = (QuestGroup)QuestReservedGroupId
			});
		__result.Add(new Quest("cp_void_portal_only_left_invisible")
			{
				OnSpecialAction = (string action) => action == "cp_void_portal_only_left",
				QuestGroup = (QuestGroup)QuestReservedGroupId,
				ShowCompleteAnimation = false
			});
		__result.Add(new Quest("cp_void_portal_rescued_by_friend_invisible")
			{
				OnCardCreate =  (CardData card) => card is CoopPortal portal && portal.HasConnectedFriend && portal.ConnectedFriend != SteamUser.GetSteamID() && portal._ConnectedFriend != "0" && (WorldManager.instance.GetCardCount() - WorldManager.instance.GetCardCount<VoidPortal>() == 1),
				QuestGroup = (QuestGroup)QuestReservedGroupId
			});
		__result.Add(new Quest("cp_void_portal_villager_invisible")
			{
				OnCardCreate =  (CardData card) => card is Villager && QuestManager.instance.QuestIsComplete("cp_void_portal_rescued_by_friend_invisible"),
				QuestGroup = (QuestGroup)QuestReservedGroupId
			});
		__result.Add(new Quest("cp_destroy_void_portal_invisible")
			{
				OnSpecialAction = (string action) => action == "cp_destroy_void_portal",
				QuestGroup = (QuestGroup)QuestReservedGroupId
			});
	}

	[HarmonyPatch(typeof(GameScreen), "GetAchievementGroupName")]
	[HarmonyPrefix]
	public static bool GameScreen_GetAchievementGroupName_Prefix(QuestGroup group, ref string __result)
	{
		if (group == (QuestGroup)QuestReservedGroupId)
		{
			__result = SokLoc.Translate("questgroup_cp");
			return false;
		}
		return true;
	}

	[HarmonyPatch(typeof(QuestManager), "QuestIsVisible")]
	[HarmonyPrefix]
	public static bool QuestManager_QuestIsVisible_Prefix(Quest quest, ref bool __result)
	{
		if (!quest.Id.EndsWith("_invisible"))
		{
			return true;
		}

		__result = QuestManager.instance.QuestIsComplete(quest);
		
		if (!__result && quest.Id == "cp_void_portal_villager_invisible" && QuestManager.instance.QuestIsComplete("cp_void_portal_rescued_by_friend_invisible"))
		{
			__result = true;
		}

		if (!__result && quest.Id == "cp_destroy_void_portal_invisible" && QuestManager.instance.QuestIsComplete("cp_void_portal_villager_invisible"))
		{
			__result = true;
		}

		return false;
	}
	
	[HarmonyPatch(typeof(GameScreen), "CreateQuestElements")]
	[HarmonyPrefix]
	public static void GameScreen_CreateQuestElements_Prefix(RectTransform parent, ref List<Quest> quests)
	{
		if (!quests.Any((Quest x) => x.QuestGroup == (QuestGroup)QuestReservedGroupId)
			&& (WorldManager.instance.CurrentBoard == null
				|| WorldManager.instance.CurrentBoard?.Id == "main"
				|| WorldManager.instance.CurrentBoard?.Id == "island"))
		{
			quests.AddRange( QuestManager.instance.AllQuests.Where((Quest x) => x.QuestGroup == (QuestGroup)QuestReservedGroupId) );
		}
		quests.RemoveAll(q=>q.Id.EndsWith("_invisible")
				&& !QuestManager.instance.QuestIsComplete(q)
				&& !(q.Id == "cp_void_portal_villager_invisible"
					&& QuestManager.instance.QuestIsComplete("cp_void_portal_rescued_by_friend_invisible")
					&& WorldManager.instance.GetCardCount<VoidPortal>() >= 1)
				&& !(q.Id == "cp_destroy_void_portal_invisible"
					&& QuestManager.instance.QuestIsComplete("cp_void_portal_villager_invisible")));
	}

	[HarmonyPatch(typeof(WorldManager), "QuestCompleted")]
	[HarmonyPrefix]
	public static bool WorldManager_QuestCompleted_Prefix(Quest quest, WorldManager __instance)
	{
		// cut the sound
		if (quest.Id != "cp_void_portal_invisible" && quest.Id != "cp_void_portal_only_left_invisible")
		{
			return true;
		}

		Debug.Log("Completed quest " + quest.Id);
		if (GameScreen.instance != null && quest.QuestLocation == __instance.CurrentBoard.Location)
		{
			GameScreen.instance.AddNotification(SokLoc.Translate("label_quest_completed"), quest.Description, delegate
			{
				GameScreen.instance.ScrollToQuest(quest);
			});
		}
		// AudioManager.me.PlaySound2D(AudioManager.me.QuestComplete, 1f, 0.1f);
		BoosterpackData boosterpackData = QuestManager.instance.JustUnlockedPack();
		if (boosterpackData != null)
		{
			bool flag = boosterpackData.BoosterLocation == quest.QuestLocation;
			if (boosterpackData.BoosterLocation != __instance.CurrentBoard.Location)
			{
				flag = false;
			}
			if (__instance.InAnimation || TransitionScreen.InTransition)
			{
				flag = false;
			}
			if (flag)
			{
				__instance.QueueCutscene(Cutscenes.JustUnlockedPack(boosterpackData));
			}
		}
		return false;
	}

	
	[HarmonyPatch(typeof(AudioManager), "LateUpdate")]
	[HarmonyPostfix]
	public static void AudioManager_LateUpdate_Postfix(AudioManager __instance)
	{
		if (WorldManager.instance.GetCardCount<VoidPortal>() >= 1)
		{
			__instance.SfxGroup.audioMixer.SetFloat("MusicVolume", -80f);
			//__instance.SfxGroup.audioMixer.SetFloat("SfxVolume", -80f);
		}
	}

	[HarmonyPatch(typeof(WorldManager), "StackSend")]
	[HarmonyPrefix]
	public static bool WorldManager_StackSend_Prefix(GameCard myCard, GameCard initialParent, bool sendToChest, WorldManager __instance)
	{
		if (myCard.CardData is not CoopPortal)
		{
			return true;
		}

		if (__instance.TrySendToMagnet(myCard) || !sendToChest || __instance.TrySendToChest(myCard) || myCard.BounceTarget != null)
		{
			return false;
		}
		GameCard? gameCard = null;
		float num = float.MaxValue;
		Vector3 value = Vector3.zero;
		foreach (GameCard allCard in __instance.AllCards)
		{
			if (!allCard.MyBoard.IsCurrent || allCard == myCard)
			{
				continue;
			}
			GameCard cardWithStatusInStack = allCard.GetCardWithStatusInStack();
			//make two coop portals not jump one on the other
			if ((!(cardWithStatusInStack != null) || cardWithStatusInStack.CardData.CanHaveCardsWhileHasStatus()) && !(allCard.GetCardInCombatInStack() != null) && !allCard.BeingDragged && !allCard.IsChildOf(myCard) && !allCard.IsParentOf(myCard) && !(initialParent != null && (allCard.IsChildOf(initialParent) || allCard == initialParent)) && allCard.CardData is not CoopPortal && !allCard.HasChild && allCard.CardData.CanHaveCardOnTop(myCard.CardData) && allCard.CardData.Id == myCard.CardData.Id)
			{
				Vector3 vector = allCard.transform.position - myCard.transform.position;
				vector.y = 0f;
				if (vector.magnitude <= 2f && vector.magnitude <= num)
				{
					gameCard = allCard;
					num = vector.magnitude;
					value = new Vector3(vector.x * 4f, 7f, vector.z * 4f);
				}
			}
		}
		if (gameCard != null)
		{
			myCard.BounceTarget = gameCard;
			myCard.Velocity = value;
		}
		else
		{
			myCard.SendIt();
		}

		return false;
	}

	public static void SendPortalPacket(CSteamID steamIDRemote, byte[] pubData, uint cubData, EP2PSend eP2PSendType = EP2PSend.k_EP2PSendReliable)
	{
		SteamNetworking.SendP2PPacket(steamIDRemote, pubData, cubData, eP2PSendType,NetworkReservedChannel);
	}
}
