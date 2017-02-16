namespace Ardi.ApacheNMS.Client
{
    public interface IAmqPublishMessage : IAmqMessage
    {
        string CorrelationID { get; set; }
    }
}