using System;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

namespace ServerConsoleApp
{
    public class SendKills : WebSocketBehavior
    {
        private WssvClient _client;
        private string _name;
        private static int _number = 0;
        private string _prefix;

        private static WebSocket ws =
            new WebSocket("wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167");

        private Notifier nf = new Notifier();


        public SendKills()
            : this(null)
        {
            if (!ws.IsAlive)
            {
                ws.Connect();
                Console.WriteLine("OPEN !!!!!!!!");
                Console.WriteLine("OPEN !!!!!!!!");
                Console.WriteLine("OPEN !!!!!!!!");
                Console.WriteLine("OPEN !!!!!!!!");
                Console.WriteLine("OPEN !!!!!!!!");

                string[] allIds = SqlQuerries.GetAllIds();
                string allIdsJ = string.Join(", ", allIds);
                string sendString =
                    "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[" + allIdsJ + "],\r\n\t\"eventNames\":[\"PlayerLogin\", \"PlayerLogout\"]\r\n}";
                Console.WriteLine($"Sending Ids for Alive: {allIdsJ}");
                ws.Send(sendString);
            }
        }

        protected override void OnOpen()
        {
            ws.OnOpen += (sender, e) =>
            {

            };
            ws.OnMessage += (sender, e) =>
            {
                Notify(e);

                if (e.Data.Contains("PlayerLogin"))
                {
                    LoginMsg thisMsg = JsonConvert.DeserializeObject<LoginMsg>(e.Data);
                    SqlQuerries.ReplaceAlive(true, thisMsg.payload.character_id.ToString());
                }
                if (e.Data.Contains("PlayerLogout"))
                {
                    LogoutMsg thisMsg = JsonConvert.DeserializeObject<LogoutMsg>(e.Data);
                    SqlQuerries.ReplaceAlive(false, thisMsg.payload.character_id.ToString());
                }

                if (e.Data.Contains("Death"))
                {
                    DeathMsg thisMsg = JsonConvert.DeserializeObject<DeathMsg>(e.Data);
                    if (SqlQuerries.GetNameById(thisMsg.payload.attacker_character_id) == _client.Querry.ToString())
                    {
                        Send($"{_client.Querry.ToString()} Kill {thisMsg.payload.character_id}");
                    }
                    else if (SqlQuerries.GetNameById(thisMsg.payload.attacker_character_id) == _client.Querry.ToString())
                    {
                        Send($"{_client.Querry.ToString()} Death by {thisMsg.payload.character_id}");
                    }
                }

                // ↓ sends to client data, containing death, !! SWICH WITH E.DATA
                if (e.Data.Contains(_client.Querry.ToString()))
                {
                    Send(e.Data);
                }
            };
            _name = getName();
        }


        protected override void OnMessage(MessageEventArgs e)
        {
            WebSocketContext thiSocketContext = Context;
            string ipAddress = "";
            ipAddress += thiSocketContext.UserEndPoint.Address.ToString();
            Console.WriteLine($"IPADDRESS = {ipAddress}");
            _client = new WssvClient(ipAddress, e.Data);
            Console.WriteLine($"e.Data = {e.Data}");
            bool champExists = SqlQuerries.SearchChampion(e.Data);
            Console.WriteLine($"champExists = {champExists}");
            if (!champExists)
            {
                SqlQuerries.AddChamp(e.Data);
            }
            string id = SqlQuerries.GetChampIdFromDb(e.Data);
            string sendString = "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[\"" +
                        id + "\"],\r\n\t\"eventNames\":[\"Death\"]\r\n}";
            Console.WriteLine($"sendString = {sendString}");
            ws.Send(sendString);

            // ↓ sets name to client to match his querry, SWICH WITH E.DATA
            //_name = e.Data;
            SqlQuerries.AddQuerry(_client);
        }
        protected override void OnClose(CloseEventArgs e)
        {
            string id = SqlQuerries.GetChampIdFromDb(_client.Querry);
            string sendString = "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"clearSubscribe\",\r\n\t\"characters\":[\"" +
                       id + "\"],\r\n\t\"eventNames\":[\"Death\"]\r\n}";
            Console.WriteLine($"sendString = {sendString}");
            ws.Send(sendString);
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
        public SendKills(string prefix)
        {
            _prefix = !prefix.IsNullOrEmpty() ? prefix : "anon#";
        }
        private string getName()
        {
            var name = Context.QueryString["name"];
            return !name.IsNullOrEmpty() ? name : _prefix + getNumber();
        }
        private static int getNumber()
        {
            return Interlocked.Increment(ref _number);
        }
    }

    public class WssvClient
    {
        public string IpAddress { get; set; }
        public string Querry { get; set; }

        public WssvClient()
        {
        }

        public WssvClient(string ip, string querry)
        {
            IpAddress = ip;
            Querry = querry;
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