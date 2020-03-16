//------------------------------------------------------------------------------
// <copyright file="DealerListener.cs" company="FurtherSystem Co.,Ltd.">
// Copyright (C) 2018 FurtherSystem Co.,Ltd. All rights reserved.
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
// </copyright>
// <author>FurtherSystem Co.,Ltd.</author>
// <email>info@furthersystem.com</email>
// <summary>
// OpenRelay Client Scripts.
// </summary>
//------------------------------------------------------------------------------
using UnityEngine;

namespace Com.FurtherSystems.OpenRelay
{
    public static partial class OpenRelayClient
    {
        private static string LogPrefix;
        private static int EnableLevel;
        private static void SetupLog(LogLevel enableLevel, Color color)
        {
            LogPrefix = "<color=" + color.ToString() + ">[OpenRelayCDK]</color>";
            EnableLevel = (int)enableLevel;
        }

        //[Conditional("UNITY_EDITOR")]
        private static void OrLog(LogLevel level, object o)
        {
            if (EnableLevel >= (int)level)
            {
                UnityEngine.Debug.Log(LogPrefix + o);
            }
        }

        private static void OrLogError(LogLevel level, object o)
        {
            if (EnableLevel >= (int)level)
            {
                UnityEngine.Debug.LogError(LogPrefix + o);
            }
        }

        private static void OrLogWarn(LogLevel level, object o)
        {
            if (EnableLevel >= (int)level)
            {
                UnityEngine.Debug.LogWarning(LogPrefix + o);
            }
        }
    }
}
