﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using KaitoKid.ArchipelagoUtilities.Net.Interfaces;

namespace KaitoKid.ArchipelagoUtilities.Net.Client
{
    public abstract class ArchipelagoClient
    {
        private const string MISSING_LOCATION_NAME = "Thin Air";
        protected ILogger Logger;
        private ArchipelagoSession _session;
        private DeathLinkService _deathLinkService;
        private ArchipelagoConnectionInfo _connectionInfo;
        private DataPackageCache _localDataPackage;

        private Action _itemReceivedFunction;

        public ArchipelagoSession Session => _session;
        public bool IsConnected { get; private set; }
        protected ISlotData _slotData;
        public Dictionary<string, ScoutedLocation> ScoutedLocations { get; set; }
        public bool DeathLink => _connectionInfo?.DeathLink == true;
        public abstract string GameName { get; }
        public abstract string ModName { get; }
        public abstract string ModVersion { get; }

        public ArchipelagoClient(ILogger logger, DataPackageCache dataPackageCache, Action itemReceivedFunction)
        {
            Logger = logger;
            _localDataPackage = dataPackageCache;
            _itemReceivedFunction = itemReceivedFunction;

            IsConnected = false;
            ScoutedLocations = new Dictionary<string, ScoutedLocation>();
        }

        public virtual void Connect(ArchipelagoConnectionInfo connectionInfo, out string errorMessage)
        {
            DisconnectPermanently();
            var success = TryConnect(connectionInfo, out errorMessage);
            if (!success)
            {
                DisconnectPermanently();
                return;
            }

            if (!IsMultiworldVersionSupported())
            {
                var genericVersion = _slotData.MultiworldVersion.Replace("0", "x");
                errorMessage = $"This Multiworld has been created for {ModName} version {genericVersion},\nbut this is {ModName} version {ModVersion}.\nPlease update to a compatible mod version.";
                DisconnectPermanently();
                return;
            }
        }

        private bool TryConnect(ArchipelagoConnectionInfo connectionInfo, out string errorMessage)
        {
            LoginResult result;
            try
            {
                InitSession(connectionInfo);
                var itemsHandling = ItemsHandlingFlags.AllItems;
                var minimumVersion = new Version(0, 4, 0);
                var tags = connectionInfo.DeathLink == true ? new[] { "AP", "DeathLink" } : new[] { "AP" };
                result = _session.TryConnectAndLogin(GameName, _connectionInfo.SlotName, itemsHandling, minimumVersion, tags, null, _connectionInfo.Password);
            }
            catch (Exception e)
            {
                var message = e.GetBaseException().Message;
                result = new LoginFailure(message);
                Logger.LogError($"An error occured trying to connect to archipelago. Message: {message}");
            }

            if (!result.Successful)
            {
                var failure = (LoginFailure)result;
                errorMessage = $"Failed to Connect to {_connectionInfo?.HostUrl}:{_connectionInfo?.Port} as {_connectionInfo?.SlotName}:";
                foreach (var error in failure.Errors)
                {
                    errorMessage += $"\n    {error}";
                }

                var detailedErrorMessage = errorMessage;
                foreach (var error in failure.ErrorCodes)
                {
                    detailedErrorMessage += $"\n    {error}";
                }

                Logger.LogError(detailedErrorMessage);
                DisconnectAndCleanup();
                return false; // Did not connect, show the user the contents of `errorMessage`
            }

            errorMessage = "";

            // Successfully connected, `ArchipelagoSession` (assume statically defined as `session` from now on) can now be used to interact with the server and the returned `LoginSuccessful` contains some useful information about the initial connection (e.g. a copy of the slot data as `loginSuccess._slotData`)
            var loginSuccess = (LoginSuccessful)result;
            var loginMessage = $"Connected to Archipelago server as {connectionInfo.SlotName} (Team {loginSuccess.Team}).";
            Logger.LogInfo(loginMessage);

            // Must go AFTER a successful connection attempt
            InitializeSlotData(connectionInfo.SlotName, loginSuccess.SlotData);
            if (connectionInfo.DeathLink == null)
            {
                connectionInfo.DeathLink = _slotData.DeathLink;
            }

            InitializeAfterConnection();
            return true;
        }

