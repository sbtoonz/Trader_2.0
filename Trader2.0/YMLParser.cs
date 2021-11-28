using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    }


}