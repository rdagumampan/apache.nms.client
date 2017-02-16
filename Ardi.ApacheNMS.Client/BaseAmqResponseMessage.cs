namespace Ardi.ApacheNMS.Client
{
    public class BaseAmqResponseMessage : BaseAmqMessage, IAmqResponseMessage
    {
        public string CorrelationID { get; set; }
    }
}