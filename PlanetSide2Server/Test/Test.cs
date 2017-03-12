using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using WebSocketSharp.Server;

namespace Test
{
    class Test
    {
        static void Main(string[] args)
        {
            DateTime timer = DateTime.Now;
            Console.WriteLine($"First: {timer:T}");
            while (true)
            {
                TimeSpan period = new TimeSpan(0,0,0,3);
                DateTime temp = DateTime.Now;
                if (temp > timer.Add(period))
                {
                    Console.WriteLine("timer ticked");
                    Console.WriteLine($"First: {timer:T}");
                    timer = temp;
                }
            }
            /* WebSocketServer wssv = new WebSocketServer(System.Net.IPAddress.Any, 4649);
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
             wssv.Stop();*/


            //Console.WriteLine(GetOnlineStatus("shigeruban"));

        }

        public static bool GetOnlineStatus(string name)
        {
            using (MySqlConnection db = new MySqlConnection())
            {
                string result = "";
                db.ConnectionString =
                //"Server=192.168.0.105;Database=PS2LS;Uid=orlin;Pwd=razor;connection timeout=30";
                "Server=192.168.0.106;Database=PS2LS;Uid=orlin;Pwd=razor;Pooling=false";
                try
                {
                    db.Open();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                try
                {
                    MySqlCommand searchCommand =
                        new MySqlCommand(
                            $"SELECT DISTINCT IsOnline FROM PS2LS.Champions  WHERE ChampNameLower= \'{name.ToLower()}\'", db);
                    var reader = searchCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        result += reader["IsOnline"].ToString();
                    }
                    Console.WriteLine($"Result is: {result}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                db.Close();
                if (result == "1")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}

