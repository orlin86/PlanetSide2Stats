using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Security.Principal;
using System.Threading;
using System.Timers;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;
using WebSocket = WebSocketSharp.WebSocket;
using ServerConsoleApp;

namespace ServerConsoleApp
{
    public class SendKills : WebSocketBehavior
    {
        
        private WssvClient _client;
        private Notifier nf = new Notifier();

        public void ProcessMessage(Object sender, MessageEventArgs e)
        {
            if (e.Data.Contains("heartbeat"))
            {
                Send($"{e.Data}");
            }
            DeathNotifier(e);
            LoginLogoutNotifier(e);
        }
        public SendKills()
        {
            //IgnoreExtensions = true;
            //Server.ws.EmitOnPing = true;
            //Server.ws.OnOpen += (sender, e) =>
            //    {
            //    };
            Server.ws.OnMessage += ProcessMessage;
        }

        protected override void OnOpen()
        {
            Send("You are connected");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            WebSocketSharp.Net.WebSockets.WebSocketContext thiSocketContext = Context;
            string ipAddress = "";
            ipAddress += thiSocketContext.UserEndPoint.Address.ToString();
            Console.WriteLine($"IPADDRESS = {ipAddress}");
            Console.WriteLine($"e.Data = {e.Data}");
            bool champExists = SqlQuerries.SearchChampion(e.Data);
            Console.WriteLine($"champExists = {champExists}");
            if (!champExists)
            {
                SqlQuerries.AddChampByName(e.Data);
                string thisChampId = SqlQuerries.GetChampIdFromDb(e.Data);
                string sendAlive =
            "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[" + thisChampId + "],\r\n\t\"eventNames\":[\"PlayerLogin\", \"PlayerLogout\"]\r\n}";
                Server.ws.Send(sendAlive);
            }
            string id = SqlQuerries.GetChampIdFromDb(e.Data);
            string sendString = "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[\"" +
                        id + "\"],\r\n\t\"eventNames\":[\"Death\"]\r\n}";
            Console.WriteLine($"sendString = {sendString}");
            Server.ws.Send(sendString);
            _client = new WssvClient(ipAddress, e.Data, id);
            Console.WriteLine($"Client IP:{_client.IpAddress}");
            Console.WriteLine($"Client Querry:{_client.Querry}");
            Console.WriteLine($"Client QuerryID:{_client.QuerryId}");
            Send($"ClientIdIs:{_client.QuerryId}");
            if (SqlQuerries.GetOnlineStatus(_client.Querry.ToLower()))
            {
                Send($"{_client.Querry} Online");
            }
            // ↓ sets name to client to match his querry
            SqlQuerries.AddQuerry(_client);
        }
        protected override void OnClose(CloseEventArgs e)
        {
            if (null != _client)
            {
                Console.WriteLine($"{DateTime.Now}:{_client.IpAddress} Closed the connection");
            }
            UnsubscribeFromWs();
        }
        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now}: Error!!!");
            Console.WriteLine($"{DateTime.Now}: {e.Exception}");
            Console.WriteLine($"{DateTime.Now}: {e.Message}");
            UnsubscribeFromWs();
            if (Context.WebSocket.IsAlive)
            {
                this.Context.WebSocket.Close();
            }
        }

        protected void UnsubscribeFromWs()
        {
            Server.ws.OnMessage -= ProcessMessage;
        }
        private void LoginLogoutNotifier(MessageEventArgs e)
        {
            if (e.Data.Contains("PlayerLogin") && e.Data.Contains("payload"))
            {
                LoginMsg thisMsg = new LoginMsg();
                thisMsg = JsonConvert.DeserializeObject<LoginMsg>(e.Data);
                if (!thisMsg.payload.character_id.IsNullOrEmpty())
                {
                    SqlQuerries.ReplaceAlive(true, thisMsg.payload.character_id.ToString());
                    if (null != _client.QuerryId)
                        if (thisMsg.payload.character_id == _client.QuerryId)
                    {
                        Console.WriteLine("/////");
                        Console.WriteLine($"Sending PlayerLogin To Client {_client.IpAddress}");
                        Console.WriteLine("/////");
                        Send($"{_client.Querry} Online");
                    }
                }
            }
            else if (e.Data.Contains("PlayerLogout") && e.Data.Contains("payload"))
            {
                LogoutMsg thisMsg = JsonConvert.DeserializeObject<LogoutMsg>(e.Data);
                SqlQuerries.ReplaceAlive(false, thisMsg.payload.character_id.ToString());
                if (null != _client.QuerryId)
                    if (thisMsg.payload.character_id == _client.QuerryId)
                {
                    Console.WriteLine("/////");
                    Console.WriteLine($"Sending PlayerLogout To Client {_client.IpAddress}");
                    Console.WriteLine("/////");
                    Send($"{_client.Querry} Offline");
                }
            }
        }
        private void DeathNotifier(MessageEventArgs e)
        {
            if (e.Data.Contains("Death") && e.Data.Contains("payload"))
            {
                DeathMsg thisMsg = new DeathMsg();
                thisMsg = JsonConvert.DeserializeObject<DeathMsg>(e.Data);
                if (!thisMsg.payload.attacker_character_id.IsNullOrEmpty())
                {
                    if (thisMsg.payload.attacker_character_id == _client.QuerryId || thisMsg.payload.character_id == _client.QuerryId)
                    {
                        if (!SqlQuerries.SearchById(thisMsg.payload.character_id))
                        {
                            SqlQuerries.AddChampById(thisMsg.payload.character_id);
                            Console.WriteLine($"Added Champ to dB: {SqlQuerries.GetNameById(thisMsg.payload.character_id)}");
                        }
                        else if (!SqlQuerries.SearchById(thisMsg.payload.attacker_character_id))
                        {
                            SqlQuerries.AddChampById(thisMsg.payload.attacker_character_id);
                            Console.WriteLine($"Added Champ to dB: {SqlQuerries.GetNameById(thisMsg.payload.attacker_character_id)}");
                        }
                        Send($"{e.Data}");
                    }
                }
            }
        }
        private void Notify(MessageEventArgs e)
        {
            String msg = !e.IsPing ? e.Data : "Received a ping.";
            msg += "User context for notify: ";
            if (_client == null)
                msg += "NULL";
            else
                msg += _client.ToString();
            nf.Notify(new NotificationMessage
            {

                Summary = "Planetside2 Api MSG",
                Body = msg,
                Icon = "notification-message-im"
            });
        }
    }

    public class WssvClient
    {
        public string IpAddress { get; set; }
        public string Querry { get; set; }
        public string QuerryId { get; set; }

        public WssvClient()
        {
            IpAddress = "";
            Querry = "";
            QuerryId = "";
        }

        public WssvClient(string ip, string querry, string querryId)
        {
            IpAddress = ip;
            Querry = querry;
            QuerryId = querryId;
        }
    }
    public class LoginMsg
    {
        public Payload payload { get; set; }
        public string service { get; set; }
        public string type { get; set; }
        public class Payload
        {
            public string character_id { get; set; }
            public string event_name { get; set; }
            public string timestamp { get; set; }
            public string world_id { get; set; }
        }
    }
    public class LogoutMsg
    {
        public Payload payload { get; set; }
        public string service { get; set; }
        public string type { get; set; }

        public class Payload
        {
            public string character_id { get; set; }
            public string event_name { get; set; }
            public string timestamp { get; set; }
            public string world_id { get; set; }
        }
    }
    public class DeathMsg
    {
        public Payload payload { get; set; }
        public string service { get; set; }
        public string type { get; set; }

        public class Payload
        {
            public string attacker_character_id { get; set; }
            public string attacker_fire_mode_id { get; set; }
            public string attacker_loadout_id { get; set; }
            public string attacker_vehicle_id { get; set; }
            public string attacker_weapon_id { get; set; }
            public string character_id { get; set; }
            public string character_loadout_id { get; set; }
            public string event_name { get; set; }
            public string is_headshot { get; set; }
            public string timestamp { get; set; }
            public string world_id { get; set; }
            public string zone_id { get; set; }
        }
    }

}