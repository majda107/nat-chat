using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NatChatCore
{
    public class ChatProcessor
    {
        public Logger Logger = new Logger();

        public IPEndPoint Endpoint { get; set; }
        public UdpClient Client { get; set; }
        public MagicToken Me { get; set; }

        public ConcurrentBag<MagicUser> Remotes { get; set; }


        public event EventHandler KeepaliveTick;

        private Thread keepaliveThread;
        private Thread receiveThread;


        public ChatProcessor()
        {
            this.InitStun();

            this.Client = new UdpClient(this.Endpoint);
            this.Remotes = new ConcurrentBag<MagicUser>();

            this.StartKeepAlive();
            this.StartReceiving();
        }

        private void InitStun()
        {
            var address = Dns.GetHostAddresses("stun.l.google.com");
            var stunep = new IPEndPoint(address[0], 19302);

            var stunResult = STUN.STUNClient.Query(stunep, STUN.STUNQueryType.ExactNAT, true);

            this.Me = new MagicToken(stunResult.PublicEndPoint);
            this.Endpoint = stunResult.LocalEndPoint;
        }

        private void StartKeepAlive()
        {
            this.keepaliveThread = new Thread(th =>
            {
                int count = 0;
                while (true)
                {
                    Thread.Sleep(1000);
                    this.KeepaliveTick?.Invoke(this, EventArgs.Empty);

                    count++;
                    if (count >= 30)
                    {
                        this.Send(new Packet {Cmd = PacketType.Keepalive});
                        count = 0;
                    }

                    foreach (var u in this.Remotes)
                        if (u.PastSeconds >= 120)
                            u.Suspended = true;
                }
            });

            this.keepaliveThread.Start();
        }

        private void StartReceiving()
        {
            this.receiveThread = new Thread(th =>
            {
                while (true)
                {
                    try
                    {
                        var remoteEp = new IPEndPoint(IPAddress.Any, 0);

                        // this.Client.AllowNatTraversal(true);

                        var bytes = this.Client.Receive(ref remoteEp);
                        // Console.WriteLine($"{remoteEp} -> {Encoding.ASCII.GetString(msg)}");
                        var message = Encoding.ASCII.GetString(bytes);

                        if (Packet.TryParse(message, remoteEp, out Packet p))
                            this.Receive(p);
                        else
                            this?.Logger?.LogError($"Couldn't parse packet '{message}'");
                    }
                    catch (Exception e)
                    {
                        this?.Logger?.LogError(e.Message);
                    }
                }
            });

            this.receiveThread.Start();
        }

        public void SendRaw(string message)
        {
            Logger.LogDeafen($"Sending as a RAW '{message}'");
            this.SendAll(Encoding.ASCII.GetBytes(message));
        }

        private void SendAll(byte[] bytes)
        {
            foreach (var magic in this.Remotes)
                try
                {
                    if (magic.Suspended) continue; // IGNORE SUSPENDED

                    this.Client.Send(bytes, bytes.Length, magic.Endpoint);
                }
                catch (Exception e)
                {
                    this.Logger.LogError($"Invalid endpoint! Suspending... {magic.Endpoint} {e.Message}");
                    magic.Suspended = true;
                }
        }


        private void SendTo(byte[] bytes, IPEndPoint end)
        {
            this.Client.Send(bytes, bytes.Length, end);
        }

        private void IntersectDiscover(Packet p)
        {
            var tokens = p.Value?.Split(';');
            var added = new List<string>();

            foreach (var token in tokens)
            {
                if (token == "" || token == this.Me.Token) continue;

                var magicToken = new MagicToken(token);
                if (this.Remotes.Contains(magicToken)) continue;

                this.Logger.LogDeafen($"Adding {magicToken.Endpoint.ToString()}");

                this.Remotes.Add(new MagicUser(magicToken));
                added.Add(token);
            }

            // SEND ALL ADDED
            if (added.Count <= 0) return;
            this.Send(new Packet {Cmd = PacketType.Discover, Value = String.Join(';', added)});
        }


        public void AddEndpoint(MagicUser user)
        {
            this.Remotes.Add(user);
        }

        public void Receive(Packet p)
        {
            this.Logger.LogDeafen($"Received {p.Cmd} packet | '{p.Value}'");

            var user = this.Remotes.FirstOrDefault(m => m.Token == p.Magic.Token);
            if (user != null && user.Suspended)
                user.Suspended = false; // UNSUSPEND

            switch (p.Cmd)
            {
                case PacketType.Message:
                    if (user != null)
                        this.Logger.Log($"-> [{user.Alias}] {p.Value}");
                    else
                        this.Logger.Log($"-> [{p.Magic.Token}] {p.Value}");

                    break;
                case PacketType.Keepalive:
                    // if (user != null)
                    //     user.LastValid = DateTime.Now;

                    this.Send(new Packet {Cmd = PacketType.KeepaliveReply}, p.Magic.Endpoint);
                    break;
                case PacketType.KeepaliveReply:
                    if (user != null)
                        user.LastValid = DateTime.Now;
                    break;
                case PacketType.Discover:
                    this.IntersectDiscover(p);
                    break;
            }
        }

        public void Send(Packet p, IPEndPoint end = null)
        {
            var user = end == null ? "all" : end.ToString();
            this.Logger.LogDeafen($"Sending to {user} {p.Cmd} packet | '{p.Value}'");

            if (end == null)
                this.SendAll(p.ToByteArray());
            else
                this.SendTo(p.ToByteArray(), end);

            // switch (p.Cmd)
            // {
            //     case PacketType.Message:
            //         var b = p.ToByteArray();
            //         this.SendAll(b);
            //         break;
            //     case PacketType.Keepalive:
            //         this.SendAll(p.ToByteArray());
            //         break;
            // }
        }
    }
}