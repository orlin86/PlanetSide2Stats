using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MySql.Data.MySqlClient;


namespace ServerConsoleApp
{
    public class SqlQuerries
    {
        private static string connectionString;

        static SqlQuerries()
        {
            //connectionString ="Server=192.168.0.105;Database=PS2LS;Uid=orlin;Pwd=razor;connection timeout=30";
            connectionString = "Server=127.0.0.1;Database=PS2LS;Uid=root;Pwd=razor;Pooling=false";
        }

        public static bool SearchChampion(string name)
        {
            using (MySqlConnection db = new MySqlConnection())
            {
                string result = "";
                db.ConnectionString = connectionString;
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
                            $"SELECT DISTINCT ChampId FROM PS2LS.Champions WHERE ChampNameLower=@name;", db);
                    searchCommand.Parameters.AddWithValue("@name", name.ToLower());
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
                if (result!="")
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
            using (MySqlConnection db = new MySqlConnection())
            {
                db.ConnectionString = connectionString;
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
                            $"SELECT DISTINCT ChampId FROM PS2LS.Champions WHERE ChampNameLower=\'{name.ToLower()}\';",
                            db);
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
        public static bool AddChampByName(string name)
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
                    using (MySqlConnection db = new MySqlConnection())
                    {
                        db.ConnectionString = connectionString;
                        try
                        {
                            db.Open();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        MySqlCommand addCommand =
                            new MySqlCommand(
                                $"INSERT INTO PS2LS.Champions(ChampName, ChampNameLower, ChampId, FactionId, DateAdded) VALUES (\'{champ.name.first}\', \'{champ.name.first_lower}\',{champ.character_id}, \'{champ.faction_id}\', \'{DateTime.Now:yyyy-MM-dd HH:MM:ss}\' )",
                                db);
                        int rowsAffected = addCommand.ExecuteNonQuery();
                        Console.WriteLine($"Champ {name} Added to dB! Code: {rowsAffected}");
                        db.Close();
                    }
                }
                return true;
            }
            return false;
        }
        public static void AddQuerry(WssvClient client, bool disconnConn)
        {
            using (MySqlConnection db = new MySqlConnection())
            {
                db.ConnectionString = connectionString;
                try
                {
                    db.Open();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                if (disconnConn)
                {
                    MySqlCommand addCommand =
                    new MySqlCommand(
                        $"INSERT INTO PS2LS.Querries(`ClientIpAddress`, `ChampName`, `Time`, `DisconnConn`) VALUES (\'{client.IpAddress}\', \'{client.Querry}\', \'{DateTime.Now:yyyy-MM-dd HH:MM:ss}\', 1)",
                        db);
                    int rowsAffected = addCommand.ExecuteNonQuery();
                    Console.WriteLine($"Added Connected, Code {rowsAffected}");
                    db.Close();
                }
                else
                {
                    MySqlCommand addCommand =
                    new MySqlCommand(
                        $"INSERT INTO PS2LS.Querries(`ClientIpAddress`, `ChampName`, `Time`,`DisconnConn`) VALUES (\'{client.IpAddress}\', \'{client.Querry}\', \'{DateTime.Now:yyyy-MM-dd HH:MM:ss}\', 0)",
                        db);
                    int rowsAffected = addCommand.ExecuteNonQuery();
                    Console.WriteLine($"Added Disconnected, Code {rowsAffected}");
                    db.Close();
                }
            }
        }
        public static string[] GetAllIds()
        {
            using (MySqlConnection db = new MySqlConnection())
            {
                List<string> result = new List<string>();
                db.ConnectionString = connectionString;
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
                    MySqlCommand searchCommand = new MySqlCommand($"select ChampId from PS2LS.Champions", db);
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
        public static void ReplaceAlive(bool isAlive, string id)
        {
            if (isAlive)
            {
                using (MySqlConnection db = new MySqlConnection())
                {
                    int rowsAffected = 0;
                    db.ConnectionString = connectionString;
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
                        MySqlCommand addCommand =
                            new MySqlCommand($"UPDATE PS2LS.Champions SET IsOnline=1 WHERE ChampId=" + id, db);
                        rowsAffected = addCommand.ExecuteNonQuery();

                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    Console.WriteLine($"Set IsOnline=1! Code: {rowsAffected}");
                    db.Close();
                }
            }
            if (!isAlive)
            {
                using (MySqlConnection db = new MySqlConnection())
                {
                    int rowsAffected = 0;

                    db.ConnectionString = connectionString;
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
                        MySqlCommand addCommand =
                            new MySqlCommand($"UPDATE PS2LS.Champions SET IsOnline=0 WHERE ChampId=" + id, db);
                        rowsAffected = addCommand.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    Console.WriteLine($"Set IsOnline=0! Code: {rowsAffected}");
                    db.Close();
                }
            }
        }
        public static KeyValuePair<string,string> GetNameById(string id)
        {
            string resName = "";
            string resFiD = "";
            using (MySqlConnection db = new MySqlConnection())
            {
                db.ConnectionString = connectionString;
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
                            $"SELECT DISTINCT ChampName, FactionId FROM PS2LS.Champions WHERE ChampId=\'{id}\'\r\n; ", db);
                    var reader = searchCommand.ExecuteReader();
                    while (reader.Read())
                    {
                        resName = reader["ChampName"].ToString();
                        resFiD = reader["FactionId"].ToString();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                KeyValuePair<string, string> result = new KeyValuePair<string, string>(resName, resFiD);
                db.Close();
                return result;
            }
        }
        public static bool GetOnlineStatus(string name)
        {
            using (MySqlConnection db = new MySqlConnection())
            {
                string result = "";
                db.ConnectionString = connectionString;
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
        public static bool SearchById(string id)
        {
            string result = "";
            using (MySqlConnection db = new MySqlConnection())
            {
                db.ConnectionString = connectionString;
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
                            $"SELECT DISTINCT ChampName FROM PS2LS.Champions WHERE ChampId=\'{id}\'\r\n; ", db);
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
        public static void AddChampById(string id)
        {
            var input = id;
            string url = @"http://census.daybreakgames.com/s:3216732167/get/ps2:v2/character/?character_id=" + input;
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
                    using (MySqlConnection db = new MySqlConnection())
                    {
                        db.ConnectionString = connectionString;
                        try
                        {
                            db.Open();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
                        }
                        MySqlCommand addCommand =
                            new MySqlCommand(
                                 $"INSERT INTO PS2LS.Champions(ChampName, ChampNameLower, ChampId, FactionId, DateAdded) VALUES (\'{champ.name.first}\', \'{champ.name.first_lower}\',{champ.character_id}, \'{champ.faction_id}\', \'{DateTime.Now:yyyy-MM-dd HH:MM:ss}\' )",
                                db);
                        int rowsAffected = addCommand.ExecuteNonQuery();
                        Console.WriteLine($"Champ {champ.name.first} added, Code: {rowsAffected}");
                        db.Close();
                    }
                }
            }
        }
    }
}
