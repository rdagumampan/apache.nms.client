namespace Ardi.ApacheNMS.Client
{
    public interface ISerializer
    {
        T Deserialize<T>(string raw);
        string Serialize<T>(T data);
    }
}