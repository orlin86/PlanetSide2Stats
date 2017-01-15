using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Widget;
using Android.OS;
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
            Button submitButton = FindViewById<Button>(Resource.Id.SubmitNameButton);
            Button disconnecButton = FindViewById<Button>(Resource.Id.DisconnectButton);
            TextView dataBig = FindViewById<TextView>(Resource.Id.DataBig);
            TextView dataSmall = FindViewById<TextView>(Resource.Id.DataSmall);

            WebSocket ws = new WebSocket("wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167");


            ThreadPool.QueueUserWorkItem(o => ws.OnOpen += (sender, e) => ws.Send("Hi, there!"));

            ThreadPool.QueueUserWorkItem(o=>ws.OnMessage +=  (sender, e) =>
            {
                RunOnUiThread(() => dataSmall.Text +=  e.Data+ "\r\n");
            });
            ws.OnError += (sender, e) =>
            {
                dataBig.Text = e.Exception.ToString();
            };

            ws.OnClose += (sender, e) =>
            {
                dataBig.Text = "Connection Closed"; 
            };
            
            submitButton.Click +=  (object senderer, EventArgs eer) =>
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

    }
}

