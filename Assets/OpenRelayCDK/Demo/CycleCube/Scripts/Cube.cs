//------------------------------------------------------------------------------
// <copyright file="Cube.cs" company="FurtherSystem Co.,Ltd.">
// Copyright (C) 2018 FurtherSystem Co.,Ltd. All rights reserved.
// </copyright>
// <author>FurtherSystem Co.,Ltd.</author>
// <email>info@furthersystem.com</email>
// <summary>
// OpenRelay Client Scripts.
// </summary>
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.IO;
using Com.FurtherSystems.OpenRelay;
using UnityEngine;

namespace Com.FurtherSystems.OpenRelaySample
{
    [RequireComponent(typeof(VoicePlayer))]
    [RequireComponent(typeof(VoiceRecorder))]
    public class Cube : MonoBehaviour
    {
        public int SyncRate = 30;
        int RateCount = 0;

        VoicePlayer player;
        VoiceRecorder recorder;
        byte[] transformBytes = new byte[sizeof(float)*3 + sizeof(float)*4];
        byte[] alignment3 = new byte[3];

        Vector3 beforePosition;
        Quaternion beforeRotation;
        Vector3 beforeScale;
        float deltaFilter = 0.001f;

        bool active = false;
        bool owner = false;
        int playerId = 0;
        bool autoRolling = false;
        float total = 0f;
        int count = 0;

        void Awake()
        {
            RateCount = 0;
            beforePosition = transform.position;
            beforeRotation = transform.rotation;
            beforeScale = transform.localScale;
            player = GetComponent<VoicePlayer>();
            recorder = GetComponent<VoiceRecorder>();
        }

        void Update()
        {
            gameObject.GetComponent<Renderer>().material.color = new Color(total / count * 0.5f, total / count * 0.5f, 0.1f);
            count += 1;

            if (RateCount % SyncRate == 0)
            {
                SyncTransform();
                RateCount = 0;
            }
            else
            {
                RateCount++;
            }
        }

        void OnDestroy()
        {
            active = false;
        }

        public void Initialize(bool own, UInt16 id, bool enableAutoRolling)
        {
            active = true;
            owner = own;
            playerId = id;
            autoRolling = enableAutoRolling;
            StartCoroutine(CallAutoRolling());
        }

        public void SwitchVoice(bool enable)
        {
            player.enabled = enable;
            recorder.enabled = enable;
        }

        public void SwitchAutoRolling(bool enable)
        {
            autoRolling = enable;
        }

        private IEnumerator CallAutoRolling()
        {
            while (active)
            {
                if (OpenRelayClient.inRoom && autoRolling) SetRotate(2);
                yield return new WaitForSeconds(0.05f);
            }
        }

        public void SetRotate(int valve)
        {
            transform.Rotate(-(float)valve, -(float)valve, -(float)valve);
            total += valve;
        }

        public void TapHold(ref RaycastHit ray)
        {
            if (owner && !autoRolling) SetRotate(10);
        }

        public void TapRelease(ref RaycastHit ray)
        {
        }

        [Flags]
        public enum Delta
        {
            None = 0x000,
            Position = 0x001,
            Rotation = 0x002,
            Scale = 0x004,
        }

        public void RecvTransform(ref byte[] data)
        {
            var stream = new MemoryStream(data);
            var message = new EndiannessBinaryReader(stream);

            try
            {
                // 1byte
                var delta = (Delta)message.ReadByte();
                // alignment
                message.ReadBytes(3);

                if (delta.HasFlag(Delta.Position))
                {
                    // 12byte
                    var px = message.ReadSingle();
                    var py = message.ReadSingle();
                    var pz = message.ReadSingle();
                    transform.position = new Vector3(px, py, pz);
                    Debug.Log("position changed" + transform.position);
                }

                if (delta.HasFlag(Delta.Rotation))
                {
                    // 16byte
                    var qx = message.ReadSingle();
                    var qy = message.ReadSingle();
                    var qz = message.ReadSingle();
                    var qw = message.ReadSingle();
                    transform.rotation = new Quaternion(qx, qy, qz, qw);
                    Debug.Log("rotation changed" + transform.rotation);
                }

                if (delta.HasFlag(Delta.Scale))
                {
                    // 12byte
                    var sx = message.ReadSingle();
                    var sy = message.ReadSingle();
                    var sz = message.ReadSingle();
                    transform.localScale = new Vector3(sx, sy, sz);
                    Debug.Log("localScale changed" + transform.localScale);
                }

            }
            catch (Exception e)
            {
                Debug.LogError("error: " + e.Message);
                Debug.LogError("stacktrace: " + e.StackTrace);
            }
            message.Close();
            stream.Close();
        }

        public void SyncTransform()
        {
            if (!owner) return;

            var delta = Delta.None;

            if (beforePosition != transform.position) delta |= Delta.Position;
            //Debug.Log(beforePosition + ":" + transform.position);
            if (beforeRotation != transform.rotation) delta |= Delta.Rotation;
            //Debug.Log(beforeRotation + ":" + transform.rotation);
            if (beforeScale != transform.localScale) delta |= Delta.Scale;
            //Debug.Log(beforeScale + ":" + transform.localScale);

            if (delta == Delta.None) return;

            Debug.Log("called");

            //var messageBytes = new byte[sizeof(float) * 3 + sizeof(float) * 4];
            var stream = new MemoryStream(transformBytes);
            var message = new EndiannessBinaryWriter(stream);
            try
            {
                // 1byte
                message.Write((byte)delta);
                // alignment
                message.Write(alignment3);

                if (delta.HasFlag(Delta.Position))
                {
                    // 12byte
                    message.Write(transform.position.x);
                    message.Write(transform.position.y);
                    message.Write(transform.position.z);
                }

                if (delta.HasFlag(Delta.Rotation))
                {
                    // 16byte
                    message.Write(transform.rotation.x);
                    message.Write(transform.rotation.y);
                    message.Write(transform.rotation.z);
                    message.Write(transform.rotation.w);
                }

                if (delta.HasFlag(Delta.Scale))
                {
                    // 12byte
                    message.Write(transform.position.x);
                    message.Write(transform.position.y);
                    message.Write(transform.position.z);
                }

                OpenRelayClient.Sync((byte)EventType.TransformContent, transformBytes, (UInt16)playerId, OpenRelayClient.BroadcastFilter.Default);

                if (delta.HasFlag(Delta.Position)) beforePosition = transform.position;
                if (delta.HasFlag(Delta.Rotation)) beforeRotation = transform.rotation;
                if (delta.HasFlag(Delta.Scale)) beforeScale = transform.localScale;

            }
            catch (Exception e)
            {
                Debug.LogError("error: " + e.Message);
                Debug.LogError("stacktrace: " + e.StackTrace);
            }
            message.Close();
            stream.Close();
        }
    }
}