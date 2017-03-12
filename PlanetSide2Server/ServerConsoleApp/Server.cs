using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Security.Cryptography.X509Certificates;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using System.Threading;
using System.Timers;
using System.Xml;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace ServerConsoleApp
{
    //ARGS:
    // "log" - Logs all e.data to IncomingNotifications.txt !not recommended
    // "showN" Shows API e.data msgs in terminal
    // "all" sends Querry to Api with ALL ids in the dB; not recommended

    // Send Codes ( from SendKills.cs )
    //↓ Code 01 - Client Logged ON
    //↓ Code 02 - Client Logged OFF
    //↓ Code 03 - Client KILLED someone
    //↓ Code 04 - Client Died by someone
    //↓ Code 05 - Character with such name does not exist! Disconnecting client!

    public class Server
    {
        public static WebSocket ws;
        public static WebSocketServer wssv;
        public static Dictionary<string, string> Factions;
        public static List<string> ClientsSendstrings;
        static Server()
        {
            ws = new WebSocket("wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167");
            wssv = new WebSocketServer(System.Net.IPAddress.Any, 4649);
            Factions = new Dictionary<string, string>()
            {
                { "1", "VS"},
                { "2", "NC"},
                { "3", "TR"}
            };
            ClientsSendstrings = new List<string>();
        }

        //↓ Method related to the timer in Server.ws.Connect
        private static void SendAllQuerry(object source, ElapsedEventArgs e)
        {
            string[] allIds = SqlQuerries.GetAllIds();
            foreach (var item in allIds)
            {
                if (ws.IsAlive)
                {
                    string sendString =
            "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[" + item + "],\r\n\t\"eventNames\":[\"Deaths\", \"PlayerLogin\", \"PlayerLogout\"]\r\n}";
                    ws.Send(sendString);
                    //Thread.Sleep(300);
                }
                else
                {
                    break;
                }
            }
        }
        private static void SendAliveQuerry(object source, ElapsedEventArgs e)
        {
            string[] allIds = SqlQuerries.GetAllIds();
            foreach (var item in allIds)
            {
                if (ws.IsAlive)
                {
                    string sendString =
            "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[" + item + "],\r\n\t\"eventNames\":[ \"PlayerLogin\", \"PlayerLogout\"]\r\n}";
                    ws.Send(sendString);
                    //Thread.Sleep(300);
                }
                else
                {
                    break;
                }
            }
        }
        static void Main(string[] args)
        {
            DateTime msgTime = DateTime.Now;
            bool wsIsOnline = false;
            string input = "";
#if DEBUG
            wssv.Log.Level = LogLevel.Trace;
#endif
            wssv.KeepClean = true;
            wssv.WaitTime = TimeSpan.FromSeconds(120);
            wssv.ReuseAddress = false;
            wssv.AddWebSocketService<SendKills>("/SendKills");

            ws.OnOpen += (sender, e) =>
            {
                wsIsOnline = true;
            };
            ws.OnMessage += (sender, e) =>
            {
                msgTime = DateTime.Now;
                // ↓ If args == log => Logs all e.data to IncomingNotifications.txt
                foreach (var arg in args)
                {
                    if (arg == "log")
                    {
                        NotifierLogger(e);
                    }
                }
                foreach (var arg in args)
                {
                    // ↓ If args == showN => Shows API e.data msgs in terminal
                    if (arg == "showN")
                    {
                        Console.WriteLine(e.Data);
                    }
                }

                if (e.Data.Contains("heartbeat"))
                {
                    wssv.WebSocketServices["/SendKills"].Sessions.Broadcast(e.Data);
                }
                if (e.Data.Contains("payload"))
                {
                    if (e.Data.Contains("PlayerLogin"))
                    {
                        // ↓ Replaces Login in dB   
                        LoginMsg thisMsg = JsonConvert.DeserializeObject<LoginMsg>(e.Data);
                        SqlQuerries.ReplaceAlive(true, thisMsg.payload.character_id.ToString());
                    }
                    else if (e.Data.Contains("PlayerLogout"))
                    {
                        // ↓ Replaces Logout in dB
                        LogoutMsg thisMsg = JsonConvert.DeserializeObject<LogoutMsg>(e.Data);
                        SqlQuerries.ReplaceAlive(false, thisMsg.payload.character_id.ToString());
                    }
                    else if (e.Data.Contains("Death"))
                    {
                        // ↓ Checks if champId exists in dB, if not adds it
                        DeathMsg thisMsg = JsonConvert.DeserializeObject<DeathMsg>(e.Data);
                        if (!thisMsg.payload.attacker_character_id.IsNullOrEmpty())
                        {
                            if (!SqlQuerries.SearchById(thisMsg.payload.character_id))
                            {
                                SqlQuerries.AddChampById(thisMsg.payload.character_id);
                                Console.WriteLine($"Added Champ to dB: {SqlQuerries.GetNameById(thisMsg.payload.character_id)}");
                                foreach (var arg in args)
                                {
                                    if (arg == "all")
                                    {
                                        SendAliveToApi(thisMsg.payload.character_id);
                                    }
                                }
                            }
                            else if (!SqlQuerries.SearchById(thisMsg.payload.attacker_character_id))
                            {
                                SqlQuerries.AddChampById(thisMsg.payload.attacker_character_id);
                                Console.WriteLine($"Added Champ to dB: {SqlQuerries.GetNameById(thisMsg.payload.attacker_character_id)}");
                                foreach (var arg in args)
                                {
                                    if (arg == "all")
                                    {
                                        SendAliveToApi(thisMsg.payload.attacker_character_id);
                                    }
                                }
                            }
                        }
                    }
                }
            };
            ws.OnClose += (sender, eventArgs) =>
            {
                StreamWriter error = new StreamWriter(@"error.txt", true);
                error.WriteLine($"{DateTime.Now}: Closed ! {sender}, Was clean: {eventArgs.WasClean}, Reason: {eventArgs.Reason}");
                error.Close();
                //Thread.Sleep(10000);
                wsIsOnline = false;
            };
            ws.OnError += (sender, eventArgs) =>
            {
                StreamWriter error = new StreamWriter(@"error.txt", true);
                error.WriteLine($"{DateTime.Now}: ERROR ! {sender}, Exception: {eventArgs.Message}, Message: {eventArgs.Message}");
                error.Close();
                //Thread.Sleep(10000);
                wsIsOnline = false;
            };

            if (!ws.IsAlive)
            {
                ws.Connect();
                if (ws.IsAlive)
                {
                    wsIsOnline = true;
                    Console.WriteLine("OPEN !!!!!!!!");
                    Console.WriteLine("OPEN !!!!!!!!");
                    Console.WriteLine("OPEN !!!!!!!!");
                    Console.WriteLine("OPEN !!!!!!!!");
                    Console.WriteLine("OPEN !!!!!!!!");

                    // ↓ if args = all sends Querry to Api with ALL ids in the dB; not recommended
                    bool all = false;
                    foreach (var arg in args)
                    {
                        if (arg == "all")
                        {
                            System.Timers.Timer initTimer = new System.Timers.Timer
                            {
                                Interval = 1000,
                                AutoReset = false
                            };
                            initTimer.Elapsed += SendAllQuerry;
                            initTimer.Enabled = true;
                            all = true;
                        }
                    }
                    if (!all)
                    {
                        System.Timers.Timer initTimer = new System.Timers.Timer
                        {
                            Interval = 1000,
                            AutoReset = false
                        };
                        initTimer.Elapsed += SendAliveQuerry;
                        initTimer.Enabled = true;
                    }
                }
            }

            wssv.Start();
            if (wssv.IsListening)
            {
                Console.WriteLine("Listening on port {0}, and providing WebSocket services:", wssv.Port);
                foreach (var path in wssv.WebSocketServices.Paths)
                    Console.WriteLine("- {0}", path);
            }

            while (input != "stop")
            {
                if (DateTime.Now > msgTime.AddMinutes(5))
                {
                    wsIsOnline = false;
                }
                if (input == "sessions")
                {
                    foreach (var session in wssv.WebSocketServices["/SendKills"].Sessions.ActiveIDs)
                    {
                        Console.WriteLine(session);
                    }
                }
                if (input == "scount")
                {
                    Console.WriteLine($"Number of connected users: {wssv.WebSocketServices["/SendKills"].Sessions.ActiveIDs.Count()}");
                }
                if (input == "apistatus")
                {
                    if (ws.IsAlive)
                    {
                        Console.WriteLine($"PS2 Api Connected");
                    }
                    else
                    {
                        Console.WriteLine($"PS2 Api NOT Connected");
                    }
                }
                if (input == "help")
                {
                    Console.WriteLine("sessions - Shows ActiveIds");
                    Console.WriteLine("scount - Shows count of ActiveIds");
                    Console.WriteLine("apistatus - Shows if Api is Connected");
                    Console.WriteLine("wsstatus - shows bool parameter wsIsOnline's value");
                    Console.WriteLine("wsclose - closes ws' connection");
                }
                if (input == "wsclose")
                {
                    ws.Close();
                }
                if (input == "wsstatus")
                {
                    Console.WriteLine($"wsIsOnline status: {wsIsOnline}");
                }

                wsIsOnline = WsReconnect(args, wsIsOnline);

                input = Console.ReadLine();
            }
            wssv.Stop();
        }

        private static bool WsReconnect(string[] args, bool wsIsOnline)
        {
            if (!wsIsOnline)
            {
                if (!ws.IsAlive)
                {
                    ws.Connect();
                    if (ws.IsAlive)
                    {
                        wsIsOnline = true;
                        Console.WriteLine("OPEN !!!!!!!!");
                        Console.WriteLine("OPEN !!!!!!!!");
                        Console.WriteLine("OPEN !!!!!!!!");
                        Console.WriteLine("OPEN !!!!!!!!");
                        Console.WriteLine("OPEN !!!!!!!!");

                        // ↓ if args = all sends Querry to Api with ALL ids in the dB; not recommended
                        // ↓ if !=all sends only client's querry
                        bool allArg = false;
                        foreach (var arg in args)
                        {
                            if (arg == "all")
                            {
                                allArg = true;
                                System.Timers.Timer initTimer = new System.Timers.Timer
                                {
                                    Interval = 1000,
                                    AutoReset = false
                                };
                                initTimer.Elapsed += SendAllQuerry;
                                initTimer.Enabled = true;
                            }
                        }
                        if (!allArg)
                        {
                            foreach (var sendString in ClientsSendstrings)
                            {
                                ws.Send(sendString);
                            }
                        }
                    }
                }
            }

            return wsIsOnline;
        }
        private static void SendAliveToApi(string thisChampId)
        {
            string sendAlive =
                    "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[" + thisChampId + "],\r\n\t\"eventNames\":[\"PlayerLogin\", \"PlayerLogout\"]\r\n}";
            ws.Send(sendAlive);
        }
        private static void NotifierLogger(MessageEventArgs e)
        {
            string path = @"..\IncNotifications.txt";
            File.AppendAllText(path, $"{DateTime.Now}: {e.Data}");
        }
    }
}
