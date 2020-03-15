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
using System;
using UnityEngine;

[Serializable]
public class OpenRelayCDKBuildSettings : ScriptableObject
{
    [SerializeField]
    public string CDKBuildVersion;
    [SerializeField]
    public string CDKBuildSpecificKeyword;
    [SerializeField]
    public string UnitypackageTargetPath;
    [SerializeField]
    public string UnitypackageOutputPath;
    [SerializeField]
    public string DLLOutputPath = "Assets/CDKBuilds/Output";
    [SerializeField]
    public string DLLOutputName = "OpenRelay.dll";
    [SerializeField]
    public string DLLsRootPath = "Assets/OpenRelay";
    [SerializeField]
    public string FIlesRootPath = "Assets/OpenRelay";
}