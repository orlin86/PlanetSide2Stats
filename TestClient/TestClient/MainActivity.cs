using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Webkit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.Json;
using WebSocketSharp;



namespace TestClient
{
    [Activity(Label = "TestClient", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            EditText inputName = FindViewById<EditText>(Resource.Id.InputName);
            Button testNameButton = FindViewById<Button>(Resource.Id.TestNameButton);
            Button connectButton = FindViewById<Button>(Resource.Id.ConnectButton);
            Button disconnecButton = FindViewById<Button>(Resource.Id.DisconnectButton);
            TextView dataBig = FindViewById<TextView>(Resource.Id.DataBig);
            TextView dataSmall = FindViewById<TextView>(Resource.Id.DataSmall);

            WebSocket ws = new WebSocket("wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167");


            ThreadPool.QueueUserWorkItem(o => ws.OnOpen += (sender, e) => ws.Send("Hi, there!"));

            ThreadPool.QueueUserWorkItem(o => ws.OnMessage += (sender, e) =>
             {
                 RunOnUiThread(() => dataSmall.Text += e.Data + "\r\n");
             });
            ws.OnError += (sender, e) =>
            {
                dataBig.Text = e.Exception.ToString();
            };

            ws.OnClose += (sender, e) =>
            {
                dataBig.Text = "Connection Closed";
            };


            //// Enter rest querry to PS2 API
            testNameButton.Click += async (object sender, EventArgs er) =>
            {
                var input = inputName.Text.ToLower();
                string url = @"http://census.daybreakgames.com/s:3216732167/get/ps2:v2/character/?name.first_lower=" + input;

                string result = "";
                ////ENTER METHOD HERE!
                RunOnUiThread(() => dataBig.Text = url);
                result += await GetIdAsync(url);
                RunOnUiThread(() => dataBig.Text = "RECEIVED DATA");
                ////
                ////Enter result here
                RunOnUiThread(() => dataSmall.Text =($"Char_ID = {result}" ));
            };



            connectButton.Click += (object senderer, EventArgs eer) =>
       {

           //////////////////////////// 
           ThreadPool.QueueUserWorkItem(o => WSConnect(ws));
           ///////////////////////////
           if (ws.IsAlive)
           {
               RunOnUiThread(() => dataBig.Text = "Connection Alive");
           }
       };

            disconnecButton.Click += (object sender, EventArgs er) =>
            {
                if (ws.IsAlive)
                {
                    ws.Close();
                    dataBig.Text = "Connection Closed";
                }
                else
                {
                    dataBig.Text = "Not Connected";
                }
            };
        }

        private void WSConnect(WebSocket ws)
        {
            ws.Connect();
        }

        private async Task<string> GetIdAsync(string url)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                Character_List root = new Character_List();
                string data = response.Content.ReadAsStringAsync().Result;
                string[] temp = data.Split(new char[] {'{', ','}).ToArray();
                string temp2 = "";
                foreach (var item in temp)
                {
                    if (item.Contains("character_id"))
                    {
                        temp2 = item;
                    }
                }    
                Regex reg = new Regex("\\d+");
                Match mat = reg.Match(temp2);
                string result = mat.Value;
                return (result);
            }
            else
            {
                return ("Err");
            }
        }

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

