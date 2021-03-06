using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BZFlag.Networking.Messages;

using BZFlag.Game.Host.Players;
using BZFlag.Networking.Messages.BZFS;
using BZFlag.Networking.Messages.BZFS.Player;
using BZFlag.Data.Teams;
using BZFlag.Networking.Messages.BZFS.Control;
using BZFlag.Networking.Messages.BZFS.Shots;
using System.Diagnostics;
using BZFlag.Networking.Messages.BZFS.Flags;

namespace BZFlag.Game.Host.Processors
{
    public class GamePlayZone : PlayerProcessor
    {
        public GameState State = null;

        public event EventHandler UpdatePublicListServer = null;
        public event EventHandler<ServerPlayer> PlayerRejected;


        public event EventHandler<ServerPlayer> PlayerPreAdd;


        public event EventHandler<ServerPlayer> RemovedPlayer;

        public GamePlayZone(GameState state) : base(state.ConfigData)
        {
            State = state;

            State.BZDatabase.ValueChanged += this.BZDatabase_ValueChanged;

            RegisterCommonHandlers();

            MessageDispatch.Add(new MsgAlive(), HandleAlive);
            MessageDispatch.Add(new MsgPlayerUpdateSmall(), HandlePlayerUpdate);
            MessageDispatch.Add(new MsgPlayerUpdate(), HandlePlayerUpdate);

            MessageDispatch.Add(new MsgMessage(), new ServerMessageDispatcher.MessageHandler((x, y) => Chat.HandleChatMessage(x, y as MsgMessage)));

            MessageDispatch.Add(new MsgShotBegin(), new ServerMessageDispatcher.MessageHandler((x, y) => Shots.HandleShotBegin(x, y as MsgShotBegin)));
            MessageDispatch.Add(new MsgShotEnd(), new ServerMessageDispatcher.MessageHandler((x, y) => Shots.HandleShotEnd(x, y as MsgShotEnd)));
            MessageDispatch.Add(new MsgGMUpdate(), new ServerMessageDispatcher.MessageHandler((x, y) => Shots.HandleGMUpdate(x, y as MsgGMUpdate)));
            MessageDispatch.Add(new MsgKilled(), new ServerMessageDispatcher.MessageHandler((x, y) => Players.HandleKilled(x, y as MsgKilled)));

            MessageDispatch.Add(new MsgGrabFlag(), new ServerMessageDispatcher.MessageHandler((x, y) => Flags.HandleFlagGrab(x, y as MsgGrabFlag)));
            MessageDispatch.Add(new MsgDropFlag(), new ServerMessageDispatcher.MessageHandler((x, y) => Flags.HandleDropFlag(x, y as MsgDropFlag)));
            MessageDispatch.Add(new MsgTransferFlag(), new ServerMessageDispatcher.MessageHandler((x, y) => Flags.HandleFlagTransfer(x, y as MsgTransferFlag)));
        }

        protected override void HandleUnknownMessage(ServerPlayer player, NetworkMessage msg)
        {
            byte[] b = BitConverter.GetBytes((UInt16)msg.Code);
            Array.Reverse(b);
            string code = Encoding.ASCII.GetString(b);

            Logger.Log2("Unknown message in gameplay handler, " + code);
            base.HandleUnknownMessage(player, msg);
        }

        protected override void PlayerAdded(ServerPlayer player)
        {
            base.PlayerAdded(player);
        }

        protected override void PlayerRemoved(ServerPlayer player)
        {
            base.PlayerRemoved(player);

            Players.RemovePlayer(player);

            // tell everyone they went away

            RemovedPlayer?.Invoke(this,player);
            UpdatePublicListServer?.Invoke(this, EventArgs.Empty);
        }

        protected override void Update()
        {
            GameTime.Update();
            Flags.Update(GameTime);
            Shots.Update(GameTime);
            Players.Update(GameTime);
        }

        protected override void UpdatePlayer(ServerPlayer player)
        {
            base.UpdatePlayer(player);

            if (player.NeedStartupInfo)
                DoPlayerAdd(player);
        }

        public override void ProcessClientMessage(ServerPlayer player, NetworkMessage msg)
        {
            base.ProcessClientMessage(player, msg);
        }

        protected void DoPlayerAdd(ServerPlayer player)
        {
            player.ActualTeam = TeamColors.ObserverTeam;
            PlayerPreAdd?.Invoke(this, player); // let someone set the team

            if (!Players.AddPlayer(player))
            {
                SendReject(player, MsgReject.RejectionCodes.RejectTeamFull, Resources.TeamFullMessage.Replace("%T",player.DesiredTeam.ToString()));
                return;
            }
            player.NeedStartupInfo = false;

            UpdatePublicListServer?.Invoke(this, EventArgs.Empty);
        }

        private void HandleAlive(ServerPlayer player, NetworkMessage msg)
        {
            if (!player.Allowances.AllowPlay)
                return;

            Players.StartSpawn(player, msg as MsgAlive);
        }

        private void HandlePlayerUpdate(ServerPlayer player, NetworkMessage msg)
        {
            Players.PlayerUpdate(player, msg as MsgPlayerUpdateBase);
        }

        private void BZDatabase_ValueChanged(object sender, Data.BZDB.Database.DatabaseItem e)
        {
  
        }

        private void SendReject(ServerPlayer player, MsgReject.RejectionCodes code, string reason)
        {
            player.Accepted = false;
            player.RejectionReason = code.ToString() + " :" + reason;

            Logger.Log1("Reject Player " + player.PlayerID + " " + player.RejectionReason);
            player.SendMessage(new MsgReject(code, reason));

            PlayerRejected?.Invoke(this, player);

            player.Disconnect();
            RemovePlayer(player);
        }
    }
}