        protected virtual void InitializeAfterConnection()
        {
            IsConnected = true;

            _session.Items.ItemReceived += OnItemReceived;
            _session.MessageLog.OnMessageReceived += OnMessageReceived;
            _session.Socket.ErrorReceived += SessionErrorReceived;
            _session.Socket.SocketClosed += SessionSocketClosed;

            InitializeDeathLink();
            // MultiRandom = new Random(_slotData.Seed);
        }

        public void Sync()
        {
            if (!MakeSureConnected(0))
            {
                return;
            }

            _session.Socket.SendPacket(new SyncPacket());
        }

        protected virtual void InitializeDeathLink()
        {
            _deathLinkService = _session.CreateDeathLinkService();
            _deathLinkService.OnDeathLinkReceived += ReceiveDeathLink;
            if (_connectionInfo.DeathLink == true)
            {
                _deathLinkService.EnableDeathLink();
            }
            else
            {
                _deathLinkService.DisableDeathLink();
            }
        }

        public void ToggleDeathlink()
        {
            if (_connectionInfo.DeathLink == true)
            {
                _deathLinkService.DisableDeathLink();
                _connectionInfo.DeathLink = false;
            }
            else
            {
                _deathLinkService.EnableDeathLink();
                _connectionInfo.DeathLink = true;
            }
        }

        protected abstract void InitializeSlotData(string slotName, Dictionary<string, object> slotDataFields);

        private void InitSession(ArchipelagoConnectionInfo connectionInfo)
        {
            _session = ArchipelagoSessionFactory.CreateSession(connectionInfo.HostUrl, connectionInfo.Port);
            _connectionInfo = connectionInfo;
        }

        protected abstract void OnMessageReceived(LogMessage message);

        public void SendMessage(string text)
        {
            if (!MakeSureConnected())
            {
                return;
            }

            var packet = new SayPacket()
            {
                Text = text,
            };

            _session.Socket.SendPacket(packet);
        }

        private void OnItemReceived(ReceivedItemsHelper receivedItemsHelper)
        {
            if (!MakeSureConnected())
            {
                return;
            }

            _itemReceivedFunction();
        }

        public void ReportCheckedLocations(long[] locationIds)
        {
            if (!MakeSureConnected())
            {
                return;
            }

            _session.Locations.CompleteLocationChecksAsync(locationIds);
            if (_session?.RoomState == null)
            {
                return;
            }
        }

        public int GetTeam()
        {
            if (!MakeSureConnected())
            {
                return -1;
            }

            return _session.ConnectionInfo.Team;
        }

        public string GetPlayerName()
        {
            return GetPlayerName(_session.ConnectionInfo.Slot);
        }

        public string GetPlayerName(int playerSlot)
        {
            if (!MakeSureConnected())
            {
                return "Archipelago Player";
            }

            return _session.Players.GetPlayerName(playerSlot) ?? "Archipelago Player";
        }

        public string GetPlayerAlias(string playerName)
        {
            if (!MakeSureConnected())
            {
                return null;
            }

            var player = _session.Players.AllPlayers.FirstOrDefault(x => x.Name == playerName);

            return player?.Alias;
        }

        public bool PlayerExists(string playerName)
        {
            if (!MakeSureConnected())
            {
                return false;
            }

            return _session.Players.AllPlayers.Any(x => x.Name == playerName) || _session.Players.AllPlayers.Any(x => x.Alias == playerName);
        }

        public string GetPlayerGame(string playerName)
        {
            if (!MakeSureConnected())
            {
                return null;
            }

            var player = _session.Players.AllPlayers.FirstOrDefault(x => x.Name == playerName) ??
                         _session.Players.AllPlayers.FirstOrDefault(x => x.Alias == playerName);

            return player?.Game;
        }

        public string GetPlayerGame(int playerSlot)
        {
            if (!MakeSureConnected())
            {
                return null;
            }

            var player = _session.Players.AllPlayers.FirstOrDefault(x => x.Slot == playerSlot);
            return player?.Game;
        }

