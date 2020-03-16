//------------------------------------------------------------------------------
// <copyright file="MessageHandler.cs" company="FurtherSystem Co.,Ltd.">
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
using MiniJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Com.FurtherSystems.OpenRelay
{
    using PlayerId = UInt16;
    public static partial class OpenRelayClient
    {
        private static void HandleMessage(EndiannessBinaryReader message)
        {
            // TODO fix provisional logics.
            //if (!_roomJoining || !_roomJoined) return;   

            var headerSize = Marshal.SizeOf(typeof(Header));
            var headerBytes = message.ReadBytes(headerSize);
            OrLog(LogLevel.VeryVerbose, "raw header:" + BitConverter.ToString(headerBytes));
            var ptr = Marshal.AllocCoTaskMem(headerSize);
            var gch = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
            Header header;
            try
            {
                header = Marshal.PtrToStructure<Header>(gch.AddrOfPinnedObject());
            }
            catch (Exception e)
            {
                OrLogError(LogLevel.Normal, "handle error: " + e.Message);
                OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                gch.Free();

                return;
            }
            gch.Free();

            PlayerId[] destsPids = new PlayerId[header.DestLen];
            byte[] content = null;
            if (header.DestLen > 0)
            {
                var destsBytes = message.ReadBytes(sizeof(PlayerId) * header.DestLen);
                Buffer.BlockCopy(destsBytes, 0, destsPids, 0, sizeof(PlayerId) * header.DestLen);
            }

            // TODO fix provisional logics.
            if ((RelayCode)header.RelayCode != RelayCode.JOIN && !(_roomJoined || _PropertiesInitializing)) return;

            switch ((RelayCode)header.RelayCode)
            {
                case RelayCode.JOIN:
                    var alignmentLen = (UInt16)0;
                    var alignment = new byte[] { };
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.JOIN");
                    var assginPid = (PlayerId)message.ReadUInt16();
                    OrLog(LogLevel.Verbose, "read bytes assginPid:" + assginPid.ToString());
                    var masterPid = (PlayerId)message.ReadUInt16();
                    OrLog(LogLevel.Verbose, "read bytes masterPid:" + masterPid.ToString());
                    var seedLen = message.ReadUInt16();
                    OrLog(LogLevel.Verbose, "read bytes seedLen:" + seedLen.ToString());
                    var nameLen = message.ReadUInt16();
                    OrLog(LogLevel.Verbose, "read bytes nameLen:" + nameLen.ToString());

                    var joinSeedByte = message.ReadBytes(seedLen);

                    if (seedLen > 0)
                    {
                        alignmentLen = (UInt16)(seedLen % 4);
                        if (alignmentLen > 0) { message.ReadBytes(alignmentLen); }
                    }
                    byte[] nameBytes = null;
                    if (nameLen > 0)
                    {
                        nameBytes = new byte[nameLen];
                        nameBytes = message.ReadBytes(nameLen);
                    }

                    OrLog(LogLevel.Verbose, "joinGuid Compare:" + BitConverter.ToString(joinSeedByte) + " : " + BitConverter.ToString(dealerListener.JoinGuid));
                    if (BitConverter.ToString(joinSeedByte) != BitConverter.ToString(dealerListener.JoinGuid) && _roomJoined)
                    {

                        var otherPlayer = new UserSession(header.SrcPid, header.SrcOid, false, header.SrcPid == masterPid);
                        if (nameLen > 0)
                        {
                            OrLog(LogLevel.Verbose, "receive name bytes: " + BitConverter.ToString(nameBytes));
                            otherPlayer.NickName = Encoding.UTF8.GetString(nameBytes);// TODO use Unicode(UTF-16) here?
                        }
                        if (header.SrcPid == masterPid)
                        {
                            _masterClient = otherPlayer;
                        }

                        _players.Add(otherPlayer);
                        OrLog(LogLevel.Verbose, "OnOpenRelayPlayerConnectedCall :" + otherPlayer.ID.ToString());
                        OnOpenRelayPlayerConnectedCall(otherPlayer);
                    }

                    break;
                case RelayCode.LEAVE:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.LEAVE");
                    var newMasterPid = (PlayerId)message.ReadUInt16();
                    if (header.SrcPid == Player.ID)
                    {
                        OrLog(LogLevel.Verbose, "player left :" + header.SrcPid.ToString());
                        _leaveComplete = true;
                    }
                    else
                    {
                        OrLog(LogLevel.Verbose, "other player left :" + header.SrcPid.ToString());
                        UserSession otherPlayer = null;
                        foreach (var p in _players)
                        {
                            if ((PlayerId)p.ID == header.SrcPid)
                            {
                                otherPlayer = p;
                                _players.Remove(otherPlayer);
                                break;
                            }
                        }
                        if (newMasterPid > 0)
                        {
                            _masterClient.IsMasterClient = false;
                            foreach (var p in _players)
                            {
                                if (newMasterPid == p.ID)
                                {
                                    p.IsMasterClient = true;
                                    _masterClient = p;
                                    break;
                                }
                            }
                        }
                        if (otherPlayer != null)
                        {
                            OrLog(LogLevel.Verbose, "player :" + otherPlayer.ID);
                            OnOpenRelayPlayerDisconnectedCall(otherPlayer);
                        }
                    }

                    break;
                case RelayCode.RELAY:
                    if (!_roomJoined) return;
                    OrLog(LogLevel.VeryVerbose, "HandleMessage RelayCode.RELAY");
                    switch ((DestinationCode)header.DestCode)
                    {
                        case DestinationCode.StrictBroadcast:

                            break;
                        case DestinationCode.Broadcast:
                            if (header.SrcPid == _player.ID) return;//ignore

                            break;
                        case DestinationCode.Exclude:
                            if (destsPids.Contains<PlayerId>((PlayerId)_player.ID)) return;//ignore

                            // exclude case
                            break;
                        case DestinationCode.Include:
                            if (!destsPids.Contains<PlayerId>((PlayerId)_player.ID)) return;//ignore

                            // include case
                            break;
                        case DestinationCode.MasterOnly:
                            if (isMasterClient) break;

                            return;
                        default:

                            return;//default is ignore
                    }
                    content = message.ReadBytes(header.ContentLen);
                    OrLog(LogLevel.Verbose, "OnEventCall from:" + header.SrcPid.ToString() + " - " + header.SrcOid.ToString() + " DestCode:" + header.DestCode.ToString() + " destsPids:" + destsPids.Length.ToString());
                    if (_players.Any(x => x.ID == header.SrcPid)) OnSyncCall((byte)header.ContentCode, content, header.SrcPid, header.SrcOid);
                    else OrLog(LogLevel.Verbose, "PlayerId: " + header.SrcPid + " addn't yet.");

                    break;
                case RelayCode.RELAY_STREAM:
                    if (!_roomJoined) return;
                    OrLog(LogLevel.VeryVerbose, "HandleMessage RelayCode.RELAY_STREAM");

                    switch ((DestinationCode)header.DestCode)
                    {
                        case DestinationCode.StrictBroadcast:

                            break;
                        case DestinationCode.Broadcast:
                            if (header.SrcPid == _player.ID) return;//ignore

                            break;
                        case DestinationCode.Exclude:
                            if (header.SrcPid == _player.ID) return;//ignore
                            if (destsPids.Contains<PlayerId>((PlayerId)_player.ID)) return;//ignore

                            // exclude case
                            break;
                        case DestinationCode.Include:
                            if (header.SrcPid == _player.ID) return;//ignore
                            if (!destsPids.Contains<PlayerId>((PlayerId)_player.ID)) return;//ignore

                            // include case
                            break;
                        case DestinationCode.MasterOnly:
                            if (isMasterClient) break;

                            return;
                        default:

                            return;//default is ignore
                    }
                    content = message.ReadBytes(header.ContentLen);
                    OrLog(LogLevel.VeryVerbose, "OnSyncVoiceCall from:" + header.SrcPid.ToString() + " - " + header.SrcOid.ToString() + " DestCode:" + header.DestCode.ToString() + " destsPids:" + destsPids.Length.ToString());
                    //if (_player != null && _players != null && _players.Count > 0 && _players.Any(x => x.ID == header.SrcPid)) OnSyncVoiceCall((byte)header.ContentCode, content, header.SrcPid, header.SrcOid);
                    //else OrLog(LogLevel.Verbose, "PlayerId: " + header.SrcPid + " addn't yet.");
                    OnSyncStreamCall((byte)header.ContentCode, content, header.SrcPid, header.SrcOid);
                    
                    break;
                case RelayCode.UNITY_CDK_RELAY:
                    if (!_roomJoined) return;
                    OrLog(LogLevel.VeryVerbose, "HandleMessage RelayCode.UNITY_CDK_RELAY");
                    switch ((DestinationCode)header.DestCode)
                    {
                        case DestinationCode.StrictBroadcast:

                            break;
                        case DestinationCode.Broadcast:
                            if (header.SrcPid == _player.ID) return;//ignore

                            break;
                        case DestinationCode.Exclude:
                            if (destsPids.Contains<PlayerId>((PlayerId)_player.ID)) return;//ignore

                            // exclude case
                            break;
                        case DestinationCode.Include:
                            if (!destsPids.Contains<PlayerId>((PlayerId)_player.ID)) return;//ignore

                            // include case
                            break;
                        case DestinationCode.MasterOnly:
                            if (isMasterClient) break;

                            return;
                        default:

                            return;//default is ignore
                    }
                    content = message.ReadBytes(header.ContentLen);
                    OrLog(LogLevel.Verbose, "OnEventCall from:" + header.SrcPid.ToString() + " - " + header.SrcOid.ToString() + " DestCode:" + header.DestCode.ToString() + " destsPids:" + destsPids.Length.ToString());
                    if (_players.Any(x => x.ID == header.SrcPid)) OnSyncPlatformCall((byte)header.ContentCode, content, header.SrcPid, header.SrcOid);
                    else OrLog(LogLevel.Verbose, "PlayerId: " + header.SrcPid + " addn't yet.");

                    break;
                case RelayCode.TIMEOUT:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.TIMEOUT");
                    //timeout leave

                    break;
                case RelayCode.REJOIN:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.REJOIN");
                    //re join

                    break;
                case RelayCode.SET_LEGACY_MAP:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.SET_LEGACY_MAP");
                    var keysBytesLen = message.ReadUInt16();
                    alignmentLen = (UInt16)(keysBytesLen % 4);
                    var contentBytesLen = message.ReadUInt16();
                    var keysBytes = message.ReadBytes(keysBytesLen);
                    if (alignmentLen > 0) { message.ReadBytes(alignmentLen); }
                    content = message.ReadBytes(contentBytesLen);
                    UpdateHashtable(content, keysBytes, _room.Properties);
                    OnOpenRelayRoomPropertiesChangedCall(_room.Properties);
                    if (_PropertiesInitializing)
                    {
                        _PropertiesInitializing = false;
                        _PropertiesReady = true;
                    }

                    break;
                case RelayCode.GET_LEGACY_MAP:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.GET_LEGACY_MAP");
                    if (header.ContentLen > 0)
                    {
                        content = message.ReadBytes(header.ContentLen);
                        UpdateHashtable(content, null, _room.Properties);
                    }
                    if (_PropertiesInitializing)
                    {
                        _PropertiesInitializing = false;
                        _PropertiesReady = true;
                    }

                    break;
                case RelayCode.GET_USERS:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.GET_USERS");
                    //get users responce

                    break;
                case RelayCode.SET_MASTER:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.SET_MASTER");
                    var setMasterPid = (PlayerId)message.ReadUInt16();
                    _masterClient.IsMasterClient = false;
                    if (setMasterPid == Player.ID)
                    {
                        _player.IsMasterClient = true;
                        _masterClient = _player;
                    }
                    else
                    {
                        foreach (var p in _players)
                        {
                            if (setMasterPid == p.ID)
                            {
                                p.IsMasterClient = true;
                                _masterClient = p;
                                break;
                            }
                        }
                    }

                    break;
                case RelayCode.GET_MASTER:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.GET_MASTER");
                    var getMasterPid = (PlayerId)message.ReadUInt16();
                    _masterClient.IsMasterClient = false;
                    foreach (var p in _players)
                    {
                        if (getMasterPid == p.ID)
                        {
                            p.IsMasterClient = true;
                            _masterClient = p;
                            break;
                        }
                    }

                    break;
                case RelayCode.GET_SERVER_TIMESTAMP:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.GET_SERVER_TIMESTAMP");
                    //get server timestamp responce
                    var timestamp = (UInt16)message.ReadUInt16();
                    // TODO set server timestamp logic.

                    break;
                case RelayCode.RELAY_LATEST:
                    if (!_roomJoined) return;
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.RELAY_LATEST");

                    content = message.ReadBytes(header.ContentLen);
                    OrLog(LogLevel.Verbose, "OnEventCall from:" + header.SrcPid.ToString() + " - " + header.SrcOid.ToString() + " DestCode:" + header.DestCode.ToString() + " destsPids:" + destsPids.Length.ToString());
                    if (_players.Any(x => x.ID == header.SrcPid)) OnSyncLatestCall((byte)header.ContentCode, content, header.SrcPid, header.SrcOid);
                    else OrLog(LogLevel.Verbose, "PlayerId: " + header.SrcPid + " addn't yet.");

                    break;
                case RelayCode.GET_LATEST:
                    if (!_roomJoined || header.SrcPid != _player.ID) return;
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.GET_LATEST");

                    content = message.ReadBytes(header.ContentLen);
                    OrLog(LogLevel.Verbose, "OnEventCall from:" + header.SrcPid.ToString() + " - " + header.SrcOid.ToString() + " DestCode:" + header.DestCode.ToString() + " destsPids:" + destsPids.Length.ToString());
                    //if (_player.ID == header.SrcPid) OnRelayEventCall((byte)header.ContentCode, content, header.SrcPid, header.SrcOid);

                    break;
                case RelayCode.UNITY_CDK_RELAY_LATEST:
                    if (!_roomJoined) return;
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.UNITY_CDK_RELAY_LATEST");

                    content = message.ReadBytes(header.ContentLen);
                    OrLog(LogLevel.Verbose, "OnEventCall from:" + header.SrcPid.ToString() + " - " + header.SrcOid.ToString() + " DestCode:" + header.DestCode.ToString() + " destsPids:" + destsPids.Length.ToString());
                    if (_players.Any(x => x.ID == header.SrcPid)) OnSyncLatestPlatformCall((byte)header.ContentCode, content, header.SrcPid, header.SrcOid);
                    else OrLog(LogLevel.Verbose, "PlayerId: " + header.SrcPid + " addn't yet.");

                    break;
                case RelayCode.UNITY_CDK_GET_LATEST:
                    if (!_roomJoined || header.SrcPid != _player.ID) return;
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.UNITY_CDK_GET_LATEST");

                    content = message.ReadBytes(header.ContentLen);
                    OrLog(LogLevel.Verbose, "OnEventCall from:" + header.SrcPid.ToString() + " - " + header.SrcOid.ToString() + " DestCode:" + header.DestCode.ToString() + " destsPids:" + destsPids.Length.ToString());
                    //if (_player.ID == header.SrcPid) OnRelayEventCall((byte)header.ContentCode, content, header.SrcPid, header.SrcOid);
                    //else OrLog(LogLevel.Verbose, "other target PlayerId: " + header.SrcPid);

                    break;
                case RelayCode.SET_LOBBY_MAP:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.SET_LEGACY_MAP");
                    content = message.ReadBytes(header.ContentLen);
                    //_room.Properties = ToHash(content);
                    //OnOpenRelayRoomPropertiesChangedCall(ToHash(content));
                    //_PropertiesInitialized = true;

                    break;
                case RelayCode.GET_LOBBY_MAP:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.GET_LEGACY_MAP");
                    content = message.ReadBytes(header.ContentLen);
                    //_room.Properties = ToHash(content);
                    //_PropertiesInitialized = true;

                    break;
                case RelayCode.SET_MASK:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.SET_MASK");

                    break;
                case RelayCode.GET_MASK:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.GET_MASK");

                    break;
                case RelayCode.REPLAY_JOIN:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.REPLAY_JOIN");

                    break;
                case RelayCode.LOAD_PLAYER:
                    OrLog(LogLevel.Verbose, "HandleMessage RelayCode.LOAD_PLAYER");
                    var total = message.ReadByte();
                    var position = message.ReadByte();
                    var nameContainsMax = (UInt16)message.ReadUInt16();
                    message.ReadByte(); // 4byte alignment
                    var names = new List<string>();
                    for (int count = 0; count < nameContainsMax; count++)
                    {
                        var nameSize = (UInt16)message.ReadUInt16();
                        var name = message.ReadBytes(nameSize);
                        names.Add(Encoding.UTF8.GetString(name));
                        alignmentLen = (UInt16)(nameSize % 4);
                        if (alignmentLen > 0) { message.ReadBytes(alignmentLen); }
                    }

                    break;
                default:
                    OrLogError(LogLevel.Verbose, "HandleMessage error: RelayCode not match, invalid case:" + (RelayCode)header.RelayCode);

                    break;
            }
        }

        private static readonly char[] rowSeparator = { ';', ';' };
        private static readonly char[] colSeparator = { ':', ':' };
        private static void UpdateHashtable(byte[] databyte, byte[] keysbyte, Hashtable hashtable)
        {
            var data = Encoding.ASCII.GetString(databyte);
            var createdHash = new Hashtable();
            var logDecoded = new StringBuilder();
            var rows = data.Split(rowSeparator);
            OrLog(LogLevel.Verbose, "ToHash row counts: " + rows.Length);
            OrLog(LogLevel.Verbose, "ToHash data bytes: " + System.Environment.NewLine + data);
            foreach (var row in rows)
            {
                var col = row.Split(colSeparator, StringSplitOptions.RemoveEmptyEntries);
                if (col.Length == 2)
                {
                    var decodedKey = Encoding.ASCII.GetString(Convert.FromBase64String(col[0]));
                    var decodedValue = Encoding.ASCII.GetString(Convert.FromBase64String(col[1]));
                    createdHash.Add(Json.Deserialize(decodedKey), Json.Deserialize(decodedValue));
                    if (_settings.LogVerboseLevel >= LogLevel.Verbose) logDecoded.Append("    ").Append(decodedKey).Append(":").Append(decodedValue).Append(System.Environment.NewLine);
                }
            }
            OrLog(LogLevel.Verbose, "ToHash data hash: " + System.Environment.NewLine + logDecoded.ToString());
            var keysLength = createdHash.Keys.Count;
            var keys = new object[keysLength];
            var values = new object[keysLength];
            createdHash.Keys.CopyTo(keys, 0);
            createdHash.Values.CopyTo(values, 0);
            if (keysbyte == null) // null is full copy
            {
                for (int index = 0; index < keysLength; index++)
                {
                    hashtable[keys[index]] = values[index];
                }
            }
            else // not null is diff copy
            {
                var keyslist = ToStringList(keysbyte);
                for (int index = 0; index < keysLength; index++)
                {
                    if (keyslist.Contains(keys[index])) hashtable[keys[index]] = values[index];
                }
            }
            OrLog(LogLevel.Verbose, "Update data hash");
        }

        private static byte[] ToBytes(Hashtable data)
        {
            var exploded = new StringBuilder();
            var logDecoded = new StringBuilder();
            var logEncoded = new StringBuilder();
            var isFirst = true;
            OrLog(LogLevel.Verbose, "ToBytes row counts:" + data.Count);
            foreach (DictionaryEntry d in data)
            {
                if (isFirst) isFirst = false; else exploded.Append(rowSeparator);

                var jsonedKey = Json.Serialize(d.Key);
                var jsonedValue = Json.Serialize(d.Value);
                var encodedKey = Convert.ToBase64String(Encoding.ASCII.GetBytes(jsonedKey));
                var encodedValue = Convert.ToBase64String(Encoding.ASCII.GetBytes(jsonedValue));
                if (_settings.LogVerboseLevel >= LogLevel.Verbose) logDecoded.Append("    ").Append(jsonedKey).Append(":").Append(jsonedValue).Append(System.Environment.NewLine);
                if (_settings.LogVerboseLevel >= LogLevel.Verbose) logEncoded.Append("    ").Append(encodedKey).Append(":").Append(encodedValue).Append(System.Environment.NewLine);

                exploded.Append(encodedKey)
                .Append(colSeparator)
                .Append(encodedValue);
            }
            OrLog(LogLevel.Verbose, "ToBytes data hash:" + System.Environment.NewLine + logDecoded.ToString());
            OrLog(LogLevel.Verbose, "ToBytes data bytes:" + System.Environment.NewLine + logEncoded.ToString());
            return Encoding.ASCII.GetBytes(exploded.ToString());
        }

        private static string[] ToStringList(byte[] data)
        {
            var decoded = Encoding.ASCII.GetString(data);
            var createdList = new List<string>();
            foreach (var row in decoded.Split(rowSeparator))
            {
                if (row.Length > 0)
                {
                    createdList.Add(Encoding.ASCII.GetString(Convert.FromBase64String(row)));
                }
            }
            return createdList.ToArray();
        }

        private static byte[] ToExplodeBytes(string[] list)
        {
            StringBuilder exploded = new StringBuilder();
            foreach (var e in list)
            {
                exploded.Append(Convert.ToBase64String(Encoding.ASCII.GetBytes(e)))
                    .Append(rowSeparator);
            }
            return Encoding.ASCII.GetBytes(exploded.ToString());
        }
    }
}
