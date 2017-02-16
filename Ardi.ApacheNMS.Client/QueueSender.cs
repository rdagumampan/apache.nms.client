using System;
using System.Diagnostics;
using Apache.NMS;
using Apache.NMS.ActiveMQ.Commands;

namespace Ardi.ApacheNMS.Client
{
    public class QueueSender : IQueueSender, IDisposable
    {
        private readonly Lazy<IQueue> _queue;
        private Lazy<IMessageProducer> _producer;

        private readonly dynamic _settings = new
        {
            MsgPriority = MsgPriority.High,
            DeliveryMode =  MsgDeliveryMode.Persistent,
            TimeToLive = TimeSpan.FromHours(24)
        };

        public QueueSender(ISession session, string destinationQueueName)
        {
            //create instance of destination queue only when its first used
            _queue = new Lazy<IQueue>(() => new ActiveMQQueue(destinationQueueName));

            _producer = new Lazy<IMessageProducer>(() =>
            {
                var producer = session.CreateProducer(_queue.Value);
                producer.DeliveryMode = _settings.DeliveryMode;
                producer.TimeToLive = _settings.TimeToLive;
                return producer;
            });
        }

        public QueueSender(ISession session, ActiveMQTopic topic)
        {
            _producer = new Lazy<IMessageProducer>(() =>
            {
                var destination = session.GetDestination(topic.TopicName, DestinationType.Topic);
                var producer = session.CreateProducer(destination);
                producer.DeliveryMode = _settings.DeliveryMode;
                producer.TimeToLive = _settings.TimeToLive;
                return producer;
            });
        }
        public void Send<T>(T message) where T : IAmqMessage
        {
            var serializer = new DataSerializer();
            var serializedMessage = serializer.Serialize(message);
            var requestMessage = _producer.Value.CreateTextMessage(serializedMessage);

            requestMessage.NMSType = typeof(T).FullName;
            requestMessage.NMSTimeToLive = _settings.TimeToLive;
            requestMessage.NMSDeliveryMode = _settings.DeliveryMode;
            requestMessage.NMSPriority =  _settings.MsgPriority;

            _producer.Value.Send(requestMessage);
        }

        public void Send(object message, Type type)
        {
            var serializer = new DataSerializer();
            var serializedMessage = serializer.Serialize(message, type);
            var requestMessage = _producer.Value.CreateTextMessage(serializedMessage);

            requestMessage.NMSType = type.FullName;
            requestMessage.NMSTimeToLive = _settings.TimeToLive;
            requestMessage.NMSDeliveryMode = _settings.DeliveryMode;
            requestMessage.NMSPriority = _settings.MsgPriority;

            _producer.Value.Send(requestMessage);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed;
        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (!disposing)
                    return;

                try
                {                   
                    _producer.Value.Dispose();
                    _queue.Value.Dispose();
                }
                finally
                {
                    Trace.TraceError("QueueSender disposed");
                }
            }

            this.disposed = true;
        }
    }
}