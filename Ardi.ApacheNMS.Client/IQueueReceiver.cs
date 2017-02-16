using System;
using Apache.NMS;

namespace Ardi.ApacheNMS.Client
{
    public interface IQueueReceiver: IDisposable
    {
        void Listen(Action<IMessage> clientAction);
    }
}