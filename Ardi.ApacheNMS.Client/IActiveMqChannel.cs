using System;
using Apache.NMS;

namespace Ardi.ApacheNMS.Client
{
    public interface IActiveMqChannel
    {
        Lazy<IConnection> Connection { get; }
        Lazy<ISession> Session { get; }
        IQueueSender CreateSender(string destinationName, DestinationType destinationType);
        IQueueReceiver CreateReceiver(string sourceName, DestinationType destinationType);
    }
}