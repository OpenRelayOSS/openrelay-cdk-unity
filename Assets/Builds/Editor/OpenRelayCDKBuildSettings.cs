//------------------------------------------------------------------------------
// <copyright file="OpenRelayCDKBuildSettings.cs" company="FurtherSystem Co.,Ltd.">
// Copyright (C) 2018 FurtherSystem Co.,Ltd. All rights reserved.
// </copyright>
// <author>FurtherSystem Co.,Ltd.</author>
// <email>info@furthersystem.com</email>
// <summary>
// OpenRelay Client Scripts.
// </summary>
//------------------------------------------------------------------------------
using Com.FurtherSystems.OpenRelay;
using System;
using UnityEngine;

[Serializable]
public class OpenRelayCDKBuildSettings : ScriptableObject
{
    [SerializeField]
    public string CDKBuildVersion = OpenRelayClient.UNITY_CDK_VERSION;
    [SerializeField]
    public string CDKPackagePrefix = "openrelay-cdk-";
    [SerializeField]
    public string UnitypackageSuffix = ".unitypackage";
    [SerializeField]
    public string OutputPath = "Builds";
    [SerializeField]
    public string OutputCDKPath = "Assets/OpenRelayCDK/Cdk";
    [SerializeField]
    public string OutputDll = "OpenRelay.dll";
    [SerializeField]
    public string DllsRootPath = "Assets/OpenRelayCDK/Plugins";
    [SerializeField]
    public string SourceActiveRootPath = "Assets/OpenRelayCDKSource";
    [SerializeField]
    public string SourceInactiveRootPath = "OpenRelayCDKSource";
    [SerializeField]
    public string LicenseName = "LICENSE";
    [SerializeField]
    public string LicenseDestPath = "Assets/OpenRelayCDK";
}