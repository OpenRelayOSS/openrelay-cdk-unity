//------------------------------------------------------------------------------
// <copyright file="StateHandler.cs" company="FurtherSystem Co.,Ltd.">
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
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Com.FurtherSystems.OpenRelay
{
    using ObjectId = UInt16;
    using PlayerId = UInt16;
    public static partial class OpenRelayClient
    {
        internal class StateHandler : MonoBehaviour
        {

            [StructLayout(LayoutKind.Explicit)]
            internal class RoomResponse
            {
                [FieldOffset(0)]
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                public byte[] Id; // 16byte
                [FieldOffset(16)]
                public UInt16 Capacity;
                [FieldOffset(18)]
                public UInt16 UserCount; // 4byte
                [FieldOffset(20)]
                public UInt16 StfDealPort;
                [FieldOffset(22)]
                public UInt16 StfSubPort; // 4byte
                [FieldOffset(24)]
                public UInt16 StlDealPort;
                [FieldOffset(26)]
                public UInt16 StlSubPort; // 4byte
                [FieldOffset(28)]
                public byte QueuingPolicy;
                [FieldOffset(29)]
                public byte Flags;
                [FieldOffset(30)]
                public byte NameLen;
                [FieldOffset(31)]
                public byte FilterLen; // 4byte
                //[FieldOffset(30)]
                //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
                //public byte[] Name; // 256byte
                //[FieldOffset(46)]
                //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
                //public byte[] Filter; // 256byte
                //[FieldOffset(20)]
                //public byte ListenMode;
                //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
                //[FieldOffset(21)]
                //public byte[] _alignment1; // 4byte
                //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
                //[FieldOffset(24)]
                //public byte[] ListenAddressIpv4; // 4byte
                //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
                //[FieldOffset(28)]
                //public byte[] ListenAddressIpv6; // 16byte

                public RoomResponse()
                {
                    Id = new byte[16];
                    Capacity = (UInt16)0;
                    UserCount = (UInt16)0;
                    StfDealPort = (UInt16)0;
                    StfSubPort = (UInt16)0;
                    StlDealPort = (UInt16)0;
                    StlSubPort = (UInt16)0;
                    QueuingPolicy = (byte)0;
                    Flags = (byte)0;
                    NameLen = (byte)0;
                    FilterLen = (byte)0;
                    //Name = new byte[256];
                    //Filter = new byte[256];
                    //ListenMode = (byte)3;
                    //_alignment1 = new byte[3];
                    //ListenAddressIpv4 = new byte[4];
                    //ListenAddressIpv6 = new byte[16];
                }
            }
            public IEnumerator Version()
            {
                OrLog(LogLevel.Verbose, "get version start");
                UnityWebRequest webRequest = UnityWebRequest.Get(BASE_URL + _serverAddress + ":" + _entryPort + "/version");
                webRequest.SetRequestHeader("User-Agent", UA_UNITY_CDK);
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError)
                {
                    OrLogError(LogLevel.Info, webRequest.error);
                    OnFailedToConnectCall( "failed polling");//TODO error handle case;
                    yield break;
                }
                var responseAPIversion = webRequest.downloadHandler.text;
                OrLog(LogLevel.Verbose, "require OpenRelay api version:" + responseAPIversion);
                OrLog(LogLevel.Verbose, "current CDK api version:" + UNITY_CDK_VERSION);

                if (responseAPIversion != UNITY_CDK_VERSION)
                {
                    OrLogError(LogLevel.Info, "bad version, please update CDK or APPLICATION. require:" + responseAPIversion + " current:" + UNITY_CDK_VERSION);
                    OnFailedToConnectCall("failed polling");//TODO error handle case;
                    yield break;
                }
                OrLog(LogLevel.Verbose, "get version end");
                _connected = true;
            }
            public IEnumerator LogonEntry()
            {
                OrLog(LogLevel.Verbose, "post logon start");
                UnityWebRequest webRequest = UnityWebRequest.Post(BASE_URL + _serverAddress + ":" + _entryPort + "/logon", string.Empty);
                webRequest.SetRequestHeader("User-Agent", UA_UNITY_CDK);
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError)
                {
                    OrLogError(LogLevel.Info, webRequest.error);
                    // TODO VERSION ERROR HANDLE CALLBACK
                    yield break;
                }
                if (webRequest.responseCode == 200)
                {
                    _entryJoined = true;
                }
                else
                {
                    // TODO VERSION ERROR HANDLE CALLBACK
                    yield break;
                }
                InitializeRoom();
                OrLog(LogLevel.Verbose, "post logon end");
            }

            public bool GetRoomListLock = false;

            public List<RoomInfo> RoomList = new List<RoomInfo>();

            private IEnumerator GetRoomList()
            {
                if (!_entryJoined || _roomJoining || _roomJoined || GetRoomListLock) yield break;

                GetRoomListLock = true;
                OrLog(LogLevel.Verbose, "Get rooms info");
                UnityWebRequest webRequest = UnityWebRequest.Get(BASE_URL + _serverAddress + ":" + _entryPort + "/rooms");
                webRequest.SetRequestHeader("User-Agent", UA_UNITY_CDK);
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError)
                {
                    OrLogError(LogLevel.Info, webRequest.error);
                    // TODO VERSION ERROR HANDLE CALLBACK
                    yield break;
                }

                var streamReader = new MemoryStream(webRequest.downloadHandler.data);
                var messageReader = new EndiannessBinaryReader(streamReader);

                if (webRequest.responseCode != 200)
                {
                    OrLog(LogLevel.Verbose, "post create room failed. http status code:" + webRequest.responseCode);
                    OnOpenRelayCreateRoomFailedCall((short)webRequest.responseCode, " failed polling");

                    yield break;
                }

                var responseCode = (ResponseCode)messageReader.ReadUInt16();
                var roomListLen = messageReader.ReadUInt16();

                if (responseCode == ResponseCode.OPENRELAY_RESPONSE_CODE_OK_NO_ROOM)
                {
                    OrLog(LogLevel.Verbose, "no room");
                }
                else if (responseCode == ResponseCode.OPENRELAY_RESPONSE_CODE_OK)
                {
                    List<RoomInfo> list = new List<RoomInfo>();
                    for (int index = 0; index < roomListLen; index++)
                    {
                        var responseSize = Marshal.SizeOf(typeof(RoomResponse));
                        var responseBytes = messageReader.ReadBytes(responseSize);
                        OrLog(LogLevel.VeryVerbose, "raw response:" + BitConverter.ToString(responseBytes));
                        var ptr = Marshal.AllocCoTaskMem(responseSize);
                        var gch = GCHandle.Alloc(responseBytes, GCHandleType.Pinned);
                        RoomResponse response;
                        try
                        {
                            response = Marshal.PtrToStructure<RoomResponse>(gch.AddrOfPinnedObject());
                            OrLog(LogLevel.Verbose, "read bytes get room Id:" + BitConverter.ToString(response.Id));
                            OrLog(LogLevel.Verbose, "read bytes get room Capacity:" + response.Capacity.ToString());
                            OrLog(LogLevel.Verbose, "read bytes get room StfDealPort:" + response.StfDealPort.ToString());
                            OrLog(LogLevel.Verbose, "read bytes get room StfSubPort:" + response.StfSubPort.ToString());
                            OrLog(LogLevel.Verbose, "read bytes get room StlDealPort:" + response.StlDealPort.ToString());
                            OrLog(LogLevel.Verbose, "read bytes get room StlSubPort:" + response.StlSubPort.ToString());
                            OrLog(LogLevel.Verbose, "read bytes get room QueuingPolicy:" + response.QueuingPolicy.ToString());
                            OrLog(LogLevel.Verbose, "read bytes get room Flags:" + response.Flags.ToString());
                            OrLog(LogLevel.Verbose, "read bytes get room NameLen:" + response.NameLen.ToString());
                            OrLog(LogLevel.Verbose, "read bytes get room FilterLen:" + response.FilterLen.ToString());
                        }
                        catch (Exception e)
                        {
                            OrLogError(LogLevel.Info, "handle error: " + e.Message);
                            OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                            OrLogError(LogLevel.Verbose, "post create room failed ");
                            gch.Free();

                            yield break;
                        }
                        gch.Free();

                        byte[] name = new byte[] { };
                        //if (0 < response.NameLen)
                        //{
                            name = messageReader.ReadBytes(256);
                            OrLog(LogLevel.Verbose, "read bytes get room Name:" + Encoding.UTF8.GetString(name).Substring(0, response.NameLen));
                        //}
                        byte[] filter;
                        //if (0 < response.FilterLen)
                        //{
                            filter = messageReader.ReadBytes(256);
                            OrLog(LogLevel.Verbose, "read bytes get room Filter:" + BitConverter.ToString(filter).Substring(0, response.FilterLen));
                        //}
                        var listenMode = messageReader.ReadByte();
                        OrLog(LogLevel.Verbose, "read bytes get room listen mode:" + listenMode);
                        messageReader.ReadBytes(3); // alignment 3bytes for 4byte alignment.
                        var ipv4Bytes = messageReader.ReadBytes(4);
                        var ipv4Addr = new IPAddress(ipv4Bytes).ToString();
                        //var ipv4Addr = _serverAddress; // TODO ISSUE 24 provisional fix
                        OrLog(LogLevel.Verbose, "read bytes get room listen ipv4 addr:" + ipv4Addr);
                        var ipv6Bytes = messageReader.ReadBytes(16);
                        var ipv6Addr = new IPAddress(ipv6Bytes).ToString();
                        OrLog(LogLevel.Verbose, "read bytes get room listen ipv6 addr:" + ipv6Addr);
                        var roomInfo = new RoomInfo(Encoding.UTF8.GetString(name).Substring(0, response.NameLen),
                            response.Id,
                            listenMode,
                            ipv4Addr,
                            ipv6Addr,
                            response.StfDealPort.ToString(),
                            response.StfSubPort.ToString(),
                            response.StlDealPort.ToString(),
                            response.StlSubPort.ToString(),
                            response.Capacity,
                            new RoomOptions());
                        roomInfo = new RoomInfo(response.UserCount, roomInfo);
                        list.Add(roomInfo);
                    }

                    foreach (var room in list)
                    {
                        yield return StartCoroutine(GetRoomProperties(room));
                        yield return new WaitForSeconds(0.01f);
                    }

                    RoomList = list;
                    OnRoomListUpdateCall(RoomList);
                }

                GetRoomListLock = false;
            }

            private IEnumerator UpdateRoomList()
            {
                while (true)
                {
                    if (_roomJoined || _roomJoining)
                    {
                        yield return new WaitForSeconds(1f);
                    }
                    else
                    {
                        yield return StartCoroutine(GetRoomList());
                        yield return new WaitForSeconds(6f);
                    }
                }
            }

            public IEnumerator GetRoomInfo()
            {
                OrLog(LogLevel.Verbose, "Get room info start");
                UnityWebRequest webRequest = UnityWebRequest.Get(BASE_URL + _serverAddress + ":" + _entryPort + "/room/info/");
                webRequest.SetRequestHeader("User-Agent", UA_UNITY_CDK);
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError)
                {
                    OrLogError(LogLevel.Info, webRequest.error);
                    // TODO VERSION ERROR HANDLE CALLBACK
                    yield break;
                }
                if (webRequest.responseCode == 200)
                {
                }
                else
                {
                    // TODO VERSION ERROR HANDLE CALLBACK
                    yield break;
                }
                OrLog(LogLevel.Verbose, "Get room info end");
            }

            bool createAndJoinRoomAbort = false;
            public IEnumerator CreateAndJoinRoom(string roomName, UInt16 maxPlayer, RoomOptions presetRoomOptions)
            {
                OrLog(LogLevel.Verbose, "post create and join room start");
                yield return StartCoroutine(CreateRoom(roomName, maxPlayer, presetRoomOptions, false));
                if (createAndJoinRoomAbort) yield break;
                //prepare join
                //player variables :name load
                //prepare complete
                yield return new WaitForSeconds(0.05f);
                OrLog(LogLevel.Verbose, "relay message handle start.");
                stateHandler.StartQueue();

                yield return StartCoroutine(PrepareAndJoinRoom(roomName, new string[] { }));
                OrLog(LogLevel.Verbose, "post create and join room end");
                yield break;
            }

            public IEnumerator CreateOrJoinRoom(string roomName, UInt16 maxPlayer, RoomOptions presetRoomOptions)
            {
                OrLog(LogLevel.Verbose, "post create or join room start");
                yield return StartCoroutine(CreateRoom(roomName, maxPlayer, presetRoomOptions, true));
                //prepare join
                //player variables :name load
                //prepare complete
                yield return new WaitForSeconds(0.05f);
                OrLog(LogLevel.Verbose, "relay message handle start.");
                stateHandler.StartQueue();

                yield return StartCoroutine(PrepareAndJoinRoom(roomName, new string[] { }));
                OrLog(LogLevel.Verbose, "post create or join room end");
                yield break;
            }

            private IEnumerator CreateRoom(string roomName, UInt16 maxPlayer, RoomOptions presetRoomOptions, bool ignoreExist)
            {
                createAndJoinRoomAbort = false;
                OrLog(LogLevel.Verbose, "room name: " + roomName);
                OrLog(LogLevel.Verbose, "room max player: " + maxPlayer);
                var messageBytes = new byte[sizeof(UInt16)];
                var stream = new MemoryStream(messageBytes);
                var message = new EndiannessBinaryWriter(stream);
                try
                {
                    message.Write(maxPlayer);
                }
                catch (Exception e)
                {
                    OrLogError(LogLevel.Info, "error: " + e.Message);
                    OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                }
                message.Close();

                UnityWebRequest webRequest = UnityWebRequest.Put(BASE_URL + _serverAddress + ":" + _entryPort + "/room/create/" + roomName, messageBytes);
                webRequest.method = "POST";
                webRequest.SetRequestHeader("User-Agent", UA_UNITY_CDK);
                webRequest.SetRequestHeader("Content-Type", "application/octet-stream");
                //webRequest.SetRequestHeader("Content-Length", messageBytes.Length.ToString()); // auto setting.
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError)
                {
                    OrLogError(LogLevel.Info, webRequest.error);
                    OnOpenRelayCreateRoomFailedCall((short)webRequest.responseCode, "failed polling");//TODO error handle case;
                    yield break;
                }

                var streamReader = new MemoryStream(webRequest.downloadHandler.data);
                var messageReader = new EndiannessBinaryReader(streamReader);

                if (webRequest.responseCode != 200)
                {
                    OrLog(LogLevel.Verbose, "post create room failed. http status code:" + webRequest.responseCode);
                    OnOpenRelayCreateRoomFailedCall((short)webRequest.responseCode, " failed polling");

                    yield break;
                }

                var responseCode = (ResponseCode)messageReader.ReadUInt16();
                OrLog(LogLevel.Verbose, "read bytes responseCode:" + responseCode.ToString());
                messageReader.ReadUInt16();// read alignment

                if (ignoreExist && responseCode == ResponseCode.OPENRELAY_RESPONSE_CODE_NG_CREATE_ROOM_ALREADY_EXISTS)
                {
                    OrLog(LogLevel.Verbose, "ResponseCode.OPENRELAY_RESPONSE_CODE_NG_CREATE_ROOM_ALREADY_EXISTS, but ignore ");
                }
                else if (!ignoreExist && responseCode == ResponseCode.OPENRELAY_RESPONSE_CODE_NG_CREATE_ROOM_ALREADY_EXISTS)
                {
                    createAndJoinRoomAbort = true;
                    OrLogError(LogLevel.Info, "post create room failed. http status code:" + webRequest.responseCode);
                    OrLogError(LogLevel.Info, "post create room failed. response code:" + responseCode);
                    OnOpenRelayCreateRoomFailedCall((short)webRequest.responseCode, " failed polling");

                    yield break;
                }
                else if (responseCode != ResponseCode.OPENRELAY_RESPONSE_CODE_OK_ROOM_ASSGIN_AND_CREATED)
                {
                    OrLogError(LogLevel.Info, "post create room failed. http status code:" + webRequest.responseCode);
                    OrLogError(LogLevel.Info, "post create room failed. response code:" + responseCode);
                    OnOpenRelayCreateRoomFailedCall((short)webRequest.responseCode, " failed polling");

                    yield break;
                }

                var responseSize = Marshal.SizeOf(typeof(RoomResponse));
                var responseBytes = messageReader.ReadBytes(responseSize);
                OrLog(LogLevel.VeryVerbose, "raw response:" + BitConverter.ToString(responseBytes));
                var ptr = Marshal.AllocCoTaskMem(responseSize);
                var gch = GCHandle.Alloc(responseBytes, GCHandleType.Pinned);
                RoomResponse response;
                try
                {
                    response = Marshal.PtrToStructure<RoomResponse>(gch.AddrOfPinnedObject());
                    OrLog(LogLevel.Verbose, "read bytes created room Id:" + BitConverter.ToString(response.Id));
                    OrLog(LogLevel.Verbose, "read bytes created room Capacity:" + response.Capacity.ToString());
                    OrLog(LogLevel.Verbose, "read bytes created room StfDealPort:" + response.StfDealPort.ToString());
                    OrLog(LogLevel.Verbose, "read bytes created room StfSubPort:" + response.StfSubPort.ToString());
                    OrLog(LogLevel.Verbose, "read bytes created room StlDealPort:" + response.StlDealPort.ToString());
                    OrLog(LogLevel.Verbose, "read bytes created room StlSubPort:" + response.StlSubPort.ToString());
                    OrLog(LogLevel.Verbose, "read bytes created room QueuingPolicy:" + response.QueuingPolicy.ToString());
                    OrLog(LogLevel.Verbose, "read bytes created room Flags:" + response.Flags.ToString());
                    OrLog(LogLevel.Verbose, "read bytes created room NameLen:" + response.NameLen.ToString());
                    OrLog(LogLevel.Verbose, "read bytes created room FilterLen:" + response.FilterLen.ToString());
                }
                catch (Exception e)
                {
                    OrLogError(LogLevel.Info, "handle error: " + e.Message);
                    OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                    OrLogError(LogLevel.Verbose, "post create room failed ");
                    gch.Free();

                    yield break;
                }
                gch.Free();

                //if (0 < response.NameLen)
                //{
                    var name = messageReader.ReadBytes(256);
                    OrLog(LogLevel.Verbose, "read bytes created room Name:" + Encoding.UTF8.GetString(name));
                //}
                //if (0 < response.FilterLen)
                //{
                    var filter = messageReader.ReadBytes(256);
                    OrLog(LogLevel.Verbose, "read bytes created room Filter:" + BitConverter.ToString(filter));
                //}
                var listenMode = messageReader.ReadByte();
                OrLog(LogLevel.Verbose, "read bytes get room listen mode:" + listenMode);
                messageReader.ReadBytes(3); // alignment 3bytes for 4byte alignment.
                var ipv4Bytes = messageReader.ReadBytes(4);
                var ipv4Addr = new IPAddress(ipv4Bytes).ToString();
                //var ipv4Addr = _settings.ServerAddress; // TODO ISSUE 24 provisional fix
                OrLog(LogLevel.Verbose, "read bytes get room listen ipv4 addr:" + ipv4Addr);
                var ipv6Bytes = messageReader.ReadBytes(16);
                var ipv6Addr = new IPAddress(ipv6Bytes).ToString();
                OrLog(LogLevel.Verbose, "read bytes get room listen ipv6 addr:" + ipv6Addr);
                _room = new RoomInfo(roomName,
                    response.Id,
                    listenMode,
                    ipv4Addr,
                    ipv6Addr,
                    response.StfDealPort.ToString(),
                    response.StfSubPort.ToString(),
                    response.StlDealPort.ToString(),
                    response.StlSubPort.ToString(),
                    response.Capacity,
                    presetRoomOptions
                    );

                subscriberListener.Start();
                dealerListener.Start();

                _room = new RoomInfo(
                    dealerListener.SetProperties,
                    dealerListener.SetPropertiesListedInLobby,
                    _room);

                OnCreatedRoomCall();
                OrLog(LogLevel.Verbose, "OnCreatedRoomCall");
                OrLog(LogLevel.Verbose, "post create room end");
            }

            bool prepareAndJoinRoomAbort = false;
            public IEnumerator PrepareAndJoinRoom(string roomName, string[] propKeys)
            {
                OrLog(LogLevel.Verbose, "post PrepareAndJoinRoom start");

                // create request message data.
                var newGuid = Guid.NewGuid().ToByteArray();
                var joinSeedSize = newGuid.Length;

                var messageBytes = new byte[sizeof(UInt16) + joinSeedSize];
                var stream = new MemoryStream(messageBytes);
                var message = new EndiannessBinaryWriter(stream);
                try
                {
                    message.Write((UInt16)joinSeedSize);
                    message.Write(newGuid);
                    OrLog(LogLevel.Verbose, "prepare join: " + BitConverter.ToString(messageBytes));
                }
                catch (Exception e)
                {
                    OrLogError(LogLevel.Info, "error: " + e.Message);
                    OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                }
                message.Close();
                stream.Close();

                yield return StartCoroutine(JoinRoomPreparePolling(roomName, messageBytes, propKeys));
                if (prepareAndJoinRoomAbort) yield break;
                yield return StartCoroutine(JoinRoomPrepareComplate(roomName, messageBytes));
                if (prepareAndJoinRoomAbort) yield break;
                yield return StartCoroutine(JoinRoom(roomName, newGuid));
                OrLog(LogLevel.Verbose, "post PrepareAndJoinRoom end");
                yield break;
            }

            private IEnumerator JoinRoomPreparePolling(string roomName, byte[] messageBytes, string[] propKeys)
            {
                OrLog(LogLevel.Verbose, "Join room prepare polling start");
                _roomJoining = true;

                UnityWebRequest webRequest;
                while (true)
                {
                    webRequest = UnityWebRequest.Put(BASE_URL + _serverAddress + ":" + _entryPort + "/room/join_prepare_polling/" + roomName, messageBytes);
                    //webRequest.method = "POST";
                    webRequest.SetRequestHeader("User-Agent", UA_UNITY_CDK);
                    webRequest.SetRequestHeader("Content-Type", "application/octet-stream");
                    //webRequest.SetRequestHeader("Content-Length", messageBytes.Length.ToString()); // auto setting.
                    yield return webRequest.SendWebRequest();
                    if (webRequest.isNetworkError)
                    {
                        OrLogError(LogLevel.Info, webRequest.error);
                        OnOpenRelayJoinRoomFailedCall((short)webRequest.responseCode, "failed polling");//TODO error handle case;
                        prepareAndJoinRoomAbort = true;
                        _roomJoining = false;
                        _roomJoined = false;
                        _roomJoinComplete = false;
                        yield break;
                    }
                    if (webRequest.responseCode == 200) // Ok
                    {
                        OrLog(LogLevel.Verbose, "Join room prepare load start");
                        break;
                    }
                    else if (webRequest.responseCode == 100) // Continue
                    {
                        OrLog(LogLevel.Verbose, "Join room prepare polling continue, please wait.");
                        // TODO Timeout logics.
                        continue;
                    }
                    else if (webRequest.responseCode == 408) // request timeout, should use 500?
                    {
                        OrLog(LogLevel.Verbose, "post join room polling timeout " + webRequest.responseCode);
                        OnOpenRelayJoinRoomFailedCall((short)webRequest.responseCode, "failed polling");//TODO error handle case;
                        prepareAndJoinRoomAbort = true;
                        _roomJoining = false;
                        _roomJoined = false;
                        _roomJoinComplete = false;
                        yield break;
                    }
                    else
                    {
                        OrLog(LogLevel.Verbose, "post join room Bad Response " + webRequest.responseCode);
                        OnOpenRelayJoinRoomFailedCall((short)webRequest.responseCode, "failed polling");//TODO error handle case;
                        prepareAndJoinRoomAbort = true;
                        _roomJoining = false;
                        _roomJoined = false;
                        _roomJoinComplete = false;
                        yield break;
                    }
                }

                var stream = new MemoryStream(webRequest.downloadHandler.data);
                var message = new BinaryReader(stream);

                var alignmentLen = (UInt16)0;
                var alignment = new byte[] { };
                var masterPid = (PlayerId)message.ReadUInt16();
                OrLog(LogLevel.Verbose, "read bytes masterPid:" + masterPid.ToString());
                var assginPid = (PlayerId)message.ReadUInt16();
                OrLog(LogLevel.Verbose, "read bytes assginPid:" + assginPid.ToString());
                var joinedPidsLen = message.ReadUInt16();
                OrLog(LogLevel.Verbose, "read bytes joinedPidsLen:" + joinedPidsLen.ToString());
                var joinedNamesLen = message.ReadUInt16();
                OrLog(LogLevel.Verbose, "read bytes joinedNamesLen(name total count):" + joinedNamesLen.ToString());

                var joinedPids = new PlayerId[joinedPidsLen];
                if (joinedPidsLen > 0)
                {
                    var joinedPidsByte = message.ReadBytes(sizeof(PlayerId) * joinedPidsLen);
                    Buffer.BlockCopy(joinedPidsByte, 0, joinedPids, 0, sizeof(PlayerId) * joinedPidsLen);
                    alignmentLen = (UInt16)(joinedPidsLen % 4);
                    if (alignmentLen > 0) { message.ReadBytes(alignmentLen); }
                }
                var joinedNames = new string[joinedNamesLen];
                if (joinedNamesLen > 0)
                {
                    for (int index = 0; index < joinedNamesLen; index++)
                    {

                        var nameLen = message.ReadUInt16();
                        OrLog(LogLevel.Verbose, "read bytes nameLen:" + nameLen.ToString());

                        var nameBytes = message.ReadBytes(nameLen);
                        alignmentLen = (UInt16)((sizeof(UInt16) + nameLen) % 4);
                        if (alignmentLen > 0) { message.ReadBytes(alignmentLen); }
                        joinedNames[index] = Encoding.UTF8.GetString(nameBytes);
                    }
                }

                stream.Close();
                message.Close();
                _players.Clear();

                for (int index = 0; index < joinedPids.Length; index++)
                {
                    var otherId = joinedPids[index];
                    OrLog(LogLevel.Verbose, "other pid:" + otherId.ToString());
                    string otherName = string.Empty;
                    otherName = joinedNames[index];
                    OrLog(LogLevel.Verbose, "other name:" + otherName);

                    var isMaster = otherId == masterPid;
                    var otherPlayer = new UserSession(otherId, (ObjectId)1, false, isMaster);
                    otherPlayer.NickName = otherName;
                    if (isMaster)
                    {
                        _masterClient = otherPlayer;
                    }
                    _players.Add(otherPlayer);
                }
                dealerListener.SetLogin(assginPid);
                _player.Login(assginPid, assginPid == masterPid);
                _players.Add(_player);
                foreach (var p in _players)
                {
                    OrLog(LogLevel.Verbose, "joined player Id:" + p.ID);
                    OrLog(LogLevel.Verbose, "joined player IsLocal:" + p.IsLocal);
                    OrLog(LogLevel.Verbose, "joined player IsMasterClient:" + p.IsMasterClient);
                }

                // TODO must be other network objects create.

                _PropertiesInitializing = true;
                OrLog(LogLevel.Verbose, "Join room Properties initializing start");

                var retryOver = 3;
                var retry = 0;

                do {
                    if (Player.IsMasterClient)
                    {
                        _masterClient = _player;
                        _room.InitializeProperties();
                        if (_room.Properties.Count == 0)
                        {
                            _PropertiesReady = true;
                        }
                    }
                    else
                    {
                        dealerListener.GetProperties();// ISSUE 1 timing bug. require gap load logic.
                    }

                    OrLog(LogLevel.Verbose, "Join room Properties initializing ... ");

                    var timeout = 1.5f;
                    var step = 0.01f;
                    var counter = 0f;
                    // wait ready for Properties.
                    while (counter < timeout)
                    {
                        if (_PropertiesReady) break;

                        yield return new WaitForSeconds(step);
                        counter += step;
                    }

                    if (_PropertiesReady)
                    {
                        OrLog(LogLevel.Verbose, "Join room Properties initializing ok");
                        break;
                    }
                    else if(retry >= retryOver)
                    {
                        OrLog(LogLevel.Verbose, "Join room Properties initializing retry over failed. " + retry.ToString()); prepareAndJoinRoomAbort = true;
                        _roomJoining = false;
                        _roomJoined = false;
                        _roomJoinComplete = false;
                        yield break;
                    }
                    else
                    {
                        retry++;
                        OrLog(LogLevel.Verbose, "Join room Properties initializing retry ... " + retry.ToString());
                    }

                } while (true);

                _roomJoining = false;
                _roomJoined = true;
                OnJoinedRoomCall();
                OrLog(LogLevel.Verbose, "OnJoinedRoomCall");
                OnReadyNewPlayerCall();
                OrLog(LogLevel.Verbose, "OnReadyNewPlayerCall");

                OrLog(LogLevel.Verbose, "Join room prepare polling end");
            }

            public IEnumerator GetRoomProperties(RoomInfo room)
            {
                OrLog(LogLevel.Verbose, "Get room properties start");
                UnityWebRequest webRequest = UnityWebRequest.Get(BASE_URL + _serverAddress + ":" + _entryPort + "/room/prop/" + room.Name);
                webRequest.SetRequestHeader("User-Agent", UA_UNITY_CDK);
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError)
                {
                    OrLogError(LogLevel.Info, webRequest.error);
                    // TODO VERSION ERROR HANDLE CALLBACK
                    OrLogError(LogLevel.Info, "get room properties failed. http status code:" + webRequest.responseCode);

                    yield break;
                }

                var streamReader = new MemoryStream(webRequest.downloadHandler.data);
                var messageReader = new EndiannessBinaryReader(streamReader);

                if (webRequest.responseCode != 200)
                {
                    OrLog(LogLevel.Verbose, "get room properties failed. http status code:" + webRequest.responseCode);

                    yield break;
                }

                var responseCode = (ResponseCode)messageReader.ReadUInt16();
                var contentLen = messageReader.ReadUInt16();
                if (0 < contentLen)
                {
                    var content = messageReader.ReadBytes(contentLen);
                    UpdateHashtable(content, null, room.Properties);
                }

                OrLog(LogLevel.Verbose, "Get room properties end");
            }

            private IEnumerator JoinRoomPrepareComplate(string roomName, byte[] messageBytes)
            {
                OrLog(LogLevel.Verbose, "Join room prepare complate start");
                UnityWebRequest webRequest = UnityWebRequest.Put(BASE_URL + _serverAddress + ":" + _entryPort + "/room/join_prepare_complete/" + roomName, messageBytes);
                webRequest.method = "POST";
                webRequest.SetRequestHeader("User-Agent", UA_UNITY_CDK);
                webRequest.SetRequestHeader("Content-Type", "application/octet-stream");
                //webRequest.SetRequestHeader("Content-Length", messageBytes.Length.ToString()); // auto setting.
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError)
                {
                    OrLogError(LogLevel.Info, webRequest.error);
                    OnOpenRelayJoinRoomFailedCall((short)webRequest.responseCode, "failed polling");//TODO error handle case;
                    prepareAndJoinRoomAbort = true;
                    _roomJoining = false;
                    _roomJoined = false;
                    _roomJoinComplete = false;
                    yield break;
                }
                if (webRequest.responseCode == 200) // should be Switch protocol?
                {
                }
                else
                {
                    OrLog(LogLevel.Verbose, "post create room Bad Response " + webRequest.responseCode);
                    OnOpenRelayJoinRoomFailedCall((short)webRequest.responseCode, "failed polling");//TODO error handle case;
                    prepareAndJoinRoomAbort = true;
                    _roomJoining = false;
                    _roomJoined = false;
                    _roomJoinComplete = false;
                    yield break;
                }
                OrLog(LogLevel.Verbose, "Join room prepare complate end");
                yield break;
            }

            private IEnumerator JoinRoom(string roomName, byte[] newGuid)
            {
                OrLog(LogLevel.Verbose, "join room start");

                dealerListener.Join(roomName, newGuid);
                while (_roomJoining)
                {
                    yield return new WaitForSeconds(0.01f);
                }

                OrLog(LogLevel.Verbose, "join room end");
            }

            public IEnumerator LeaveRoom()
            {
                OrLog(LogLevel.Verbose, "leave room start");
                dealerListener.Leave();

                OrLog(LogLevel.Verbose, "wait leave api complete");
                _leaveComplete = false;
                while (true)
                {
                    if (_leaveComplete) break;
                    yield return new WaitForSeconds(0.01f);
                }

                OrLog(LogLevel.Verbose, "listener stop");
                dealerListener.Stop();
                subscriberListener.Stop();
                InitializeRoom();

                OnLeftRoomCall();
                OrLog(LogLevel.Verbose, "leave room end");
                yield break;
            }

            public IEnumerator LogoutEntry()
            {
                OrLog(LogLevel.Verbose, "post logout start");
                UnityWebRequest webRequest = UnityWebRequest.Post(BASE_URL + _serverAddress + ":" + _entryPort + "/logoff", string.Empty);
                webRequest.SetRequestHeader("User-Agent", UA_UNITY_CDK);
                yield return webRequest.SendWebRequest();
                if (webRequest.isNetworkError)
                {
                    OrLogError(LogLevel.Info, webRequest.error);
                    // TODO VERSION ERROR HANDLE CALLBACK
                    yield break;
                }
                if (webRequest.responseCode == 200)
                {
                    _entryJoined = false;
                }
                else
                {
                    // TODO VERSION ERROR HANDLE CALLBACK
                    yield break;
                }
                OnDisconnectedCall(string.Empty);
                OrLog(LogLevel.Verbose, "post logout end");
            }

            bool standaloneEntrySequenceAbort = false;
            IEnumerator StandaloneEntrySequence()
            {
                OrLog(LogLevel.Verbose, "start connect sequence");
                OrLog(LogLevel.Verbose, "check version");
                yield return StartCoroutine(Version());
                if (standaloneEntrySequenceAbort) yield break;
                OnConnectedToOpenRelayCall();
                OrLog(LogLevel.Verbose, "OnConnectedToOpenRelayCall");

                if (AutoEntry)
                {
                    yield return StartCoroutine(LogonEntry());
                    if (standaloneEntrySequenceAbort) yield break;
                }

                while (true)
                {
                    if (_entryJoined) { break; }
                    yield return new WaitForSeconds(0.01f);
                }

                // post /logon ok.
                OnJoinedLobbyCall();
                OrLog(LogLevel.Verbose, "OnJoinedLobbyCall");
                OrLog(LogLevel.Verbose, "Entry Service connected Ok");
                yield return null;
            }

            IEnumerator Start()
            {
                yield return StartCoroutine(StandaloneEntrySequence());
                yield return StartCoroutine(UpdateRoomList());
            }

            // ISSUE 21 Bad performance.
            public void StartQueue()
            {
                StartCoroutine(RetrieveQueueStatefull()); // no yield return ok.
            }
            
            IEnumerator RetrieveQueueStatefull()
            {
                while (!subscriberListener.Aborted)
                {
                    subscriberListener.RetrieveQueueStatefull();
                    yield return null;
                }

                OrLog(LogLevel.Verbose, "RetrieveQueueStatefull end");
            }
        }
    }
}
