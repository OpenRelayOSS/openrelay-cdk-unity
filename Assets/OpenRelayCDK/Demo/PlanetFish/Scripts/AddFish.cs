//------------------------------------------------------------------------------
// <copyright file="AddFish.cs" company="FurtherSystem Co.,Ltd.">
// Copyright (C) 2018 FurtherSystem Co.,Ltd. All rights reserved.
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
// </copyright>
// <author>FurtherSystem Co.,Ltd.</author>
// <email>info@furthersystem.com</email>
// <summary>
// OpenRelay performance sample.
// </summary>
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Com.FurtherSystems.OpenRelayPerformanceSample
{
    public class AddFish : MonoBehaviour
    {
        [SerializeField]
        Camera mainCamera;
        [SerializeField]
        GameObject fishPrefab;
        [SerializeField]
        Transform ParentNode;
        [SerializeField]
        int AddIntervalMSec = 50;
        [SerializeField]
        int ClearIntervalMSec = 50;
        [SerializeField]
        bool FixedSeed;


        List<GameObject> list = new List<GameObject>();
        public int Count
        {
            get
            {
                return list.Count;
            }
        }
        bool Adding = false;
        bool Clearing = false;

        void Start()
        {
            Initialize();
        }

        void Initialize()
        {
            if (FixedSeed)
            {
                UnityEngine.Random.InitState(1);
            }
            else
            {
                UnityEngine.Random.InitState(DateTime.Now.Second);
            }
        }

        IEnumerator Instantiate()
        {
            Debug.Log("Called");
            GameObject go = null;
            var position = GetRandPosition();
            var rotate = Quaternion.Euler(0f, GetRandYRotate(), 0f);
            go = Instantiate(fishPrefab, position, rotate, ParentNode);
            var instantiateType = GetRandInstantiateType();
            Debug.Log("Called" + instantiateType);
            list.Add(go);
            var controller = go.GetComponent<FishController>();
            controller.InitializeDummy(Words.GeneratePhrase(), GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>()); ;
            controller.SetColor(instantiateType);
            var swim = go.GetComponent<Swim>();
            swim.FishId = list.Count + 5;
            swim.Initialize();
            yield break;
        }

        public void AddFish100Call()
        {
            StartCoroutine(AddFish100());
        }

        IEnumerator AddFish100()
        {
            int count = 100;
            if (Adding) yield break;
            Adding = true;
            for (int i = 0; i < count; i++)
            {
                yield return Instantiate();
                yield return new WaitForSeconds(AddIntervalMSec / 1000);
            }
            Adding = false;
            yield break;
        }

        public void ClearFishCall()
        {
            StartCoroutine(ClearFish());
        }

        IEnumerator ClearFish()
        {
            if (Clearing) yield break;
            Clearing = true;
            for (int i = 0; i < list.Count; i++)
            {
                Destroy(list[i]);
                list[i] = null;
                yield return new WaitForSeconds(ClearIntervalMSec / 1000);
            }
            list.Clear();
            Clearing = false;
            yield break;
        }

        public static FishController.ColorType GetRandInstantiateType()
        {
            return (FishController.ColorType)UnityEngine.Random.Range(0, 4);
        }

        public static Vector3 GetRandPosition()
        {
            return new Vector3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(0f, 4f), UnityEngine.Random.Range(-10f, 10f));
        }
        public static Vector3 GetRandPositionSquashHeight()
        {
            return new Vector3(UnityEngine.Random.Range(-10f, 10f), 1.5f, UnityEngine.Random.Range(-10f, 10f));
        }

        public static float GetRandYRotate()
        {
            return UnityEngine.Random.Range(0, 359);
        }
    }
}