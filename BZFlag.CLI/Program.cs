using System;
using System.IO;

using BZFlag.Replay;

namespace BZFlag.CLI
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var REC_FILE = @"/Volumes/Lelouch/Users/allejo/Desktop/20170701-1926-fun.rec";
            var recBytes = File.ReadAllBytes(REC_FILE);

            var test = new ReplayHeader(recBytes);
            test.UnpackHeader();

            Console.WriteLine("Hello World!");
        }
    }
}
