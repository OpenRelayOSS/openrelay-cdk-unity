//------------------------------------------------------------------------------
// <copyright file="FishController.cs" company="FurtherSystem Co.,Ltd.">
// Copyright (C) 2018 FurtherSystem Co.,Ltd. All rights reserved.
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
// </copyright>
// <author>FurtherSystem Co.,Ltd.</author>
// <email>info@furthersystem.com</email>
// <summary>
// OpenRelay performance sample.
// </summary>
//------------------------------------------------------------------------------
using UnityEngine;
using System;
using UnityEngine.UI;
using Com.FurtherSystems.OpenRelay;
using System.IO;

namespace Com.FurtherSystems.OpenRelayPerformanceSample
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(VoicePlayer))]
    [RequireComponent(typeof(VoiceRecorder))]
    //[RequireComponent(typeof(TransformReplicator))]
    public class FishController : MonoBehaviour
    {
        public enum ColorType
        {
            A = 0,
            B = 1,
            C = 2,
            D = 3,
            E = 4,
        }
        public enum FishInputType
        {
            Keyboard = 0,
            Mouse = 1,
        }

        enum InputStatus
        {
            Input,
            NoInputToXIdentify,
            NoInputToIdentify,
            NoInputIdentified,
        }
        [SerializeField]
        bool AutoStart = false;
        [SerializeField]
        bool AutoDummyStart = false;
        [SerializeField]
        FishInputType inputType;
        [SerializeField]
        Transform modelTransform;
        [SerializeField]
        SkinnedMeshRenderer modelRenderer;
        [SerializeField]
        Texture[] fishTextures;
        [SerializeField]
        Animation anim;
        [SerializeField]
        float swimCoefficient = 10f;
        [SerializeField]
        long swimCoolTime = 10000000;
        [SerializeField]
        float swimAccelCoefficient = 50f;
        [SerializeField]
        long swimAccelCoolTime = 12000000;
        [SerializeField]
        long angleCoefficient = 1;
        [SerializeField]
        Transform cameraMountPoint;
        [SerializeField]
        Vector3 cameraAngleOffset = new Vector3(5f, 0f, 0f);
        [SerializeField]
        NamePlate namePlate;
        [SerializeField]
        int SyncPerSec = 10;
        float second = 1f;
        float elapsedTime = 0;
        float syncTime = 0;

        long swimCoolTimeLimit;
        long swimAccelCoolTimeLimit;
        UInt16 playerId;
        UInt16 currentObjectId = 1;
        long noInputCounter = 0;
        long noXInputCounter = 0;
        long noInputLimit = 3;
        InputStatus inputStatus = InputStatus.NoInputIdentified;
        Transform cameraTransform;
        RawImage distantView;
        Material DistantViewMaterial;
        private Rigidbody rigiedbody;
        bool initialized = false;
        Quaternion recvRotation;
        Vector3 recvPosition;
        VoicePlayer player;
        VoiceRecorder recorder;

        void Awake()
        {
            rigiedbody = GetComponent<Rigidbody>();
            player = GetComponent<VoicePlayer>();
            recorder = GetComponent<VoiceRecorder>();
            owner = false;
            syncTime = second / SyncPerSec;
            elapsedTime = 0f;
        }

        public void Initialize(UInt16 pid, string name, Camera targetCamera, RawImage rawImage = null)
        {
            playerId = pid;
            CoolTimeReset();
            if (OpenRelayClient.Player.ID == playerId)
            {
                beforePosition = modelTransform.position;
                beforeRotation = modelTransform.rotation;
                //beforeScale = modelTransform.localScale;
                targetCamera.transform.SetParent(cameraMountPoint.transform);
                targetCamera.transform.localPosition = new Vector3(0f, 0f, 0f);
                cameraTransform = targetCamera.transform;
                DistantViewMaterial = rawImage.material;
                owner = true;
            }
            namePlate.Initialize(name, targetCamera.transform);
            recvUpdate = Delta.None;
            initialized = true;
        }

        public void InitializeDummy(string name, Camera targetCamera)
        {
            rigiedbody = GetComponent<Rigidbody>();
            namePlate.Initialize(name, targetCamera.transform);
        }

        public void ChangeCameraParent(Transform cameraDefaultRoot)
        {
            if (!owner) return;
            cameraTransform.rotation = Quaternion.identity;
            cameraTransform.position = new Vector3(0f, 0.5f, -7f);
            cameraTransform.SetParent(cameraDefaultRoot);
        }

        public void Dispose()
        {

        }

        public void SetColor(ColorType color)
        {
            modelRenderer.materials[0].SetTexture("_MainTex", fishTextures[(int)color]);

            if (initialized && owner)  ; // sync color here.
        }

        public void SetName(string name)
        {
            namePlate.SetName(name);

            if (initialized && owner) ; // sync color here.
        }

        public void MuteSound(bool enable)
        {
            player.enabled = enable;
        }

        public void MuteVoice(bool enable)
        {
            recorder.enabled = enable;
        }

        void Update()
        {
            if (!initialized) return;

            if (owner)
            {
                ControlUpdate();
                elapsedTime += Time.deltaTime;
                if (syncTime < elapsedTime)
                {
                    SyncTransform();
                    elapsedTime = 0f;
                }
            }
            else 
            { 
                RecvTransformUpdate();
            }
        }

        private void ControlUpdate()
        {
            short h = 0, v = 0;
            bool swim = false;
            bool accel = false;
            if (inputType == FishInputType.Keyboard)
            {
                if (Input.GetKey(KeyCode.A)) RotateY(-1);
                else if (Input.GetKey(KeyCode.D)) RotateY(1);
                else if (Input.GetKey(KeyCode.W)) RotateX(-1);
                else if (Input.GetKey(KeyCode.S)) RotateX(1);
                else if (inputStatus == InputStatus.Input)
                {
                    inputStatus = InputStatus.NoInputToIdentify;
                    noInputCounter = (DateTimeOffset.Now - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).Ticks / 10000000;
                }


                if (inputStatus != InputStatus.NoInputToIdentify && !Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
                {
                    inputStatus = InputStatus.NoInputToXIdentify;
                    noXInputCounter = (DateTimeOffset.Now - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).Ticks / 10000000;

                }

                swim = Input.GetKey(KeyCode.Space);
                accel = Input.GetKey(KeyCode.LeftShift);

                if (h != 0 || v != 0 || swim || accel)
                {
                    inputStatus = InputStatus.Input;
                    noInputCounter = 0;
                }
            }

            if (swim && !accel) Swim();
            else if (swim && accel) SwimAccel();
            else Idle();

            Vector3 modelTransformSquashY = modelTransform.forward;
            modelTransformSquashY.y = 0;

            var angle = Vector3.Angle(cameraTransform.forward, modelTransformSquashY);
            var cross = Vector3.Cross(cameraTransform.forward, modelTransformSquashY);
            if (1f < angle)
            {
                cameraTransform.RotateAround(modelTransform.position, cross, angle * Time.deltaTime);

                DistantViewMaterial.SetVector("_HorizontalBar", new Vector4(cameraTransform.localEulerAngles.y / 360f, 0f, 0f, 0f));
            }

            //x z auto water bladder
            if (inputStatus == InputStatus.NoInputToIdentify)
            {
                var check = (DateTimeOffset.Now - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).Ticks / 10000000;
                //Debug.Log("noInputLimit:" + noInputLimit + " noInputCounter:" + noInputCounter.ToString() + " check:" + check.ToString());
                if (noInputLimit + noInputCounter < check)
                {
                    RotateIdentify();
                }
            }

            if (inputStatus == InputStatus.NoInputToXIdentify)
            {
                var check = (DateTimeOffset.Now - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).Ticks / 10000000;
                //Debug.Log("noInputLimit:" + noInputLimit + " noInputCounter:" + noInputCounter.ToString() + " check:" + check.ToString());
                if (noInputLimit + noXInputCounter < check)
                {
                    RotateXIdentify();
                }
            }
            // adjust provisional fix angle bug.
            modelTransform.eulerAngles = new Vector3(modelTransform.eulerAngles.x, modelTransform.eulerAngles.y, 0f);
            cameraTransform.eulerAngles = new Vector3(cameraTransform.eulerAngles.x, cameraTransform.eulerAngles.y, 0f);
        }

        private void RotateX(float x)
        {
            if (-0.2f <= modelTransform.rotation.x && modelTransform.rotation.x <= 0.2f)
            {
                modelTransform.Rotate(x * 0.1f, 0, 0);
            }
        }

        private void RotateY(float y)
        {
            //gameObject.transform.Rotate(0, y * 0.2f, 0);
            modelTransform.Rotate(0, y * 0.2f, 0);
        }

        private void RotateIdentify()
        {
            var newAnglex = modelTransform.eulerAngles.x;
            var newAnglez = modelTransform.eulerAngles.z;

            //modelTransform.eulerAngles = new Vector3(0f, modelTransform.eulerAngles.y, 0f);
            //cameraTransform.eulerAngles = new Vector3(0f, cameraTransform.eulerAngles.y, 0f);

            if ((-0.0001f < newAnglex || newAnglex < 0.0001f)
        &&
         (-0.0001f < newAnglez || newAnglez < 0.0001f))
            { 
                noInputCounter = 0;
                inputStatus = InputStatus.NoInputIdentified;
            }
        }
        private void RotateXIdentify()
        {
            var newAnglex = modelTransform.eulerAngles.x;

            //modelTransform.eulerAngles = new Vector3(0f, modelTransform.eulerAngles.y, 0f);
            //cameraTransform.eulerAngles = new Vector3(0f, cameraTransform.eulerAngles.y, 0f);

            if (-0.0001f < newAnglex || newAnglex < 0.0001f)
            {
                noXInputCounter = 0;
                inputStatus = InputStatus.NoInputIdentified;
            }
        }
        private void Swim()
        {
            var now = DateTime.Now.Ticks;
            if (now < swimCoolTimeLimit || now < swimAccelCoolTimeLimit) return;

            CoolTimeReset();
            swimCoolTimeLimit = DateTime.Now.Ticks + swimCoolTime;
            rigiedbody.AddForce(modelTransform.forward * swimCoefficient);
            //anim.Play();
        }

        private void SwimAccel()
        {
            var now = DateTime.Now.Ticks;
            if (now < swimAccelCoolTimeLimit) return;

            CoolTimeReset();
            swimAccelCoolTimeLimit = DateTime.Now.Ticks + swimAccelCoolTime;
            rigiedbody.AddForce(modelTransform.forward * swimAccelCoefficient);
            //anim.Play();
        }

        private void Idle()
        {
            var now = DateTime.Now.Ticks;
            if (now < swimCoolTimeLimit || now < swimAccelCoolTimeLimit) return;

            //anim.Play();
        }

        private void CoolTimeReset()
        {
            var now = DateTime.Now.Ticks;
            swimCoolTimeLimit = now;
            swimAccelCoolTimeLimit = now;
            //anim.Play();
        }

        enum EventType
        {
            Heatbeat = 0,
            TransformContent = 1,
        }

        [Flags]
        public enum Delta
        {
            None = 0x000,
            Position = 0x001,
            Rotation = 0x002,
            Scale = 0x004,
        }

        byte[] alignment3 = new byte[3];
        // delta(1byte) + alignment3(3byte) + sizeof(float) * 3 + sizeof(float) * 4
        byte[] transformBytes = new byte[ 1 + 3 + sizeof(float) * 3 + sizeof(float) * 4];
        bool owner = false;
        Vector3 beforePosition;
        Quaternion beforeRotation;
        Vector3 beforeScale;
        float deltaFilter = 0.001f;
        Delta recvUpdate;

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
                    recvPosition = new Vector3(px, py, pz);
                    recvUpdate |= Delta.Position;
                    //transform.position = new Vector3(px, py, pz); // no smooth, sync immediately
                    //Debug.Log("position changed" + transform.position);
                }

                if (delta.HasFlag(Delta.Rotation))
                {
                    // 16byte
                    var qx = message.ReadSingle();
                    var qy = message.ReadSingle();
                    var qz = message.ReadSingle();
                    var qw = message.ReadSingle();
                    recvRotation = new Quaternion(qx, qy, qz, qw);
                    recvUpdate |= Delta.Rotation;
                    //modelTransform.rotation = new Quaternion(qx, qy, qz, qw); // no smooth, sync immediately
                    //Debug.Log("rotation changed" + modelTransform.rotation);
                }

                //if (delta.HasFlag(Delta.Scale))
                //{
                //    // 12byte
                //    var sx = message.ReadSingle();
                //    var sy = message.ReadSingle();
                //    var sz = message.ReadSingle();
                //    modelTransform.localScale = new Vector3(sx, sy, sz);
                //    Debug.Log("localScale changed" + modelTransform.localScale);
                //}

            }
            catch (Exception e)
            {
                Debug.LogError("error: " + e.Message);
                Debug.LogError("stacktrace: " + e.StackTrace);
            }
            message.Close();
            stream.Close();
        }

        private void RecvTransformUpdate()
        {
            if (recvUpdate.HasFlag(Delta.Rotation))
            {
                recvUpdate &= ~Delta.Rotation;
                modelTransform.rotation = recvRotation;
            }
            if (recvUpdate.HasFlag(Delta.Position))
            {
                recvUpdate &= ~Delta.Position;
                transform.position = recvPosition;
            }
        }

        public void SyncTransform()
        {
            if (!owner) return;

            var delta = Delta.None;

            if (beforePosition != transform.position) delta |= Delta.Position;
            //Debug.Log(beforePosition + ":" + modelTransform.position);
            if (beforeRotation != modelTransform.rotation) delta |= Delta.Rotation;
            //Debug.Log(beforeRotation + ":" + modelTransform.rotation);
            //if (beforeScale != modelTransform.localScale) delta |= Delta.Scale;
            //Debug.Log(beforeScale + ":" + modelTransform.localScale);

            if (delta == Delta.None) return;

            // delta(1byte) + alignment3(3byte) + sizeof(float) * 3 + sizeof(float) * 4
            //var messageBytes = new byte[ 1 + 3 + sizeof(float) * 3 + sizeof(float) * 4];
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
                    message.Write(modelTransform.rotation.x);
                    message.Write(modelTransform.rotation.y);
                    message.Write(modelTransform.rotation.z);
                    message.Write(modelTransform.rotation.w);
                }

                //if (delta.HasFlag(Delta.Scale))
                //{
                //    // 12byte
                //    message.Write(modelTransform.localScale.x);
                //    message.Write(modelTransform.localScale.y);
                //    message.Write(modelTransform.localScale.z);
                //}

                OpenRelayClient.Sync((byte)EventType.TransformContent, transformBytes, (UInt16)playerId, OpenRelayClient.BroadcastFilter.Default);

                if (delta.HasFlag(Delta.Position)) beforePosition = transform.position;
                if (delta.HasFlag(Delta.Rotation)) beforeRotation = modelTransform.rotation;
                //if (delta.HasFlag(Delta.Scale)) beforeScale = modelTransform.localScale;

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