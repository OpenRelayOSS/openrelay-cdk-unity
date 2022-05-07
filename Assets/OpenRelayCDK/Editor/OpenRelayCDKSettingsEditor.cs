﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿//------------------------------------------------------------------------------
// <copyright file="OpenRelayCDKSettingsEditor.cs" company="FurtherSystem Co.,Ltd.">
// Copyright (C) 2018 FurtherSystem Co.,Ltd. All rights reserved.
// </copyright>
// <author>FurtherSystem Co.,Ltd.</author>
// <email>info@furthersystem.com</email>
// <summary>
// OpenRelay Client Scripts.
// </summary>
//------------------------------------------------------------------------------
using UnityEngine;
using UnityEditor;

namespace Com.FurtherSystems.OpenRelay
{
    [CustomEditor(typeof(OpenRelayCDKSettings))]
    public class OpenRelayCDKSettingsEditor : Editor
    {
        OpenRelayCDKSettings settings = null;

        [MenuItem("OpenRelay CDK/Create Settings")]
        public static void CreateOpenCDKRelaySettings()
        {
            var asset = ScriptableObject.CreateInstance<OpenRelayCDKSettings>();
            AssetDatabase.CreateAsset(asset, "Assets/OpenRelayCDK/Resources/OpenRelayCDKSettings.asset");
            AssetDatabase.Refresh();
        }

        void OnEnable()
        {
            settings = target as OpenRelayCDKSettings;
        }
    }
}