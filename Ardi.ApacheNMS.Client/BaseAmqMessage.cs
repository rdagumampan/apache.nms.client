using System;

namespace Ardi.ApacheNMS.Client
{
    public class BaseAmqMessage :IAmqMessage
    {
        public BaseAmqMessage()
        {
            ID = Guid.NewGuid().ToString().ToUpper();
        }

        public string ID { get; set; }
    }
}