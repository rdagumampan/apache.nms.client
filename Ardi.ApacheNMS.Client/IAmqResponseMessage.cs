namespace Ardi.ApacheNMS.Client
{
    public interface IAmqResponseMessage : IAmqMessage
    {
        string CorrelationID { get; set; }
    }
}