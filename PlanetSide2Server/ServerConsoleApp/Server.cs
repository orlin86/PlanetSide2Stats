using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Net.Sockets;
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
#endif
            wssv.KeepClean = false;
            wssv.AddWebSocketService<SendKills>("/SendKills");
            wssv.Start();
            if (wssv.IsListening)
            {
                Console.WriteLine("Listening on port {0}, and providing WebSocket services:", wssv.Port);
                foreach (var path in wssv.WebSocketServices.Paths)
                    Console.WriteLine("- {0}", path);

            }
            Console.ReadLine();
            wssv.Stop();
        }
    }
}
