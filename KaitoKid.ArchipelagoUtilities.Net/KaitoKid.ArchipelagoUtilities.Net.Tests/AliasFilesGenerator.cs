using FluentAssertions;
using KaitoKid.ArchipelagoUtilities.Net.ItemSprites;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace KaitoKid.ArchipelagoUtilities.Net.Tests
{
    internal class AliasFilesGenerator
    {
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void GenerateDefaultFileAliases()
        {
            var path = @"..\..\..\..\KaitoKid.ArchipelagoUtilities.Net.CustomAssets\Custom Assets\";
            var subfolders = Directory.EnumerateDirectories(path, "*", SearchOption.TopDirectoryOnly).ToArray();

            foreach (var subfolderPath in subfolders)
            {
                var subFolder = new DirectoryInfo(subfolderPath);
                var subfolderName = subFolder.Name;
                var files = Directory.EnumerateFiles(subfolderPath, ArchipelagoItemSprites.ALIASES_FILE_NAME).ToArray();
                if (files.Any())
                {
                    continue;
                }

                var aliasesPath = Path.Combine(subfolderPath, ArchipelagoItemSprites.ALIASES_FILE_NAME);
                var emptyAliases = DefaultAliases;
                var jsonEmptyAliases = JsonConvert.SerializeObject(emptyAliases, Formatting.Indented);
                File.WriteAllText(aliasesPath, jsonEmptyAliases);
            }
        }

        public ItemSpriteAliases DefaultAliases => new ItemSpriteAliases()
        {
            Aliases = new List<ItemSpriteAlias>()
            {
                new ItemSpriteAlias()
                {
                    AliasName = "PlaceholderAliasA",
                    ItemNames = new List<string>() { "PlaceholderItem1", "PlaceholderItem2" }
                },
                new ItemSpriteAlias()
                {
                    AliasName = "PlaceholderAliasB",
                    ItemNames = new List<string>() { "PlaceholderItem3", "PlaceholderItem4" }
                },
            }
        };
    }
}
