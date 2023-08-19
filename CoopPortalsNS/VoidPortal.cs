using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CoopPortalsNS;

public class VoidPortal : CardData
{
    //float voidQuestTimer = 0f;
    protected override void Awake()
    {
        PickupSound = CoopPortalsPlugin.MyAudioClips["voidPickup"];
        PickupSoundGroup = PickupSoundGroup.Custom;

        base.Awake();
        QuestManager.instance.UpdateCurrentQuests();
    }
    protected override bool CanHaveCard(CardData otherCard)
	{
		return false;
	}
    public override void UpdateCard()
	{
		if (!MyGameCard.IsDemoCard)
		{
            foreach (GameCard item in MyGameCard.GetOverlappingCards())
            {
                if (item.CardData is VoidPortal)
                {
                    MyGameCard.DestroyCard(spawnSmoke: true, playSound: false);
				    AudioManager.me.PlaySound2D(CoopPortalsPlugin.MyAudioClips["voidPickup"], 1f, 0.5f);
                    QuestManager.instance.SpecialActionComplete("cp_destroy_void_portal");
                }
                item.DestroyCard(spawnSmoke: true, playSound: false);
            }
            if (!QuestManager.instance.QuestIsComplete("cp_void_portal_only_left_invisible") && WorldManager.instance.GetCardCount() - WorldManager.instance.GetCardCount<VoidPortal>() == 0)
            {
                QuestManager.instance.SpecialActionComplete("cp_void_portal_only_left");
            }
		}
	}
    public override void StoppedDragging()
    {
        //CoopPortalsPlugin.voidQuestCountdown += QuestManager.instance.QuestIsComplete("cp_void_portal_only_left_invisible") ? 1 : 0;
    }
}