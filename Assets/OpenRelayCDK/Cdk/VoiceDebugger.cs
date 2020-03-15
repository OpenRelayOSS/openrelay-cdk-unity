using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityOpus;

namespace Com.FurtherSystems.OpenRelay
{
    [RequireComponent(typeof(AudioSource))]
    //[RequireComponent(typeof(AudioListener))]
    public class VoiceDebugger : MonoBehaviour
    {
        [SerializeField]
        int MicrophoneIndex = 0;
        [SerializeField]
        bool AutoStart = true;
        [SerializeField]
        bool OpusEncode = true;

        bool DelayDebug = false;
        const int samplingFrequency = 48000;
        const int lengthSeconds = 1;
        const int bitrate = 96000;
        const int frameSize = 120;
        const int outputBufferSize = frameSize * 4; // at least frameSize * sizeof(float)
        AudioClip clip;
        int head = 0;
        float[] processBuffer = new float[512];
        float[] microphoneBuffer = new float[lengthSeconds * samplingFrequency];
        Encoder encoder;
        bool recorderInitialized = false;
        readonly float[] frameBuffer = new float[frameSize];
        readonly byte[] outputBuffer = new byte[outputBufferSize];
        readonly float[] amountFrameBuffer = new float[frameSize];
        int amountFrameBufferSize = 0;
        //for debug.
        ConcurrentQueue<EncodeSet[]> delaySyncQueue;
        ConcurrentQueue<float[]> delaySyncRawQueue;
        Queue<float> pcmQueue;
        bool DebugStart = false;
        AudioSource source;
        Decoder decoder;
        readonly float[] pcmBuffer = new float[Decoder.maximumPacketDuration * (int)NumChannels.Mono];
        int audioPosition = 0;

        private StreamWriter beforeWriter;
        private StreamWriter afterWriter;

        void Start()
        {
            beforeWriter = new StreamWriter(Application.dataPath + @"\..\..\before.txt", append: true);
            afterWriter = new StreamWriter(Application.dataPath + @"\..\..\after.txt", append: true);
            delaySyncQueue = new ConcurrentQueue<EncodeSet[]>();
            delaySyncRawQueue = new ConcurrentQueue<float[]>();
            pcmQueue = new Queue<float>();
            if (AutoStart) { StartRecorder(); }
            if (OpusEncode) { decoder = new Decoder(SamplingFrequency.Frequency_48000, NumChannels.Mono); }
            source = GetComponent<AudioSource>();
            source.clip = AudioClip.Create("VoicePlayer", 1, 1, samplingFrequency, true, OnAudioRead, OnAudioSetPosition);
            source.loop = true;
            source.Play();
        }
        public void StartRecorder()
        {
            try
            {
                //var source = gameObject.AddComponent<AudioSource>();
                clip = Microphone.Start(Microphone.devices[MicrophoneIndex], true, lengthSeconds, samplingFrequency);
                //source.clip = clip;
                //source.loop = true;
                //while (Microphone.GetPosition(null) < 0) { }
                //source.Play();
            }
            catch (Exception e)
            {
                Debug.Log("Microphone Initialize failed " + e.Message);
                return;
            }
            if (OpusEncode)
            {
                encoder = new Encoder(SamplingFrequency.Frequency_48000, NumChannels.Mono, OpusApplication.Audio)
                {
                    Bitrate = bitrate,
                    Complexity = 10,
                    Signal = OpusSignal.Music
                };
            }
            if (DelayDebug)
            {
                delaySyncQueue = new ConcurrentQueue<EncodeSet[]>();
                delaySyncRawQueue = new ConcurrentQueue<float[]>();
            }
            recorderInitialized = true;
        }
        public void EndRecorder()
        {
            recorderInitialized = false;
            Microphone.End(Microphone.devices[MicrophoneIndex]);
        }

#if DEBUG
        void OnGUI()
        {
            if (GUI.Button(new Rect(20, 20, 200, 50), recorderInitialized ? "To Disable Recorder" : "To Enable Recorder"))
            {
                if (recorderInitialized)
                {
                    EndRecorder();
                }
                else
                {
                    StartRecorder();
                }
            }
            if (GUI.Button(new Rect(20, 80, 200, 50), DelayDebug ? "To DelayDebug Off" : "To DelayDebug On"))
            {
                DelayDebug = !DelayDebug;
            }
        }
#endif
        void Update()
        {
            if (!recorderInitialized) return;
            var position = Microphone.GetPosition(null);
            if (position < 0 || head == position)
            {
                return;
            }
            clip.GetData(microphoneBuffer, 0);
            while (GetDataLength(microphoneBuffer.Length, head, position) > processBuffer.Length)
            {
                var remain = microphoneBuffer.Length - head;
                if (remain < processBuffer.Length)
                {
                    Array.Copy(microphoneBuffer, head, processBuffer, 0, remain);
                    Array.Copy(microphoneBuffer, 0, processBuffer, remain, processBuffer.Length - remain);
                }
                else
                {
                    Array.Copy(microphoneBuffer, head, processBuffer, 0, processBuffer.Length);
                }
                SyncVoice(processBuffer);
                head += processBuffer.Length;
                if (head > microphoneBuffer.Length)
                {
                    head -= microphoneBuffer.Length;
                }
            }
        }

