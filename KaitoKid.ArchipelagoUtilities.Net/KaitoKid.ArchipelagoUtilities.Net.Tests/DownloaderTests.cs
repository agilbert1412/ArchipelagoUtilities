using FluentAssertions;
using KaitoKid.ArchipelagoUtilities.AssetDownloader;
using KaitoKid.Utilities.Interfaces;

namespace KaitoKid.ArchipelagoUtilities.Net.Tests
{
    internal class DownloaderTests
    {
        private ILogger _logger;
        private Downloader _assetDownloader;

        [SetUp]
        public void SetUp()
        {
            _logger = new TestLogger();
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
    }
}
