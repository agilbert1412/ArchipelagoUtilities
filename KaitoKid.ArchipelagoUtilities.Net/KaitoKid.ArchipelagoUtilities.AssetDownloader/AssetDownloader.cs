using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KaitoKid.ArchipelagoUtilities.AssetDownloader
{
    internal class AssetDownloader
    {
        public static readonly string DownloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArchipelagoUtilities");

        public static readonly string CustomAssetsDirectory = Path.Combine(DownloadDirectory, "Custom Assets");

        private const string REPOSITORY_URL = "https://raw.githubusercontent.com/agilbert1412/ArchipelagoUtilities/";
        private const string GIT_BRANCH = "AssetDownloader"; // "main";

        private const string PROJECT_PATH = "/KaitoKid.ArchipelagoUtilities.Net/KaitoKid.ArchipelagoUtilities.AssetDownloader/";

        private const string WEB_DOWNLOAD_URL = $"{REPOSITORY_URL}{GIT_BRANCH}{PROJECT_PATH}";
        private const string ASSETS_FOLDER = "Assets/";
        private const string ZIPPED_ASSETS_FOLDER = "ZippedAssets/";
        private const string WEB_DOWNLOAD_ASSETS = $"{WEB_DOWNLOAD_URL}{ASSETS_FOLDER}";
        private const string WEB_DOWNLOAD_ZIPPED_ASSETS = $"{WEB_DOWNLOAD_URL}{ZIPPED_ASSETS_FOLDER}";

        public bool DownloadGameZip(string game)
        {
            try
            {
                var zipName = $"{game}.zip";
                var webPath = $"{WEB_DOWNLOAD_ZIPPED_ASSETS}{zipName}";
                var localPath = Path.Combine(CustomAssetsDirectory, zipName);
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
                var zipFile = Path.Combine(CustomAssetsDirectory, zipName);
                var gameFolder = Path.Combine(CustomAssetsDirectory, game);
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
                var webPath = $"{WEB_DOWNLOAD_ASSETS}{game}/{assetName}";
                var localPath = Path.Combine(CustomAssetsDirectory, filePath);
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
