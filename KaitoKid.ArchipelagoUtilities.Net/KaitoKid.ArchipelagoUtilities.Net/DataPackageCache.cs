﻿using System.Collections.Generic;
using System.Linq;

namespace KaitoKid.ArchipelagoUtilities.Net
{
    public class DataPackageCache
    {
        private readonly Dictionary<string, ArchipelagoItem> _itemCacheByName;
        private readonly Dictionary<long, ArchipelagoItem> _itemCacheById;
        private readonly Dictionary<string, ArchipelagoLocation> _locationCacheByName;
        private readonly Dictionary<long, ArchipelagoLocation> _locationCacheById;

        public DataPackageCache(string snakeCaseGameName, params string[] pathsToFolder)
        {
            var itemsPath = pathsToFolder.ToList();
            itemsPath.Add($"{snakeCaseGameName}_item_table.json");
            var items = ArchipelagoItem.LoadItems(itemsPath.ToArray());

            var locationsPath = pathsToFolder.ToList();
            locationsPath.Add($"{snakeCaseGameName}_location_table.json");
            var locations = ArchipelagoLocation.LoadLocations(locationsPath.ToArray());

            _itemCacheByName = new Dictionary<string, ArchipelagoItem>();
            _itemCacheById = new Dictionary<long, ArchipelagoItem>();
            _locationCacheByName = new Dictionary<string, ArchipelagoLocation>();
            _locationCacheById = new Dictionary<long, ArchipelagoLocation>();

            foreach (var archipelagoItem in items)
            {
                _itemCacheByName.Add(archipelagoItem.Name, archipelagoItem);
                _itemCacheById.Add(archipelagoItem.Id, archipelagoItem);
            }

            foreach (var archipelagoLocation in locations)
            {
                _locationCacheByName.Add(archipelagoLocation.Name, archipelagoLocation);
                _locationCacheById.Add(archipelagoLocation.Id, archipelagoLocation);
            }
        }

        public string GetLocalItemName(long itemId)
        {
            if (!_itemCacheById.ContainsKey(itemId))
            {
                return null;
            }

            return _itemCacheById[itemId].Name;
        }

        public long GetLocalItemId(string itemName)
        {
            if (!_itemCacheByName.ContainsKey(itemName))
            {
                return -1;
            }

            return _itemCacheByName[itemName].Id;
        }

        public string GetLocalLocationName(long locationId)
        {
            if (!_locationCacheById.ContainsKey(locationId))
            {
                return null;
            }

            return _locationCacheById[locationId].Name;
        }

        public long GetLocalLocationId(string locationName)
        {
            if (!_locationCacheByName.ContainsKey(locationName))
            {
                return -1;
            }

            return _locationCacheByName[locationName].Id;
        }
    }
}
