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
        MeshRenderer textMeshRenderer;
        [SerializeField]
        float ActiveLength = 5f;
        [SerializeField]
        TextMesh NameTextMesh;

        float xPosOffset = 0.2f;
        float yPosOffset = 0.5f;
        bool initialized = false;
        Transform TargetCamera;

        public void Initialize(string name, Transform targetCamera)
        {
            NameTextMesh.text = name;
            TargetCamera = targetCamera;
            initialized = true;
        }

        public void SetName(string name)
        {
            NameTextMesh.text = name;
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