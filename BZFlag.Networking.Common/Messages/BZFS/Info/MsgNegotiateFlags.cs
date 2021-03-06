using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BZFlag.Data.Flags;
using BZFlag.Data.Utils;

namespace BZFlag.Networking.Messages.BZFS.Info
{
    public class MsgNegotiateFlags : NetworkMessage
    {
        public List<string> FlagAbrevs = new List<string>();

        public MsgNegotiateFlags()
        {
            Code = CodeFromChars("nf");
        }

        public MsgNegotiateFlags(IEnumerable<string> abreviations)
        {
            Code = CodeFromChars("nf");
            FillFromCache(abreviations);
        }

        public void FillFromCache(IEnumerable<string> abreviations)
        {
            foreach (var f in abreviations)
                FlagAbrevs.Add(f);
        }

        public bool Contains(string abriv)
        {
            return FlagAbrevs.Contains(abriv);
        }

        public override byte[] Pack()
        {
            DynamicOutputBuffer buffer = DynamicOutputBuffer.Get(Code);
            foreach (var f in FlagAbrevs)
                buffer.WriteFixedSizeString(f, 2);

            return buffer.GetMessageBuffer();
        }

        public override void Unpack(byte[] data)
        {
            Reset(data);

            int count = data.Length / 2;
            for (int i = 0; i < count; i++)
                FlagAbrevs.Add(ReadFixedSizeString(2));
        }
    }
}
