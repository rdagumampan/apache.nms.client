using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace Ardi.ApacheNMS.Client
{
    public class DataSerializer : ISerializer
    {
        public T Deserialize<T>(string raw)
        {
            var ser = new XmlSerializer(typeof(T));
            var reader = new StringReader(raw);
            return (T)ser.Deserialize(reader);
        }

        public string Serialize<T>(T data)
        {
            var ser = new XmlSerializer(typeof(T));
            using (var writer = new Utf8StringWriter())
            {
                ser.Serialize(writer, data);
                return writer.ToString();
            }
        }

        public string Serialize(object data, Type type)
        {
            var ser = new XmlSerializer(type);
            using (var writer = new Utf8StringWriter())
            {
                ser.Serialize(writer, data);
                return writer.ToString();
            }
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => new UTF8Encoding(false);
    }
}