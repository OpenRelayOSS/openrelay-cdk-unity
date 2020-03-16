//------------------------------------------------------------------------------
// <copyright file="OpenRelayCDKBuildMenu.cs" company="FurtherSystem Co.,Ltd.">
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
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.TestTools.TestRunner.Api;

namespace Com.FurtherSystems.OpenRelay.Builds
{
    public class OpenRelayCDKBuildMenu
    {
        private static readonly string cdkPackagePrefix = "openrelay-cdk-";
        private static readonly string cdkVersion = OpenRelayClient.UNITY_CDK_VERSION;
        private static readonly string outputPath = "Builds";
        private static readonly string unitypackage = ".unitypackage";
        private static readonly string outputCdkPath = "Assets/OpenRelayCDK/Cdk";
        private static readonly string outputDll = "OpenRelay.dll";
        private static readonly string dllsRootPath = "Assets/OpenRelayCDK/Plugins";
        private static readonly string sourceActiveRootPath = "Assets/OpenRelayCDKSource";
        private static readonly string sourceInactiveRootPath = "OpenRelayCDKSource";
        private static readonly string licenseName = "LICENSE";
        private static readonly string licenseDestPath = "Assets/OpenRelayCDK";

        private static readonly string space = " ";
        private static readonly string logPrefix = "<color=black>[OpenRelay CDK Build]</color>  ";
        
        [MenuItem("OpenRelay CDK Build/Build ALL", true, 10)]
        static bool BuildALLValidate()
        {
            return CheckInactiveSourceDirectory();
        }

        [MenuItem("OpenRelay CDK Build/Build ALL", false, 10)]
        static void BuildALL()
        {
            if (!BuildClean()) return;
            if (!BuildDLL()) return;
            if (!BuildUnitypackage()) return;
            Debug.Log(logPrefix + "CDK Build ALL Ok.");
        }

        [MenuItem("OpenRelay CDK Build/Build Clean", true, 20)]
        static bool BuildCleanValidate()
        {
            return CheckInactiveSourceDirectory();
        }

        [MenuItem("OpenRelay CDK Build/Build Clean", false, 20)]
        static bool BuildClean()
        {
            // building project path
            var currentPath = Directory.GetCurrentDirectory();
            var createDir = currentPath + "/" + outputPath;
            var outputDllPath = currentPath + "/" + outputPath + "/" + outputDll;
            var licenseCopiedPath = licenseDestPath + "/" + licenseName;
            var outputPackagepath = currentPath + "/" + outputPath + "/" + cdkPackagePrefix + cdkVersion + unitypackage;

            Directory.CreateDirectory(createDir);
            Debug.Log(logPrefix + "Create Directory " + createDir);

            File.Delete(outputDllPath);
            Debug.Log(logPrefix + "DLL Delete " + outputDllPath);

            File.Delete(licenseCopiedPath);
            Debug.Log(logPrefix + "Export License Delete " + licenseCopiedPath);

            File.Delete(outputPackagepath);
            Debug.Log(logPrefix + "UnityPackage Delete " + outputPackagepath);

            return true;
        }

        [MenuItem("OpenRelay CDK Build/Switch Build Mode", true, 30)]
        static bool SwitchBuildPackageModeValidate()
        {
            return CheckActiveSourceDirectory();
        }

        [MenuItem("OpenRelay CDK Build/Switch Build Mode", false, 30)]
        static void SwitchBuildPackageMode()
        {
            ToInactiveSourceDirectory();
            Debug.Log(logPrefix + "Switch Build Package Mode Ok.");
        }

        [MenuItem("OpenRelay CDK Build/Switch Source Develop Mode", true, 30)]
        static bool SwitchBuildDLLModeValidate()
        {
            return !CheckActiveSourceDirectory();
        }

