using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServerConsoleApp
{
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
        public static void AddQuerry(WssvClient client)
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
                SqlCommand addCommand = new SqlCommand($"INSERT INTO Querries\r\nVALUES (\'{client.IpAddress}\', \'{client.Querry}\', \'{DateTime.Now}\')", db);
                int rowsAffected = addCommand.ExecuteNonQuery();
                Console.WriteLine(rowsAffected);
                db.Close();
            }
        }
        public static string[] GetAllIds()
        {
            using (SqlConnection db = new SqlConnection())
            {
                List<string> result = new List<string>();
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
                    SqlCommand searchCommand = new SqlCommand($"SELECT ChampId FROM Champions", db);
                    var reader = searchCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        result.Add(reader["ChampId"].ToString());
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                db.Close();
                string[] res = result.ToArray();
                for (int i = 0; i < res.Length; i++)
                {
                    res[i] = res[i].Insert(0, "\"");
                    res[i] = res[i] + "\"";
                }
                return res;
            }
        }
        public static void ReplaceAlive(bool isAlive,string id)
        {
            if (isAlive)
            {
                using (SqlConnection db = new SqlConnection())
                {
                    int rowsAffected = 0;
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
                        SqlCommand addCommand = new SqlCommand($"UPDATE Champions\r\nSET IsOnline=1\r\nWHERE ChampId=\'" + id + "\'", db);
                         rowsAffected = addCommand.ExecuteNonQuery();

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    Console.WriteLine(rowsAffected);
                    db.Close();
                }
            }
            if (!isAlive)
            {
                using (SqlConnection db = new SqlConnection())
                {int rowsAffected = 0;

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
                        SqlCommand addCommand = new SqlCommand($"UPDATE Champions\r\nSET IsOnline=0\r\nWHERE ChampId=\'" + id + "\'", db);
                        rowsAffected = addCommand.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    Console.WriteLine(rowsAffected);
                    db.Close();
                }
            }
        }
        public static string GetNameById(string id)
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
                    SqlCommand searchCommand = new SqlCommand($"SELECT DISTINCT ChampName FROM Champions\r\nWHERE ChampId=\'{id}\'\r\n; ", db);
                    var reader = searchCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        result = reader["ChampName"].ToString();
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
    }
}
