using System.IO.Compression;
using System.Net;

namespace KaitoKid.ArchipelagoUtilities.AssetDownloader
{
    public class Downloader
    {
        public bool DownloadGameZip(string game)
        {
            try
            {
                var zipName = $"{game}.zip";
                var webPath = $"{Paths.WEB_DOWNLOAD_ZIPPED_ASSETS}{zipName}";
                var localPath = Path.Combine(Paths.CustomAssetsDirectory, zipName);
                return DownloadFile(webPath, localPath);
            }
            catch
            {
                return false;
            }
        }

        public bool UnzipGameZip(string game)
        {
            try
            {
                var zipName = $"{game}.zip";
                var zipFile = Path.Combine(Paths.CustomAssetsDirectory, zipName);
                var gameFolder = Path.Combine(Paths.CustomAssetsDirectory, game);
                ZipFile.ExtractToDirectory(zipFile, gameFolder);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool DownloadSpecificAsset(string game, string assetName)
        {
            try
            {
                var filePath = Path.Combine(game, assetName);
                var webPath = $"{Paths.WEB_DOWNLOAD_ASSETS}{game}/{assetName}";
                var localPath = Path.Combine(Paths.CustomAssetsDirectory, filePath);
                return DownloadFile(webPath, localPath);
            }
            catch
            {
                return false;
            }
        }

        private static bool DownloadFile(string originPath, string destinationPath)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Credentials = CredentialCache.DefaultNetworkCredentials;
                    client.DownloadFile(originPath, destinationPath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