        [MenuItem("OpenRelay CDK Build/Switch Source Develop Mode", false, 30)]
        static void SwitchBuildDLLMode()
        {
            ToActiveSourceDirectory();
            Debug.Log(logPrefix + "Switch Build DLL Mode Ok.");
        }

        [MenuItem("OpenRelay CDK Build/Build Unitypackage", true, 100)]
        static bool BuildUnitypackageValidate()
        {
            return !CheckActiveSourceDirectory();
        }

        [MenuItem("OpenRelay CDK Build/Build Unitypackage", false, 100)]
        static bool BuildUnitypackage()
        {
            // building project path
            var currentPath = Directory.GetCurrentDirectory();
            var outputPackagePath = currentPath + "/" + outputPath + "/" + cdkPackagePrefix + cdkVersion + unitypackage;
            var licenseSourcePath = licenseName;
            var licenseCopyPath = licenseDestPath + "/" + licenseName;

            Directory.CreateDirectory(currentPath + "/" + outputPath);
            Debug.Log(logPrefix + "Create Directory " + currentPath + "/" + outputPath);
            File.Copy(licenseSourcePath, licenseCopyPath);
            Debug.Log(logPrefix + "CDK Build License Copy " + licenseSourcePath + " > " + licenseCopyPath + " Ok.");
            AssetDatabase.ExportPackage("Assets/OpenRelayCDK", outputPackagePath, ExportPackageOptions.Recurse);
            Debug.Log(logPrefix + "CDK Build Package Exported Ok.");
            File.Delete(licenseCopyPath);
            Debug.Log(logPrefix + "CDK Build License Delete " + licenseCopyPath + " Ok.");
            return true;
        }

        [MenuItem("OpenRelay CDK Build/Build DLL", true, 200)]
        static bool BuildDLLValidate()
        {
            return !CheckActiveSourceDirectory();
        }

        [MenuItem("OpenRelay CDK Build/Build DLL", false, 200)]
        static bool BuildDLL()
        {
            // building project path
            var currentPath = Directory.GetCurrentDirectory();

            // building common path
            var execPath = Environment.GetCommandLineArgs()[0];
#if UNITY_EDITOR_WIN
            var basePath = Path.GetDirectoryName(execPath) + "/Data/";
#else
            var basePath = Path.GetDirectoryName(execPath) + "/../";
#endif
            // building unity paths
            var unityEnginePath = basePath + "Managed/UnityEngine.dll";
            var unityEditorPath = basePath + "Managed/UnityEditor.dll";
            var unityDLLPaths = new string[] { unityEnginePath, unityEditorPath };

            // building smcs path
#if UNITY_EDITOR_WIN
            var smcsPath = basePath + "MonoBleedingEdge/bin/mcs.bat";
#else
            var smcsPath = basePath + "MonoBleedingEdge/bin/mcs";
#endif

            var useDLL = !string.IsNullOrEmpty(dllsRootPath);

            // building arguments
            var arguments = new StringBuilder(" -target:library ");

            // set output path
            arguments.Append(string.Format(" -out:\"{0}/{1}\"", currentPath + "/" + outputCdkPath, outputDll));

            if (useDLL && !Directory.Exists(dllsRootPath))
            {
                Debug.LogError(logPrefix + "DLLs root directory not found :" + dllsRootPath);
                return false;
            }

            if (!Directory.Exists(sourceInactiveRootPath))
            {
                Debug.LogError(logPrefix + "Source files root directory not found :" + sourceInactiveRootPath);
                return false;
            }

            var dllPathList = (useDLL) ? new DirectoryInfo(dllsRootPath).GetFiles("*.dll", SearchOption.TopDirectoryOnly) : null;
            if (useDLL && (dllPathList == null || dllPathList.Length == 0))
            {
                Debug.LogError(logPrefix + "DLLs not found :" + dllsRootPath);
                return false;
            }

            var sourcePathList = new DirectoryInfo(sourceInactiveRootPath).GetFiles("*.cs", SearchOption.AllDirectories);
            if (sourcePathList == null || sourcePathList.Length == 0)
            {
                Debug.LogError(logPrefix + "Source Files not found :" + sourcePathList);
                return false;
            }

            try
            {
                // set unity dll paths
                var unityDLLPathsFormatList = unityDLLPaths.Select(x => string.Format("-r:\"{0}\" ", x));
                var unityDLLArguments = string.Join(space, unityDLLPathsFormatList);
                arguments.Append(space);
                arguments.Append(unityDLLArguments);

                if (useDLL)
                {
                    // set dll paths
                    var dllPathFormatList = dllPathList.Select(x => string.Format("-r:\"{0}\" ", x.FullName));
                    var dllArguments = string.Join(space, dllPathFormatList);
                    arguments.Append(space);
                    arguments.Append(dllArguments);
                }

                // set file paths
                var sourcePathFormatList = sourcePathList.Select(x => string.Format("\"{0}\"", x.FullName));
                var sourceArguments = string.Join(space, sourcePathFormatList);
                arguments.Append(space);
                arguments.Append(sourceArguments);

                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = smcsPath;
                process.StartInfo.Arguments = arguments.ToString();
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.Start();

                var stdOut = process.StandardOutput.ReadToEnd();
                var stdErr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(stdOut)) Debug.Log(logPrefix + stdOut);

                if (stdErr.Contains(": error") || stdErr.Contains("Exception:"))
                {
                    Debug.Log(logPrefix + smcsPath + space + arguments.ToString());
                    Debug.LogError(logPrefix + stdErr);
                    Debug.Log(logPrefix + "CDK Build DLL Failed.");
                    return false;
                }
                else if (stdErr.Contains(": warning"))
                {
                    Debug.LogWarning(logPrefix + stdErr);
                }

                Debug.Log(logPrefix + "CDK Build DLL Success Ok.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(logPrefix + e.Message);
                Debug.LogError(logPrefix + e.StackTrace);
                Debug.Log(logPrefix + smcsPath + space + arguments.ToString());
                Debug.Log(logPrefix + "CDK Build DLL Failed.");
                return false;
            }
        }

