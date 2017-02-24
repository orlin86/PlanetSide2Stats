using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using System.Threading;
using System.Timers;

namespace ServerConsoleApp
{


    public class Server
    {
        public static WebSocket ws;
        //↓ Method related to the timer in Server.ws.Connect
        private static void SendAliveQuerry(object source, ElapsedEventArgs e)
        {
            string[] allIds = SqlQuerries.GetAllIds();

            foreach (var item in allIds)
            {
                string sendString =
            "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[" + item + "],\r\n\t\"eventNames\":[\"PlayerLogin\", \"PlayerLogout\"]\r\n}";
                System.Timers.Timer initTimer = new System.Timers.Timer();
                Server.ws.Send(sendString);
                Thread.Sleep(100);

            }
        }
        static void Main(string[] args)
        {
            ws = new WebSocket("wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167");
            if (!Server.ws.IsAlive)
            {
                Server.ws.Connect();
                if (Server.ws.IsAlive)
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
            string input = "";
            WebSocketServer wssv = new WebSocketServer(System.Net.IPAddress.Any, 4649);
#if DEBUG
            wssv.Log.Level = LogLevel.Trace;
#endif
            wssv.KeepClean = false;
            wssv.WaitTime = TimeSpan.FromSeconds(10);
            wssv.ReuseAddress = false;
            wssv.AddWebSocketService<SendKills>("/SendKills");
            wssv.Start();
            if (wssv.IsListening)
            {
                Console.WriteLine("Listening on port {0}, and providing WebSocket services:", wssv.Port);
                foreach (var path in wssv.WebSocketServices.Paths)
                    Console.WriteLine("- {0}", path);
            }
            while (input != "stop")
            {
                //↓ not working
                if (input=="sessions")
                {
                    foreach (var session in wssv.WebSocketServices["/SendKills"].Sessions.ActiveIDs)
                    {
                        Console.WriteLine(session);
                    }
                }
                if (input == "inactive")
                {
                    foreach (var session in wssv.WebSocketServices["/SendKills"].Sessions.InactiveIDs)
                    {
                        Console.WriteLine(session);
                    }
                }
                input = Console.ReadLine();
            }
            wssv.Stop();
        }
    }
}
