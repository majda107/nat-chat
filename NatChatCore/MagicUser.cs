using System.Net;

namespace NatChatCore
{
    public class MagicUser : MagicToken
    {
        public string Alias { get; set; }

        public MagicUser(string token, string alias) : base(token)
        {
            this.Alias = alias;
        }

        public MagicUser(IPEndPoint ip, string alias) : base(ip)
        {
            this.Alias = alias;
        }

        public MagicUser(MagicToken tok, string alias = "anonymous") : base(tok.Endpoint, tok.Token, tok.LastValid)
        {
            this.Alias = alias;
        }
    }
}