        [MenuItem("OpenRelay CDK Build/Test EditMode", false, 400)]
        static void TestEditMode()
        {
            RunTests(TestMode.EditMode);
        }

        [MenuItem("OpenRelay CDK Build/Test PlayMode", false, 410)]
        static void TestPlayMode()
        {
            RunTests(TestMode.PlayMode);
        }

        //[MenuItem("OpenRelay CDK Build/Build Settings...")]
        //static void OpenSettingsWizard()
        //{
        //}

        private static bool CheckActiveSourceDirectory()
        {
            return Directory.Exists(sourceActiveRootPath);
        }

        private static bool CheckInactiveSourceDirectory()
        {
            return Directory.Exists(sourceInactiveRootPath);
        }

        private static void ToActiveSourceDirectory()
        {
            var currentPath = Directory.GetCurrentDirectory();
            var outputDllPath = currentPath + "/" + outputPath + "/" + outputDll;
            var outputDllCdkPath = currentPath + "/" + outputCdkPath + "/" + outputDll;
            File.Move(outputDllCdkPath, outputDllPath);
            Directory.Move(sourceInactiveRootPath, sourceActiveRootPath);
        }

        private static void ToInactiveSourceDirectory()
        {
            var currentPath = Directory.GetCurrentDirectory();
            var outputDllPath = currentPath + "/" + outputPath + "/" + outputDll;
            var outputDllCdkPath = currentPath + "/" + outputCdkPath + "/" + outputDll;
            File.Move(outputDllPath, outputDllCdkPath);
            Directory.Move(sourceActiveRootPath, sourceInactiveRootPath);
        }

        private static void RunTests(TestMode testModeToRun)
        {
            var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter() { testMode = testModeToRun };
            testRunnerApi.Execute(new ExecutionSettings(filter));
        }
    }
}