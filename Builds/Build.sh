#!/bin/sh -e

unity_path=/Applications/Unity/Unity.app/Contents/MacOS
unity_exe=${unity_path}/Unity
build_path=`dirname $0`
project_path=${build_path}/..
target_method=Com.FurtherSystems.OpenRelay.Builds.OpenRelayCDKBuildMenu.BuildALL

${unity_exe} \
 -batchmode \
 -quit \
 -logFile ${build_path}\Build.log \
 -projectPath ${project_path} \
 -executeMethod ${target_method}

exit 0
