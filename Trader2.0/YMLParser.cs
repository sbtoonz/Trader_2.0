using System.IO;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Trader20
{
    public class YMLParser
    {
        public static string Serilizer(StoreEntry data)
        {
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var yml = serializer.Serialize(data);
            return yml;
        }

        public static void WriteSerializedData(string data)
        {
            if (File.ReadAllText("config.yaml", Encoding.Default).Length == data.Length)
            {
                return;
            }
            if(File.ReadAllText("config.yaml", Encoding.Default).Length > 0)
            {
                File.AppendAllText("config.yaml", data);
            }
            else
            {
                File.WriteAllText("config.yaml", data);
            }
        }
    }

    public class StoreEntry
    {
        public string ItemName;
        public int ItemCost;
        public bool enabled;
    }
}