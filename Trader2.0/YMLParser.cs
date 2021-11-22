using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;
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

        public static void ReadSerializedData()
        {
            var deseralizer = new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
        }
    }

    public class StoreEntry
    {
        public ItemDataEntry _DataEntry { get; set; }

    }

    public class ItemDataEntry
    {
        [YamlMember(Alias = "Item_Enabled", ApplyNamingConventions = false, Description = "Item enabled for sale", Order = 3)] public bool enabled { get; set; }
        [YamlMember(Alias = "Item_Cost", ApplyNamingConventions = false, Description = "Item Cost in trader shop", Order = 2)]public int ItemCostInt { get; set; }
        [YamlMember(Alias = "Item_Prefab", ApplyNamingConventions = false, Description = "Item Prefab Name", Order = 1)]public string ItemNameString{ get; set; }
    }


}