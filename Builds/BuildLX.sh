#!/bin/bash
unity_version=2019.3.5f1
unity_path=/opt/Unity/Editor
unity_exe=${unity_path}/Unity
build_path=`dirname $0`
project_path=./
target_method=Com.FurtherSystems.OpenRelay.Builds.OpenRelayCDKBuildMenu.BuildALL

${unity_exe} -batchmode -quit -logFile ${build_path}/Build.log -projectPath ${project_path} -executeMethod ${target_method}
