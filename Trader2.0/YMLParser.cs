using System.Collections.Generic;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Trader20
{
    public class YMLParser
    {
        public static List<StoreEntry> test = new();
        public static string Serializers(StoreEntry data)
        {
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var yml = serializer.Serialize(data);
            return yml;
        }

        public static void WriteSerializedData(string data)
        {
            if (File.ReadAllText(Trader20.paths+"/trader_config.yaml", Encoding.Default).Length == data.Length)
            {
                return;
            }
            if(File.ReadAllText(Trader20.paths+"/trader_config.yaml", Encoding.Default).Length > 0)
            {
                File.AppendAllText(Trader20.paths+"/trader_config.yaml", data);
            }
            else
            {
                File.WriteAllText(Trader20.paths+"/trader_config.yaml", data);
            }

        }

        public static StoreEntry ReadSerializedData(string s)
        {
            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var tmp = deserializer.Deserialize<StoreEntry>(s);

            return tmp;

        }
    }

    public class StoreEntry
    {
        [YamlMember(Alias = "Item_Entry", ApplyNamingConventions = false, Description = "Item Entry", Order = 0)]
        public List<ItemDataEntry> _DataEntry { get; set; }

    }

    public class ItemDataEntry
    {
        public bool enabled { get; set; }
        public int ItemCostInt { get; set; }
        public string ItemNameString{ get; set; }
    }


}