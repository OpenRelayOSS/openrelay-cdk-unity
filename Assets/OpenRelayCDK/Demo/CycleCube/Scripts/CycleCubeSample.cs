//------------------------------------------------------------------------------
// <copyright file="CycleCubeSample.cs" company="FurtherSystem Co.,Ltd.">
// Copyright (C) 2018 FurtherSystem Co.,Ltd. All rights reserved.
// </copyright>
// <author>FurtherSystem Co.,Ltd.</author>
// <email>info@furthersystem.com</email>
// <summary>
// OpenRelay Client Scripts.
// </summary>
//------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using Com.FurtherSystems.OpenRelay;
using UnityEngine;
using UnityOpus;
using Debug = UnityEngine.Debug;

namespace Com.FurtherSystems.OpenRelaySample
{
    public class CycleCubeSample : OpenRelay.Or.MonoBehaviour
    {
        private ConcurrentDictionary<string, GameObject> cubeList;
        // Use this for initialization
        private bool lobbyJoined = false;
        private bool roomCreated = false;
        private bool roomJoined = false;
        private UInt16 currentObjectId;

        void Start()
        {
            cubeList = new ConcurrentDictionary<string, GameObject>();
            OpenRelayClient.Connect();
            OpenRelayClient.OnSyncCall += OnRaiseEvent;
            StartCoroutine(LobbySequence());
        }

        IEnumerator LobbySequence()
        {
            OpenRelayClient.JoinEntry();
            while (true)
            {
                if (lobbyJoined) { break; }
                yield return new WaitForSeconds(0.5f);
            }
            var option = new OpenRelay.RoomOptions();
            OpenRelayClient.CreateOrJoinRoom("CycleCube", option, (byte)5);
            while (true)
            {
                if (roomCreated) { break; }
                yield return new WaitForSeconds(0.5f);
            }
            yield return null;
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("OnConnectedToMaster");
        }

        public override void OnConnectedToOpenRelay()
        {
            Debug.Log("OnConnectedToOpenRelay");
        }

        public override void OnConnectionFail(string disconnectMessage)
        {
            Debug.Log("OnConnectionFail " + disconnectMessage);
        }

        public override void OnCreatedRoom()
        {
            Debug.Log("OnCreatedRoom");
            roomCreated = true;
        }

        public override void OnDisconnected(string disconnectMessage)
        {
            Debug.Log("OnDisconnected "+ disconnectMessage);
        }

        //public override void OnFailedToConnect(string disconnectMessage)
        //{
        //    Debug.Log("OnFailedToConnect " + disconnectMessage);
        //}

        public override void OnJoinedLobby()
        {
            Debug.Log("OnJoinedLobby");
            lobbyJoined = true;
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("OnJoinedRoom");
            roomJoined = true;
        }

        public override void OnReadyNewPlayer()
        {
            Debug.Log("OnReadyNewPlayer");

            var prefab = (GameObject)Resources.Load("Cube");
            foreach (var p in OpenRelayClient.PlayerList)
            {
                // プレハブからインスタンスを生成
                var c = UnityEngine.Object.Instantiate(prefab, new Vector3(-7f + p.ID, 0f, 1f + p.ID), Quaternion.identity);
                if (p.ID == OpenRelayClient.Player.ID)
                {
                    currentObjectId = OpenRelayClient.AllocateObjectId();
                    c.GetComponent<VoiceRecorder>().StartRecorder((UInt16)p.ID, 1);
                }
                else
                {
                    c.GetComponent<VoicePlayer>().StartPlayer((UInt16)p.ID, 1);
                }
                cubeList.TryAdd(p.ID.ToString(), c);
                c.SetActive(true);
            }

            OpenRelayClient.IsMessageQueueRunning = true;
            StartCoroutine(CallRaiseEvent());
        }

        public override void OnLeftLobby()
        {
            Debug.Log("OnLeftLobby");
        }

        public override void OnLeftRoom()
        {
            Debug.Log("OnLeftRoom");
        }

        public override void OnOpenRelayCreateRoomFailed(short returnCode, string failedMessage)
        {
            Debug.Log("OnOpenRelayCreateRoomFailed");
        }

        public override void OnOpenRelayJoinRoomFailed(short returnCode, string failedMessage)
        {
            Debug.Log("OnOpenRelayJoinRoomFailed");
        }

