using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp.Server;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            WebSocketServer wssv = new WebSocketServer(System.Net.IPAddress.Any, 4649);
#if DEBUG
            wssv.WaitTime = TimeSpan.FromSeconds(10);
#endif
            wssv.KeepClean = true;
            wssv.ReuseAddress = true;
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

