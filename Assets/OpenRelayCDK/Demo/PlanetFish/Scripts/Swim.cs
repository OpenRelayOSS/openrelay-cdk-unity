//------------------------------------------------------------------------------
// <copyright file="Swim.cs" company="FurtherSystem Co.,Ltd.">
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
using System;
using UnityEngine;

namespace Com.FurtherSystems.OpenRelayPerformanceSample
{
    public class Swim : MonoBehaviour
    {
        [SerializeField]
        bool AutoStart = false;
        [SerializeField]
        private Animation anim;
        [SerializeField]
        public int FishId;

        private long startTime;
        private long nextTime;
        private long checkTime;

        void Start()
        {
            if (AutoStart) Initialize();
        }

        public void Initialize()
        {
            startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            UnityEngine.Random.InitState(FishId);
            nextTime = startTime + (long)UnityEngine.Random.Range(0, 1000);
            Debug.Log("initialized fish anim id: " + FishId.ToString());
        }

        void Update()
        {
            if (!anim.isPlaying)
            {
                checkTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (checkTime > nextTime)
                {
                    nextTime = checkTime + (long)UnityEngine.Random.Range(0, 2000);
                    anim.Play();
                }
            }
        }
    }
}