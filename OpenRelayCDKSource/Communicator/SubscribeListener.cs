﻿//------------------------------------------------------------------------------
// <copyright file="SubscriberListener.cs" company="FurtherSystem Co.,Ltd.">
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
using DTLS;
using System.Net;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;

namespace Com.FurtherSystems.OpenRelay
{
    public static partial class OpenRelayClient
    {
        internal class SubscriberListener
        {
            public bool Aborted
            {
                get { return statefullListenerEnqueueCancelled; }
            }
            private readonly Thread statefullListenerEnqueueWorker;
            private readonly Thread statelessListenerEnqueueWorker;
            private bool statefullListenerEnqueueCancelled;
            private bool statelessListenerEnqueueCancelled;
            private bool initialized = false; // ISSUE 11 

            public delegate void MessageDelegate(EndiannessBinaryReader message);
            private readonly MessageDelegate messageDelegate = delegate{ };
            private readonly ConcurrentQueue<byte[]> statefullQueue = new ConcurrentQueue<byte[]>();
            private readonly ConcurrentQueue<byte[]> statelessQueue = new ConcurrentQueue<byte[]>();

            private void StatefullListenerEnqueueLoop()
            {
                OrLog(LogLevel.Verbose, "StatefullListenerEnqueueLoop start tcp://" + Room.ListenAddrV4 + ":" + Room.StatefullSubPort);
                AsyncIO.ForceDotNet.Force();
                using (var subSocket = new SubscriberSocket())
                {
                    subSocket.Options.ReceiveHighWatermark = 1000;//default 1000
                    subSocket.Connect("tcp://" + Room.ListenAddrV4 + ":" + Room.StatefullSubPort);
                    subSocket.Subscribe("");
                    while (!statefullListenerEnqueueCancelled)
                    {
                        try
                        {
                            byte[] frameString;
                            if (!subSocket.TryReceiveFrameBytes(out frameString)) continue;
                            statefullQueue.Enqueue(frameString);
                            Thread.Yield();
                        }
                        catch (TerminatingException te)
                        {
                            OrLogWarn(LogLevel.Verbose, "Terminating Exception: " + te.Message);
                            break;
                        }
                }
                    subSocket.Close();
                }
                NetMQConfig.Cleanup();
                OrLog(LogLevel.Verbose, "StatefullListenerEnqueueLoop end");
            }
            static byte[] HexToBytes(string hex)
            {
                byte[] result = new byte[hex.Length / 2];
                int count = 0;
                for (int index = 0; index < hex.Length; index += 2)
                {
                    result[count] = Convert.ToByte(hex.Substring(index, 2), 16);
                    count++;
                }
                return result;
            }
            private void StatelessListenerEnqueueLoop()
            {
                OrLog(LogLevel.Verbose, "StatelessListenerEnqueueLoop start tcp://" + Room.ListenAddrV4 + ":" + Room.StatelessSubPort);

                bool exit = false;
                Client client = new Client(new IPEndPoint(IPAddress.Any, 0));//zero is any.
                client.PSKIdentities.AddIdentity(Encoding.UTF8.GetBytes("oFIrQFrW8EWcZ5u7eGfrkw"), HexToBytes("7CCDE14A5CF3B71C0C08C8B7F9E5"));
                //client.LoadCertificateFromPem(@"Client.pem");
                client.SupportedCipherSuites.Add(TCipherSuite.TLS_PSK_WITH_AES_128_CCM_8);
                client.ConnectToServer(new IPEndPoint(IPAddress.Parse(Room.ListenAddrV4), int.Parse(Room.StatelessSubPort)));
                while (!exit)
                {
                    if (Console.KeyAvailable)
                    {
                        //ConsoleKeyInfo pressedKey = Console.ReadKey(true);
                        //client.Send(Encoding.UTF8.GetBytes(pressedKey.KeyChar.ToString()));
                        //client.
                    }
                }
                client.Stop();

                OrLog(LogLevel.Verbose, "StatelessListenerEnqueueLoop end");
            }

            public SubscriberListener(MessageDelegate md)
            {
                messageDelegate = md;
                statefullListenerEnqueueWorker = new Thread(StatefullListenerEnqueueLoop);
                //statelessListenerEnqueueWorker = new Thread(StatelessListenerEnqueueLoop);
            }

