//------------------------------------------------------------------------------
// <copyright file="PostProcessingSwitcher.cs" company="FurtherSystem Co.,Ltd.">
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
using UnityEngine.Rendering.PostProcessing;

namespace Com.FurtherSystems.OpenRelayPerformanceSample
{
    public class PostProcessingSwitcher : MonoBehaviour
    {
        [SerializeField]
        PostProcessVolume postProcessVolume;

        public void SwitchPostProcessVolume(bool flag)
        {
            postProcessVolume.enabled = flag;
        }
    }
}