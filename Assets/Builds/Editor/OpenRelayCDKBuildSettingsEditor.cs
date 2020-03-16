//------------------------------------------------------------------------------ // <copyright file="OpenRelayCDKBuildSettingsEditor.cs" company="FurtherSystem Co.,Ltd."> // Copyright (C) 2018 FurtherSystem Co.,Ltd. All rights reserved. // </copyright> // <author>FurtherSystem Co.,Ltd.</author> // <email>info@furthersystem.com</email> // <summary> // OpenRelay Client Scripts. // </summary> //------------------------------------------------------------------------------ using UnityEngine; using UnityEditor;

namespace Com.FurtherSystems.OpenRelay.Builds
{
    [CustomEditor(typeof(OpenRelayCDKBuildSettings))]
    public class OpenRelayCDKBuildSettingsEditor : Editor
    {
        OpenRelayCDKBuildSettings settings = null;

        [MenuItem("OpenRelay CDK Build/Create Build Setting", false, 500)]         public static void CreateOpenCDKRelaySettings()         {             var asset = ScriptableObject.CreateInstance<OpenRelayCDKBuildSettings>();             AssetDatabase.CreateAsset(asset, "Assets/Builds/OpenRelayCDKBuildSettings.asset");             AssetDatabase.Refresh();         } 
        void OnEnable()
        {
            settings = target as OpenRelayCDKBuildSettings;
        }
    }
}