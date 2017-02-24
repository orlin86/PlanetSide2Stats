using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerConsoleApp
{
    class Champion
    {
        public Character_List[] character_list { get; set; }
        public int returned { get; set; }
        
        public class Character_List
        {
            public string character_id { get; set; }
            public Name name { get; set; }
            public string faction_id { get; set; }
            public string head_id { get; set; }
            public string title_id { get; set; }
            public Times times { get; set; }
            public Certs certs { get; set; }
            public Battle_Rank battle_rank { get; set; }
            public string profile_id { get; set; }
            public Daily_Ribbon daily_ribbon { get; set; }
        }

        public class Name
        {
            public string first { get; set; }
            public string first_lower { get; set; }
        }

        public class Times
        {
            public string creation { get; set; }
            public string creation_date { get; set; }
            public string last_save { get; set; }
            public string last_save_date { get; set; }
            public string last_login { get; set; }
            public string last_login_date { get; set; }
            public string login_count { get; set; }
            public string minutes_played { get; set; }
        }

        public class Certs
        {
            public string earned_points { get; set; }
            public string gifted_points { get; set; }
            public string spent_points { get; set; }
            public string available_points { get; set; }
            public string percent_to_next { get; set; }
        }

        public class Battle_Rank
        {
            public string percent_to_next { get; set; }
            public string value { get; set; }
        }

        public class Daily_Ribbon
        {
            public string count { get; set; }
            public string time { get; set; }
            public string date { get; set; }
        }

    }
}
