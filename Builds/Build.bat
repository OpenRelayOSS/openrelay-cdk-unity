echo off
set unity_version=2019.3.5f1
set unity_path="C:\Program Files\Unity\Hub\Editor\%unity_version%\Editor"
set unity_exe=%unity_path%\Unity.exe
set build_path=%~dp0
set project_path=%~dp0..
set target_method=Com.FurtherSystems.OpenRelay.Builds.OpenRelayCDKBuildMenu.BuildALL
echo on

%unity_exe% ^
 -batchmode ^
 -quit ^
 -logFile %build_path%Build.log ^
 -projectPath %project_path% ^
 -executeMethod %target_method%
