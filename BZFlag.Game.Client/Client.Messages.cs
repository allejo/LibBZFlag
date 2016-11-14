﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using BZFlag.Networking.Messages;
using BZFlag.Networking.Messages.BZFS;
using BZFlag.Networking.Messages.BZFS.UDP;
using BZFlag.Networking.Messages.BZFS.World;
using BZFlag.Networking.Messages.BZFS.BZDB;
using BZFlag.Networking.Messages.BZFS.Player;
using BZFlag.Networking.Messages.BZFS.Info;
using BZFlag.Networking.Messages.BZFS.Flags;
using BZFlag.Networking.Messages.BZFS.Shots;
using BZFlag.Networking.Messages.BZFS.Control;

namespace BZFlag.Game
{
    public partial class Client
    {
		public delegate void MessageHandler(NetworkMessage msg);
		public static Dictionary<int, MessageHandler> Handlers = new Dictionary<int, MessageHandler>();

		public class UnknownMessageEventArgs : EventArgs
		{
			public string CodeAbriv = string.Empty;
			public int CodeID = 0;
		}
		public event EventHandler<UnknownMessageEventArgs> ReceivedUnknownMessage = null;

		protected bool UDPRequestSent = false;
		protected bool UDPSendEnabled = false;
		private bool UDPOutOk = false;
		private bool UDPInOk = false;

		protected virtual void NetClient_HostMessageReceived(object sender, Networking.ClientConnection.HostMessageReceivedEventArgs e)
		{
			PreDispatchChecks(e.Message);

			if(Handlers.ContainsKey(e.Message.Code))
				Handlers[e.Message.Code](e.Message);
			else // if(e.Message as UnknownMessage != null)
			{
				if (ReceivedUnknownMessage != null)
				{
					UnknownMessageEventArgs args = new UnknownMessageEventArgs();
					args.CodeAbriv = e.Message.CodeAbreviation;
					args.CodeID = e.Message.Code;
					ReceivedUnknownMessage.Invoke(this, args);
				}
			}

			PostDispatchChecks();
		}
		
		protected void PreDispatchChecks(NetworkMessage msg)
		{
			if(!InitalSetVarsFinished && InitalSetVarsStarted && msg.Code != MsgSetVars.CodeValue)
			{
				InitalSetVarsFinished = true;
				BZDatabase.FinishLoading();
			}
		}

		private void PostDispatchChecks()
		{

		}

		protected virtual void SendTCPMessage(NetworkMessage msg)
		{
			NetClient.SendMessage(true, msg);
		}

		protected virtual void SendUDPMessage(NetworkMessage msg)
		{
			NetClient.SendMessage(!UDPSendEnabled, msg);
		}

		protected void SendEnter()
		{
			var enter = new MsgEnter();
			enter.Callsign = Params.Callsign;
			enter.Motto = Params.Motto;
			enter.Token = Params.Token;
			SendTCPMessage(enter);
		}

		protected virtual void RegisterMessageHandlers()
		{
			// basic connections
			Handlers.Add(new MsgAccept().Code, HandleAcceptMessage);
			Handlers.Add(new MsgReject().Code, HandleRejectMessage);

			Handlers.Add(new MsgGameTime().Code, HandleGameTime);
			Handlers.Add(new MsgUDPLinkRequest().Code, HandleUDPLinkRequest);
			Handlers.Add(new MsgUDPLinkEstablished().Code, HandleUDPLinkEstablished);
			Handlers.Add(new MsgLagPing().Code, HandleLagPing);

			// world data
			Handlers.Add(new MsgWantWHash().Code, HandleWorldHash);
			Handlers.Add(new MsgCacheURL().Code, HandleWorldCacheURL);
			Handlers.Add(new MsgGetWorld().Code, HandleGetWorld);

			// bzdb
			Handlers.Add(new MsgSetVars().Code, HandleSetVarsMessage);

			// teams
			Handlers.Add(new MsgTeamUpdate().Code, HandleTeamUpdate);

			// flags
			Handlers.Add(new MsgFlagUpdate().Code, HandleFlagUpdate);
			Handlers.Add(new MsgDropFlag().Code, HandleDropFlag);
			Handlers.Add(new MsgGrabFlag().Code, HandleGrabFlag);
			Handlers.Add(new MsgTransferFlag().Code, HandleTransferFlag);

			// players
			Handlers.Add(new MsgAddPlayer().Code, HandleAddPlayer);
			Handlers.Add(new MsgRemovePlayer().Code, HandleRemovePlayer);
			Handlers.Add(new MsgPlayerInfo().Code, HandlePlayerInfo);
			Handlers.Add(new MsgScore().Code, HandleScoreUpdate);
			Handlers.Add(new MsgAlive().Code, HandleAlive);
			Handlers.Add(new MsgKilled().Code, HandleKilled);
			Handlers.Add(new MsgPlayerUpdate().Code, HandlePlayerUpdate);
			Handlers.Add(new MsgPlayerUpdateSmall().Code, HandlePlayerUpdate);

			// chat
			Handlers.Add(new MsgMessage().Code, Chat.HandleChatMessage);
		}

		private void HandleAcceptMessage(NetworkMessage msg)
		{
			MsgAccept accept = msg as MsgAccept;

			PlayerID = accept.PlayerID;

			NetClientAccepted();
			UDPRequestSent = true;
			// start UDP Link
			NetClient.ConnectToUDP();
			NetClient.SendMessage(false, new MsgUDPLinkRequest(PlayerID));
		}

		private void HandleRejectMessage(NetworkMessage msg)
		{
			MsgReject reject = msg as MsgReject;
			NetClient.Shutdown();
			NetClientRejected(reject.ReasonCode, reject.ReasonMessage);
		}

		private  void HandleGameTime(NetworkMessage msg)
		{
			MsgGameTime gt = msg as MsgGameTime;
			Clock.AddTimeUpdate(gt.NetTime);
		}


		public event EventHandler UDPLinkEstablished = null;

		private void HandleUDPLinkRequest(NetworkMessage msg)
		{
			MsgUDPLinkRequest udp = msg as MsgUDPLinkRequest;

			if(udp.FromUDP)
			{
				if(UDPRequestSent)
				{
					UDPInOk = true;
					NetClient.SendMessage(false, new MsgUDPLinkEstablished());

					if(UDPOutOk)
					{
						if(UDPLinkEstablished != null)
							UDPLinkEstablished.Invoke(this, EventArgs.Empty);
						UDPSendEnabled = true;
					}
				}
			}
		}

		private void HandleUDPLinkEstablished(NetworkMessage msg)
		{
			MsgUDPLinkEstablished udp = msg as MsgUDPLinkEstablished;

			if(!udp.FromUDP)
			{
				if(UDPRequestSent)
				{
					UDPOutOk = true;
					NetClient.SendMessage(false, new MsgUDPLinkEstablished());

					if(UDPInOk)
					{
						if(UDPLinkEstablished != null)
							UDPLinkEstablished.Invoke(this, EventArgs.Empty);
						UDPSendEnabled = true;
					}
				}
			}
		}

		private void HandleLagPing(NetworkMessage msg)
		{
			MsgLagPing ping = msg as MsgLagPing;

			NetClient.SendMessage(ping.FromUDP, ping);
		}
	}
}
