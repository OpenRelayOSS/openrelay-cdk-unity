//------------------------------------------------------------------------------
// <copyright file="OpenRelayClient.cs" company="FurtherSystem Co.,Ltd.">
// Copyright (C) 2018 FurtherSystem Co.,Ltd. All rights reserved.
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
// </copyright>
// <author>FurtherSystem Co.,Ltd.</author>
// <email>info@furthersystem.com</email>
// <summary>
// OpenRelay Client Scripts.
// </summary>
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Com.FurtherSystems.OpenRelay
{
    using ObjectId = UInt16;
    using PlayerId = UInt16;
    internal enum RelayCode
    {
        CONNECT = 0,
        JOIN = 1,
        LEAVE = 2,
        RELAY = 3,
        TIMEOUT = 4,
        REJOIN = 5,
        SET_LEGACY_MAP = 6,
        GET_LEGACY_MAP = 7,
        GET_USERS = 8,
        SET_MASTER = 9,
        GET_MASTER = 10,
        GET_SERVER_TIMESTAMP = 11,
        RELAY_LATEST = 12,
        GET_LATEST = 13,
        SET_LOBBY_MAP = 14,
        GET_LOBBY_MAP = 15,
        SET_MASK = 16,
        GET_MASK = 17,
        PUSH_STACK = 18,
        FETCH_STACK = 19,
        REPLAY_JOIN = 20,
        RELAY_STREAM = 21,
        LOAD_PLAYER = 22,
        // 100 - 199 Platform Dependency Relay Code
        UNITY_CDK_RELAY = 100,
        UNITY_CDK_RELAY_LATEST = 101,
        UNITY_CDK_GET_LATEST = 102,
        UE4_CDK_RELAY = 110,
        UE4_CDK_RELAY_LATEST = 111,
        UE4_CDK_GET_LATEST = 112,
        // 200 - 255  User Customize Relay Code
    }

    static class Definitions
    {
        public const byte FrameVersion = 19;
        public const string PropKeyLegacy = "LEGACY";
        public const string PropKeyGenericPrefix = "OR_SHARE_PROP_";
        public const string PropKeyPlayerPrefix = "OR_PLAYER_PROP_";
        public const string PropKeyLobbyPrefix = "OR_LOBBY_PROP_";
    }

    public enum LogLevel
    {
        None = 0,
        Normal = 1,
        Verbose = 2,
        VeryVerbose = 3,
    }

    public enum ConnectionState
    {
        ConnectingDNS = 17,//ConnectingToNameServer
        ResolvedDNS = 18,//ConnectedToNameServer
        DisconnectingDNS = 19,//DisconnectingFromNameServer

        CorrectVersion = 1,//PeerCreated,
        Auth = 2,//Authenticating,
        Authed = 3,//Authenticated,

        ConnectingEntry = 13,//ConnectingToMasterserver
        ConnectedEntry = 16,//ConnectedToMasterserver
        JoiningEntry = 4,//JoiningLobby,
        JoinedEntry = 5,//JoinedLobby,
        LeavingEntry = 6,//DisconnectingFromMasterserver,

        ConnectingRoom = 7,//ConnectingToGameserver
        ConnectedRoom = 8,//ConnectedToGameserver
        JoiningRoom = 9,//Joining
        JoinedRoom = 10,//Joined
        LeavingRoom = 11,//Leaving
        DisconnectingRoom = 12,//DisconnectingFromGameserver,

        Disconnecting = 14,//
        Disconnected = 15,//
    }

    public static partial class OpenRelayClient
    {
        public const string UA_UNITY_CDK = "Unity-CDK";
        public const string UNITY_CDK_VERSION = "0.9.8";
        private const string BASE_URL = "http://";
        private const bool AutoEntry = true;

        public static ObjectId AllocateObjectId()
        {
            // ObjectId ... UNIQUE_ON_PLAYER
            var newObjectId = (ObjectId)(_objectIds.Count() + 1);
            _objectIds.Add(newObjectId);
            _latestObjectId = (ObjectId)_objectIds.Count();
            return _latestObjectId;
        }

        private static ConnectionState _currentState = ConnectionState.Disconnected;
        public static ConnectionState ConnectionState
        {
            get
            {
                if (_roomJoined)
                {
                    return ConnectionState.JoinedRoom;
                }
                else if (_roomJoining)
                {
                    return ConnectionState.JoiningRoom;
                }
                else if (_entryJoined)
                {
                    return ConnectionState.JoinedEntry;
                }
                else if (_connected)
                {
                    return ConnectionState.JoiningEntry;
                }
                else
                {
                    return ConnectionState.ConnectedEntry;
                }
            }
        }

        private static RoomInfo _room;
        public static RoomInfo Room
        {
            get
            {
                return _room;
            }
        }

        private static UserSession _player = new UserSession(1, (ObjectId)1, true, true);
        public static UserSession Player
        {
            get
            {
                return _player;
            }
        }

        private static List<ObjectId> _objectIds = new List<PlayerId>() { (ObjectId)1 };
        private static ObjectId _latestObjectId = (ObjectId)_objectIds.Count();
        public static List<ObjectId> ObjectIds
        {
            get
            {
                return _objectIds;
            }
        }

        private static UserSession _masterClient = _player;
        public static UserSession MasterClient
        {
            get
            {
                return _masterClient;
            }
        }

        private static List<UserSession> _players = new List<UserSession>() { _player };
        public static UserSession[] PlayerList
        {
            get
            {
                return _players.ToArray();
            }
        }

        private static OpenRelayCDKSettings _settings = (OpenRelayCDKSettings)Resources.Load("OpenRelayCDKSettings", typeof(OpenRelayCDKSettings));
        public static OpenRelayCDKSettings Settings
        {
            get
            {
                return _settings;
            }
        }

        private static List<IOrCallbacks> callbacks = new List<IOrCallbacks>();
        private static SubscriberListener subscriberListener;
        private static DealerListener dealerListener;
        
        public static void RegistCallbacks(IOrCallbacks cbs) 
        {
            OrLog(LogLevel.Normal,"Regist callbacks");
            callbacks.Add(cbs);
            RegistCallbacksAll(cbs);
        }

        public static void UnRegistCallbacks(IOrCallbacks cbs)
        {
            OrLog(LogLevel.Normal, "Unregist callbacks");
            UnRegistCallbacksAll(cbs);
            callbacks.Remove(cbs);
        }

        private static void RegistCallbacksAll(IOrCallbacks cbs)
        {
            OrLog(LogLevel.Normal, "Regist callbacks all");
            OnConnectedToMasterCall += cbs.OnConnectedToMaster;
            OnConnectedToOpenRelayCall += cbs.OnConnectedToOpenRelay;
            OnConnectionFailCall += cbs.OnConnectionFail;
            OnOpenRelayRoomPropertiesChangedCall += cbs.OnOpenRelayRoomPropertiesChanged;
            OnCreatedRoomCall += cbs.OnCreatedRoom;
            OnDisconnectedCall += cbs.OnDisconnected;
            //OnFailedToConnectCall += cbs.OnFailedToConnect;
            OnJoinedLobbyCall += cbs.OnJoinedLobby;
            OnJoinedRoomCall += cbs.OnJoinedRoom;
            OnReadyNewPlayerCall += cbs.OnReadyNewPlayer;
            OnLeftLobbyCall += cbs.OnLeftLobby;
            OnLeftRoomCall += cbs.OnLeftRoom;
            OnOpenRelayCreateRoomFailedCall += cbs.OnOpenRelayCreateRoomFailed;
            OnOpenRelayJoinRoomFailedCall += cbs.OnOpenRelayJoinRoomFailed;
            OnOpenRelayPlayerConnectedCall += cbs.OnOpenRelayPlayerConnected;
            OnOpenRelayPlayerDisconnectedCall += cbs.OnOpenRelayPlayerDisconnected;
            OnOpenRelayPlayerPropertiesChangedCall += cbs.OnOpenRelayPlayerPropertiesChanged;
            OnRoomListUpdateCall += cbs.OnRoomListUpdate;
            OnLobbyStatisticsUpdateCall += cbs.OnLobbyStatisticsUpdate;
            //OnLoadPlayerCall +=
        }

        private static void UnRegistCallbacksAll(IOrCallbacks cbs)
        {
            OrLog(LogLevel.Normal, "Unregist callbacks all");
            OnConnectedToMasterCall -= cbs.OnConnectedToMaster;
            OnConnectedToOpenRelayCall -= cbs.OnConnectedToOpenRelay;
            OnConnectionFailCall -= cbs.OnConnectionFail;
            OnOpenRelayRoomPropertiesChangedCall -= cbs.OnOpenRelayRoomPropertiesChanged;
            OnCreatedRoomCall -= cbs.OnCreatedRoom;
            OnDisconnectedCall -= cbs.OnDisconnected;
            //OnFailedToConnectCall -= cbs.OnFailedToConnect;
            OnJoinedLobbyCall -= cbs.OnJoinedLobby;
            OnJoinedRoomCall -= cbs.OnJoinedRoom;
            OnReadyNewPlayerCall -= cbs.OnReadyNewPlayer;
            OnLeftLobbyCall -= cbs.OnLeftLobby;
            OnLeftRoomCall -= cbs.OnLeftRoom;
            OnOpenRelayCreateRoomFailedCall -= cbs.OnOpenRelayCreateRoomFailed;
            OnOpenRelayJoinRoomFailedCall -= cbs.OnOpenRelayJoinRoomFailed;
            OnOpenRelayPlayerConnectedCall -= cbs.OnOpenRelayPlayerConnected;
            OnOpenRelayPlayerDisconnectedCall -= cbs.OnOpenRelayPlayerDisconnected;
            OnOpenRelayPlayerPropertiesChangedCall -= cbs.OnOpenRelayPlayerPropertiesChanged;
            OnRoomListUpdateCall -= cbs.OnRoomListUpdate;
            OnLobbyStatisticsUpdateCall -= cbs.OnLobbyStatisticsUpdate;
            //OnLoadPlayerCall -=
        }

        private static void ClearCallbacks()
        {
            OnConnectedToMasterCall = null;
            OnConnectedToOpenRelayCall = null;
            OnConnectionFailCall = null;
            OnOpenRelayRoomPropertiesChangedCall = null;
            OnCreatedRoomCall = null;
            OnDisconnectedCall = null;
            OnFailedToConnectCall = null;
            OnJoinedLobbyCall = null;
            OnJoinedRoomCall = null;
            OnReadyNewPlayerCall = null;
            OnLeftLobbyCall = null;
            OnLeftRoomCall = null;
            OnOpenRelayCreateRoomFailedCall = null;
            OnOpenRelayJoinRoomFailedCall = null;
            OnOpenRelayPlayerConnectedCall = null;
            OnOpenRelayPlayerDisconnectedCall = null;
            OnOpenRelayPlayerPropertiesChangedCall = null;
            OnRoomListUpdateCall = null;
            OnLobbyStatisticsUpdateCall = null;
            OnLoadPlayerCall = null;
        }

        private static void ReRegistCallbacks()
        {
            callbacks.ForEach((IOrCallbacks cb) => RegistCallbacksAll(cb));
        }

        #region OpenRelay base callbacks start

        private delegate void OnConnectedToMaster();
        private static OnConnectedToMaster OnConnectedToMasterCall;

        private delegate void OnConnectedToOpenRelay();
        private static OnConnectedToOpenRelay OnConnectedToOpenRelayCall;

        private delegate void OnConnectionFail(string disconnectMessage);
        private static OnConnectionFail OnConnectionFailCall;

        private delegate void OnOpenRelayRoomPropertiesChanged(Hashtable changed);
        private static OnOpenRelayRoomPropertiesChanged OnOpenRelayRoomPropertiesChangedCall;

        private delegate void OnCreatedRoom();
        private static OnCreatedRoom OnCreatedRoomCall;

        private delegate void OnDisconnected(string disconnectMessage);
        private static OnDisconnected OnDisconnectedCall;

        private delegate void OnFailedToConnect(string disconnectMessage);
        private static OnFailedToConnect OnFailedToConnectCall;

        private delegate void OnJoinedLobby();
        private static OnJoinedLobby OnJoinedLobbyCall;

        private delegate void OnJoinedRoom();
        private static OnJoinedRoom OnJoinedRoomCall;

        private delegate void OnReadyNewPlayer();
        private static OnReadyNewPlayer OnReadyNewPlayerCall;

        private delegate void OnLeftLobby();
        private static OnLeftLobby OnLeftLobbyCall;

        private delegate void OnLeftRoom();
        private static OnLeftRoom OnLeftRoomCall;

        private delegate void OnOpenRelayCreateRoomFailed(short returnCode, string failedMessage);
        private static OnOpenRelayCreateRoomFailed OnOpenRelayCreateRoomFailedCall;

        private delegate void OnOpenRelayJoinRoomFailed(short returnCode, string failedMessage);
        private static OnOpenRelayJoinRoomFailed OnOpenRelayJoinRoomFailedCall;

        private delegate void OnOpenRelayPlayerConnected(UserSession player);
        private static OnOpenRelayPlayerConnected OnOpenRelayPlayerConnectedCall;

        private delegate void OnOpenRelayPlayerDisconnected(UserSession player);
        private static OnOpenRelayPlayerDisconnected OnOpenRelayPlayerDisconnectedCall;

        private delegate void OnOpenRelayPlayerPropertiesChanged(object[] playerAndUpdatedProps);
        private static OnOpenRelayPlayerPropertiesChanged OnOpenRelayPlayerPropertiesChangedCall;

        private delegate void OnRoomListUpdate(List<RoomInfo> roomList);
        private static OnRoomListUpdate OnRoomListUpdateCall;

        private delegate void OnLobbyStatisticsUpdate(List<RoomLimit> roomLimits);
        private static OnLobbyStatisticsUpdate OnLobbyStatisticsUpdateCall;

        private delegate void OnLoadPlayer(List<string> fetchNames, byte position, byte total);
        private static OnLoadPlayer OnLoadPlayerCall;

        #endregion OpenRelay base callbacks end

        #region Provisional variables

        private static bool _connected = false;
        private static bool _entryJoined = false;
        private static bool _roomJoining = false;
        private static bool _roomJoined = false;
        private static bool _roomJoinComplete = false;
        private static bool _PropertiesInitializing = false;
        private static bool _PropertiesReady = false;
        private static bool _leaveComplete = false;

        #endregion
        public enum DestinationCode : byte
        {
            Broadcast = 0, // exclude myself.
            StrictBroadcast = 1, // include myself
            MasterOnly = 2, // master only
            Include = 3, // include only
            Exclude = 4, // exclude only
        }

        public enum EventCaching : byte
        {
            DoNotCache = 0,
            AddToRoomCache = 4,
            AddToRoomCacheGlobal = 5,
            RemoveFromRoomCache = 6,
            RemoveFromRoomCacheForActorsLeft = 7,
            SliceIncreaseIndex = 10,
            SliceSetIndex = 11,
            SlicePurgeIndex = 12,
            SlicePurgeUpToIndex = 13,
        }

        [Obsolete("Remove in future.")]
        public class SyncOptions
        {
            public readonly static SyncOptions Default = new SyncOptions();
            public SyncOptions()
            {
                CachingOption = EventCaching.DoNotCache;
                InterestGroup = 0;
                Destinations = new PlayerId[] { };
                DestCode = DestinationCode.Broadcast;
                SequenceChannel = 0;
            }
            public SyncOptions(DestinationCode dest)
            {
                CachingOption = EventCaching.DoNotCache;
                InterestGroup = 0;
                Destinations = new PlayerId[] { };
                SequenceChannel = 0;

                DestCode = dest;
            }
            public EventCaching CachingOption;
            public byte InterestGroup;
            public PlayerId[] Destinations;
            public DestinationCode DestCode;
            public byte SequenceChannel;
            //public WebFlags Flags = WebFlags.Default;
        }

        public class BroadcastFilter
        {
            public readonly static BroadcastFilter Default = new BroadcastFilter();
            public BroadcastFilter()
            {
                Destinations = new PlayerId[] { };
                DestCode = DestinationCode.Broadcast;
                SequenceChannel = 0;
            }
            public BroadcastFilter(DestinationCode dest)
            {
                Destinations = new PlayerId[] { };
                SequenceChannel = 0;

                DestCode = dest;
            }
            public PlayerId[] Destinations;
            public DestinationCode DestCode;
            public byte SequenceChannel;
        }

        private static StateHandler stateHandler;

        public static void Connect(string version, string serverAddress, string entryPort)
        {
            _settings.ServerAddress = serverAddress;
            _settings.EntryPort = entryPort;
            Connect(version);
        }

        public static void Connect(string version)
        {
            SetupLog(_settings.LogVerboseLevel, _settings.LogLabelColor);
            OrLog(LogLevel.Normal, "Log Initialized");
            //todo destroy logic here
            _room = null;

            var go = new GameObject("StateHandler", typeof(StateHandler));
            OrLog(LogLevel.Normal, "StateHandler Created");
            go.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector | HideFlags.HideAndDontSave;
            stateHandler = go.GetComponent<StateHandler>();
            OrLog(LogLevel.Normal, "StateHandler Initialized");

            OrLog(LogLevel.Normal, "Initialized ok");
        }

        public static void Connect()
        {
            Connect(string.Empty);
        }

        public static void Disconnect()
        {
            if (inRoom)
            {
                LeaveRoom();
            }
            LeaveEntry();
        }

        private static void InitializeClient()
        {
            _player = new UserSession(1, (ObjectId)1, true, true);
            _players = new List<UserSession>() { _player };
        }

        private static void InitializeEntry()
        {
            _connected = false;
            _entryJoined = false;
        }

        private static void InitializeRoom()
        {
            subscriberListener?.Stop();
            dealerListener?.Stop();
            subscriberListener = null;
            dealerListener = null;
            subscriberListener = new SubscriberListener(HandleMessage);
            dealerListener = new DealerListener();
            _roomJoining = false;
            _roomJoined = false;
            _roomJoinComplete = false;
            _PropertiesInitializing = false;
            _PropertiesReady = false;
            _objectIds = new List<PlayerId>() { (ObjectId)1 };
            _masterClient = _player;
            _room = null;
            _leaveComplete = false;
        }

        public static void JoinEntry()
        {
            stateHandler.StartCoroutine(stateHandler.LogonEntry());
        }

        private static void LeaveEntry()
        {
            stateHandler.StartCoroutine(stateHandler.LogoutEntry());
            InitializeEntry();
        }

        public static RoomInfo[] GetRoomList()
        {
            return stateHandler.RoomList.ToArray();
        }

        private static void StartCoroutine(RoomInfo[] roomInfo)
        {
            throw new NotImplementedException();
        }

        public static void CreateAndJoinRoom(string presetRoomName, RoomOptions presetRoomOptions, UInt16 maxPlayers)
        {
            stateHandler.StartCoroutine(stateHandler.CreateAndJoinRoom(presetRoomName, maxPlayers, presetRoomOptions));
        }

        public static void CreateOrJoinRoom(string presetRoomName, RoomOptions presetRoomOptions, UInt16 maxPlayers)
        {
            stateHandler.StartCoroutine(stateHandler.CreateOrJoinRoom(presetRoomName, maxPlayers, presetRoomOptions));
        }

        //public static void JoinRandomRoom(RoomOptions presetRoomOptions, byte maxPlayers)
        //{
        //    Room.SetPropertiesListedInLobbyCreate(presetRoomOptions.RoomPropertiesForLobby, true);
        //    Room.SetPropertiesCreate(presetRoomOptions.RoomProperties, true);
        //}

        public static void JoinRoom(string room)
        {
            stateHandler.StartCoroutine(stateHandler.PrepareAndJoinRoom(room, new string[] { }));
        }

        public static void LeaveRoom()
        {
            stateHandler.StartCoroutine(stateHandler.LeaveRoom());
        }

        public static bool Connected
        {
            get { return _connected; }
        }

        public static bool ConnectedAndReady
        {
            get { return _connected; }
        }

        public static bool isMasterClient
        {
            get { return _masterClient.ID == _player.ID; }
        }

        public static string playerName { get; set; }

        public static int ServerTimestamp
        {
            get { return DateTime.Now.Millisecond; }
        }

        public static bool insideLobby
        {
            get { return _entryJoined && !_roomJoined; }
        }

        public static bool inRoom
        {
            get { return _roomJoined; }
        }

        public static bool offlineMode
        {
            get { return false; }
            set { }
        }

        public static bool IsMessageQueueRunning { get; set; } = true;

        public static int sendRate { get; set; }

        public static int sendRateOnSerialize { get; set; }

        public static bool automaticallySyncScene { get; set; }

        public static void Sync(byte type, byte[] content, ObjectId senderId, BroadcastFilter filter)
        {
            dealerListener.Sync(type, content, senderId, filter);
        }

        public static void SyncLatest(byte type, byte[] content, ObjectId senderId, BroadcastFilter filter)
        {
            dealerListener.SyncLatest(type, content, senderId, filter);
        }

        public static void SyncStream(byte type, byte[] content, ObjectId senderId, BroadcastFilter filter)
        {
            dealerListener.SyncStream(type, content, senderId, filter);
        }

        public static void SyncPlatform(byte type, byte[] content, ObjectId senderId, BroadcastFilter filter)
        {
            dealerListener.SyncPlatform(type, content, senderId, filter);
        }

        public static void SyncLatestPlatform(byte type, byte[] content, ObjectId senderId, BroadcastFilter filter)
        {
            dealerListener.SyncLatestPlatform(type, content, senderId, filter);
        }

        public delegate void OnSync(byte eventcode, byte[] content, PlayerId senderPlayerId, ObjectId senderObjectId);
        public static OnSync OnSyncCall;
        public delegate void OnSyncLatest(byte eventcode, byte[] content, PlayerId senderPlayerId, ObjectId senderObjectId);
        public static OnSyncLatest OnSyncLatestCall;
        public delegate void OnSyncStream(byte eventcode, byte[] content, PlayerId senderPlayerId, ObjectId senderObjectId);
        public static OnSyncStream OnSyncStreamCall;
        public delegate void OnSyncPlatform(byte eventcode, byte[] content, PlayerId senderPlayerId, ObjectId senderObjectId);
        public static OnSyncPlatform OnSyncPlatformCall;
        public delegate void OnSyncLatestPlatform(byte eventcode, byte[] content, PlayerId senderPlayerId, ObjectId senderObjectId);
        public static OnSyncLatestPlatform OnSyncLatestPlatformCall;

        public static void OnDestroy()
        {
            OrLog(LogLevel.Verbose,"OpenRelayClient called OnDestroy");
            InitializeRoom();
            InitializeEntry();
            InitializeClient();
        }

    }
}
