using System.Collections.Generic;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Trader20
{
    public class YMLParser
    {
        public static string Serializers(Dictionary<string, ItemDataEntry> data)
        {
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var yml = serializer.Serialize(data);
            return yml;
        }

        public static Dictionary<string, ItemDataEntry> ReadSerializedData(string s)
        {
            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var tmp = deserializer.Deserialize<Dictionary<string, ItemDataEntry>>(s);
            return tmp;
        }
    }

    public struct ItemDataEntry 
    {
        [YamlMember(Alias = "cost", ApplyNamingConventions = false)]
        public int ItemCostInt { get; set; }
        [YamlMember(Alias = "stack", ApplyNamingConventions = false)]
        public int ItemCount { get; set; }
        
        [YamlMember(Alias = "inventory count", ApplyNamingConventions = false)]
        public int Invcount { get; set; }
    }


}