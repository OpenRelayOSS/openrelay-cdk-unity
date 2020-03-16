//------------------------------------------------------------------------------
// <copyright file="OpenRelaySettings.cs" company="FurtherSystem Co.,Ltd.">
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
    public enum CDKMode {
        Offline,
        Standalone,
        Cloud,
    }

    public class OpenRelayCDKSettings : ScriptableObject
    {
        [SerializeField]
        public string ServerAddress = "";
        public string EntryPort = "7000";
        public CDKMode Mode = CDKMode.Standalone;
        public Color LogLabelColor = Color.blue;
        public LogLevel LogVerboseLevel = LogLevel.Verbose;
    }
}
