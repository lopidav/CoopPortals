using System.Collections.Generic;
using System.Linq;
using Steamworks;

namespace CoopPortalsNS;

public class UnstableCoopPortal : CoopPortal
{
	public override bool CanBeDragged => false;

	public override void UpdateCard()
	{
		base.UpdateCard();
	}

	public override void Deactivate()
	{
		base.Deactivate();
		MyGameCard.DestroyCard();
	}
	
	public override PortalPacket? MakeAPack(List<SavedCard> cards, PortalPacket.MessageType type = PortalPacket.MessageType.PortalingCardsStart, short age = 10, string message = "")
	{
		if (type == PortalPacket.MessageType.Ping || type == PortalPacket.MessageType.PortalingCardsSuccess)
		{
			return null;
		}
		PortalPacket? portalPacket = base.MakeAPack(cards, type, age, message);
		if (portalPacket != null) portalPacket.IsFromUnstablePortal = true;
		return portalPacket;
	}
	public override void UpdateText()
	{
		if (HasConnectedFriend)
		{
			descriptionOverride = SokLoc.Translate("cp_unstable_coop_portal_linked_description", LocParam.Create("ConnectedFriendName", ConnectedFriendName));
		}
	}
}