            public void Start()
            {
                initialized = true; // ISSUE 11
                Initialize();
                statefullListenerEnqueueCancelled = false;
                statelessListenerEnqueueCancelled = false;
                statefullListenerEnqueueWorker.Start();
                //statelessListenerEnqueueWorker.Start();
            }

            private void Initialize()
            {
            }

            public void Stop()
            {
                statefullListenerEnqueueCancelled = true;
                statelessListenerEnqueueCancelled = true;
                if (initialized) statefullListenerEnqueueWorker.Join(); // ISSUE 11 
                //statelessListenerEnqueueWorker.Join();
                //Initialize();
            }

            public void RetrieveQueueStateless()
            {
                while (!statelessQueue.IsEmpty)
                {
                    if (!IsMessageQueueRunning) continue;

                    byte[] message;
                    if (statelessQueue.TryDequeue(out message))
                    {
                        var stream = new MemoryStream(message);
                        var reader = new EndiannessBinaryReader(stream);
                        try
                        {
                            messageDelegate(reader);
                        }
                        catch (Exception e)
                        {
                            OrLogWarn(LogLevel.Info, "error: " + e.Message);
                            OrLogWarn(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                        }
                        finally
                        {
                            reader.Close();
                            stream.Close();
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            public IEnumerator RetrieveQueueStatefull()
            {
                while (!statefullQueue.IsEmpty)
                {
                    //if (!IsMessageQueueRunning) continue; // maybe need buffering.

                    byte[] message;
                    if (statefullQueue.TryDequeue(out message))
                    {
                        if (message == null) continue;

                        var stream = new MemoryStream(message);
                        var reader = new EndiannessBinaryReader(stream);
                        try
                        {
                            messageDelegate(reader);
                        }
                        catch (Exception e)
                        {
                            OrLogWarn(LogLevel.Info, "error: " + e.Message);
                            OrLogWarn(LogLevel.Verbose, "stacktrace: " + e.StackTrace);
                        }
                        finally
                        {
                            reader.Close();
                            stream.Close();
                        }
                    }
                    else
                    {
                        break;
                    }
                    yield return null;
                }
                yield break;
            }

            public IEnumerator CloseGapDistMap()
            {
                if (_room.DistMapLatestRevision == 0)
                {
                    yield return stateHandler.GetRoomDistMap(_room);
                }

                if (_room.DistMapLatestRevision != _room.DistMapMergedRevision)
                {
                    var before = _DistMapReady;
                    _DistMapReady = false;

                    // Detect Gap
                    if (before) OnOpenRelayRoomDistMapGapDetectedCall(_room.DistMapMergedRevision, _room.DistMapLatestRevision);

                    var nextRevision = _room.DistMapMergedRevision + 1;
                    if (_room.DistMapShelved.ContainsKey(nextRevision))
                    {
                        _room.DistMapMergedRevision = nextRevision;
                        var raw = _room.DistMapShelved[_room.DistMapMergedRevision];
                        var key = Encoding.ASCII.GetString(raw.Key);
                        if (_room.DistMap.ContainsKey(key))
                        {
                            if (raw.Mode == -1)// -1 is delete
                            {
                                _room.DistMap.Remove(key);
                            }
                            else
                            {
                                _room.DistMap[key] = raw.Value;
                            }
                        }
                        else
                        {
                            _room.DistMap.Add(key, raw.Value);
                        }
                        yield return null;
                    }
                    else
                    {
                        // TODO Retry over logic.
                        // TODO dead line scheduling.
                        dealerListener.PickDistMap(nextRevision);
                        yield return null;
                    }
                }

                if (_room.DistMapLatestRevision == _room.DistMapMergedRevision)
                {
                    var before = _DistMapReady;
                    _DistMapReady = true;

                    if (!before)
                    {
                        OnOpenRelayRoomDistMapGapClosedCall(_room.DistMapMergedRevision, _room.DistMapLatestRevision);
                    }

                    if (_room.DistMapMergedRevision > _room.DistMapNotifiedRevision)
                    {
                        dealerListener.NotifyDistMapLatestRevision(_room.DistMapMergedRevision);
                        _room.DistMapNotifiedRevision = _room.DistMapMergedRevision;
                    }

                    yield return null;
                }
                yield break;
            }
        }
    }
}
