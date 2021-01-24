using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace NatChatCore
{
    public class Packet
    {
        public static Regex Re = new Regex(@"\[(?<cmd>[0-9])\]\[(?<value>.*)\]");

        public PacketType Cmd { get; set; }
        public string Value { get; set; }
        public MagicToken Magic { get; set; }

        public static bool TryParse(string msg, IPEndPoint ep, out Packet p)
        {
            p = null;

            var match = Re.Match(msg);
            if (!match.Success) return false;

            if (int.TryParse(match.Groups["cmd"].Value, out int cmd))
            {
                p = new Packet
                {
                    Cmd = (PacketType) int.Parse(match.Groups["cmd"].Value), Value = match.Groups["value"].Value,
                    Magic = new MagicToken(ep)
                };
                return true;
            }

            return false;
        }

        public byte[] ToByteArray()
        {
            var str = $"[{(int) this.Cmd}][{this.Value}]";
            return Encoding.ASCII.GetBytes(str);
        }
    }
}