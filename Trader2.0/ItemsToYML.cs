using System.IO;
using YamlDotNet;
using YamlDotNet.Serialization.NamingConventions;

namespace Trader20
{
    public class ItemsToYML
    {
        public static void Test()
        {
            var deseralizer = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
            deseralizer.Deserialize<ItemDatEntry>(File.ReadAllText("config.yaml"));
        }
    }

    public class ItemDatEntry
    {
        public ItemDrop.ItemData _itemData { get; set; }
        
    }
}