        static int GetDataLength(int bufferLength, int head, int tail)
        {
            if (head < tail)
            {
                return tail - head;
            }
            else
            {
                return bufferLength - head + tail;
            }
        }

        void SyncVoice(float[] inData)
        {
            if (OpusEncode)
            {
                if (DelayDebug)
                {
                    var data = new float[inData.Length + amountFrameBufferSize];
                    if (amountFrameBufferSize > 0)
                    {
                        Array.Copy(amountFrameBuffer, 0, data, 0, amountFrameBufferSize);
                    }
                    Array.Copy(inData, 0, data, amountFrameBufferSize, inData.Length);
                    int position = 0;
                    int counter = 0;
                    var encodeSets = new List<EncodeSet>();
                    while (data.Length - position > frameSize)
                    {
                        Array.Copy(data, position, frameBuffer, 0, frameSize);
                        var encodeSet = new EncodeSet();
                        encodeSet.Length = (UInt16)encoder.Encode(frameBuffer, outputBuffer);
                        encodeSet.Encoded = new byte[encodeSet.Length];
                        Array.Copy(outputBuffer, encodeSet.Encoded, encodeSet.Length);
                        encodeSets.Add(encodeSet);
                        position += frameSize;
                        counter++;
                    }
                    delaySyncQueue.Enqueue(encodeSets.ToArray());
                    //amount logics.
                    amountFrameBufferSize = data.Length - position;
                    if (amountFrameBufferSize >= 0)
                    {
                        Array.Copy(data, position, amountFrameBuffer, 0, amountFrameBufferSize);
                    }
                    //foreach (var sample in inData)
                    //{
                    //    pcmQueue.Enqueue(sample);
                    //}
                    //while (pcmQueue.Count > frameSize)
                    //{
                    //    for (int i = 0; i < frameSize; i++)
                    //    {
                    //        frameBuffer[i] = pcmQueue.Dequeue();
                    //    }
                    //    var encodedLength = encoder.Encode(frameBuffer, outputBuffer);
                    //    var encodedData = new byte[encodedLength];
                    //    Array.Copy(outputBuffer, encodedData, encodedLength);
                    //    delaySyncQueue.Enqueue(encodedData);
                    //}
                }
            }
            else
            {
                if (DelayDebug)
                {
                    var dataraw = new float[inData.Length];
                    Array.Copy(inData, 0, dataraw, 0, inData.Length);
                    delaySyncRawQueue.Enqueue(dataraw);
                }
            }
        }

        string FloatArrayToString(float[] array)
        {
            var builder = new System.Text.StringBuilder();
            foreach (var f in array)
            {
                builder.Append(BitConverter.ToInt32(BitConverter.GetBytes(f), 0).ToString("x2")).Append("\r\n");
            }
            return builder.ToString();
        }

        void OnAudioRead(float[] data)
        {
            if (!DelayDebug) return;
            int count = 0;
            while (count < data.Length)
            {
                float d;
                if (!PopData(out d))
                {
                    return;
                }
                else
                {
                    data[count] = (float)d;
                    count++;
                }
            }
        }

        //void OnAudioFilterRead(float[] data, int channels)
        //{
        //    if (!DelayDebug) return;
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

        int dataCounter = 0;
        int encodeSetCounter = 0;
        EncodeSet[] encodeSets;
        float[] popdRaw = null;
        bool PopData(out float data)
        {
            if (OpusEncode)
            {
                if (popdRaw == null || popdRaw.Length <= dataCounter)
                {
                    dataCounter = 0;
                    encodeSetCounter++;
                    if (encodeSets == null || encodeSets.Length <= encodeSetCounter)
                    {
                        if (delaySyncQueue.TryDequeue(out encodeSets))
                        {
                            encodeSetCounter = 0;
                        }
                        else
                        {
                            data = 0; return false;
                        }
                    }
                    var dataLength = decoder.Decode(encodeSets[encodeSetCounter].Encoded, encodeSets[encodeSetCounter].Length, pcmBuffer);
                    popdRaw = new float[dataLength];
                    Array.Copy(pcmBuffer, popdRaw, dataLength);
                }
                data = popdRaw[dataCounter];
                dataCounter++;
                return true;
            }
            else
            {
                if (popdRaw == null || popdRaw.Length <= dataCounter)
                {
                    dataCounter = 0;
                    if (!delaySyncRawQueue.TryDequeue(out popdRaw)) { data = 0; return false; }
                }
                data = popdRaw[dataCounter];
                dataCounter++;
                return true;
            }
        }
        void OnAudioSetPosition(int newPosition)
        {
            audioPosition = newPosition;
        }
    }
}