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
            }
        }
    }
}
