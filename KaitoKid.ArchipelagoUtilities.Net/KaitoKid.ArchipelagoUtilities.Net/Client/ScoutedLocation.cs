using Archipelago.MultiClient.Net.Enums;
using System;
using KaitoKid.ArchipelagoUtilities.AssetDownloader.ItemSprites;

namespace KaitoKid.ArchipelagoUtilities.Net.Client
{
    public class ScoutedLocation : IAssetLocation
    {
        private const string UNKNOWN_AP_ITEM = "Item for another world in this Archipelago";

        public string LocationName { get; set; }
        public string ItemName { get; set; }
        public string PlayerName { get; set; }
        public string GameName { get; set; }
        public long LocationId { get; set; }
        public long ItemId { get; set; }
        public long PlayerId { get; set; }
        public string Classification { get; set; }
        public ItemFlags ClassificationFlags { get; set; }

        public ScoutedLocation()
        {
            // Parameterless constructor for the json stuff
        }

        public ScoutedLocation(string locationName, string itemName, string playerName, string gameName, long locationId, long itemId,
            long playerId, ItemFlags classification)
        {
            LocationName = locationName;
            ItemName = itemName;
            PlayerName = playerName;
            GameName = gameName;
            LocationId = locationId;
            ItemId = itemId;
            PlayerId = playerId;
            ClassificationFlags = classification;
            Classification = GetClassificationString();
        }

        public string GetItemName(Func<string, string> nameTransform = null)
        {
            if (string.IsNullOrWhiteSpace(ItemName))
            {
                return ItemId.ToString();
            }

            return nameTransform == null ? ItemName : nameTransform(ItemName);
        }

        public override string ToString()
        {
            return $"{PlayerName}'s {GetItemName()}";
        }

        public static string GenericItemName()
        {
            return UNKNOWN_AP_ITEM;
        }

        public string GetClassificationString()
        {
            return GetItemClassification(ClassificationFlags);
        }

        public static string GetItemClassification(ItemFlags itemFlags)
        {
            if (itemFlags.HasFlag(ItemFlags.Advancement))
            {
                return "Progression";
            }

            if (itemFlags.HasFlag(ItemFlags.NeverExclude))
            {
                return "Useful";
            }

            if (itemFlags.HasFlag(ItemFlags.Trap))
            {
                return "Trap";
            }

            return "Filler";
        }

        public static ItemFlags GetFlagsFromText(string classification)
        {
            switch (classification)
            {
                case "Progression":
                    return ItemFlags.Advancement;
                case "Trap":
                    return ItemFlags.Trap;
                case "Useful":
                    return ItemFlags.NeverExclude;
                case "Filler":
                    return ItemFlags.None;
                default:
                    return ItemFlags.None;
            }
        }

        public int GetSeed()
        {
            unchecked
            {
                var seed = 7L;
                seed = (seed * 13) + LocationId;
                seed = (seed * 13) + ItemId;
                seed = (seed * 13) + PlayerId;
                seed = seed % int.MaxValue;
                return (int)seed;
            }
        }
    }
}
