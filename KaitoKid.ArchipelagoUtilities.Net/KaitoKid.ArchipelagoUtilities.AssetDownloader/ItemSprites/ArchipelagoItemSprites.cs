using KaitoKid.Utilities.Interfaces;
using Newtonsoft.Json;

namespace KaitoKid.ArchipelagoUtilities.AssetDownloader.ItemSprites
{
    public class ArchipelagoItemSprites
    {
        private const string SPRITE_FOLDER_NAME = "Custom Assets";
        public const string ALIASES_FILE_NAME = "aliases.json";

        private ILogger _logger;
        private string _spritesFolder;
        private Dictionary<string, List<ItemSprite>> _spritesByGame;
        private Dictionary<string, List<ItemSprite>> _spritesByItemName;
        private Dictionary<string, Dictionary<string, ItemSprite>> _spritesByGameByItemName;

        public ArchipelagoItemSprites(ILogger logger)
        {
            _logger = logger;
            var currentDirectory = Directory.GetCurrentDirectory();
            try
            {
                _spritesFolder = Directory.EnumerateDirectories(currentDirectory, SPRITE_FOLDER_NAME, SearchOption.AllDirectories).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Could not scan subdirectories of '{currentDirectory}' to find item sprites. Error: {ex}");
            }

            LoadCustomSprites();
        }

        public bool HasSpritesForGame(string gameName)
        {
            var cleanGame = CleanName(gameName);
            return _spritesByGame.ContainsKey(cleanGame) || _spritesByGameByItemName.ContainsKey(cleanGame);
        }

        private void LoadCustomSprites()
        {
            _spritesByGame = new Dictionary<string, List<ItemSprite>>();
            _spritesByItemName = new Dictionary<string, List<ItemSprite>>();
            _spritesByGameByItemName = new Dictionary<string, Dictionary<string, ItemSprite>>();

            if (string.IsNullOrWhiteSpace(_spritesFolder))
            {
                _logger.LogError("Could not find Custom Assets Directory");
                return;
            }

            var gameSubfolders = Directory.EnumerateDirectories(_spritesFolder, "*", SearchOption.TopDirectoryOnly).ToArray();
            foreach (var gameSubfolder in gameSubfolders)
            {
                RegisterGameSprites(gameSubfolder);
            }
        }

        private void RegisterGameSprites(string gameSubfolder)
        {
            var aliases = GetAliases(gameSubfolder);
            RegisterDirectSprites(gameSubfolder, out var gameName);
            RegisterAliasSprites(aliases, gameName);
        }

        private ItemSpriteAliases GetAliases(string gameSubfolder)
        {
            try
            {
                var aliasesFile = Path.Combine(gameSubfolder, ALIASES_FILE_NAME);
                if (File.Exists(aliasesFile))
                {
                    var jsonAliases = File.ReadAllText(aliasesFile);
                    var aliases = JsonConvert.DeserializeObject<ItemSpriteAliases>(jsonAliases);
                    return aliases;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error getting the aliases in {gameSubfolder}. {e.Message}");
            }

            return new ItemSpriteAliases();
        }

        private void RegisterDirectSprites(string spritesGameFolder)
        {
            RegisterDirectSprites(spritesGameFolder, out _);
        }

        private void RegisterDirectSprites(string spritesGameFolder, out string game)
        {
            game = string.Empty;
            var files = Directory.EnumerateFiles(spritesGameFolder, "*.png", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                RegisterSprite(file, out game);
            }
        }

        private void RegisterAliasSprites(ItemSpriteAliases aliases, string gameName)
        {
            foreach (var alias in aliases.Aliases)
            {
                foreach (var aliasItemName in alias.ItemNames)
                {
                    var cleanGame = CleanName(gameName);
                    var cleanAliasName = CleanName(alias.AliasName);
                    if (_spritesByGameByItemName.ContainsKey(cleanGame) &&
                        _spritesByGameByItemName[cleanGame].ContainsKey(cleanAliasName))
                    {
                        var aliasSprite = _spritesByGameByItemName[cleanGame][cleanAliasName];
                        RegisterSprite(gameName, aliasItemName, aliasSprite);
                    }
                }
            }
        }

        private void RegisterSprite(string file, out string game)
        {
            var sprite = new ItemSprite(_logger, file);
            RegisterSprite(sprite.Game, sprite.Item, sprite);
            game = sprite.Game;
        }

        private void RegisterSprite(string game, string item, ItemSprite sprite)
        {
            var cleanGame = CleanName(game);
            var cleanItem = CleanName(item);
            if (!_spritesByGame.ContainsKey(cleanGame))
            {
                _spritesByGame.Add(cleanGame, new List<ItemSprite>());
            }
            if (!_spritesByItemName.ContainsKey(cleanItem))
            {
                _spritesByItemName.Add(cleanItem, new List<ItemSprite>());
            }
            if (!_spritesByGameByItemName.ContainsKey(cleanGame))
            {
                _spritesByGameByItemName.Add(cleanGame, new Dictionary<string, ItemSprite>());
            }

            _spritesByGame[cleanGame].Add(sprite);
            _spritesByItemName[cleanItem].Add(sprite);
            if (!_spritesByGameByItemName[cleanGame].ContainsKey(cleanItem))
            {
                _spritesByGameByItemName[cleanGame].Add(cleanItem, sprite);
            }
        }

        public bool TryGetCustomAsset(IAssetLocation scoutedLocation, string myGameName, bool fallbackOnDifferentGameAsset, bool fallbackOnGenericGameAsset, out ItemSprite sprite)
        {
            sprite = null;
            if (scoutedLocation == null)
            {
                return false;
            }

            var myGame = CleanName(myGameName);
            var game = CleanName(scoutedLocation.GameName);
            var item = CleanName(scoutedLocation.ItemName);
            if (_spritesByGameByItemName.TryGetValue(game, out var itemsInCorrectGame))
            {
                if (itemsInCorrectGame.TryGetValue(item, out sprite))
                {
                    return true;
                }
            }

            if (fallbackOnDifferentGameAsset && _spritesByGameByItemName.TryGetValue(myGame, out var itemsInMyGame))
            {
                if (itemsInMyGame.TryGetValue(item, out sprite))
                {
                    return true;
                }
            }

            if (fallbackOnDifferentGameAsset && _spritesByItemName.TryGetValue(item, out var spritesWithCorrectName) && spritesWithCorrectName.Any())
            {
                var random = new Random(scoutedLocation.GetSeed());
                var index = random.Next(0, spritesWithCorrectName.Count);
                sprite = spritesWithCorrectName[index];
                return true;
            }

            if (fallbackOnGenericGameAsset && _spritesByGameByItemName.TryGetValue(game, out itemsInCorrectGame))
            {
                if (itemsInCorrectGame.TryGetValue(string.Empty, out sprite))
                {
                    return true;
                }
            }

            return false;
        }

        public string CleanName(string name)
        {
            if (name == null)
            {
                return string.Empty;
            }

            var charsToIgnore = new string[] { " ", "_", ":", "'" };
            name = name.ToLower();
            foreach (var charToIgnore in charsToIgnore)
            {
                name = name.Replace(charToIgnore, "");
            }

            return name;
        }
    }
}
