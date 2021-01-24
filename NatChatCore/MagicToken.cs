using System;
using System.Net;

namespace NatChatCore
{
    public class MagicToken
    {
        public string Token { get; set; }
        public IPEndPoint Endpoint { get; set; }

        public DateTime LastValid { get; set; }
        public bool Suspended { get; set; } = false;


        public int PastSeconds
        {
            get => (int) ((DateTime.Now - this.LastValid).TotalSeconds);
        }


        public MagicToken(string token)
        {
            this.Token = token;
            this.Endpoint = MagicToken.ToEndpoint(this.Token);

            this.LastValid = DateTime.Now;
        }

        public MagicToken(IPEndPoint endpoint)
        {
            this.Endpoint = endpoint;
            this.Token = MagicToken.ToString(this.Endpoint);

            this.LastValid = DateTime.Now;
        }

        public MagicToken(IPEndPoint endpoint, string token, DateTime valid)
        {
            this.Endpoint = endpoint;
            this.Token = token;

            this.LastValid = valid;
        }

        public override string ToString() => this.Token;


        public override int GetHashCode()
        {
            return this.Token.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            var m = obj as MagicToken;
            return m.Token.Equals(this.Token);
        }


        public static IPEndPoint ToEndpoint(string token)
        {
            try
            {
                var magicToken = Convert.FromBase64String(token);
                var remoteEp = new IPEndPoint(
                    new IPAddress(magicToken[0..^2]),
                    BitConverter.ToUInt16(magicToken[^2..]));

                return remoteEp;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static string ToString(IPEndPoint endpoint)
        {
            var ipBytes = endpoint.Address.GetAddressBytes();
            var portBytes = BitConverter.GetBytes((UInt16) (endpoint.Port));
            var magicToken = new byte[ipBytes.Length + portBytes.Length];
            ipBytes.CopyTo(magicToken, 0);
            portBytes.CopyTo(magicToken, ipBytes.Length);

            return Convert.ToBase64String(magicToken);
        }
    }
}