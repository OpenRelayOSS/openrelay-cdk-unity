using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Android;
using UnityOpus;


namespace Com.FurtherSystems.OpenRelay
{
    //[StructLayout(LayoutKind.Explicit)]
    public class EncodeSet
    {
        //[FieldOffset(0)]
        public UInt16 Length;
        //[FieldOffset(2)]
        public byte[] Encoded;
    }

    public class VoiceRecorder : MonoBehaviour
    {
        public enum Status
        {
            NoDetect = 0,
            Error = 1,
            Ready = 2,
            Broadcasting = 3,
        }
        [SerializeField]
        bool AutoStart = false;
        [SerializeField]
        public int PreferMicrophoneIndex = 0;
        [SerializeField]
        bool SyncBuffering = false;
        [SerializeField]
        int SyncBufferingMax = 800;
        [SerializeField]
        bool OpusEncode = false;
        [SerializeField]
        Status VoiceRecorderStatus = Status.NoDetect;
        [SerializeField]
        bool DebugEcho = false;

        UInt16 CurrentPlayerId = 0;
        UInt16 CurrentObjectId = 0;
        
        string microphoneName = string.Empty;

        const int samplingFrequency = 48000;
        const int lengthSeconds = 1;
        const int bitrate = 30000;
        const int frameSize = 120;
        const int outputBufferSize = frameSize * 4; // at least frameSize * sizeof(float)
        AudioClip clip;
        int head = 0;
        float[] processBuffer = new float[512];
        float[] microphoneBuffer = new float[lengthSeconds * samplingFrequency];
        Encoder encoder;
        readonly float[] frameBuffer = new float[frameSize];
        readonly byte[] outputBuffer = new byte[outputBufferSize];
        readonly float[] amountFrameBuffer = new float[frameSize];
        int amountFrameBufferSize = 0;

        List<EncodeSet> encodeSetsBuffer;
        int encodeSetBufferCounter = 0;
        bool recording = false;
        public bool IsRecording
        {
            get { return recording; }
            set { recording = value; }
        }

        bool microphonePermissionRequesting;

        private readonly OpenRelayClient.BroadcastFilter echoSyncOption = new OpenRelayClient.BroadcastFilter(OpenRelayClient.DestinationCode.StrictBroadcast);

        void Start()
        {
            if (AutoStart)
            {
                StartRecorder((UInt16)OpenRelayClient.Player.ID, OpenRelayClient.AllocateObjectId(), PreferMicrophoneIndex);
            }
        }

        public void StartRecorder(UInt16 playerId, UInt16 objectId, int microphoneIndex)
        {
            CurrentPlayerId = playerId;
            CurrentObjectId = objectId;
            StartCoroutine(Initialize(microphoneIndex));
        }

        bool initialized = false;
        public IEnumerator Initialize(int microphoneIndex)
        {
            VoiceRecorderStatus = Status.NoDetect;

            // for android only
            if (Application.platform == RuntimePlatform.Android)
            {
                if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                {
                    yield return RequestAndroidMicrophonePermission();
                }
            }
            else
            {
                yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
                if (Application.HasUserAuthorization(UserAuthorization.Microphone) == false)
                {
                    Debug.Log("microphone permission error.");
                    VoiceRecorderStatus = Status.Error;
                    yield break;
                }
            }

            if (Microphone.devices == null || Microphone.devices.Length == 0)
            {
                Debug.Log("no microphone device");
                VoiceRecorderStatus = Status.NoDetect;
                yield break;
            }

            try
            {
                microphoneName = Microphone.devices[microphoneIndex];
                clip = Microphone.Start(microphoneName, true, lengthSeconds, samplingFrequency);
            }
            catch (Exception e)
            {
                Debug.Log("microphone Initialize failed " + e.Message);
                yield break;
            }

            VoiceRecorderStatus = Status.Broadcasting;
            Debug.Log("microphone ok.");

            if (OpusEncode)
            {
                encoder = new Encoder(SamplingFrequency.Frequency_24000, NumChannels.Mono, OpusApplication.VoIP)
                {
                    Bitrate = bitrate,
                    Complexity = 10,
                    Signal = OpusSignal.Voice
                };
            }

            if (SyncBuffering) { encodeSetsBuffer = new List<EncodeSet>(); }
            initialized = true;
            recording = true;
            yield break;
        }

        public void EndRecorder()
        {
            recording = false;
            initialized = false;
            Microphone.End(microphoneName);
        }

        // for android only
        IEnumerator RequestAndroidMicrophonePermission()
        {
            microphonePermissionRequesting = true;
            Permission.RequestUserPermission(Permission.Microphone);
            float timeElapsed = 0;
            while (microphonePermissionRequesting)
            {
                if (timeElapsed > 0.5f)
                {
                    microphonePermissionRequesting = false;
                    yield break;
                }
                timeElapsed += Time.deltaTime;

                yield return null;
            }
            yield break;
        }

        void Update()
        {
            if (!initialized || !recording) return;
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

                var option = OpenRelayClient.BroadcastFilter.Default;
                if (DebugEcho)
                {
                    option = echoSyncOption;
                }

                if (!SyncBuffering)
                {
                    OpenRelayClient.SyncStream((byte)0, EncodeSetsToByteArray(encodeSets.ToArray()), CurrentObjectId, option);
                }
                else if (encodeSetBufferCounter > 4)
                {
                    encodeSetsBuffer.AddRange(encodeSets);
                    OpenRelayClient.SyncStream((byte)0, EncodeSetsToByteArray(encodeSetsBuffer.ToArray()), CurrentObjectId, option);
                    encodeSetsBuffer.Clear();
                    encodeSetBufferCounter = 0;
                }
                else
                {
                    encodeSetsBuffer.AddRange(encodeSets);
                    encodeSetBufferCounter++;
                }

                //amount logics.
                amountFrameBufferSize = data.Length - position;
                if (amountFrameBufferSize >= 0)
                {
                    Array.Copy(data, position, amountFrameBuffer, 0, amountFrameBufferSize);
                }
            }
            else
            {
                var dataraw = new byte[inData.Length * 4];
                Buffer.BlockCopy(inData, 0, dataraw, 0, dataraw.Length);
                var option = OpenRelayClient.BroadcastFilter.Default;
                if (DebugEcho)
                {
                    option = echoSyncOption;
                }
                OpenRelayClient.SyncStream((byte)0, dataraw, CurrentObjectId, option);
            }
        }

        byte[] EncodeSetsToByteArray(EncodeSet[] encodeSets)
        {
            var lengthSize = encodeSets.Length * 2;
            var encodedLength = 0;
            foreach (var e in encodeSets) { encodedLength += e.Length; }

            var messageBytes = new byte[encodedLength + lengthSize];
            var stream = new MemoryStream(messageBytes);
            var message = new BinaryWriter(stream);
            try
            {
                foreach (var e in encodeSets)
                {
                    message.Write((UInt16)e.Length);
                    message.Write(e.Encoded,0,e.Length);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("error: " + e.Message + e.StackTrace);
            }
            return messageBytes;
        }
    }
}