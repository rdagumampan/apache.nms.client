
using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Ardi.ApacheNMS.Client;

namespace Ardi.ApacheNMS.Samples
{
    public class Program
    {
        static void Main(string[] args)
        {
            Task.Factory.StartNew(() =>
            {
                var publisher = new Publisher();
                publisher.Start();
            });

            Task.Factory.StartNew(() =>
            {
                var listener = new Listener();
                listener.Start();
            });

            Console.Read();
        }
    }

    public class Publisher
    {
        public void Start()
        {
            var serverAddress = "...";
            var userName = "...";
            var password = "...";

            var channel = new ActiveMqChannel(serverAddress, userName, password);
            var sender = channel.CreateSender("test.app.request", DestinationType.Queue);

            while (true)
            {
                //create request message
                var message = new HellWorld
                {
                    Timestamp = DateTime.UtcNow,
                    Message = "Hello earth!"
                };
                sender.Send(message);

                //hang up
                Thread.Sleep(1000);
            }
        }
    }

    public class Listener
    {
        string serverAddress = "...";
        string userName = "...";
        string password = "...";

        public void Start()
        {
            var channel = new ActiveMqChannel(serverAddress, userName, password);
            var listener = channel.CreateReceiver("test.app.request", DestinationType.Queue);

            listener.Listen(Receive);
        }

        private void Receive(IMessage message)
        {
            var objectMessage = message as ITextMessage;
            if (objectMessage != null)
            {
                //proces request message
                if (objectMessage.NMSType.Equals(typeof(HellWorld).FullName, StringComparison.InvariantCultureIgnoreCase))
                {
                    var serializer = new DataSerializer();
                    var helloWorld = serializer.Deserialize<HellWorld>(objectMessage.Text);

                    Console.WriteLine($"Received: {helloWorld.Timestamp}\t{helloWorld.Message}");
                }

                //create response message
                var channel = new ActiveMqChannel(serverAddress, userName, password);
                var sender = channel.CreateSender("test.app.response", DestinationType.Queue);

                var response = new HellWorldResponse
                {
                    Timestamp = DateTime.UtcNow,
                    Message = "Hi, we got your message. " +
                              "What galaxy are you from?"
                };
                sender.Send(response);
            }
        }
    }

    public class HellWorld : BaseAmqPublishMessage
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }

    public class HellWorldResponse : BaseAmqResponseMessage
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }

}
