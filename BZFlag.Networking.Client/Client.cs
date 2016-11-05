﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Net;
using System.Net.Sockets;

using BZFlag.Networking.Messages;

namespace BZFlag.Networking
{
    public class Client
    {
		public static readonly byte[] BZFSHail = System.Text.Encoding.ASCII.GetBytes("BZFLAG\r\n\r\n");

		protected InboundMessageBuffer InboundTCP = new InboundMessageBuffer();
		protected InboundMessageBuffer InboundUDP = new InboundMessageBuffer();

		public MessageUnpacker InboundMessageProcessor = new MessageUnpacker();

		protected OutboundMessageBuffer OutboundTCP = new OutboundMessageBuffer();
		protected OutboundMessageBuffer OutboundUDP = new OutboundMessageBuffer();

		protected TcpClient TCP = null;

		protected Thread TCPNetworkPollThread = null;

		private static readonly bool RaiseDataMessages = false;

		public int MaxMessagesPerCycle = 20;

		public class HostMessageReceivedEventArgs : EventArgs
		{
			public NetworkMessage Message = null;
			
			public HostMessageReceivedEventArgs(NetworkMessage msg)
			{
				Message = msg;
			}
		}

		public event EventHandler<HostMessageReceivedEventArgs> HostMessageReceived = null;

		protected enum NetworkPushMessages
		{
			None,
			ConnectedTCP,
			ConnectedUDP,
			HostHasData,
			TCPHostDisconnect,
			HostIsNotBZFS,
		}

		private List<NetworkPushMessages> PendingNetworkNotifications = new List<NetworkPushMessages>();

		private void PushNetworkNotificatioin(NetworkPushMessages msg)
		{
			if(msg == NetworkPushMessages.None)
				return;

			lock(PendingNetworkNotifications)
				PendingNetworkNotifications.Add(msg);
		}

		protected NetworkPushMessages PopNetworkNotification()
		{
			NetworkPushMessages n = NetworkPushMessages.None;
			lock(PendingNetworkNotifications)
			{
				if (PendingNetworkNotifications.Count > 0)
				{
					n = PendingNetworkNotifications[0];
					PendingNetworkNotifications.RemoveAt(0);
				}
			}
			return n;
		}

		public Client()
		{
			MessageFactory.RegisterBSFSMessages();

			InboundTCP.CompleteMessageRecived += Inbound_CompleteMessageRecived;
			InboundUDP.CompleteMessageRecived += Inbound_CompleteMessageRecived;
		}

		private void Inbound_CompleteMessageRecived(object sender, EventArgs e)
		{
			InboundMessageBuffer buffer = sender as InboundMessageBuffer;
			if(buffer == null)
				return;

			foreach(var m in buffer.GetMessages())
				InboundMessageProcessor.Push(m);
		}

		public void Startup(string server, int port)
		{
			Shutdown();
			OutboundTCP.Push(BZFSHail);

			TCP = new TcpClient(server, port);

			InboundMessageProcessor.Start();

			TCPNetworkPollThread = new Thread(new ThreadStart(PollTCP));
			TCPNetworkPollThread.Start();
		}

		public void Shutdown()
		{
			InboundMessageProcessor.Stop();

			if(TCPNetworkPollThread != null && TCPNetworkPollThread.IsAlive)
				TCPNetworkPollThread.Abort();

			TCPNetworkPollThread = null;

			if(TCP != null)
				TCP.Close();
			TCP = null;

			PendingNetworkNotifications.Clear();
			InboundTCP.Clear();
			InboundUDP.Clear();
			OutboundTCP.Clear();
			OutboundUDP.Clear();
		}

		public event EventHandler TCPConnected = null;
		public event EventHandler HostHasData = null;
		public event EventHandler TCPHostDisconnect = null;
		public event EventHandler HostIsNotBZFS = null;

		public void Update()
		{
			NetworkPushMessages evtMsg = PopNetworkNotification();
			while (evtMsg != NetworkPushMessages.None)
			{
				switch(evtMsg)
				{
					case NetworkPushMessages.ConnectedTCP:
						if(TCPConnected != null)
							TCPConnected.Invoke(this, EventArgs.Empty);
						break;

					case NetworkPushMessages.HostHasData:
						if(HostHasData != null)
							HostHasData.Invoke(this, EventArgs.Empty);
						break;

					case NetworkPushMessages.TCPHostDisconnect:
						if(TCPHostDisconnect != null)
							TCPHostDisconnect.Invoke(this, EventArgs.Empty);
						break;

					case NetworkPushMessages.HostIsNotBZFS:
						if(HostIsNotBZFS != null)
							HostIsNotBZFS.Invoke(this, EventArgs.Empty);
						break;
				}
				evtMsg = PopNetworkNotification();
			}

			int count = 0;
			var msg = InboundMessageProcessor.Pop();
			while (msg != null)
			{
				if(HostMessageReceived != null)
					HostMessageReceived.Invoke(this, new HostMessageReceivedEventArgs(msg));
				count++;
				if(count >= MaxMessagesPerCycle)
					msg = null;
				else
					msg = InboundMessageProcessor.Pop();
			}
			
		}

		// TCP polling local data
		private bool Connected = false;
		private string HostProtoVersion = string.Empty;

		protected void PollTCP()
		{
			var stream = TCP.GetStream();
			while(true)
			{
				byte[] outbound = OutboundTCP.Pop();
				while(outbound != null)
				{
					stream.Write(outbound, 0, outbound.Length);
					
					outbound = OutboundTCP.Pop();
				}
				stream.Flush();

				if (!Connected)
				{
					if (TCP.Available >= 9)
					{
						byte[] header = new byte[8];
						if (stream.Read(header, 0, 8) != 8)
						{
							PushNetworkNotificatioin(NetworkPushMessages.HostIsNotBZFS);
							return;
						}
						HostProtoVersion = Encoding.ASCII.GetString(header);
						if (HostProtoVersion.Substring(0,4) != "BZFS")
						{
							PushNetworkNotificatioin(NetworkPushMessages.HostIsNotBZFS);
							return;
						}

						Connected = true;
						int b = stream.ReadByte();

					}
				}

				if (Connected && TCP.Available > 0)
				{
					byte[] data = new byte[TCP.Available];
					int read = stream.Read(data, 0, data.Length);
					InboundTCP.AddData(data);
					if (RaiseDataMessages)
						PushNetworkNotificatioin(NetworkPushMessages.HostHasData);
				}
				Thread.Sleep(10);
			}
		}
    }
}