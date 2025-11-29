using FluentAssertions;
using KaitoKid.ArchipelagoUtilities.AssetDownloader;
using KaitoKid.Utilities.Interfaces;

namespace KaitoKid.ArchipelagoUtilities.Net.Tests
{
    internal class DownloaderTests
    {
        private ILogger _logger;
        private NameCleaner _cleaner;
        private Downloader _assetDownloader;

        [SetUp]
        public void SetUp()
        {
            _logger = new TestLogger();
            _cleaner = new NameCleaner();
            _assetDownloader = new Downloader();

            if (Directory.Exists(Paths.CustomAssetsDirectory))
            {
                Directory.Delete(Paths.CustomAssetsDirectory, true);
            }
        }

        [TestCase("Hollow Knight", true)]
        [TestCase("Stardew Valley", true)]
        [TestCase("ANOGF", false)]
        [TestCase("A Hat in Time", true)]
        [TestCase("Half-Life", false)]
        public void TestDownloadGameZip(string gameName, bool expectedSuccess)
        {
            // Arrange
            var expectedPath = Path.Combine(Paths.CustomAssetsDirectory, $"{gameName}.zip");

            // Assume
            File.Exists(expectedPath).Should().BeFalse();

            // Act
            var success = _assetDownloader.DownloadGameZip(gameName);

            // Assert
            success.Should().Be(expectedSuccess);
            File.Exists(expectedPath).Should().Be(expectedSuccess);
        }

        [TestCase("Hollow Knight", "Dream_Nail")]
        [TestCase("Stardew Valley", "Progressive Pickaxe")]
        public void TestUnzipGameZip(string gameName, string itemName)
        {
            // Arrange
            var zipPath = Path.Combine(Paths.CustomAssetsDirectory, $"{gameName}.zip");
            var folderPath = Path.Combine(Paths.CustomAssetsDirectory, $"{gameName}");
            var gameAssetPath = Path.Combine(folderPath, $"{gameName}.png");
            var itemAssetPath = Path.Combine(folderPath, $"{gameName}_{itemName}.png");
            var downloadSuccess = _assetDownloader.DownloadGameZip(gameName);

            // Assume
            downloadSuccess.Should().BeTrue();
            Directory.Exists(folderPath).Should().BeFalse($"{folderPath} Should not exist yet");
            File.Exists(gameAssetPath).Should().BeFalse($"{gameAssetPath} Should not exist yet");
            File.Exists(itemAssetPath).Should().BeFalse($"{itemAssetPath} Should not exist yet");

            // Act
            var success = _assetDownloader.UnzipGameZip(gameName);

            // Assert
            success.Should().BeTrue();
            Directory.Exists(folderPath).Should().BeTrue($"{folderPath} Should exist now");
            File.Exists(gameAssetPath).Should().BeTrue($"{gameAssetPath} Should exist now");
            File.Exists(itemAssetPath).Should().BeTrue($"{itemAssetPath} Should exist now");
        }

        [TestCase("Hollow Knight", "Dream_Nail")]
        [TestCase("Stardew Valley", "Progressive Pickaxe")]
        public void TestDownloadSpecificAsset(string gameName, string itemName)
        {
            // Arrange
            var zipPath = Path.Combine(Paths.CustomAssetsDirectory, $"{gameName}.zip");
            var folderPath = Path.Combine(Paths.CustomAssetsDirectory, $"{gameName}");
            var gameAssetPath = Path.Combine(folderPath, $"{gameName}.png");
            var itemAssetPath = Path.Combine(folderPath, $"{gameName}_{itemName}.png");
            var downloadSuccess = _assetDownloader.DownloadGameZip(gameName);
            var success = _assetDownloader.UnzipGameZip(gameName);
            File.Delete(gameAssetPath);
            File.Delete(itemAssetPath);

            // Assume
            downloadSuccess.Should().BeTrue();
            Directory.Exists(folderPath).Should().BeTrue($"{folderPath} Should exist already");
            File.Exists(gameAssetPath).Should().BeFalse($"{gameAssetPath} Should not exist yet");
            File.Exists(itemAssetPath).Should().BeFalse($"{itemAssetPath} Should not exist yet");

            // Act
            var successGameAsset = _assetDownloader.DownloadSpecificItemAsset(gameName, itemName);

            // Assert
            successGameAsset.Should().BeTrue();
            Directory.Exists(folderPath).Should().BeTrue($"{folderPath} Should still exist");
            File.Exists(gameAssetPath).Should().BeFalse($"{gameAssetPath} Should still not exist");
            File.Exists(itemAssetPath).Should().BeTrue($"{itemAssetPath} Should exist now");
        }
    }
}