        public bool IsCurrentGamePlayer(string playerName)
        {
            var game = GetPlayerGame(playerName);
            return game != null && game == GameName;
        }

        public Dictionary<string, long> GetAllCheckedLocations()
        {
            if (!MakeSureConnected())
            {
                return new Dictionary<string, long>();
            }

            var allLocationsCheckedIds = _session.Locations.AllLocationsChecked;
            var allLocationsChecked = allLocationsCheckedIds.ToDictionary(GetLocationName, x => x);
            return allLocationsChecked;
        }

        public List<ReceivedItem> GetAllReceivedItems()
        {
            if (!MakeSureConnected())
            {
                return new List<ReceivedItem>();
            }

            var allReceivedItems = new List<ReceivedItem>();
            var apItems = _session.Items.AllItemsReceived.ToArray();
            for (var itemIndex = 0; itemIndex < apItems.Length; itemIndex++)
            {
                var apItem = apItems[itemIndex];
                var itemName = GetItemName(apItem);
                var playerName = GetPlayerName(apItem.Player);
                var locationName = GetLocationName(apItem);

                var receivedItem = new ReceivedItem(locationName, itemName, playerName, apItem.LocationId, apItem.ItemId, apItem.Player, itemIndex);

                allReceivedItems.Add(receivedItem);
            }

            return allReceivedItems;
        }

        public Dictionary<string, int> GetAllReceivedItemNamesAndCounts()
        {
            if (!MakeSureConnected())
            {
                return new Dictionary<string, int>();
            }

            var receivedItemsGrouped = _session.Items.AllItemsReceived.GroupBy(x => x.ItemName);
            var receivedItemsWithCount = receivedItemsGrouped.ToDictionary(x => x.Key, x => x.Count());
            return receivedItemsWithCount;
        }

        public bool HasReceivedItem(string itemName)
        {
            return HasReceivedItem(itemName, out _);
        }

        public bool HasReceivedItem(string itemName, out string sendingPlayer)
        {
            sendingPlayer = "";
            if (!MakeSureConnected())
            {
                return false;
            }

            foreach (var receivedItem in _session.Items.AllItemsReceived)
            {
                if (GetItemName(receivedItem) != itemName)
                {
                    continue;
                }

                sendingPlayer = _session.Players.GetPlayerName(receivedItem.Player);
                return true;
            }

            return false;
        }

        public int GetReceivedItemCount(string itemName)
        {
            if (!MakeSureConnected())
            {
                return 0;
            }

            return _session.Items.AllItemsReceived.Count(x => GetItemName(x) == itemName);
        }

        public Hint[] GetHints()
        {
            if (!MakeSureConnected())
            {
                return Array.Empty<Hint>();
            }

            var hintTask = _session.DataStorage.GetHintsAsync();
            hintTask.Wait(2000);
            if (hintTask.IsCanceled || hintTask.IsFaulted || !hintTask.IsCompleted || hintTask.Status != TaskStatus.RanToCompletion)
            {
                return Array.Empty<Hint>();
            }

            return hintTask.Result;
        }

        public Hint[] GetMyActiveHints()
        {
            if (!MakeSureConnected())
            {
                return Array.Empty<Hint>();
            }

            return GetHints().Where(x => !x.Found && GetPlayerName(x.FindingPlayer) == _slotData.SlotName).ToArray();
        }

        public void ReportGoalCompletion()
        {
            if (!MakeSureConnected())
            {
                return;
            }

            var statusUpdatePacket = new StatusUpdatePacket
            {
                Status = ArchipelagoClientState.ClientGoal,
            };
            _session.Socket.SendPacket(statusUpdatePacket);
        }

        public string GetLocationName(ItemInfo item)
        {
            return item?.LocationName ?? GetLocationName(item.LocationId, true);
        }

        public string GetLocationName(long locationId)
        {
            return GetLocationName(locationId, true);
        }

