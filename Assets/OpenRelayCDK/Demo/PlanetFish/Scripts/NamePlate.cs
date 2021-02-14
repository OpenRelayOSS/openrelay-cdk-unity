//------------------------------------------------------------------------------
// <copyright file="NamePlate.cs" company="FurtherSystem Co.,Ltd.">
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

namespace Com.FurtherSystems.OpenRelayPerformanceSample
{
    public class NamePlate : MonoBehaviour
    {
        [SerializeField]
        public Transform TargetCamera;
        [SerializeField]
        MeshRenderer textMeshRenderer;
        [SerializeField]
        float ActiveLength = 5f;

        bool initialized = false;

        public void Initialize()
        {

            initialized = true;
        }

        void Update()
        {
            if (!initialized) return;

            var length = Vector3.Distance(transform.position, TargetCamera.position);
            if (length < ActiveLength)
            {
                if (!textMeshRenderer.enabled) textMeshRenderer.enabled = true;
                transform.LookAt(TargetCamera);
            }
            else if (textMeshRenderer.enabled)
            {
                textMeshRenderer.enabled = false;
            }
        }
    }
}