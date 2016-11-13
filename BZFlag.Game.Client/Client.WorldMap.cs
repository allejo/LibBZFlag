﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

using BZFlag.Networking.Messages.BZFS.Info;
using BZFlag.Networking.Messages.BZFS.World;
using BZFlag.IO.BZW.Binary;
using BZFlag.Networking.Messages;

namespace BZFlag.Game
{
	public partial class Client
	{
		private WorldUnpacker Unpacker = null;

		private string WorldURL = string.Empty;

		public class WorldDownloadProgressEventArgs : EventArgs
		{
			public float Paramater = 0;

			public WorldDownloadProgressEventArgs(float p)
			{
				Paramater = p;
			}
		}
		public event EventHandler<WorldDownloadProgressEventArgs> WorldDownloadProgress = null;

		protected string WorldHash = string.Empty;
		protected BZWCache WorldCache = null;

		private void HandleWorldHash(NetworkMessage msg)
		{
			MsgWantWHash hash = msg as MsgWantWHash;
			WorldHash = hash.WorldHash;

			bool getWorld = true;

			if (Params.CacheFolder != null)
			{
				WorldCache = new BZWCache(Params.CacheFolder);
				if (WorldCache.CheckCacheForHash(hash.WorldHash))
				{
					Map = WorldCache.ReadMapFromCache(hash.WorldHash);
					if(Map != null)
						getWorld = false;
				}
			}

			if (getWorld && WorldURL != string.Empty)
			{
				if(WorldDownloadProgress != null)
					WorldDownloadProgress.Invoke(this, new WorldDownloadProgressEventArgs(0));

				// try to download it from the interwebs
				WebClient worldWWW = new WebClient();
				worldWWW.DownloadDataCompleted += worldWWW_DownloadDataCompleted;
				worldWWW.DownloadProgressChanged += worldWWW_DownloadProgressChanged;
				worldWWW.DownloadDataAsync(new Uri(WorldURL));
			}

			if(getWorld)
				SendGetWorld();
			else
				SendEnter();
		}

		protected void SendGetWorld()
		{
			if(WorldDownloadProgress != null)
				WorldDownloadProgress.Invoke(this, new WorldDownloadProgressEventArgs(0));
			Unpacker = new WorldUnpacker();
			NetClient.SendMessage(new MsgGetWorld(0));
		}

		private void worldWWW_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			if(WorldDownloadProgress != null)
				WorldDownloadProgress.Invoke(this, new WorldDownloadProgressEventArgs(e.ProgressPercentage/100.0f));
		}

		private void worldWWW_DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
		{
			if(WorldDownloadProgress != null)
				WorldDownloadProgress.Invoke(this, new WorldDownloadProgressEventArgs(1.0f));

			if(e.Cancelled || e.Error != null)
				SendGetWorld();
			else
			{
				WorldUnpacker unpacker = new WorldUnpacker(e.Result);
				Map = unpacker.Unpack();
				if (Map == null)
					SendGetWorld();
				else
				{
					WorldCache.SaveMapToCache(WorldHash, unpacker.GetBuffer());
					SendEnter();
				}
			}
		}

		private void HandleGetWorld(NetworkMessage msg)
		{
			MsgGetWorld wldChunk = msg as MsgGetWorld;

			Unpacker.AddData(wldChunk.Data);

			if(wldChunk.Offset > 0)
			{
				if(WorldDownloadProgress != null)
					WorldDownloadProgress.Invoke(this, new WorldDownloadProgressEventArgs((float)Unpacker.Size() / (float)(((UInt32)wldChunk.Offset + Unpacker.Size()))));
				NetClient.SendMessage(new MsgGetWorld((UInt32)Unpacker.Size()));
			}
			else
			{
				if(WorldDownloadProgress != null)
					WorldDownloadProgress.Invoke(this, new WorldDownloadProgressEventArgs(1));
				
				Map = Unpacker.Unpack();
				WorldCache.SaveMapToCache(WorldHash, Unpacker.GetBuffer());
				SendEnter();
			}
		}

		private void HandleWorldCacheURL(NetworkMessage msg)
		{
			MsgCacheURL url = msg as MsgCacheURL;
			WorldURL = url.URL;
		}
	}
}