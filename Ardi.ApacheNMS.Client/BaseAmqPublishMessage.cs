namespace Ardi.ApacheNMS.Client
{
    public class BaseAmqPublishMessage : BaseAmqMessage, IAmqPublishMessage
    {
        public string CorrelationID { get; set; }
    }
}