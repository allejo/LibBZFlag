using System;
using System.Collections.Generic;

using BZFlag.Data.Flags;
using BZFlag.Game.Host.Players;
using BZFlag.LinearMath;
using BZFlag.Networking.Messages.BZFS.Flags;

namespace BZFlag.Game.Host.World
{
    partial class FlagManager
    {
        public class FlagEventArgs : EventArgs
        {
            public ServerPlayer Player = null;
            public FlagInstance Flag = null;
            public bool Allow = true;
        }
        public event EventHandler<FlagEventArgs> FlagPreGrab;

        public delegate void FlagCallback(ServerPlayer player, FlagInstance flag);
        public FlagCallback OnGrantFlag = null;
        public FlagCallback ComputeFlagDrop = null;
        public FlagCallback ComputeFlagAdd = null;

        internal void HandleFlagTransfer(ServerPlayer player, MsgTransferFlag message)
        {
            if (message == null)
                return;

            if (player.PlayerID != message.ToID)
                return;

            ServerPlayer target = Players.GetPlayerByID(message.FromID);

            if (target.Info.CariedFlag == null)
                return;

            Logger.Log4("Player " + player.Callsign + " wants to grab flag " + target.Info.CariedFlag.Abreviation + " from " + target.Callsign);

            TransferFlag(target, player);
        }

        public void TransferFlag(ServerPlayer from, ServerPlayer to)
        {
            if (from == null || !from.CanDoPlayActions() || from.Info.CariedFlag == null)
                return;
            if (to == null || !to.CanDoPlayActions())
                return;

            FlagTransferEventArgs args = new FlagTransferEventArgs();
            args.From = from;
            args.To = to;
            args.Flag = from.Info.CariedFlag;

            FlagPreTransfer?.Invoke(this, args);
            if (!args.Allow)
                return;

            DropFlag(to);
            from.Info.CariedFlag = null;
            to.Info.CariedFlag = args.Flag;

            MsgTransferFlag transfer = new MsgTransferFlag();
            transfer.FromID = from.PlayerID;
            transfer.ToID = to.PlayerID;
            transfer.FlagID = to.Info.CariedFlag.FlagID;

            Players.SendToAll(transfer, false);

            FlagTransfered?.Invoke(this, args);

            Logger.Log2("Flag transfered from  " + from.Callsign + " to " + to.Callsign );
        }

        internal void HandleFlagGrab(ServerPlayer player, MsgGrabFlag message)
        {
            if (message == null || !player.CanDoPlayActions())
                return;

            int flagID = message.FlagData.FlagID;

            FlagInstance candidateFlag = FindFlagByID(flagID);

            if (candidateFlag == null || !candidateFlag.Grabable())
                return;

            float dist = Vector3F.Distance(player.Info.LastSentUpdate.Position, candidateFlag.Position);
            if (false && dist > GetFlagGrabDistance(player))
                return;

            Logger.Log4("Player " + player.Callsign + " wants to grab flag " + candidateFlag.FlagID.ToString() + " " + candidateFlag.ToString());

            GrantFlag(player, candidateFlag);
        }

        protected float GetFlagGrabDistance(ServerPlayer player)
        {
            float grabRadius = Cache.FlagRadius.Value + Cache.TankRadius.Value;

            float speedDeviation = (float)((GameTime.Now - player.Info.LastSentUpdate.TimeStamp) * Cache.TankSpeed);

            return grabRadius + speedDeviation;
        }

