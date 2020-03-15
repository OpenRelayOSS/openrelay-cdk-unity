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
        private static readonly string unitypackage = ".unitypackage";
        private static readonly string outputPath = "Builds";
        private static readonly string outputDll = "OpenRelay.dll";
        private static readonly string cdkOutputPath = "Assets/OpenRelayCDK/Cdk";
        private static readonly string dllsRootPath = "Assets/OpenRelayCDK/Plugins";
        private static readonly string sourceRootPath = "Assets/OpenRelayCDKSource";
        private static readonly string licenseName = "LICENSE";
        private static readonly string licenseDestPath = "Assets/OpenRelayCDK";

        private static readonly string space = " ";
        private static readonly string logPrefix = "<color=black>[OpenRelay CDK Build]</color>  ";

        [MenuItem("OpenRelay CDK Build/ReBuild ALL", false, 10)]
        static void ReBuildALL()
        {
            if (!BuildClean()) return;
            if (!BuildDLL()) return;
            if (!BuildUnitypackage()) return;
            Debug.Log(logPrefix + "CDK ReBuild ALL Ok.");
        }

        [MenuItem("OpenRelay CDK Build/Build Unitypackage", false, 20)]
        static bool BuildUnitypackage()
        {
            // building project path
            var currentPath = Directory.GetCurrentDirectory();
            var outputDllPath = currentPath + "/" + outputPath + "/" + outputDll;
            var licenseSourcePath = licenseName;
            var licenseCopyPath = licenseDestPath + "/" + licenseName;
            var cdkOutputDllPath = cdkOutputPath +"/" + outputDll;

            File.Copy(outputDllPath, cdkOutputDllPath);
            Debug.Log(logPrefix + "CDK Build Package Copy " + outputDllPath + " > " + cdkOutputDllPath + " Ok.");
            File.Copy(licenseSourcePath, licenseCopyPath);
            Debug.Log(logPrefix + "CDK Build License Copy " + licenseSourcePath + " > " + licenseCopyPath + " Ok.");
            AssetDatabase.ExportPackage("Assets/OpenRelayCDK", outputPath + "/" + cdkPackagePrefix + cdkVersion + unitypackage, ExportPackageOptions.Recurse);
            Debug.Log(logPrefix + "CDK Build Package Exported Ok.");
            File.Delete(licenseCopyPath);
            Debug.Log(logPrefix + "CDK Build License Delete " + licenseCopyPath + " Ok.");
            File.Delete(cdkOutputDllPath);
            Debug.Log(logPrefix + "CDK Build Package Delete " + cdkOutputDllPath + " Ok.");
            return true;
        }

        [MenuItem("OpenRelay CDK Build/Build DLL", false, 30)]
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
            arguments.Append(string.Format(" -out:\"{0}/{1}\"", currentPath + "/" + outputPath, outputDll));

            if (useDLL && !Directory.Exists(dllsRootPath))
            {
                Debug.LogError(logPrefix + "DLLs root directory not found :" + dllsRootPath);
                return false;
            }

            if (!Directory.Exists(sourceRootPath))
            {
                Debug.LogError(logPrefix + "Source files root directory not found :" + sourceRootPath);
                return false;
            }

            var dllPathList = (useDLL) ? new DirectoryInfo(dllsRootPath).GetFiles("*.dll", SearchOption.TopDirectoryOnly) : null;
            if (useDLL && (dllPathList == null || dllPathList.Length == 0))
            {
                Debug.LogError(logPrefix + "DLLs not found :" + dllsRootPath);
                return false;
            }

            var sourcePathList = new DirectoryInfo(sourceRootPath).GetFiles("*.cs", SearchOption.AllDirectories);
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

        [MenuItem("OpenRelay CDK Build/Build Clean", false, 40)]
        static bool BuildClean()
        {
            // building project path
            var currentPath = Directory.GetCurrentDirectory();
            var outputDllPath = currentPath + "/" + outputPath + "/" + outputDll;
            var licenseCopiedPath = licenseDestPath + "/" + licenseName;
            var cdkOutputDllPath = cdkOutputPath + "/" + outputDll;

            File.Delete(outputDllPath);
            Debug.Log(logPrefix + "DLL Delete " + outputDllPath);
            File.Delete(licenseCopiedPath);
            Debug.Log(logPrefix + "Export License Delete " + licenseCopiedPath);
            File.Delete(cdkOutputDllPath);
            Debug.Log(logPrefix + "Export DLL Delete " + cdkOutputDllPath);
            File.Delete(outputPath + "/" + cdkPackagePrefix + cdkVersion + unitypackage);
            Debug.Log(logPrefix + "UnityPackage Delete " + outputPath + "/" + cdkPackagePrefix + cdkVersion + unitypackage);

            return true;
        }

        [MenuItem("OpenRelay CDK Build/Test EditMode", false, 100)]
        static void TestEditMode()
        {
            RunTests(TestMode.EditMode);
        }

        [MenuItem("OpenRelay CDK Build/Test PlayMode", false, 110)]
        static void TestPlayMode()
        {
            RunTests(TestMode.PlayMode);
        }

        //[MenuItem("OpenRelay CDK Build/Build Settings...")]
        //static void OpenSettingsWizard()
        //{
        //}

        private static void RunTests(TestMode testModeToRun)
        {
            var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter() { testMode = testModeToRun };
            testRunnerApi.Execute(new ExecutionSettings(filter));
        }
    }
}