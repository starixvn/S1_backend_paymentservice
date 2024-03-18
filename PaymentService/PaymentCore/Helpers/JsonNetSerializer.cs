using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PaymentCore.Helpers
{
    public class JsonNetSerializer
    {
        private readonly Encoding _designatedEncoding = Encoding.UTF8;

        public string SerializeToString<T>(T input)
        {
            using var ms = new MemoryStream();
            SerializerToStream(input, ms);
            var result = _designatedEncoding.GetString(ms.ToArray());

            return result;
        }

        private void SerializerToStream<T>(T input, Stream ms)
        {
            using var writer = new StreamWriter(ms, _designatedEncoding);
            using var json = new JsonTextWriter(writer);
            
            var ser = new JsonSerializer
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
            };
            
            ser.Serialize(json, input);
            ms.Seek(0, SeekOrigin.Begin);
        }

        public T DeserializeFormString<T>(string input)
        {
            using var ms = new MemoryStream(_designatedEncoding.GetBytes(input));
            using var reader = new StreamReader(ms, _designatedEncoding);
            using var json = new JsonTextReader(reader);
            
            var ser = new JsonSerializer
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
            
            var result = ser.Deserialize<T>(json);

            return result;
        }

        public T DeserializeFormStream<T>(Stream input)
        {
            using var reader = new StreamReader(input, _designatedEncoding);
            using var json = new JsonTextReader(reader);
            
            var ser = new JsonSerializer
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
            
            var result = ser.Deserialize<T>(json);

            return result;
        }
    }
}
