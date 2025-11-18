namespace KaitoKid.ArchipelagoUtilities.AssetDownloader.ItemSprites
{
    public interface IAssetLocation
    {
        string GameName { get; }
        string ItemName { get; }
        int GetSeed();
    }
}
