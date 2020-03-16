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
using UnityEngine;

namespace Com.FurtherSystems.OpenRelaySample
{
    public class Cube : MonoBehaviour
    {
        float total = 0f;
        int count = 0;

        void Update()
        {
            gameObject.GetComponent<Renderer>().material.color = new Color(total / count * 0.5f, total / count * 0.5f, 0.1f);
            count += 1;
        }

        public void SetRotate(int valve)
        {
            transform.Rotate(-(float)valve, -(float)valve, -(float)valve);
            total += valve;
        }
    }
}