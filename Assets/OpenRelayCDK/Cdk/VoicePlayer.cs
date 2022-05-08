using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using UnityEngine;
using UnityOpus;
using System.Collections;

namespace Com.FurtherSystems.OpenRelay
{
    [RequireComponent(typeof(AudioSource))]
    //[RequireComponent(typeof(AudioListener))]
    public class VoicePlayer : MonoBehaviour
    {
        [SerializeField]
        bool AutoStart = false;
        [SerializeField]
        bool OpusEncode = false;
        [SerializeField]
        bool DebugEcho = false;
        [SerializeField]
        long DebugEchoDelay = 5;

        UInt16 CurrentPlayerId = 0;
        UInt16 CurrentObjectId = 0;

        const int samplingFrequency = 24000;
        const int lengthSeconds = 1;
        const int bitrate = 30000;
        const int frameSize = 120;
        const int outputBufferSize = frameSize * 4; // at least frameSize * sizeof(float)
        int head = 0;
        ConcurrentQueue<EncodeSet[]> delaySyncQueue;
        ConcurrentQueue<float[]> delaySyncRawQueue;
        const int queueLimit = 25;
        AudioClip clip;
        AudioSource source;
        Decoder decoder;
        readonly float[] pcmBuffer = new float[Decoder.maximumPacketDuration * (int)NumChannels.Mono];
        int audioPosition = 0;

        long startTime = 0;
        bool playing = false;
        public bool IsPlaying
        {
            get { return playing; }
            set { playing = value; }
        }

        void Start()
        {
            OpenRelayClient.OnSyncStreamCall += delegate { };
            if (AutoStart)
            {
                StartPlayer((UInt16)OpenRelayClient.Player.ID, OpenRelayClient.AllocateObjectId());
            }
        }

        //void Update()
        //{
        //    if (!initialized || !playing) return;

        //    var data = Dequeue();
        //    if (data != null)
        //    {
        //        source.clip.SetData(data, 0);
        //    }
        //}

        public void StartPlayer(UInt16 playerId, UInt16 objectId)
        {
            CurrentPlayerId = playerId;
            CurrentObjectId = objectId;
            StartCoroutine(Initialize());
        }

        public bool initialized = false;
        public IEnumerator Initialize()
        {
            initialized = false;
            OpenRelayClient.OnSyncStreamCall += OnSyncStream;
            //delaySyncQueue = new ConcurrentQueue<EncodeSet[]>();
            delaySyncRawQueue = new ConcurrentQueue<float[]>();
            if (OpusEncode) { decoder = new Decoder(SamplingFrequency.Frequency_24000, NumChannels.Mono);}

            source = GetComponent<AudioSource>();
            //OnAudioRead
            //source.clip = AudioClip.Create("VoicePlayer", 1, 1, samplingFrequency, true, OnAudioRead, OnAudioSetPosition);
            //OnAudioFilterRead
            source.clip = AudioClip.Create("VoicePlayer", 1, 1, samplingFrequency, true, false);
            //clip.SetData
            //source.clip = AudioClip.Create("VoicePlayer", 1, 1, samplingFrequency, false);
            source.loop = true;
            source.Play();
            initialized = true;
            playing = true;
            yield break;
        }

        public void EndPlayer()
        {
            playing = false;
            initialized = false;
            OpenRelayClient.OnSyncStreamCall -= OnSyncStream;
            source.Stop();
        }

        void OnSyncStream(byte code, byte[] content, UInt16 senderPlayerId, UInt16 senderObjectId)
        {
            if (!initialized || !playing) return;
            if (CurrentPlayerId != senderPlayerId) return;
            if (CurrentObjectId != senderObjectId) return;

            if (OpusEncode && delaySyncRawQueue.Count < queueLimit)
            {
                //var encodeSets = ByteArrayToEncodeSets(content);
                ////delaySyncQueue.Enqueue(encodeSets);
                //float[] data;
                //var list = new List<float>();
                //foreach (var sets in encodeSets)
                //{
                //    var dataLength = decoder.Decode(sets.Encoded, sets.Length, pcmBuffer);
                //    data = new float[dataLength];
                //    Array.Copy(pcmBuffer, data, dataLength);
                //    list.AddRange(data);
                //}
                //delaySyncRawQueue.Enqueue(list.ToArray());
                var data = ByteArrayToDecode(content);
                if (data != null) delaySyncRawQueue.Enqueue(data);
            }
            else if (delaySyncRawQueue.Count < queueLimit)
            {
                var dataraw = new float[content.Length / 4];
                Buffer.BlockCopy(content, 0, dataraw, 0, content.Length);
                delaySyncRawQueue.Enqueue(dataraw);
            }
        }

