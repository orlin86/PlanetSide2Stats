using System;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using WebSocketSharp.Net.WebSockets;
using WebSocketSharp.Server;

namespace ServerConsoleApp
{
    public class SendKills : WebSocketBehavior
    {
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
                Console.WriteLine("////");
                Console.WriteLine("////");
                Console.WriteLine("CONNECTED TO API");
                Console.WriteLine("////");
                Console.WriteLine("////");
            }
        }

        protected override void OnOpen()
        {
            ws.OnOpen += (sender, e) =>
            {
                Send("You are connected \r\n");
            };
            ws.OnMessage += (sender, e) =>
            {
                nf.Notify(new NotificationMessage
                {
                    Summary = "Planetside2 Api MSG",
                    Body = !e.IsPing ? e.Data : "Received a ping.",
                    Icon = "notification-message-im"
                });
                // ↓ sends to client data, containing death, !! SWICH WITH E.DATA
                if (e.Data.Contains(_name))
                {
                    Send(e.Data);
                    Console.WriteLine("//////");
                    Console.WriteLine("//////");
                    Console.WriteLine($"DATA SENT TO CLIENT {e.Data}");
                    Console.WriteLine("//////");
                    Console.WriteLine("//////");
                }
            };
            _name = getName();
            WebSocketContext thiSocketContext = this.Context;
            Console.WriteLine("//////");
            Console.WriteLine("//////");
            Console.WriteLine($"{thiSocketContext.UserEndPoint.Address.ToString()}");
            Console.WriteLine("//////");
            Console.WriteLine("//////");
        }

        protected override void OnMessage(MessageEventArgs e)
        {
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
            _name = e.Data;

        }

        protected override void OnClose(CloseEventArgs e)
        {

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

    public class SqlQuerries
    {
        public static bool SearchChampion(string name)
        {
            using (SqlConnection db = new SqlConnection())
            {
                string result = "";
                db.ConnectionString =
                    "Server=Orlin-Home\\SqlExpress;Database=LolServer;Trusted_Connection=true;connection timeout=30";
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
                    SqlCommand searchCommand = new SqlCommand($"SELECT DISTINCT * FROM Champions\r\nWHERE ChampNameLower=\'{name}\';", db);
                    var reader = searchCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        result += reader["ChampId"].ToString();
                    }
                    Console.WriteLine(reader);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                db.Close();
                if (result != "")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public static string GetChampIdFromDb(string name)
        {
            string result = "";
            using (SqlConnection db = new SqlConnection())
            {
                db.ConnectionString =
                    "Server=Orlin-Home\\SqlExpress;Database=LolServer;Trusted_Connection=true;connection timeout=30";
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
                    SqlCommand searchCommand = new SqlCommand($"SELECT DISTINCT * FROM Champions\r\nWHERE ChampName=\'{name}\';", db);
                    var reader = searchCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        result = reader["ChampId"].ToString();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                db.Close();
                return result;
            }
        }
        public static void AddChamp(string name)
        {
            var input = name.ToLower();
            string url = @"http://census.daybreakgames.com/s:3216732167/get/ps2:v2/character/?name.first_lower=" + input;
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Received response");
            }
            string data = response.Content.ReadAsAsync<JObject>().Result.ToString();
            Console.WriteLine(data);
            Champion thisChampion = JsonConvert.DeserializeObject<Champion>(data);
            Console.WriteLine("ThisChamp returned: " + thisChampion.returned);
            if (thisChampion.returned == 1)
            {
                foreach (var champ in thisChampion.character_list)
                {
                    using (SqlConnection db = new SqlConnection())
                    {
                        db.ConnectionString =
                    "Server=Orlin-Home\\SqlExpress;Database=LolServer;Trusted_Connection=true;connection timeout=30";
                        try
                        {
                            db.Open();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        SqlCommand addCommand = new SqlCommand($"INSERT INTO Champions\r\nVALUES (\'{champ.name.first}\', \'{champ.name.first_lower}\', \'{champ.character_id}\', \'0\')", db);
                        int rowsAffected = addCommand.ExecuteNonQuery();
                        Console.WriteLine(rowsAffected);
                        db.Close();
                    }
                }
            }
        }
    }
}