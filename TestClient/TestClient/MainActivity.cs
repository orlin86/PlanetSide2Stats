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
            Button testIfOnline = FindViewById<Button>(Resource.Id.testIfOnlineButton);
            Button disconnecButton = FindViewById<Button>(Resource.Id.DisconnectButton);
            TextView dataBig = FindViewById<TextView>(Resource.Id.DataBig);
            TextView dataSmall = FindViewById<TextView>(Resource.Id.DataSmall);


            //// Wbsocket Params
            //WebSocket ws = new WebSocket("wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167");

            string champId = "";
            WebSocket ws = new WebSocket("ws://92.247.240.220:4649/SendKills");
            ws.EmitOnPing = false;
            
            ThreadPool.QueueUserWorkItem(o => ws.OnOpen += (sender, e) =>
            {
                    RunOnUiThread(() => dataBig.Text += "  Data Sent  " + inputName);
                    ws.Send(inputName.Text);                
            });
            ThreadPool.QueueUserWorkItem(o => ws.OnMessage += (sender, e) =>
             {
                 RunOnUiThread(() => dataSmall.Text += e.Data + "\r\n");
             });

            ws.OnError += (sender, e) =>
            {
                //dataBig.Text = e.Exception.ToString();
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
                string data = "";
                ////ENTER METHOD HERE!
                data += await GetIdAsync(url);
                string result = FindChampId(data);
                ////
                ////Enter result here
                RunOnUiThread(() => dataSmall.Text =($"Char_ID = {result}" ));
                champId = result;
            };

            testIfOnline.Click += async (object sender, EventArgs er) =>
            {
                if (champId != "")
                {
                    string url =
                        @"http://census.daybreakgames.com/s:3216732167/get/ps2:v2/characters_online_status/?character_id=" +
                        champId;
                    string data = "";
                    data += await GetIdAsync(url);
                    string result = CheckIfOnline(data);
                    RunOnUiThread(() => dataSmall.Text = ($"{inputName.Text} is {result}"));
                }
            };
            
            connectButton.Click += (object senderer, EventArgs eer) =>
       {

           //↓Connect to WebSocketServer
           ThreadPool.QueueUserWorkItem(o => WSConnect(ws));
           //↓Send result from testNameButton
           
           ///////////////////////////
           /*if (ws.IsAlive)
           {
               RunOnUiThread(() => dataBig.Text = "Connection Alive");
           }*/
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
                string data = response.Content.ReadAsStringAsync().Result;
                return (data);
            }
            else
            {
                return ("Err");
            }
        }

        private static string FindChampId(string data)
        {
            string[] temp = data.Split(new char[] { '{', ',' }).ToArray();
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
            return result;
        }

        private static string CheckIfOnline(string data)
        {
            string[] temp = data.Split(new char[] { '{', ',' }).ToArray();
            string temp2 = "";
            foreach (var item in temp)
            {
                if (item.Contains("online_status"))
                {
                    temp2 = item;
                }
            }
            Regex reg = new Regex("\\d");
            Match mat = reg.Match(temp2);
            string result = "Err";
            if (mat.Success)
            {
                if (mat.Value == "1")
                {
                    result = "Online";

                }
                if (mat.Value == "0")
                {
                    result = "Offline";
                }
            }
            return result;
        }
    }
}

