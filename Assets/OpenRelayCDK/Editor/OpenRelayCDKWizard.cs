//------------------------------------------------------------------------------
// <copyright file="OpenRelayCDKWizard.cs" company="FurtherSystem Co.,Ltd.">
// Copyright (C) 2018 FurtherSystem Co.,Ltd. All rights reserved.
// </copyright>
// <author>FurtherSystem Co.,Ltd.</author>
// <email>info@furthersystem.com</email>
// <summary>
// OpenRelay Client Scripts.
// </summary>
//------------------------------------------------------------------------------
using UnityEditor;

namespace Com.FurtherSystems.OpenRelay
{
    public class OpenRelayCDKWizard : ScriptableWizard
    {
        [MenuItem("OpenRelay CDK/Display Setup Wizard")]
        static void CreateWizard()
        {
            //　ウィザードを表示
            //ScriptableWizard.DisplayWizard<OpenRelayWizard>("自作ウィザード", "実行", "データの初期化");
        }

        //　ウィザードの作成ボタンを押した時に実行
        void OnWizardCreate()
        {
            //　ゲームオブジェクトを選択している
            //if (Selection.activeTransform != null)
            //{
            //    OpenRelayWizard data = Selection.activeTransform.GetComponent<OpenRelayWizard>();
            //    if (data != null)
            //    {
            //        data.intValue = intValue;
            //        data.floatValue = floatValue;
            //        data.color = color;
            //        Debug.Log("データを書き換えました");
            //    }
            //    else
            //    {
            //        Debug.Log("ScriptableWizardDateが設定されていません。");
            //    }
            //}
        }
    }
}