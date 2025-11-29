using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using KaitoKid.ArchipelagoUtilities.AssetDownloader.Extensions;
using KaitoKid.ArchipelagoUtilities.AssetDownloader.ItemSprites;

namespace KaitoKid.ArchipelagoUtilities.AssetDownloader
{
    public class AssetService
    {
        private Downloader _downloader;
        private HashSet<string> _downloadedGameZips;
        private HashSet<string> _downloadedSpecificAssets;

        public AssetService()
        {
            _downloader = new Downloader();
            _downloadedGameZips = new HashSet<string>();
            _downloadedSpecificAssets = new HashSet<string>();
        }

        public void TryDownloadGameAssets(string gameName, ArchipelagoItemSprites itemSprites, bool async)
        {
            if (_downloadedGameZips.Contains(gameName))
            {
                return;
            }

            _downloadedGameZips.Add(gameName);

            if (async)
            {
                TryDownloadGameAssetsAsync(gameName, itemSprites).FireAndForget();
            }
            else
            {
                TryDownloadGameAssetsSync(gameName, itemSprites);
            }
        }

        private async Task TryDownloadGameAssetsAsync(string gameName, ArchipelagoItemSprites itemSprites)
        {
            TryDownloadGameAssetsSync(gameName, itemSprites);
        }

        private void TryDownloadGameAssetsSync(string gameName, ArchipelagoItemSprites itemSprites)
        {
            var zipPath = Path.Combine(Paths.CustomAssetsDirectory, $"{gameName}.zip");
            var hasZip = File.Exists(zipPath);
            if (!hasZip)
            {
                hasZip = _downloader.DownloadGameZip(gameName);
            }

            var gamePath = Path.Combine(Paths.CustomAssetsDirectory, $"{gameName}");
            var hasSprites = Directory.Exists(gamePath);
            if (hasZip)
            {
                hasSprites = _downloader.UnzipGameZip(gameName);
            }

            if (hasSprites)
            {
                itemSprites.RegisterGameSprites(gamePath);
            }
        }

        public void TryDownloadAsset(string gameName, string itemName, ArchipelagoItemSprites itemSprites)
        {
            var hashKey = GetKey(gameName, itemName);
            if (_downloadedSpecificAssets.Contains(hashKey))
            {
                return;
            }

            _downloadedSpecificAssets.Add(hashKey);

            TryDownloadAssetAsync(gameName, itemName, itemSprites).FireAndForget();

        }

        private async Task TryDownloadAssetAsync(string gameName, string itemName, ArchipelagoItemSprites itemSprites)
        {
            var assetPath = Path.Combine(Paths.CustomAssetsDirectory, gameName, $"{gameName}_{itemName}.png");
            if (File.Exists(assetPath))
            {
                return;
            }

            if (_downloader.DownloadSpecificItemAsset(gameName, itemName, out var fileName))
            {
                itemSprites.RegisterSprite(fileName, out _);
            }
        }

        private string GetKey(string gameName, string itemName)
        {
            return $"{gameName}_{itemName}";
        }
    }
}
