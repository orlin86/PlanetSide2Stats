using System;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Timers;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

namespace ServerConsoleApp
{
    public class SendKills : WebSocketBehavior
    {
        private WssvClient _client;
        private static readonly WebSocket ws =
            new WebSocket("wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167");
        private Notifier nf = new Notifier();


        public SendKills()
        {
            //IgnoreExtensions = true;
            //ws.EmitOnPing = true;
            ws.OnOpen += (sender, e) =>
                {
                };
            ws.OnMessage += (sender, e) =>
            {
                Notify(e);
                if (e.Data.Contains("heartbeat"))
                {
                    Send($"{e.Data}");
                }
                DeathNotifier(e);
                LoginLogoutNotifier(e);
            };

            if (!ws.IsAlive)
            {
                ws.Connect();
                if (ws.IsAlive)
                {
                    Console.WriteLine("OPEN !!!!!!!!");
                    Console.WriteLine("OPEN !!!!!!!!");
                    Console.WriteLine("OPEN !!!!!!!!");
                    Console.WriteLine("OPEN !!!!!!!!");
                    Console.WriteLine("OPEN !!!!!!!!");
                    System.Timers.Timer initTimer = new System.Timers.Timer();
                    initTimer.Interval = 1000;
                    initTimer.AutoReset = false;
                    initTimer.Elapsed += new ElapsedEventHandler(SendAliveQuerry);
                    initTimer.Enabled = true;
                }
            }
        }

        //↓ Method related to the timer in ws.Connect
        private static void SendAliveQuerry(object source, ElapsedEventArgs e)
        {
            string[] allIds = SqlQuerries.GetAllIds();

            foreach (var item in allIds)
            {
                string sendString =
            "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[" + item + "],\r\n\t\"eventNames\":[\"PlayerLogin\", \"PlayerLogout\"]\r\n}";
                System.Timers.Timer initTimer = new System.Timers.Timer();
                ws.Send(sendString);
                Thread.Sleep(100);

            }
        }

        protected override void OnOpen()
        {
            Send("You are connected");
        }
        protected override void OnMessage(MessageEventArgs e)
        {
            WebSocketContext thiSocketContext = Context;
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
                ws.Send(sendAlive);
            }
            string id = SqlQuerries.GetChampIdFromDb(e.Data);
            string sendString = "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[\"" +
                        id + "\"],\r\n\t\"eventNames\":[\"Death\"]\r\n}";
            Console.WriteLine($"sendString = {sendString}");
            ws.Send(sendString);
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
            Console.WriteLine($"{DateTime.Now}:{_client.IpAddress} Closed the connection");
            
        }
        protected override void OnError(ErrorEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now}: Error!!!");
            Console.WriteLine($"{DateTime.Now}: {e.Exception}");
            Console.WriteLine($"{DateTime.Now}: {e.Message}");
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
            nf.Notify(new NotificationMessage
            {
                Summary = "Planetside2 Api MSG",
                Body = !e.IsPing ? e.Data : "Received a ping.",
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