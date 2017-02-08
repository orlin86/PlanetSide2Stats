using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Android.App;
using Android.Widget;
using Android.OS;
using WebSocketSharp;

namespace App1
{
    [Activity(Label = "App1", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

            Button connectButton = FindViewById<Button>(Resource.Id.connectButton);
            Button sendButton = FindViewById<Button>(Resource.Id.sendButton);
            TextView textView = FindViewById<TextView>(Resource.Id.textView);


            WebSocket ws = new WebSocket("wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167");
            ws.EmitOnPing = true;
            ws.WaitTime = TimeSpan.FromSeconds(10);

            List<string> text = new List<string>();

            ThreadPool.QueueUserWorkItem(o => ws.OnOpen += (sender, e) =>
            {
                text.Insert(0, $"{DateTime.Now}: Connected");
                if (text.Count >= 25)
                {
                    text.Remove(text.Last());
                }
                RunOnUiThread(() => textView.Text = string.Join("\r\n", text));
                
            });

            ThreadPool.QueueUserWorkItem(o => ws.OnMessage += (sender, e) =>
            {
                text.Insert(0, $"{DateTime.Now}: {e.Data}");
                if (text.Count >= 25)
                {
                    text.Remove(text.Last());
                }
                RunOnUiThread(() => textView.Text = string.Join("\r\n", text));
            });

            ThreadPool.QueueUserWorkItem(o => ws.OnError += (sender, e) =>
            {
                text.Insert(0, e.Message.ToString());
                if (text.Count >= 25)
                {
                    text.Remove(text.Last());
                }
                RunOnUiThread(() => textView.Text = string.Join("\r\n", text));
            });

            connectButton.Click += (object senderer, EventArgs eer) =>
            {
                    ThreadPool.QueueUserWorkItem(o => ws.Connect());
            };

            sendButton.Click += (object senderer, EventArgs eer) =>
            {
                string sendString = "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[\"shigeruban\"],\r\n\t\"eventNames\":[\"Death\"]\r\n}";
                ws.Send(sendString);
            };

        }
    }
    
}

