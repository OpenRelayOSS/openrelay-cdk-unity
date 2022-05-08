﻿//------------------------------------------------------------------------------
// <copyright file="DealerListener.cs" company="FurtherSystem Co.,Ltd.">
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
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Com.FurtherSystems.OpenRelay
{
    using ObjectId = UInt16;
    using PlayerId = UInt16;
    public static partial class OpenRelayClient
    {
        internal class DealerListener
        {
            public bool Aborted
            {
                get { return statefullListenerCancelled; }
            }
            private readonly Thread statefullListenerWorker;
            private readonly Thread statelessListenerWorker;
            private bool statefullListenerCancelled;
            private bool statelessListenerCancelled;
            private bool initialized = false; // ISSUE 11
            private int LoginId = -1;
            private byte[] joinGuid = new byte[] { };
            public byte[] JoinGuid
            {
                get { return joinGuid; }
                private set { joinGuid = value; }
            }

            private readonly ConcurrentQueue<byte[]> statefullQueue = new ConcurrentQueue<byte[]>();
            private readonly ConcurrentQueue<byte[]> statelessQueue = new ConcurrentQueue<byte[]>();

            private void StatefullListenerLoop()
            {
                OrLog(LogLevel.Verbose, "StatefullListenerLoop start tcp://" + Room.ListenAddrV4 + ":" + Room.StatefullDealPort);
                AsyncIO.ForceDotNet.Force();
                using (var dealerSocket = new DealerSocket())
                {
                    dealerSocket.Options.ReceiveHighWatermark = 1;
                    dealerSocket.Connect("tcp://" + Room.ListenAddrV4 + ":" + Room.StatefullDealPort);
                    while (!statefullListenerCancelled)
                    {
                        if (!IsMessageQueueRunning) continue;

                        byte[] message;
                        if (statefullQueue.TryDequeue(out message))
                        {
                            dealerSocket.SendFrame(message, false);
                        }
                        Thread.Yield();
                    }
                    dealerSocket.Close();
                }
                NetMQConfig.Cleanup();
                OrLog(LogLevel.Verbose, "StatefullListenerLoop end");
            }

            private void StatelessListenerLoop()
            {
                OrLog(LogLevel.Verbose, "StatelessListenerLoop start tcp://" + Room.ListenAddrV4 + ":" + Room.StatelessDealPort);
                AsyncIO.ForceDotNet.Force();
                using (var dealerSocket = new DealerSocket())
                {
                    dealerSocket.Options.ReceiveHighWatermark = 1;
                    dealerSocket.Connect("tcp://" + Room.ListenAddrV4 + ":" + Room.StatelessDealPort);
                    while (!statelessListenerCancelled)
                    {
                        if (!IsMessageQueueRunning) continue;

                        byte[] message;
                        if (statelessQueue.TryDequeue(out message))
                        {
                            dealerSocket.SendFrame(message, false);
                        }
                        Thread.Yield();
                    }
                    dealerSocket.Close();
                }
                NetMQConfig.Cleanup();
                OrLog(LogLevel.Verbose, "StatelessListenerLoop end");
            }

            public DealerListener()
            {
                statefullListenerWorker = new Thread(StatefullListenerLoop);
                //statelessListenerWorker = new Thread(StatelessListenerLoop);
                Initialize();
            }

            public void Start()
            {
                initialized = true; // ISSUE 11
                Initialize();
                statefullListenerCancelled = false;
                statelessListenerCancelled = false;
                statefullListenerWorker.Start();
                //statelessListenerWorker.Start();
            }

            private void Initialize()
            {
                LoginId = -1;
                joinGuid = new byte[] { };
            }

            public void Stop()
            {
                statefullListenerCancelled = true;
                statelessListenerCancelled = true;
                if (initialized) statefullListenerWorker.Join(); // ISSUE 11
                //statelessListenerWorker.Join();
                //Initialize();
            }

            public void Sync(byte contentCode, byte[] content, ObjectId senderOid, BroadcastFilter filter)
            {
                OrLog(LogLevel.VeryVerbose, "MessageSend RelayCode.RELAY");
                SyncPrivate(RelayCode.RELAY, contentCode, content, senderOid, filter);
            }

            public void SyncLatest(byte contentCode, byte[] content, ObjectId senderOid, BroadcastFilter filter)
            {
                OrLog(LogLevel.VeryVerbose, "MessageSend RelayCode.RELAY_LATEST");
                SyncPrivate(RelayCode.RELAY_LATEST, contentCode, content, senderOid, filter);
            }

            public void SyncStream(byte contentCode, byte[] content, ObjectId senderOid, BroadcastFilter filter)
            {
                OrLog(LogLevel.VeryVerbose, "MessageSend RelayCode.RELAY_STREAM");
                SyncPrivate(RelayCode.RELAY_STREAM, contentCode, content, senderOid, filter);
            }

            public void SyncPlatform(byte contentCode, byte[] content, ObjectId senderOid, BroadcastFilter filter)
            {
                OrLog(LogLevel.VeryVerbose, "MessageSend RelayCode.UNITY_CDK_RELAY");
                SyncPrivate(RelayCode.UNITY_CDK_RELAY, contentCode, content, senderOid, filter);
            }

            public void SyncLatestPlatform(byte contentCode, byte[] content, ObjectId senderOid, BroadcastFilter filter)
            {
                OrLog(LogLevel.VeryVerbose, "MessageSend RelayCode.UNITY_CDK_RELAY_LATEST");
                SyncPrivate(RelayCode.UNITY_CDK_RELAY_LATEST, contentCode, content, senderOid, filter);
            }

            private void SyncPrivate(RelayCode relayCode, byte contentCode, byte[] content, ObjectId senderOid, BroadcastFilter filter)
            {
                if (!_roomJoined)
                {
                    OrLog(LogLevel.VeryVerbose, "Couldnt send Sync");
                    return;
                }

                if (filter.DestCode == DestinationCode.MasterOnly)
                {
                    OrLog(LogLevel.VeryVerbose, "send receivers type master client " + filter.DestCode + " master client id is :" + MasterClient.ID.ToString());
                }
                else if (filter.DestCode == DestinationCode.Include && filter.Destinations.Length > 0)
                {
                    OrLog(LogLevel.VeryVerbose, "send receivers type include " + filter.DestCode);
                    foreach (var t in filter.Destinations)
                    {
                        OrLog(LogLevel.VeryVerbose, "send target id " + t.ToString());
                    }
                }
                else if (filter.DestCode == DestinationCode.Exclude && filter.Destinations.Length > 0)
                {
                    OrLog(LogLevel.VeryVerbose, "send receivers type exclude " + filter.DestCode);
                    foreach (var t in filter.Destinations)
                    {
                        OrLog(LogLevel.VeryVerbose, "send target id " + t.ToString());
                    }
                }
                //else if(options.DestCode == DestinationCode.All)
                //else if (options.DestCode == DestinationCode.Broadcast)
                else
                {
                    OrLog(LogLevel.VeryVerbose, "send receivers type all " + filter.DestCode);
                }

                var headerBytes = Header.CreateHeader(
                    Definitions.FrameVersion,
                    relayCode,
                    (byte)contentCode,
                    0,
                    (byte)filter.DestCode,
                    (PlayerId)LoginId,
                    (ObjectId)senderOid,
                    (UInt16)filter.Destinations.Length,
                    (UInt16)content.Length
                );

                var destPidsSize = sizeof(UInt16) * filter.Destinations.Length;
                var contentSize = content.Length;
                var messageBytes = new byte[headerBytes.Length + destPidsSize + contentSize];
                var stream = new MemoryStream(messageBytes);
                var message = new EndiannessBinaryWriter(stream);
                try
                {
                    message.Write(headerBytes);
                    if (destPidsSize > 0)
                    {
                        message.Write(new byte[destPidsSize]);
                        Buffer.BlockCopy(filter.Destinations, 0, messageBytes, headerBytes.Length, destPidsSize);
                    }
                    message.Write(content);
                    OrLog(LogLevel.VeryVerbose, "prepare Sync: " + BitConverter.ToString(messageBytes));
                    statefullQueue.Enqueue(messageBytes);
                }
                catch (Exception e)
                {
                    OrLogError(LogLevel.Info, "error: " + e.Message);
                    OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                }
                message.Close();
                stream.Close();
            }

            public byte[] Join(string roomName, byte[] newGuid)
            {
                if (_roomJoinComplete) return JoinGuid;

                OrLog(LogLevel.Verbose, "MessageSend RelayCode.JOIN");

                JoinGuid = newGuid;
                var nameBytes = Encoding.UTF8.GetBytes(_player.NickName);
                OrLog(LogLevel.Verbose, "send name bytes: " + BitConverter.ToString(nameBytes));
                var joinSeedSize = JoinGuid.Length;
                var alignmentLen = 4 - joinSeedSize % 4;
                var nameSize = nameBytes.Length;

                var header = new Header();
                //header.Ver = 0;
                header.RelayCode = (byte)RelayCode.JOIN;
                header.ContentCode = (byte)0;
                header.Mask = (byte)0;
                header.DestCode = (byte)DestinationCode.StrictBroadcast;
                header.SrcPid = (PlayerId)Player.ID;// didn't assign yet.
                header.SrcOid = (ObjectId)0;// didn't assign yet.
                header.DestLen = (UInt16)0;
                header.ContentLen = (UInt16)(sizeof(UInt16) + sizeof(UInt16) + joinSeedSize + alignmentLen + nameSize);

                var headerSize = Marshal.SizeOf<Header>(header);
                var headerBytes = new byte[headerSize];
                var gch = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
                Marshal.StructureToPtr(header, gch.AddrOfPinnedObject(), false);
                gch.Free();

                var messageBytes = new byte[headerSize + header.ContentLen];
                var stream = new MemoryStream(messageBytes);
                var message = new EndiannessBinaryWriter(stream);
                try
                {
                    message.Write(headerBytes);
                    message.Write((UInt16)joinSeedSize);
                    message.Write((UInt16)nameSize);
                    message.Write(JoinGuid);
                    if (alignmentLen > 0) { message.Write(new byte[alignmentLen]); }
                    message.Write(nameBytes);
                    OrLog(LogLevel.VeryVerbose, "prepare join: " + BitConverter.ToString(messageBytes));
                    statefullQueue.Enqueue(messageBytes);
                }
                catch (Exception e)
                {
                    OrLogError(LogLevel.Info, "error: " + e.Message);
                    OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                    JoinGuid = new byte[] { };
                }
                message.Close();
                stream.Close();

                _roomJoinComplete = true;
                return JoinGuid;
            }

            public void Leave()
            {
                if (!_roomJoined) return;

                OrLog(LogLevel.Verbose, "MessageSend RelayCode.LEAVE");

                var header = new Header();
                //header.Ver = 0;
                header.RelayCode = (byte)RelayCode.LEAVE;
                header.ContentCode = (byte)0;
                header.Mask = (byte)0;
                header.DestCode = (byte)DestinationCode.StrictBroadcast;
                //msg.LoginSeed = LoginGuid; //Convert.ToBase64String(content)
                header.SrcPid = (PlayerId)Player.ID;
                header.SrcOid = (ObjectId)Player.ObjectId;
                header.DestLen = (UInt16)0;
                header.ContentLen = (UInt16)JoinGuid.Length;

                var headerSize = Marshal.SizeOf<Header>(header);
                var headerBytes = new byte[headerSize];
                var gch = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
                Marshal.StructureToPtr(header, gch.AddrOfPinnedObject(), false);
                gch.Free();

                var joinSeedSize = header.ContentLen;
                var messageBytes = new byte[headerSize + joinSeedSize];
                var stream = new MemoryStream(messageBytes);
                var message = new EndiannessBinaryWriter(stream);
                try
                {
                    message.Write(headerBytes);
                    message.Write(JoinGuid);
                    statefullQueue.Enqueue(messageBytes);
                }
                catch (Exception e)
                {
                    OrLogError(LogLevel.Info, "error: " + e.Message);
                    OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                }
                message.Close();
                stream.Close();

                JoinGuid = new byte[] { };
            }

            public void SetLogin(int id)
            {
                LoginId = id;
            }

            private void UpdateDistMap(sbyte mode, string key, byte[] value)
            {
                if (string.IsNullOrEmpty(key) || key.Length == 0) return;// ignore blank record update.

                if (!_roomJoined && !_DistMapInitializing) return; // throw exception

                if (mode == 0)
                {
                    if (_room.DistMap.ContainsKey(key))
                    {
                        _room.DistMap[key] = value;
                    }
                    else
                    {
                        _room.DistMap.Add(key, value);
                    }
                }
                else
                {
                    if (_room.DistMap.ContainsKey(key))
                    {
                        _room.DistMap.Remove(key);
                    }
                    else
                    {
                        OrLog(LogLevel.Verbose, "Key not found. cannot remove " + key);
                    }
                }

                OrLog(LogLevel.Verbose, "MessageSend RelayCode.UPDATE_DIST_MAP");

                var keyBytes = Encoding.ASCII.GetBytes(key);
                //var valueBytes = ObjectToBytes(value);
                var valueBytes = value;

                var header = new Header();
                //header.Ver = 0;
                header.RelayCode = (byte)RelayCode.UPDATE_DIST_MAP;
                header.ContentCode = (byte)0;
                header.Mask = (byte)0;
                header.DestCode = (byte)DestinationCode.StrictBroadcast;
                header.SrcPid = (PlayerId)Player.ID;
                header.SrcOid = (ObjectId)Player.ObjectId;
                header.DestLen = (UInt16)0;

                var revision = (UInt32)0; //request = 0
                var timestamp = (int)0; //request = 0
                var keyBytesLen = (byte)keyBytes.Length;
                var alignmentLen = 4 - keyBytesLen % 4;
                var valueBytesLen = (UInt16)valueBytes.Length;

                header.ContentLen = (UInt16)(sizeof(UInt16) + sizeof(UInt16) + sizeof(UInt32) + sizeof(UInt32) + keyBytesLen + alignmentLen + valueBytesLen);

                var headerSize = Marshal.SizeOf<Header>(header);
                var headerBytes = new byte[headerSize];
                var gch = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
                Marshal.StructureToPtr(header, gch.AddrOfPinnedObject(), false);
                gch.Free();

                var messageBytes = new byte[headerSize + header.ContentLen];
                var stream = new MemoryStream(messageBytes);
                var message = new EndiannessBinaryWriter(stream);
                try
                {
                    message.Write(headerBytes);
                    message.Write(revision);
                    message.Write(timestamp);
                    message.Write(mode);
                    message.Write(keyBytesLen);
                    message.Write(valueBytesLen);
                    message.Write(keyBytes);
                    if (alignmentLen > 0) { message.Write(new byte[alignmentLen]); }
                    message.Write(valueBytes);
                    OrLog(LogLevel.Verbose, "keylen " + keyBytesLen.ToString() +" valuelen " + valueBytesLen.ToString() + " key " + keyBytes.ToString() + " value " + valueBytes.ToString());
                    statefullQueue.Enqueue(messageBytes);
                }
                catch (Exception e)
                {
                    OrLogError(LogLevel.Info, "error: " + e.Message);                    OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                }
                message.Close();
                stream.Close();
            }

            public void UpdateDistMap(string key, byte[] value)
            {
                UpdateDistMap(0, key, value);
            }

            public void RemoveDistMap(string key)
            {
                UpdateDistMap(-1, key, new byte[1] { 0});
            }

            public void PickDistMap(uint revision)
            {
                if (revision == 0) return;// ignore revision = 0.

                OrLog(LogLevel.Verbose, "MessageSend RelayCode.PICK_DIST_MAP");

                var header = new Header();
                //header.Ver = 0;
                header.RelayCode = (byte)RelayCode.PICK_DIST_MAP;
                header.ContentCode = (byte)0;
                header.Mask = (byte)0;
                header.DestCode = (byte)DestinationCode.StrictBroadcast;
                header.SrcPid = (PlayerId)Player.ID;
                header.SrcOid = (ObjectId)Player.ObjectId;
                header.DestLen = (UInt16)0;

                header.ContentLen = (UInt16)(sizeof(UInt32));

                var headerSize = Marshal.SizeOf<Header>(header);
                var headerBytes = new byte[headerSize];
                var gch = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
                Marshal.StructureToPtr(header, gch.AddrOfPinnedObject(), false);
                gch.Free();

                var messageBytes = new byte[headerSize + header.ContentLen];
                var stream = new MemoryStream(messageBytes);
                var message = new EndiannessBinaryWriter(stream);
                try
                {
                    message.Write(headerBytes);
                    message.Write(revision);
                    statefullQueue.Enqueue(messageBytes);
                }
                catch (Exception e)
                {
                    OrLogError(LogLevel.Info, "error: " + e.Message);
                    OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                }
                message.Close();
                stream.Close();
            }

            public void NotifyDistMapLatestRevision(uint revision)
            {
                if (revision == 0) return;// ignore revision = 0.

                OrLog(LogLevel.Verbose, "MessageSend RelayCode.NOTIFY_DIST_MAP_LATEST");

                var header = new Header();
                //header.Ver = 0;
                header.RelayCode = (byte)RelayCode.NOTIFY_DIST_MAP_LATEST;
                header.ContentCode = (byte)0;
                header.Mask = (byte)0;
                header.DestCode = (byte)DestinationCode.StrictBroadcast;
                header.SrcPid = (PlayerId)Player.ID;
                header.SrcOid = (ObjectId)Player.ObjectId;
                header.DestLen = (UInt16)0;

                header.ContentLen = (UInt16)(sizeof(UInt32));

                var headerSize = Marshal.SizeOf<Header>(header);
                var headerBytes = new byte[headerSize];
                var gch = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
                Marshal.StructureToPtr(header, gch.AddrOfPinnedObject(), false);
                gch.Free();

                var messageBytes = new byte[headerSize + header.ContentLen];
                var stream = new MemoryStream(messageBytes);
                var message = new EndiannessBinaryWriter(stream);
                try
                {
                    message.Write(headerBytes);
                    message.Write(revision);
                    statefullQueue.Enqueue(messageBytes);
                }
                catch (Exception e)
                {
                    OrLogError(LogLevel.Info, "error: " + e.Message);
                    OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                }
                message.Close();
                stream.Close();
            }

            public void SetMaster(UserSession player)
            {
                if (!_roomJoined) return;

                OrLog(LogLevel.Verbose, "MessageSend RelayCode.SET_MASTER");
                OrLog(LogLevel.Info, "Set Master player:" + player.ID.ToString());

                var header = new Header();
                //header.Ver = 0;
                header.RelayCode = (byte)RelayCode.SET_MASTER;
                header.ContentCode = (byte)0;
                header.Mask = (byte)0;
                header.DestCode = (byte)DestinationCode.StrictBroadcast;
                header.SrcPid = (PlayerId)Player.ID;
                header.SrcOid = (ObjectId)Player.ObjectId;
                header.DestLen = (UInt16)0;
                header.ContentLen = (UInt16)0;

                var headerSize = Marshal.SizeOf<Header>(header);
                var masterPidSize = sizeof(PlayerId);
                var headerBytes = new byte[headerSize];
                var gch = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
                Marshal.StructureToPtr(header, gch.AddrOfPinnedObject(), false);
                gch.Free();

                var messageBytes = new byte[headerSize + masterPidSize];
                var stream = new MemoryStream(messageBytes);
                var message = new EndiannessBinaryWriter(stream);
                try
                {
                    message.Write(headerBytes);
                    message.Write((PlayerId)player.ID);
                    statefullQueue.Enqueue(messageBytes);
                }
                catch (Exception e)
                {
                    OrLogError(LogLevel.Info, "error: " + e.Message);
                    OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                }
                message.Close();
                stream.Close();
            }

            public void GetMaster()
            {
                if (!_roomJoined) return;

                OrLog(LogLevel.Verbose, "MessageSend RelayCode.GET_MASTER");
                OrLog(LogLevel.Info, "Get Master player");

                var header = new Header();
                //header.Ver = 0;
                header.RelayCode = (byte)RelayCode.GET_MASTER;
                header.ContentCode = (byte)0;
                header.Mask = (byte)0;
                header.DestCode = (byte)DestinationCode.StrictBroadcast;
                header.SrcPid = (PlayerId)Player.ID;
                header.SrcOid = (ObjectId)Player.ObjectId;
                header.DestLen = (UInt16)0;
                header.ContentLen = (UInt16)0;

                var headerSize = Marshal.SizeOf<Header>(header);
                var headerBytes = new byte[headerSize];
                var gch = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
                Marshal.StructureToPtr(header, gch.AddrOfPinnedObject(), false);
                gch.Free();

                var messageBytes = new byte[headerSize];
                var stream = new MemoryStream(messageBytes);
                var message = new EndiannessBinaryWriter(stream);
                try
                {
                    message.Write(headerBytes);
                    statefullQueue.Enqueue(messageBytes);
                }
                catch (Exception e)
                {
                    OrLogError(LogLevel.Info, "error: " + e.Message);
                    OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                }
                message.Close();
                stream.Close();
            }

            public void LoadPlayer(UInt16 offset = 0)
            {
                if (!_roomJoined) return;

                OrLog(LogLevel.Verbose, "MessageSend RelayCode.LOAD_PLAYER");

                var header = new Header();
                //header.Ver = 0;
                header.RelayCode = (byte)RelayCode.LOAD_PLAYER;
                header.ContentCode = (byte)0;
                header.Mask = (byte)0;
                header.DestCode = (byte)DestinationCode.StrictBroadcast;
                header.SrcPid = (PlayerId)Player.ID;
                header.SrcOid = (ObjectId)Player.ObjectId;
                header.DestLen = (UInt16)0;
                header.ContentLen = (UInt16)(sizeof(UInt16));

                var headerSize = Marshal.SizeOf<Header>(header);
                var masterPidSize = sizeof(PlayerId);
                var headerBytes = new byte[headerSize];
                var gch = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
                Marshal.StructureToPtr(header, gch.AddrOfPinnedObject(), false);
                gch.Free();

                var messageBytes = new byte[headerSize + masterPidSize];
                var stream = new MemoryStream(messageBytes);
                var message = new EndiannessBinaryWriter(stream);
                try
                {
                    message.Write(headerBytes);
                    message.Write(offset);
                    statefullQueue.Enqueue(messageBytes);
                }
                catch (Exception e)
                {
                    OrLogError(LogLevel.Info, "error: " + e.Message);
                    OrLogError(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                }
                message.Close();
                stream.Close();
            }
        }
    }
}