        public bool GrantFlag(ServerPlayer player, FlagInstance flag)
        {
            if (flag.Owner != null || player.Info.CariedFlag != null)
                return false;

            OnGrantFlag?.Invoke(player, flag);

            FlagEventArgs args = new FlagEventArgs();
            args.Player = player;
            args.Flag = flag;

            FlagPreGrab?.Invoke(this, args);

            if (flag.Status == FlagStatuses.FlagNoExist)
                return false;

            lock (ActiveFlags)
            {
                if (!args.Allow || flag.Owner != null)
                    return false;

                lock (WorldFlags)
                    WorldFlags.Remove(flag.FlagID);

                lock (CarriedFlags)
                    CarriedFlags.Add(flag.FlagID, flag);

                flag.Owner = player;
                flag.Status = FlagStatuses.FlagOnTank;
                flag.OwnerID = player.PlayerID;
            }

            FlagGrabbed?.Invoke(this, flag);

            player.Info.CariedFlag = flag;

            MsgGrabFlag grabMessage = new MsgGrabFlag();
            grabMessage.PlayerID = player.PlayerID;
            grabMessage.FlagData = flag;

            Logger.Log2("Player " + player.Callsign + " granted flag " + flag.FlagID.ToString() + " " + flag.ToString());

            Players.SendToAll(grabMessage, false);
            return true;
        }

        internal void HandleDropFlag(ServerPlayer player, MsgDropFlag message)
        {
            if (message == null)
                return;

            if (player.Info.CariedFlag == null)
                return;

            player.Info.CariedFlag.Position = message.Postion;

            DropFlag(player);
        }

        public void StandardDrop(ServerPlayer player, FlagInstance flag)
        {
            flag.LaunchPosition = flag.Position + new Vector3F(0,0,Cache.TankHeight);

            float thrownAltitude = Cache.FlagAltitude;
            if (flag.Flag == FlagTypeList.Shield)
                thrownAltitude *= Cache.ShieldFlight;

            // TODO, compute the intersection point
            flag.FlightTime = 0;

            if (player == null)
            {
                flag.FlightEnd = 2.0f * (float)Math.Sqrt(-2.0f * Cache.FlagAltitude / Cache.Gravity);

                flag.InitalVelocity = -0.5f * Cache.Gravity * flag.FlightEnd;
                flag.Status = FlagStatuses.FlagComing;
                flag.LandingPostion = new Vector3F(flag.Position.X, flag.Position.Y, 0);
            }
            else if (flag.Endurance == FlagEndurances.FlagUnstable)
            {
                flag.FlightEnd = 2.0f * (float)Math.Sqrt(-2.0f * Cache.FlagAltitude / Cache.Gravity);

                flag.InitalVelocity = -0.5f * Cache.Gravity * flag.FlightEnd;
                flag.Status =FlagStatuses.FlagGoing;

                flag.LandingPostion = new Vector3F(flag.Position.X, flag.Position.Y, flag.Position.Z + thrownAltitude);
            }
            else
            {
                float maxAltitude = flag.Position.Z + thrownAltitude;

                float upTime = (float)Math.Sqrt(-2.0f * thrownAltitude / Cache.Gravity);
                float downTime = (float)Math.Sqrt(-2.0f * (maxAltitude - flag.Position.Z) / Cache.Gravity);
                flag.FlightEnd = upTime + downTime;
                flag.InitalVelocity = -Cache.Gravity * upTime;

                flag.Status = FlagStatuses.FlagInAir;
                flag.LandingPostion = new Vector3F(flag.Position.X, flag.Position.Y, 0);
            }

            flag.DropStarted = GameTime.Now;
        }

        public void DropFlag(ServerPlayer player)
        {
            if (player == null || player.Info.CariedFlag == null)
                return;

            if (player.Info.CariedFlag.Flag == FlagTypeList.Identify)
                player.SendMessage(new MsgNearFlag());  // send them an empty ID message to clear out the display

            FlagInstance flag = player.Info.CariedFlag;

            ComputeFlagDrop?.Invoke(player, flag);

            MsgDropFlag drop = new MsgDropFlag();
            drop.FlagID = flag.FlagID;
            drop.PlayerID = player.PlayerID;
            drop.Data = flag;

            player.Info.CariedFlag = null;
            flag.Owner = null;

            Players.SendToAll(drop, false);

            lock (CarriedFlags)
            {
                if (CarriedFlags.ContainsKey(flag.FlagID))
                    CarriedFlags.Remove(flag.FlagID);
            }

            lock (WorldFlags)
                WorldFlags.Add(flag.FlagID, flag);

            FlagDropped?.Invoke(this, flag);
        }
    }
}
