using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apache.NMS;
using Ardi.ApacheNMS.Client;

namespace Ardi.ApacheNMS.ExPubsub
{
    public class ConnectionInfo
    {
        public string ServerAddress = "...";
        public string UserName = "...";
        public string Password = "...";
        public string ClientId = "";
    }

    public class Program
    {       
        static void Main(string[] args)
        {
            Task.Factory.StartNew(() =>
            {
                var connection = new ConnectionInfo();
                var publisher = new Publisher(connection);
                publisher.Start();
            });

            Task.Factory.StartNew(() =>
            {
                var connection = new ConnectionInfo();
                connection.ClientId = "SVC01";

                var listener = new Listener(connection);
                listener.Start();
            });

            Task.Factory.StartNew(() =>
            {
                var connection = new ConnectionInfo();
                connection.ClientId = "SVC02";

                var listener = new Listener(connection);
                listener.Start();
            });

            Task.Factory.StartNew(() =>
            {
                var connection = new ConnectionInfo();
                connection.ClientId = "SVC03";

                var listener = new Listener(connection);
                listener.Start();
            });

            Console.Read();
        }
    }

    public class Publisher
    {
        private readonly ConnectionInfo _connection;

        public Publisher(ConnectionInfo connection)
        {
            _connection = connection;
        }

        public void Start()
        {

            var channel = new ActiveMqChannel(_connection.ServerAddress, _connection.UserName, _connection.Password);
            var sender = channel.CreateSender("ExSampleTopic", DestinationType.Topic);

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
        private readonly ConnectionInfo _connection;

        public Listener(ConnectionInfo connection)
        {
            _connection = connection;
        }

        public void Start()
        {
            var channel = new ActiveMqChannel(_connection.ServerAddress, _connection.UserName, _connection.Password);
            var listener = channel.CreateReceiver("ExSampleTopic", DestinationType.Topic);

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

                    Console.WriteLine($"{_connection.ClientId} {helloWorld.Timestamp}\t{helloWorld.Message}");
                }
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
