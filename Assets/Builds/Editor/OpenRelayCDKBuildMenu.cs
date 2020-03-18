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
        private static readonly string space = " ";
        private static readonly string logPrefix = "<color=black>[OpenRelay CDK Build]</color>  ";
        private static OpenRelayCDKBuildSettings settings = null;

        static void LoadBuildSettings()
        {
            if (settings == null)
            {
                settings = AssetDatabase.LoadAssetAtPath<OpenRelayCDKBuildSettings>("Assets/Builds/OpenRelayCDKBuildSettings.asset");
            }
        }

        [MenuItem("OpenRelay CDK Build/Build ALL", true, 10)]
        static bool BuildALLValidate()
        {
            LoadBuildSettings();
            return CheckInactiveSourceDirectory();
        }

        [MenuItem("OpenRelay CDK Build/Build ALL", false, 10)]
        static void BuildALL()
        {
            LoadBuildSettings(); // NEED. call by -batchmode.
            if (!BuildClean()) return;
            if (!BuildDLL()) return;
            if (!BuildUnitypackage()) return;
            Debug.Log(logPrefix + "CDK Build ALL Ok.");
        }

        [MenuItem("OpenRelay CDK Build/Build Clean", true, 20)]
        static bool BuildCleanValidate()
        {
            LoadBuildSettings();
            return CheckInactiveSourceDirectory();
        }

        [MenuItem("OpenRelay CDK Build/Build Clean", false, 20)]
        static bool BuildClean()
        {
            // building project path
            var currentPath = Directory.GetCurrentDirectory();
            var createDir = currentPath + "/" + settings.OutputPath;
            var outputDllPath = currentPath + "/" + settings.OutputPath + "/" + settings.OutputDll;
            var licenseCopiedPath = settings.LicenseDestPath + "/" + settings.LicenseName;
            var outputPackagepath = currentPath + "/" + settings.OutputPath + "/" + settings.CDKPackagePrefix + settings.CDKBuildVersion + settings.UnitypackageSuffix;

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
            LoadBuildSettings();
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
            LoadBuildSettings();
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
            LoadBuildSettings();
            return !CheckActiveSourceDirectory();
        }

        [MenuItem("OpenRelay CDK Build/Build Unitypackage", false, 100)]
        static bool BuildUnitypackage()
        {
            // building project path
            var currentPath = Directory.GetCurrentDirectory();
            var outputPackagePath = currentPath + "/" + settings.OutputPath + "/" + settings.CDKPackagePrefix + settings.CDKBuildVersion + settings.UnitypackageSuffix;
            var licenseSourcePath = settings.LicenseName;
            var licenseCopyPath = settings.LicenseDestPath + "/" + settings.LicenseName;

            Directory.CreateDirectory(currentPath + "/" + settings.OutputPath);
            Debug.Log(logPrefix + "Create Directory " + currentPath + "/" + settings.OutputPath);
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
            LoadBuildSettings();
            return !CheckActiveSourceDirectory();
        }

        [MenuItem("OpenRelay CDK Build/Build DLL", false, 200)]
        static bool BuildDLL()
        {
            // building project path
            var currentPath = Directory.GetCurrentDirectory();

            // building common path
            var execPath = Environment.GetCommandLineArgs()[0];
#if UNITY_EDITOR_WIN ||  UNITY_EDITOR_LINUX
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

            var useDLL = !string.IsNullOrEmpty(settings.DllsRootPath);

            // building arguments
            var arguments = new StringBuilder(" -target:library ");

            // set output path
            arguments.Append(string.Format(" -out:\"{0}/{1}\"", currentPath + "/" + settings.OutputCDKPath, settings.OutputDll));

            if (useDLL && !Directory.Exists(settings.DllsRootPath))
            {
                Debug.LogError(logPrefix + "DLLs root directory not found :" + settings.DllsRootPath);
                return false;
            }

            if (!Directory.Exists(settings.SourceInactiveRootPath))
            {
                Debug.LogError(logPrefix + "Source files root directory not found :" + settings.SourceInactiveRootPath);
                return false;
            }

            var dllPathList = (useDLL) ? new DirectoryInfo(settings.DllsRootPath).GetFiles("*.dll", SearchOption.TopDirectoryOnly) : null;
            if (useDLL && (dllPathList == null || dllPathList.Length == 0))
            {
                Debug.LogError(logPrefix + "DLLs not found :" + settings.DllsRootPath);
                return false;
            }

            var sourcePathList = new DirectoryInfo(settings.SourceInactiveRootPath).GetFiles("*.cs", SearchOption.AllDirectories);
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
            LoadBuildSettings();
            RunTests(TestMode.EditMode);
        }

        [MenuItem("OpenRelay CDK Build/Test PlayMode", false, 410)]
        static void TestPlayMode()
        {
            LoadBuildSettings();
            RunTests(TestMode.PlayMode);
        }

        //[MenuItem("OpenRelay CDK Build/Build Settings...")]
        //static void OpenSettingsWizard()
        //{
        //}

        private static bool CheckActiveSourceDirectory()
        {
            return Directory.Exists(settings.SourceActiveRootPath);
        }

        private static bool CheckInactiveSourceDirectory()
        {
            return Directory.Exists(settings.SourceInactiveRootPath);
        }

        private static void ToActiveSourceDirectory()
        {
            var currentPath = Directory.GetCurrentDirectory();
            var outputDllPath = currentPath + "/" + settings.OutputPath + "/" + settings.OutputDll;
            var outputDllCdkPath = currentPath + "/" + settings.OutputCDKPath + "/" + settings.OutputDll;
            try { File.Move(outputDllCdkPath + ".meta", outputDllPath + ".meta"); } catch (FileNotFoundException noe) { }
            try { File.Move(outputDllCdkPath, outputDllPath); } catch(FileNotFoundException noe) {  }
            try { File.Move(settings.SourceInactiveRootPath + ".meta", settings.SourceActiveRootPath + ".meta"); } catch(FileNotFoundException noe) {  }
            Directory.Move(settings.SourceInactiveRootPath, settings.SourceActiveRootPath);
        }

        private static void ToInactiveSourceDirectory()
        {
            var currentPath = Directory.GetCurrentDirectory();
            var outputDllPath = currentPath + "/" + settings.OutputPath + "/" + settings.OutputDll;
            var outputDllCdkPath = currentPath + "/" + settings.OutputCDKPath + "/" + settings.OutputDll;
            try { File.Move(outputDllPath + ".meta", outputDllCdkPath + ".meta"); } catch(FileNotFoundException noe) {  }
            try { File.Move(outputDllPath, outputDllCdkPath); } catch (FileNotFoundException noe) { }
            try { File.Move(settings.SourceActiveRootPath + ".meta", settings.SourceInactiveRootPath + ".meta"); } catch(FileNotFoundException noe) {  }
            Directory.Move(settings.SourceActiveRootPath, settings.SourceInactiveRootPath);
        }

        private static void RunTests(TestMode testModeToRun)
        {
            var testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter() { testMode = testModeToRun };
            testRunnerApi.Execute(new ExecutionSettings(filter));
        }
    }
}