        float[] Dequeue()
        {
            EncodeSet[] encodeSets;
            float[] data = null;
            var list = new List<float>();
            //if (OpusEncode)
            //{
            //    if (delaySyncQueue.TryDequeue(out encodeSets))
            //    {
            //        foreach (var sets in encodeSets)
            //        {
            //            var dataLength = decoder.Decode(sets.Encoded, sets.Length, pcmBuffer);
            //            data = new float[dataLength];
            //            Array.Copy(pcmBuffer, data, dataLength);
            //            list.AddRange(data);
            //        }
            //        data = list.ToArray();
            //    }
            //    else
            //    {
            //        data = null;
            //    }
            //}
            //else
            //{
                if (!delaySyncRawQueue.TryDequeue(out data)) { data = null; }
            //}
            return data;
        }

        //void OnAudioRead(float[] data)
        //{
        //    int count = 0;
        //    while (count < data.Length)
        //    {
        //        float d;
        //        if (!PopData(out d))
        //        {
        //            return;
        //        }
        //        else
        //        {
        //            data[count] = (float)d;
        //            count++;
        //        }
        //    }
        //}

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!initialized) return;

            for (int i = 0; i < data.Length / channels; ++i)
            {
                float d;
                if (!PopData(out d))
                {
                    return;
                }
                data[channels * i] = data[channels * i + 1] = d;
            }
        }

        int dataCounter = 0;
        int encodeSetCounter = 0;
        EncodeSet[] encodeSets;
        float[] popdRaw = null;
        bool PopData(out float data)
        {
            if (DebugEcho)
            {
                if (startTime == 0)
                {
                    startTime = ToUnixTime(DateTime.Now);
                    data = 0;
                    return false;
                }
                else if (startTime + DebugEchoDelay >= ToUnixTime(DateTime.Now))
                {
                    data = 0;
                    return false;
                }
            }
            //if (OpusEncode)
            //{
            //    if (popdRaw == null || popdRaw.Length <= dataCounter)
            //    {
            //        dataCounter = 0;
            //        encodeSetCounter++;
            //        if (encodeSets == null || encodeSets.Length <= encodeSetCounter)
            //        {
            //            if (delaySyncQueue.TryDequeue(out encodeSets))
            //            {
            //                encodeSetCounter = 0;
            //            }
            //            else
            //            {
            //                data = 0; return false;
            //            }
            //        }
            //        var dataLength = decoder.Decode(encodeSets[encodeSetCounter].Encoded, encodeSets[encodeSetCounter].Length, pcmBuffer);
            //        popdRaw = new float[dataLength];
            //        Array.Copy(pcmBuffer, popdRaw, dataLength);
            //    }
            //    data = popdRaw[dataCounter];
            //    dataCounter++;
            //    return true;
            //}
            //else
            //{
                if (popdRaw == null || popdRaw.Length <= dataCounter)
                {
                    dataCounter = 0;
                    if (!delaySyncRawQueue.TryDequeue(out popdRaw)) { data = 0; return false; }
                }
                data = popdRaw[dataCounter];
                dataCounter++;
                return true;
            //}
        }

        public static DateTime UNIX_EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public static long ToUnixTime(DateTime targetTime)
        {
            targetTime = targetTime.ToUniversalTime();
            TimeSpan elapsedTime = targetTime - UNIX_EPOCH;
            return (long)elapsedTime.TotalSeconds;
        }

        void OnAudioSetPosition(int newPosition)
        {
            audioPosition = newPosition;
        }

        EncodeSet[] ByteArrayToEncodeSets(byte[] content)
        {
            var contentLength = content.Length;
            var encodeSets = new List<EncodeSet>();
            var stream = new MemoryStream(content);
            var message = new BinaryReader(stream);
            try
            {
                while(contentLength > 0)
                {
                    var length = message.ReadUInt16();
                    var encoded = message.ReadBytes(length);
                    encodeSets.Add(new EncodeSet{
                        Length = length,
                        Encoded = encoded
                    });
                    contentLength -= 2 + length;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("error: " + e.Message + e.StackTrace);
            }
            return encodeSets.ToArray();
        }

        float[] ByteArrayToDecode(byte[] content)
        {
            var contentLength = content.Length;
            var stream = new MemoryStream(content);
            var message = new BinaryReader(stream);
            float[] decoded = null;
            float[] data = null;
            var list = new List<float>();
            try
            {
                while (contentLength > 0)
                {
                    var length = message.ReadUInt16();
                    var encoded = message.ReadBytes(length);
                    var dataLength = decoder.Decode(encoded, length, pcmBuffer);
                    decoded = new float[dataLength];
                    Array.Copy(pcmBuffer, decoded, dataLength);
                    list.AddRange(decoded);
                    contentLength -= 2 + length;
                }
                if (list.Count > 0) data = list.ToArray();
            }
            catch (Exception e)
            {
                Debug.LogError("error: " + e.Message + e.StackTrace);
            }
            return data;
        }
    }
}