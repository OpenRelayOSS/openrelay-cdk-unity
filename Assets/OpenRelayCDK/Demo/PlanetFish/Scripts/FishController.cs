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
//using Com.FurtherSystems.OpenRelay;

namespace Com.FurtherSystems.OpenRelayPerformanceSample
{
    [RequireComponent(typeof(Rigidbody))]
    //[RequireComponent(typeof(TransformReplicator))]
    public class FishController : MonoBehaviour
    {
        public enum FishInputType
        {
            Keyboard = 0,
            Mouse = 1,
        }

        enum InputStatus
        {
            Input,
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
        Transform cameraTransform;
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
        RawImage DistantView;
        Material DistantViewMaterial;

        // TODO stamina 
        private long swimCoolTimeLimit;
        private long swimAccelCoolTimeLimit;

        private long noInputCounter = 0;
        private long noInputLimit = 3;
        InputStatus inputStatus = InputStatus.NoInputIdentified;

        private Rigidbody rigiedbody;

        bool initialized = false;

        void Start()
        {
            if (AutoStart) Initialize();
            else if (AutoDummyStart) InitializeDummy();
        }

        public void Initialize()
        {
            rigiedbody = GetComponent<Rigidbody>();

            CoolTimeReset();
            var mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            mainCamera.transform.SetParent(cameraMountPoint.transform);
            mainCamera.transform.localPosition = new Vector3(0f, 0f, 0f);
            namePlate.Initialize();

            DistantViewMaterial = DistantView.material;
            initialized = true;
        }

        public void InitializeDummy()
        {
            rigiedbody = GetComponent<Rigidbody>();

            namePlate.TargetCamera = GameObject.FindGameObjectWithTag("MainCamera").transform;
            namePlate.Initialize();
        }

        public void Instantiate()
        {
            //if (OpenRelayClient.Player.IsLocal)
            //{
            //CoolTimeReset();
            //var mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            //mainCamera.transform.SetParent(cameraMountPoint.transform);
            //}
        }

        void Update()
        {
            if (!initialized) return;
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

                RotateY(h);

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

            var angle = Vector3.Angle(cameraTransform.forward, modelTransform.forward);
            var cross = Vector3.Cross(cameraTransform.forward, modelTransform.forward);
            if (1f < angle)
            {
                cameraTransform.RotateAround(modelTransform.position, cross, angle * Time.deltaTime);

                DistantViewMaterial.SetVector("_HorizontalBar", new Vector4(cameraTransform.localEulerAngles.y / 360f, 0f, 0f, 0f));
                DistantView.material = DistantViewMaterial;
            }
            //x z auto water bladder
            if (inputStatus == InputStatus.NoInputToIdentify)
            {
                var check = (DateTimeOffset.Now - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).Ticks / 10000000;
                Debug.Log("noInputLimit:" + noInputLimit + " noInputCounter:" + noInputCounter.ToString() + " check:" + check.ToString());
                if (noInputLimit + noInputCounter < check)
                {
                    RotateIdentify();
                }
            }
            // adjust provisional fix angle bug.
            modelTransform.eulerAngles = new Vector3(modelTransform.eulerAngles.x, modelTransform.eulerAngles.y, 0f);
            cameraTransform.eulerAngles = new Vector3(cameraTransform.eulerAngles.x, cameraTransform.eulerAngles.y, 0f);
        }

        private void RotateX(float x)
        {
            //modelTransform.Rotate(x * 0.2f, 0, 0);
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
    }
}