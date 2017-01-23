using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
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
                foreach (var item in result)
                {
                    Console.WriteLine(item);
                }
            }*/

            ReplaceAlive(true, 8250613664849594641.ToString());

        }
        public static void ReplaceAlive(bool isAlive, string id)
        {
            if (isAlive)
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
                    SqlCommand addCommand =
                        new SqlCommand($"UPDATE Champions\r\nSET IsOnline=1\r\nWHERE ChampId=\'" + id + "\'", db);
                    int rowsAffected = addCommand.ExecuteNonQuery();
                    Console.WriteLine(rowsAffected);
                    db.Close();
                }
            }
            if (!isAlive)
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
                    SqlCommand addCommand =
                        new SqlCommand($"UPDATE Champions\r\nSET IsOnline=0\r\nWHERE ChampId=\'" + id + "\';", db);
                    int rowsAffected = addCommand.ExecuteNonQuery();
                    Console.WriteLine(rowsAffected);
                    db.Close();
                }
            }
        }
    }
}