        public override void OnOpenRelayPlayerConnected(UserSession player)
        {
            Debug.Log("OnOpenRelayPlayerConnected " + player.ID.ToString());
            var prefab = (GameObject)Resources.Load("Cube");
            // プレハブからインスタンスを生成
            var cube = UnityEngine.Object.Instantiate(prefab, new Vector3(-7f + player.ID, 0f, 1f + player.ID), Quaternion.identity);
            cube.GetComponent<VoicePlayer>().StartPlayer((UInt16)player.ID, 1);
            cubeList.TryAdd(player.ID.ToString(), cube);
            cube.SetActive(true);
        }

        public override void OnOpenRelayPlayerDisconnected(UserSession player)
        {
            Debug.Log("OnOpenRelayPlayerDisconnected " + player.ID.ToString());
            GameObject cube;
            cubeList.TryRemove(player.ID.ToString(),out cube);
            cube.SetActive(false);
            GameObject.Destroy(cube);
        }

        IEnumerator CallRaiseEvent()
        {
            while (true)
            {
                if (OpenRelayClient.inRoom)
                {
                    OpenRelayClient.Sync(0, System.Text.Encoding.ASCII.GetBytes("2"), currentObjectId, OpenRelayClient.BroadcastFilter.Default);
                }
                yield return new WaitForSeconds(0.05f);
            }
        }

        enum EventType
        {
            Content=0,
        }

        private void OnRaiseEvent(byte eventcode, byte[] content, UInt16 senderPlayerId, UInt16 senderObjectId)
        {
            switch ((EventType)eventcode)
            {
                case EventType.Content:
                    if (cubeList.ContainsKey(senderPlayerId.ToString()))
                    {
                        var valve = int.Parse(System.Text.Encoding.ASCII.GetString(content));
                        cubeList[senderPlayerId.ToString()].GetComponent<OpenRelaySample.Cube>().SetRotate(valve);
                    }

                    break;
                default:
                    break;
            }
            //switch (eventType)
            //{
            //    case EventType.Login:
            //        var guid = System.Text.Encoding.ASCII.GetString(content).Split('/');
            //        prefab = (GameObject)Resources.Load("Cube");
            //        // プレハブからインスタンスを生成
            //        cube = UnityEngine.Object.Instantiate(prefab, new Vector3(-7f + senderid, 0f, 1f + senderid), Quaternion.identity);
            //        cubeList.TryAdd(senderid.ToString(), cube);
            //        cube.SetActive(true);

            //        var uids = guid[1].Split(':');
            //        foreach (var u in uids)
            //        {
            //            Debug.Log("id:" + u);
            //            if (!u.Equals(string.Empty) && int.Parse(u) != senderid)
            //            {
            //                // プレハブからインスタンスを生成
            //                var c = UnityEngine.Object.Instantiate(prefab, new Vector3(-7f + int.Parse(u), 0f, 1f + int.Parse(u)), Quaternion.identity);
            //                cubeList.TryAdd(u, c);
            //                c.SetActive(true);
            //            }
            //        }
            //        break;
            //    case EventType.Logout:
            //        // logout
            //        if (cubeList.ContainsKey(senderid.ToString()))
            //        {
            //            // ignore
            //            cube = cubeList[senderid.ToString()];
            //            cube.SetActive(false);
            //            cubeList.TryRemove(senderid.ToString(), out cube);
            //        }
            //        break;
            //    case EventType.Content:
            //        // content relay
            //        if (cubeList.ContainsKey(senderid.ToString()))
            //        {
            //            var valve = int.Parse(System.Text.Encoding.ASCII.GetString(content));
            //            cubeList[senderid.ToString()].GetComponent<OpenRelaySample.Cube>().SetRotate(valve);
            //        }
            //        break;
            //    case EventType.Join:
            //        // other user instanciate
            //        prefab = (GameObject)Resources.Load("Cube");
            //        // プレハブからインスタンスを生成
            //        cube = UnityEngine.Object.Instantiate(prefab, new Vector3(-7f + senderid, 0f, 1f + senderid), Quaternion.identity);
            //        cubeList.TryAdd(senderid.ToString(), cube);
            //        cube.SetActive(true);
            //        break;
            //    default:
            //        break;
            //}
        }
    }
}