        public string GetLocationName(long locationId, bool required)
        {
            if (!MakeSureConnected())
            {
                return _localDataPackage.GetLocalLocationName(locationId);
            }

            var locationName = _session.Locations.GetLocationNameFromId(locationId);
            if (string.IsNullOrWhiteSpace(locationName))
            {
                locationName = _localDataPackage.GetLocalLocationName(locationId);
            }

            if (string.IsNullOrWhiteSpace(locationName))
            {
                if (required)
                {
                    Logger.LogError($"Failed at getting the location name for location {locationId}. This is probably due to a corrupted datapackage. Unexpected behaviors may follow");
                }

                return MISSING_LOCATION_NAME;
            }

            return locationName;
        }

        public bool LocationExists(string locationName)
        {
            if (locationName == null || !MakeSureConnected())
            {
                return false;
            }

            var id = GetLocationId(locationName);
            return _session.Locations.AllLocations.Contains(id);
        }

        public IReadOnlyCollection<long> GetAllMissingLocations()
        {
            if (!MakeSureConnected())
            {
                return new List<long>();
            }

            return _session.Locations.AllMissingLocations;
        }

        public long GetLocationId(string locationName)
        {
            return GetLocationId(locationName, GameName);
        }

        public long GetLocationId(string locationName, string gameName)
        {
            if (!MakeSureConnected())
            {
                return _localDataPackage.GetLocalLocationId(locationName);
            }

            var locationId = _session.Locations.GetLocationIdFromName(gameName, locationName);
            if (locationId <= 0)
            {
                locationId = _localDataPackage.GetLocalLocationId(locationName);
            }

            return locationId;
        }

        public string GetItemName(ItemInfo item)
        {
            return item?.ItemName ?? GetItemName(item.ItemId);
        }

        public string GetItemName(long itemId)
        {
            if (!MakeSureConnected())
            {
                return _localDataPackage.GetLocalItemName(itemId);
            }

            var itemName = _session.Items.GetItemName(itemId);
            if (string.IsNullOrWhiteSpace(itemName))
            {
                itemName = _localDataPackage.GetLocalItemName(itemId);
            }

            if (string.IsNullOrWhiteSpace(itemName))
            {
                Logger.LogError($"Failed at getting the item name for item {itemId}. This is probably due to a corrupted datapackage. Unexpected behaviors may follow");
                return "Error Item";
            }

            return itemName;
        }

        public void SendDeathLink(string reason = "Unknown cause")
        {
            if (!MakeSureConnected())
            {
                return;
            }

            Logger.LogMessage($"Sending a deathlink with reason [{reason}]");
            _deathLinkService.SendDeathLink(new DeathLink(GetPlayerName(), reason));
        }

        private void ReceiveDeathLink(DeathLink deathlink)
        {
            if (_connectionInfo.DeathLink != true)
            {
                return;
            }

            var deathLinkMessage = $"You have been killed by {deathlink.Source} ({deathlink.Cause})";
            Logger.LogInfo(deathLinkMessage);

            KillPlayerDeathLink(deathlink);
        }

        protected abstract void KillPlayerDeathLink(DeathLink deathlink);

        public ScoutedLocation ScoutSingleLocation(string locationName, bool createAsHint = false)
        {
            if (ScoutedLocations.ContainsKey(locationName))
            {
                return ScoutedLocations[locationName];
            }

            if (!MakeSureConnected())
            {
                Logger.LogWarning($"Could not find the id for location \"{locationName}\".");
                return null;
            }

            try
            {
                var locationId = GetLocationId(locationName);
                if (locationId == -1)
                {
                    Logger.LogWarning($"Could not find the id for location \"{locationName}\".");
                    return null;
                }

                var scoutedItemInfo = ScoutLocation(locationId, createAsHint);
                if (scoutedItemInfo == null)
                {
                    Logger.LogWarning($"Could not scout location \"{locationName}\".");
                    return null;
                }

                var itemName = GetItemName(scoutedItemInfo);
                var playerSlotName = _session.Players.GetPlayerName(scoutedItemInfo.Player);
                var classification = GetItemClassification(scoutedItemInfo.Flags);

                var scoutedLocation = new ScoutedLocation(locationName, itemName, playerSlotName, locationId,
                    scoutedItemInfo.ItemId, scoutedItemInfo.Player, classification);

                ScoutedLocations.Add(locationName, scoutedLocation);
                return scoutedLocation;
            }
            catch (Exception e)
            {
                Logger.LogError($"Could not scout location \"{locationName}\". Message: {e.Message}");
                return null;
            }
        }

