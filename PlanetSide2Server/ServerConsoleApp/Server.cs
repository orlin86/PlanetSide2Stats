using System;
using System.Configuration;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;
using System.Threading;

namespace ServerConsoleApp
{
    

    public class Server
    {
        static void Main(string[] args)
        {
            WebSocketServer wssv = new WebSocketServer(System.Net.IPAddress.Any, 4649);
#if DEBUG
            wssv.Log.Level = LogLevel.Trace;
            wssv.WaitTime = TimeSpan.FromSeconds(10);
#endif
            wssv.AddWebSocketService<SendKills>("/SendKills");

            //var nf = new Notifier();
            //var ws = new WebSocket("wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167");

            ////
            wssv.Start();
            if (wssv.IsListening)
            {
                Console.WriteLine("Listening on port {0}, and providing WebSocket services:", wssv.Port);
                foreach (var path in wssv.WebSocketServices.Paths)
                    Console.WriteLine("- {0}", path);
            }
            
            ////
            Console.ReadLine();
            wssv.Stop();

        }                       
    }
    /*
            WsServClient conn = new WsServClient();
            conn.SetClientUrl("wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167");
            conn.SetServerUrl(System.Net.IPAddress.Any);
            conn.SetClienParams();
            conn.SetServerParams();

            //conn.StartClient();
            conn.StartServer();

            Console.ReadLine();
            //stop servers;
            conn.Stop();
        }

    */
        

        /* public static void SendData(string names)
         {
             var ws = new WebSocket("wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167");
             var nf = new Notifier();
             WsClientParams(nf, ws);
             ws.Connect();
             Thread.Sleep(1000);
             TimeSpan wait = new TimeSpan(0, 0, 10);
             Thread.Sleep(wait);
             string sendString = "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[\"" +
                               names +
                               "\"],\r\n\t\"eventNames\":[\"Death\"]\r\n}";
             Console.WriteLine("sending string" + sendString);
             ws.SendDataToClient(sendString);
         }
         */
    

    public class WsServClient
    {
        private string ClientUrl;
        private System.Net.IPAddress ServerIP;

        public WebSocket Client { get; set; }
        public WebSocketServer Server { get; set; }

        public WsServClient()
        {

        }

        public WsServClient(string clientUrl, System.Net.IPAddress serverIp)
        {
            ClientUrl = clientUrl;
            ServerIP = serverIp;
        }

        public void SetClientUrl(string clientUrl)
        {
            ClientUrl = clientUrl;
            Client = new WebSocket(ClientUrl);
        }

        public void SetServerUrl(System.Net.IPAddress serverIP)
        {
            ServerIP = serverIP;
            Server = new WebSocketServer(ServerIP, 4649);
        }

        public void StartClient()
        {
            Client.Connect();
            Thread.Sleep(1000);
            TimeSpan wait = new TimeSpan(0, 0, 10);
            Thread.Sleep(wait);
        }

        public void StartServer()
        {
            Server.Start();
        }

        public void SetClienParams()
        {
            var nf = new Notifier();

            Client.OnOpen += (sender, e) =>
            {
                Client.Send("Hi, there!");
                Console.WriteLine("Client started");
            };
            ////
            Client.OnMessage += (sender, e) =>
            {
                nf.Notify(new NotificationMessage
                {
                    Summary = "WebSocket Message",
                    Body = !e.IsPing ? e.Data : "Received a ping.",
                    Icon = "notification-message-im"
                });

                Server.WebSocketServices.Broadcast(e.Data);
            };

            Client.OnError += (sender, e) => nf.Notify(new NotificationMessage
            {
                Summary = "WebSocket Error",
                Body = e.Message,
                Icon = "notification-message-im"
            });

            Client.OnClose += (sender, e) =>
                nf.Notify(
                    new NotificationMessage
                    {
                        Summary = String.Format("WebSocket Close ({0})", e.Code),
                        Body = e.Reason,
                        Icon = "notification-message-im"
                    }
                );
#if DEBUG
            // To change the logging level.
            Client.Log.Level = LogLevel.Trace;
            Client.WaitTime = TimeSpan.FromSeconds(10);
#endif
        }

        public void SetServerParams()
        {
            Server.AddWebSocketService<SendKills>("/SendKills");

        }

        public void SendDataToClient(string data)
        {
            string sendString = "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[\"" +
                           data +
                           "\"],\r\n\t\"eventNames\":[\"Death\"]\r\n}";
            Console.WriteLine("sending string" + sendString);
            Client.Send(sendString);
        }

        public void Stop()
        {
            Client.Close();
            Server.Stop();
        }


    }
}