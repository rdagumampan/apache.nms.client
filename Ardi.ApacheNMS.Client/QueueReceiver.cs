using System;
using System.Diagnostics;
using Apache.NMS;
using Apache.NMS.ActiveMQ.Commands;

namespace Ardi.ApacheNMS.Client
{
    public class QueueReceiver : IQueueReceiver, IDisposable
    {
        private readonly Lazy<IQueue> _queue;
        private Lazy<IMessageConsumer> _consumer;

        public QueueReceiver(ISession session, string sourceQueueName)
        {
            //create instance of queue only when its first used
            _queue = new Lazy<IQueue>(() => new ActiveMQQueue(sourceQueueName));

            _consumer = new Lazy<IMessageConsumer>(() =>
            {
                //we can also create durable consumer to make sure we dont
                //lost messages due to reconnection issues and connection breakdown
                var consumer = session.CreateConsumer(_queue.Value);
                return consumer;
            });
        }

        public QueueReceiver(ISession session, ActiveMQTopic topic)
        {
            _consumer = new Lazy<IMessageConsumer>(() =>
            {
                //we can also create durable consumer to make sure we dont
                //lost messages due to reconnection issues and connection breakdown
                var destination = session.GetDestination(topic.TopicName, DestinationType.Topic);
                var consumer = session.CreateConsumer(destination);
                return consumer;
            });
        }

        public void Listen(Action<IMessage> clientAction)
        {
            _consumer.Value.Listener += (m => clientAction(m));
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
                    _consumer.Value.Dispose();
                    _queue.Value.Dispose();
                }
                finally
                {
                    Trace.TraceError("QueueReceiver disposed");
                }
            }

            this.disposed = true;
        }
    }
}