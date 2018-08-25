using BZFlag.Data.Utils;

namespace BZFlag.Replay
{
    public class ReplayHeader : DynamicBufferReader
    {
        public uint magic { get; set; }
        public uint version { get; set; }
        public uint offset { get; set; }
        public long filetime { get; set; }
        public uint player { get; set; }
        public uint flagsSize { get; set; }
        public uint worldSize { get; set; }
        public string callSign { get; set; }
        public string motto { get; set; }
        public string ServerVersion { get; set; }
        public string appVersion { get; set; }
        public string realHash { get; set; }
        public string flags { get; set; }
        public string world { get; set; }

        public ReplayHeader(byte[] data) : base(data)
        {
        }

        public void UnpackHeader()
        {
            magic = ReadUInt32();
            version = ReadUInt32();
            offset = ReadUInt32();
            filetime = ReadInt64();
            player = ReadUInt32();
            flagsSize = ReadUInt32();
            worldSize = ReadUInt32();
            callSign = ReadFixedSizeString(32);
            motto = ReadFixedSizeString(128);
            ServerVersion = ReadFixedSizeString(8);
            appVersion = ReadFixedSizeString(128);
            realHash = ReadFixedSizeString(64);

            if (flagsSize > 0)
            {
                flags = ReadFixedSizeString((int)flagsSize);
            }

            world = ReadFixedSizeString((int)worldSize);
        }
    }
}
