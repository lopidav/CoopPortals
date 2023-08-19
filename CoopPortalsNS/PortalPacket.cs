using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Steamworks;

namespace CoopPortalsNS;

[Serializable]
public class PortalPacket
{
	public enum MessageType
	{
		ConnectionInitiation,
		Ping,
		Pong,
		PortalingCardsStart,
		PortalingCardsSuccess,
		PortalingCardsFail
	}
	public short Age = 10;
	public List<SavedCard> Cards = new List<SavedCard>();

	public MessageType Type;
	public string FromFriendId = SteamUser.GetSteamID().ToString();
	public string FromPortalUId = "";
	public string ToPortalUId = "";
	public bool IsFromUnstablePortal = false;

	public PortalPacket(MessageType _messageType, string _fromPortalUId = "", string _toPortalUId = "", short _age = 10)
	{
		Type = _messageType;
		FromPortalUId = _fromPortalUId;
		ToPortalUId = _toPortalUId;
		Age = _age;
		
	}

	public PortalPacket(MessageType _messageType, List<SavedCard> _cards, string _fromPortalUId = "", string _toPortalUId = "", short _age = 10)
	{
		Cards = _cards;
		Type = _messageType;
		FromPortalUId = _fromPortalUId;
		ToPortalUId = _toPortalUId;
		Age = _age;
	}

	public byte[] ToBytes()
	{
		BinaryFormatter binaryFormatter = new BinaryFormatter();
		using MemoryStream memoryStream = new MemoryStream();
		SurrogateSelector surrogateSelector = new SurrogateSelector();
		Vector3SerializationSurrogate surrogate = new Vector3SerializationSurrogate();
		surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), surrogate);
		binaryFormatter.SurrogateSelector = surrogateSelector;
		binaryFormatter.Serialize(memoryStream, this);
		return memoryStream.ToArray();
	}

	public static PortalPacket FromBytes(byte[] _bytes)
	{
		using MemoryStream memoryStream = new MemoryStream();
		BinaryFormatter binaryFormatter = new BinaryFormatter();
		SurrogateSelector surrogateSelector = new SurrogateSelector();
		Vector3SerializationSurrogate surrogate = new Vector3SerializationSurrogate();
		surrogateSelector.AddSurrogate(typeof(Vector3), new StreamingContext(StreamingContextStates.All), surrogate);
		binaryFormatter.SurrogateSelector = surrogateSelector;
		memoryStream.Write(_bytes, 0, _bytes.Length);
		memoryStream.Seek(0L, SeekOrigin.Begin);
		return (PortalPacket)binaryFormatter.Deserialize(memoryStream);
	}
}
