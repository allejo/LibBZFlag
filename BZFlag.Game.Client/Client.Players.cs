﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BZFlag.Game.Players;
using BZFlag.Networking.Messages;
using BZFlag.Networking.Messages.BZFS.Player;
using BZFlag.Networking.Messages.BZFS.Info;
using BZFlag.Data.Players;

namespace BZFlag.Game
{
	public partial class Client
	{
		public Dictionary<int, Player> PlayerList = new Dictionary<int, Player>();
		public Player LocalPlayer = null;

		public Player GetPlayerByID(int id)
		{
			if(!PlayerList.ContainsKey(id))
				return null;

			return PlayerList[id];
		}

		public class PlayerEventArgs : EventArgs
		{
			public Player PlayerRecord = null;

			public PlayerEventArgs(Player p)
			{
				PlayerRecord = p;
			}
		}

		public event EventHandler<PlayerEventArgs> PlayerAdded = null;
		public event EventHandler<PlayerEventArgs> PlayerRemoved = null;

		public event EventHandler<PlayerEventArgs> PlayerInfoUpdated = null;
		public event EventHandler<PlayerEventArgs> PlayerStateUpdated = null;

		public event EventHandler<PlayerEventArgs> SelfAdded = null;
		public event EventHandler<PlayerEventArgs> SelfRemoved = null;

		private void HandleAddPlayer(NetworkMessage msg)
		{
			MsgAddPlayer ap = msg as MsgAddPlayer;
			if(!PlayerList.ContainsKey(ap.PlayerID))
				PlayerList.Add(ap.PlayerID, new Player());

			Player player = PlayerList[ap.PlayerID];

			player.Callsign = ap.Callsign;
			player.Motto = ap.Motto;

			player.PlayerType = (PlayerTypes)ap.PlayerType;
			player.Team = ap.Team;
			player.Wins = ap.Wins;
			player.Losses = ap.Losses;
			player.TeamKills = ap.TeamKills;

			if(PlayerID == player.PlayerID) // hey it's us!
			{
				LocalPlayer = player;
				if(SelfAdded != null)
					SelfAdded.Invoke(this, new PlayerEventArgs(player));
			}

			if(PlayerAdded != null)
				PlayerAdded.Invoke(this, new PlayerEventArgs(player));
		}

		private void HandleRemovePlayer(NetworkMessage msg)
		{
			MsgRemovePlayer rp = msg as MsgRemovePlayer;

			var player = GetPlayerByID(rp.PlayerID);
			if(player == null)
				return;

			PlayerList.Remove(player.PlayerID);

			if(PlayerRemoved != null)
				PlayerRemoved.Invoke(this, new PlayerEventArgs(player));

			if (LocalPlayer == player)	//oh shit it's us!
			{
				LocalPlayer = null;
				if(SelfRemoved != null)
					SelfRemoved.Invoke(this, new PlayerEventArgs(player));
			}
		}

		private void HandlePlayerInfo(NetworkMessage msg)
		{
			MsgPlayerInfo info = msg as MsgPlayerInfo;
			foreach(var p in info.PlayerUpdates)
			{
				Player player = GetPlayerByID(p.PlayerID);
				if (player != null)
				{
					player.Attributes = p.Attributes;

					if(PlayerInfoUpdated != null)
						PlayerInfoUpdated.Invoke(this, new PlayerEventArgs(player));
				}
			}
		}

		private void HandleScoreUpdate(NetworkMessage msg)
		{
			MsgScore sc = msg as MsgScore;
			Player player = GetPlayerByID(sc.PlayerID);
			if(player != null)
			{
				player.Wins = sc.Wins;
				player.Losses = sc.Losses;
				player.TeamKills = sc.TeamKills;

				if(PlayerInfoUpdated != null)
					PlayerInfoUpdated.Invoke(this, new PlayerEventArgs(player));
			}
		}
	}
}
