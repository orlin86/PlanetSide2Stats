using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ServerConsoleApp
{
    public class SendKills : WebSocketBehavior
    {
        private string _name;
        private static int _number = 0;
        private string _prefix;
        private static WebSocket ws = new WebSocket("wss://push.planetside2.com/streaming?environment=ps2&service-id=s:3216732167");
        private Notifier nf = new Notifier();


        public SendKills()
      : this(null)
        {
            if (!ws.IsAlive)
            {
                ws.Connect();
                Console.WriteLine("////");
                Console.WriteLine("////");
                Console.WriteLine("CONNECTED TO API");
                Console.WriteLine("////");
                Console.WriteLine("////");
            }
        }

        protected override void OnOpen()
        {
            ws.OnOpen += (sender, e) =>
            {
                ws.Send("Hi, there!");
            };
            ws.OnMessage += (sender, e) =>
            {
                var name = Context.QueryString["name"];
                nf.Notify(new NotificationMessage
                {
                    Summary = "Planetside2 Api MSG",
                    Body = !e.IsPing ? e.Data : "Received a ping.",
                    Icon = "notification-message-im"
                });
                // ↓ sends to client data, containing death, !! SWICH WITH E.DATA
                if (_name.Contains("Death"))
                {
                    Send(e.Data);
                }
            };
            _name = getName();
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            string sendString = "{\r\n\t\"service\":\"event\",\r\n\t\"action\":\"subscribe\",\r\n\t\"characters\":[\"" +
                           e.Data +
                           "\"],\r\n\t\"eventNames\":[\"Death\"]\r\n}";
            Console.WriteLine("////");
            Console.WriteLine("////");
            Console.WriteLine("sending");
            Console.WriteLine("////");
            Console.WriteLine(sendString);
            ws.Send(sendString);
            // ↓ sets name to client to match his querry, SWICH WITH E.DATA
            _name += "Death";
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Sessions.Broadcast(String.Format("{0} got logged off...", _name));
        }


        public SendKills(string prefix)
        {
            _prefix = !prefix.IsNullOrEmpty() ? prefix : "anon#";

        }

        private string getName()
        {
            var name = Context.QueryString["name"];
            return !name.IsNullOrEmpty() ? name : _prefix + getNumber();
        }

        private static int getNumber()
        {
            return Interlocked.Increment(ref _number);
        }
    }
}