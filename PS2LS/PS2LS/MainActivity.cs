﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Widget;
using Android.OS;
using WebSocketSharp;


namespace PS2LS
{
    [Activity(Label = "PS2LS", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            EditText inputName = FindViewById<EditText>(Resource.Id.InputName);
            Button connectButton = FindViewById<Button>(Resource.Id.ConnectButton);
            TextView kills = FindViewById<TextView>(Resource.Id.Kills);
            TextView deaths = FindViewById<TextView>(Resource.Id.Deaths);
            TextView isOnline = FindViewById<TextView>(Resource.Id.IsOnline);
            TextView timePlayed = FindViewById<TextView>(Resource.Id.TimePlayed);
            TextView smallText = FindViewById<TextView>(Resource.Id.SmallText);

            WebSocket ws = new WebSocket("ws://92.247.240.220:4649/SendKills");
            ws.EmitOnPing = false;
            int Kills = 0;
            int Deaths = 0;
            List<string> text = new List<string>();

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
            ThreadPool.QueueUserWorkItem(o => ws.OnMessage += (sender, e) =>
            {
                if (e.Data.Contains("Kill"))
                {
                    Kills++;
                    RunOnUiThread(() => kills.Text = Kills.ToString());
                }
                if (e.Data.Contains("Death"))
                {
                    Deaths++;
                    RunOnUiThread(() => deaths.Text = Deaths.ToString());
                }
                if (e.Data.Contains("Online"))
                {
                    RunOnUiThread(() => connectButton.SetBackgroundColor(Android.Graphics.Color.Green));
                    RunOnUiThread(() => isOnline.Text = "Character is ONLINE");
                }
                if (e.Data.Contains("Offline"))
                {
                    RunOnUiThread(() => connectButton.SetBackgroundColor(Android.Graphics.Color.Red));
                    RunOnUiThread(() => isOnline.Text = "Character is OFFLINE");
                }

                text.Insert(0, e.Data);
                if (text.Count >= 5)
                {
                    text.Remove(text.Last());
                }
                //RunOnUiThread(() => smallText.Text = string.Join("\r\n", text));
                smallText.Text = e.Data;
            });
            ws.OnError += (sender, e) =>
            {
                text.Insert(0, e.Message.ToString());
                if (text.Count >= 5)
                {
                    text.Remove(text.Last());
                }
                RunOnUiThread(() => smallText.Text = string.Join("\r\n", text));
            };
            ws.OnClose += (sender, e) =>
            {
                text.Insert(0, "Disconnected");
                if (text.Count >= 5)
                {
                    text.Remove(text.Last());
                }
                RunOnUiThread(() => smallText.Text = string.Join("\r\n", text));
                ws.Send(inputName.Text);
            };

            //// Enter rest querry to PS2 API
            connectButton.Click += (object senderer, EventArgs eer) =>
            {
                
                if (connectButton.Text == "CONNECT TO SERVER")
                {
                    ThreadPool.QueueUserWorkItem(o => WSConnect(ws));
                    RunOnUiThread(() => connectButton.SetBackgroundColor(Android.Graphics.Color.Red));
                    RunOnUiThread(() => connectButton.Text = "DISCONNECT");
                }
                else if (connectButton.Text == "DISCONNECT")
                {
                    if (ws.IsAlive)
                    {
                        ws.Close();
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
            ws.Connect();
        }
    }
}

