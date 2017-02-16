using System;

namespace Ardi.ApacheNMS.Client
{
    public interface IQueueSender: IDisposable
    {
        void Send<T>(T message) where T : IAmqMessage;
        void Send(object message, Type type);
    }
}