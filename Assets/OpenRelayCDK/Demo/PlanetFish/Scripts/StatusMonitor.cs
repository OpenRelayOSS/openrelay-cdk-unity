//------------------------------------------------------------------------------
// <copyright file="StatusMonitor.cs" company="FurtherSystem Co.,Ltd.">
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
using UnityEngine.UI;

namespace Com.FurtherSystems.OpenRelayPerformanceSample
{
    public class StatusMonitor : MonoBehaviour
    {
        [SerializeField]
        Text text;
        [SerializeField]
        AddFish addFish;

        int frameCount;
        float prevTime;
        float fps;

        void Start()
        {
            frameCount = 0;
            prevTime = 0.0f;
        }

        void Update()
        {

            frameCount++;
            float time = Time.realtimeSinceStartup - prevTime;
            if (time >= 0.5f)
            {
                fps = frameCount / time;
                text.text = addFish.Count.ToString("####") + "Fishes  " + fps.ToString("F") + " FPS";

                frameCount = 0;
                prevTime = Time.realtimeSinceStartup;
            }
        }
    }
}