using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Trader20
{
    /// <summary>
    /// 
    /// </summary>
    public class YMLParser
    {
        /// <summary>
        /// Serialize data for YML
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Serializers(Dictionary<string, ItemDataEntry> data)
        {
            var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            var yml = serializer.Serialize(data);
            return yml;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Dictionary<string, ItemDataEntry> ReadSerializedData(string s)
        {
            var deserializer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            var tmp = deserializer.Deserialize<Dictionary<string, ItemDataEntry>>(s);
            return tmp;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataEntries"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        internal static bool CheckForEntry(Dictionary<string, ItemDataEntry> dataEntries, string keyName)
        {
            return dataEntries.ContainsKey(keyName);
        }
        
        /// <summary>
        /// Appends the YML file with items you have sold Knarr
        /// </summary>
        /// <param name="s"></param>
        internal static void AppendYmLfile(string s)
        {
            string file = Trader20.Paths + "/trader_config.yaml";
            using var sw = File.AppendText(file);
            sw.WriteLineAsync(s);
        }
    }
    
    

    /// <summary>
    /// Structure for YML
    /// </summary>
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