using System;
using System.Collections.Generic;
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

        public void ProcessMessage(Object sender, MessageEventArgs e)
        {
            DeathNotifier(e);
            LoginLogoutNotifier(e);
        }
        public SendKills()
        {
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
                if (SqlQuerries.AddChampByName(e.Data))
                {
                    SendAliveQuerryToApi(e);
                }
                else if (!SqlQuerries.AddChampByName(e.Data))
                {
                    //↓ Code 05 - Character with such name does not exist! Disconnecting client!
                    Send($"05Champion, named: {e.Data} does not exist!");
                }
            }
            string id = SqlQuerries.GetChampIdFromDb(e.Data);
            string sendString = "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[\"" +
                        id + "\"],\r\n\t\"eventNames\":[\"Death\", \"PlayerLogin\", \"PlayerLogout\"]\r\n}";
            Console.WriteLine($"sendString = {sendString}");
            Server.ws.Send(sendString);
            Server.ClientsSendstrings.Add(sendString);
            _client = new WssvClient(ipAddress, e.Data, id);
            Console.WriteLine($"Client IP:{_client.IpAddress}");
            Console.WriteLine($"Client Querry:{_client.Querry}");
            Console.WriteLine($"Client QuerryID:{_client.QuerryId}");
            Send($"ClientIdIs:{_client.QuerryId}");

            bool isOnline = SqlQuerries.GetOnlineStatus(_client.Querry.ToLower());
            if (isOnline)
            {
                Send($"01{_client.Querry} Online");
                SqlQuerries.ReplaceAlive(true, _client.QuerryId);
            }
            else if (!isOnline)
            {
                Send($"02{_client.Querry} Offline");
                SqlQuerries.ReplaceAlive(false, _client.QuerryId);
            }

            // ↓ sets name to client to match his querry
            SqlQuerries.AddQuerry(_client,true);
        }
        protected override void OnClose(CloseEventArgs e)
        {
            if (null != _client)
            {
                SqlQuerries.AddQuerry(_client, false);
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
        private static void SendAliveQuerryToApi(MessageEventArgs e)
        {
            string thisChampId = SqlQuerries.GetChampIdFromDb(e.Data);
            string sendAlive =
        "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[" + thisChampId + "],\r\n\t\"eventNames\":[\"PlayerLogin\", \"PlayerLogout\"]\r\n}";
            Server.ws.Send(sendAlive);
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
                    if (_client.QuerryId!=null)
                    {
                        if (thisMsg.payload.character_id == _client.QuerryId)
                        {
                            //↓ Code 01 - Client Logged ON
                            Console.WriteLine($"{DateTime.Now}: Sending PlayerLogin To Client {_client.IpAddress}");
                            Send($"01{_client.Querry} Online");
                        }
                    }
                }
            }
            else if (e.Data.Contains("PlayerLogout") && e.Data.Contains("payload"))
            {
                LogoutMsg thisMsg = JsonConvert.DeserializeObject<LogoutMsg>(e.Data);
                if (null != _client.QuerryId)
                {
                    if (thisMsg.payload.character_id == _client.QuerryId)
                    {
                        //↓ Code 02 - Client Logged OFF
                        Console.WriteLine($"{DateTime.Now}: Sending PlayerLogout To Client {_client.IpAddress}");
                        Send($"02{_client.Querry} Offline");
                    }
                }
            }
        }
        private void DeathNotifier(MessageEventArgs e)
        {
            if (e.Data.Contains("Death") && e.Data.Contains("payload"))
            {
                DeathMsg thisMsg = JsonConvert.DeserializeObject<DeathMsg>(e.Data);
                if (!thisMsg.payload.attacker_character_id.IsNullOrEmpty())
                {
                    if (thisMsg.payload.attacker_character_id == _client.QuerryId)
                    {
                        // ↓ SqlQuerry returns KVP - Key: Name Value: FactionId
                        var victim = SqlQuerries.GetNameById(thisMsg.payload.character_id);
                        var attacker = SqlQuerries.GetNameById(_client.QuerryId);
                        if (attacker.Value != victim.Value && attacker.Key != victim.Key)
                        {

                            //↓ Code 03 - Client KILLED enemy
                            Send($"03{_client.Querry} killed {victim.Key} ( {Server.Factions[victim.Value]} )");
                            Console.WriteLine($"Sending kill (#03): {_client.Querry} killed {victim.Key} ( {Server.Factions[victim.Value]} )");
                        }
                        else if (attacker.Value == victim.Value&&attacker.Key!=victim.Key)
                        {
                            //↓ Code 06 - Client KILLED Ally!
                            Send($"06{_client.Querry} killed ALLY {victim.Key}");
                            Console.WriteLine($"Sending ally kill (#06): {_client.Querry} killed {victim.Key}");
                        }


                    }
                    else if (thisMsg.payload.character_id == _client.QuerryId)
                    {
                        var victim = SqlQuerries.GetNameById(thisMsg.payload.character_id);
                        var attacker = SqlQuerries.GetNameById(thisMsg.payload.attacker_character_id);
                        if (victim.Key!= attacker.Key)
                        {
                            //↓ Code 04 - Client Died by someone
                            var killer = SqlQuerries.GetNameById(thisMsg.payload.attacker_character_id);
                            Send($"04{_client.Querry} died by {killer.Key} ( {Server.Factions[killer.Value]} )");
                            Console.WriteLine($"Sending kill (#03): {_client.Querry} killed {killer.Key}");
                        }
                        else if (victim.Key==attacker.Key)
                        {
                            //↓ Code 04 - Client killed himself
                            Send($"04{_client.Querry} killed himself");
                            Console.WriteLine($"Sending kill (#04): {_client.Querry} killed himself");
                        }
                        
                    }
                }
            }
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