        private string GetItemClassification(ItemFlags itemFlags)
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

        private ScoutedItemInfo ScoutLocation(long locationId, bool createAsHint)
        {
            var scoutTask = _session.Locations.ScoutLocationsAsync(createAsHint, locationId);
            scoutTask.Wait();
            var scoutedItems = scoutTask.Result;
            if (scoutedItems == null || !scoutedItems.Any())
            {
                return null;
            }

            return scoutedItems.First().Value;
        }

        private void SessionErrorReceived(Exception e, string message)
        {
            Logger.LogError(message);
            OnError();
            _lastConnectFailure = DateTime.Now;
            DisconnectAndCleanup();
        }

        private void SessionSocketClosed(string reason)
        {
            Logger.LogError($"Connection to Archipelago lost: {reason}");
            OnError();
            _lastConnectFailure = DateTime.Now;
            DisconnectAndCleanup();
        }

        protected virtual void OnError()
        {
        }

        public void DisconnectAndCleanup()
        {
            if (!IsConnected)
            {
                return;
            }

            if (_session != null)
            {
                _session.Items.ItemReceived -= OnItemReceived;
                _session.MessageLog.OnMessageReceived -= OnMessageReceived;
                _session.Socket.ErrorReceived -= SessionErrorReceived;
                _session.Socket.SocketClosed -= SessionSocketClosed;
                _session.Socket.DisconnectAsync();
            }
            _session = null;
            IsConnected = false;
        }

        public void DisconnectPermanently()
        {
            DisconnectAndCleanup();
            _connectionInfo = null;
        }

        private DateTime _lastConnectFailure;
        private const int THRESHOLD_TO_RETRY_CONNECTION_IN_SECONDS = 15;

        public bool MakeSureConnected(int threshold = THRESHOLD_TO_RETRY_CONNECTION_IN_SECONDS)
        {
            if (IsConnected)
            {
                return true;
            }

            if (_connectionInfo == null)
            {
                return false;
            }

            var now = DateTime.Now;
            var timeSinceLastFailure = now - _lastConnectFailure;
            if (timeSinceLastFailure.TotalSeconds < threshold)
            {
                return false;
            }

            TryConnect(_connectionInfo, out _);
            if (!IsConnected)
            {
                OnReconnectFailure();
                _lastConnectFailure = DateTime.Now;
                return false;
            }

            OnReconnectSuccess();
            return IsConnected;
        }

        protected virtual void OnReconnectSuccess()
        {
        }

        protected virtual void OnReconnectFailure()
        {
        }

        public void APUpdate()
        {
            MakeSureConnected(60);
        }

        private bool IsMultiworldVersionSupported()
        {
            var majorVersion = ModVersion.Split('.').First();
            var multiworldVersionParts = _slotData.MultiworldVersion.Split('.');
            if (multiworldVersionParts.Length < 3)
            {
                return false;
            }

            var multiworldMajor = multiworldVersionParts[0];
            var multiworldMinor = multiworldVersionParts[1];
            var multiworldFix = multiworldVersionParts[2];
            return majorVersion == multiworldMajor;
        }

        public IEnumerable<PlayerInfo> GetAllPlayers()
        {
            if (!MakeSureConnected())
            {
                return Enumerable.Empty<PlayerInfo>();
            }

            return Session.Players.AllPlayers;
        }

        public PlayerInfo GetCurrentPlayer()
        {
            if (!MakeSureConnected())
            {
                return null;
            }

            return GetAllPlayers().FirstOrDefault(x => x.Slot == _session.ConnectionInfo.Slot);
        }
    }
}