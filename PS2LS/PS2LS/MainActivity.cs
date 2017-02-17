using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Views;
using Newtonsoft.Json;
using WebSocketSharp;
using System.Timers;


namespace PS2LS
{
    [Activity(Label = "PS2LS", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]

    public class MainActivity : Activity
    {
        TimeSpan stopWatch;
        private System.Timers.Timer timer;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);


            this.Window.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
            EditText inputName = FindViewById<EditText>(Resource.Id.InputName);
            Button connectButton = FindViewById<Button>(Resource.Id.ConnectButton);
            TextView kills = FindViewById<TextView>(Resource.Id.Kills);
            TextView deaths = FindViewById<TextView>(Resource.Id.Deaths);
            TextView isOnline = FindViewById<TextView>(Resource.Id.IsOnline);
            TextView timePlayed = FindViewById<TextView>(Resource.Id.TimePlayed);
            TextView smallText = FindViewById<TextView>(Resource.Id.SmallText);

            WebSocket ws = new WebSocket("ws://92.247.240.220:4649/SendKills");
            ws.EmitOnPing = true;
            ws.WaitTime = TimeSpan.FromSeconds(10);

            int Kills = 0;
            int Deaths = 0;
            string ChampId = "";
            List<string> text = new List<string>();
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += (senderer, er) =>
            {
                TimeSpan sec = new TimeSpan(0, 0, 0, 1);
                stopWatch = stopWatch.Add(sec);
                RunOnUiThread(() => { timePlayed.Text = $"{stopWatch}"; });
            };

            ThreadPool.QueueUserWorkItem(o => ws.OnOpen += (sender, e) =>
            {
                text.Insert(0, "Connected");
                if (text.Count >= 5)
                {
                    text.Remove(text.Last());
                }
                RunOnUiThread(() => smallText.Text = string.Join("\r\n", text));
                ws.Send(inputName.Text);
            });
            RunOnUiThread(() => ws.OnMessage += (sender, e) =>
            {
                if (e.IsPing)
                {
                    text.Insert(0, "Ping");
                    if (text.Count >= 5)
                    {
                        text.Remove(text.Last());
                    }
                    RunOnUiThread(() => smallText.Text = $"{DateTime.Now}: {string.Join("\r\n", text)}");
                }
                if (e.IsText)
                {
                    ws.Ping();
                    if (e.Data.Contains("ClientIdIs:"))
                    {
                        ChampId = e.Data.Remove(0, 11);
                        text.Insert(0, $"{DateTime.Now:HH:mm:ss}: ChampId is {ChampId}");
                        if (text.Count >= 5)
                        {
                            text.Remove(text.Last());
                        }
                        RunOnUiThread(() => smallText.Text = string.Join("\r\n", text));
                    }
                    if (e.Data.Contains("Death") && e.Data.Contains("payload"))
                    {
                        DeathMsg thisMsg = new DeathMsg();
                        thisMsg = JsonConvert.DeserializeObject<DeathMsg>(e.Data);
                        if (!thisMsg.payload.attacker_character_id.IsNullOrEmpty())
                        {
                            if (thisMsg.payload.attacker_character_id == ChampId)
                            {
                                Kills++;
                                RunOnUiThread(() => kills.Text = Kills.ToString());
                            }
                            else if (thisMsg.payload.character_id == ChampId)
                            {
                                if (thisMsg.payload.attacker_weapon_id == "93" ||
                                thisMsg.payload.attacker_weapon_id == "1626" ||
                                thisMsg.payload.attacker_weapon_id == "1627" ||
                                thisMsg.payload.attacker_weapon_id == "1628" ||
                                thisMsg.payload.attacker_weapon_id == "1629" ||
                                thisMsg.payload.attacker_weapon_id == "1690")
                                {
                                    Deaths--;
                                    RunOnUiThread(() => deaths.Text = Deaths.ToString());
                                }
                                else
                                {
                                    Deaths++;
                                    RunOnUiThread(() => deaths.Text = Deaths.ToString());
                                }
                            }
                        }
                    }
                    if (e.Data.Contains("Online")|| e.Data.Contains("Death") && e.Data.Contains("payload"))
                    {
                        RunOnUiThread(() => isOnline.SetBackgroundColor(Android.Graphics.Color.Green));
                        RunOnUiThread(() => isOnline.Text = "Character is ONLINE");
                        timer.Enabled = true;
                    }
                    if (e.Data.Contains("Offline"))
                    {
                        RunOnUiThread(() => isOnline.SetBackgroundColor(Android.Graphics.Color.Red));
                        RunOnUiThread(() => isOnline.Text = "Character is OFFLINE");
                        timer.Enabled = false;
                    }

                    text.Insert(0, $"{DateTime.Now:HH:mm:ss}: {e.Data}");
                    if (text.Count >= 5)
                    {
                        text.Remove(text.Last());
                    }
                    RunOnUiThread(() => smallText.Text = string.Join("\r\n", text));
                }

            });
            ThreadPool.QueueUserWorkItem(o => ws.OnError += (sender, e) =>
            {
                text.Insert(0, e.Message.ToString());
                if (text.Count >= 5)
                {
                    text.Remove(text.Last());
                }
                RunOnUiThread(() => smallText.Text = string.Join("\r\n", text));
            });
            ThreadPool.QueueUserWorkItem(o => ws.OnClose += (sender, e) =>
            {
                timer.Enabled = false;
                text.Insert(0, "Disconnected");
                if (text.Count >= 5)
                {
                    text.Remove(text.Last());
                }
                RunOnUiThread(() => smallText.Text = string.Join("\r\n", text));
                //ws.Send(inputName.Text);
            });

            //// Enter rest querry to PS2 API
            connectButton.Click += (object senderer, EventArgs eer) =>
            {
                if (connectButton.Text == "CONNECT TO SERVER")
                {
                    ThreadPool.QueueUserWorkItem(o => WSConnect(ws));
                    connectButton.SetBackgroundColor(Android.Graphics.Color.ParseColor("#AADC143C"));
                    connectButton.Text = "DISCONNECT";
                }
                else if (connectButton.Text == "DISCONNECT")
                {
                    if (ws.IsAlive)
                    {
                        ThreadPool.QueueUserWorkItem(o => ws.Close());
                    }
                    connectButton.Text = "CONNECT TO SERVER";
                    connectButton.SetBackgroundColor(Android.Graphics.Color.Green);
                }
            };
            inputName.Click += (object senderer, EventArgs eer) =>
            {
                if (inputName.Text == "Enter character name:")
                {
                    inputName.Text = "";
                }
            };

        }
        private static void WSConnect(WebSocket ws)
        {
            ws.ConnectAsync();
        }
        public class LoginMsg
        {
            public Payload payload { get; set; }
            public string service { get; set; }
            public string type { get; set; }
            public class Payload
            {
                public string character_id { get; set; }
                public string event_name { get; set; }
                public string timestamp { get; set; }
                public string world_id { get; set; }
            }
        }
        public class LogoutMsg
        {
            public Payload payload { get; set; }
            public string service { get; set; }
            public string type { get; set; }

            public class Payload
            {
                public string character_id { get; set; }
                public string event_name { get; set; }
                public string timestamp { get; set; }
                public string world_id { get; set; }
            }
        }
        public class DeathMsg
        {
            public Payload payload { get; set; }
            public string service { get; set; }
            public string type { get; set; }

            public class Payload
            {
                public string attacker_character_id { get; set; }
                public string attacker_fire_mode_id { get; set; }
                public string attacker_loadout_id { get; set; }
                public string attacker_vehicle_id { get; set; }
                public string attacker_weapon_id { get; set; }
                public string character_id { get; set; }
                public string character_loadout_id { get; set; }
                public string event_name { get; set; }
                public string is_headshot { get; set; }
                public string timestamp { get; set; }
                public string world_id { get; set; }
                public string zone_id { get; set; }
            }
        }
    }
}

