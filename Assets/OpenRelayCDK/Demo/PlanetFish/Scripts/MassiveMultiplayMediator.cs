//------------------------------------------------------------------------------
// <copyright file="MassiveMultiplayMediator.cs" company="FurtherSystem Co.,Ltd.">
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
using Com.FurtherSystems.OpenRelay;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Com.FurtherSystems.OpenRelayPerformanceSample
{
    public class MassiveMultiplayMediator : OpenRelay.Or.MonoBehaviour
    {
        enum EventType
        {
            Heatbeat = 0,
            TransformContent = 1,
            SetColor = 2,
        }
        enum Status
        {
            Disconnected,
            Connected,
            Joined,
            Disconnecting,
            Connecting,
            Leaving,
            Joining,
            NameSetting,
        }
        [SerializeField]
        Transform cameraDefaultRoot;
        [SerializeField]
        Camera mainCamera;
        [SerializeField]
        GameObject fishPrefab;
        [SerializeField]
        RawImage distantView;

        [SerializeField]
        TMP_InputField EndpointInputField;
        [SerializeField]
        TMP_InputField NickNameInputField;
        [SerializeField]
        TMP_Dropdown ColorDropdown;
        [SerializeField]
        Button JoinButton;
        [SerializeField]
        Button LeaveButton;

        [SerializeField]
        Image StatusBar;
        [SerializeField]
        TMP_Text StatusText;
        [SerializeField]
        TMP_Text ErrormsgText;

        Status currentStatus = Status.Disconnected;
        ConcurrentDictionary<string, FishController> Fishes;
        UInt16 currentObjectId;
        bool useVoice = false;
        float rayDistance = 20f;

        void Start()
        {
            Initialize();
        }

        void Initialize()
        {
            distantView.material.SetVector("_HorizontalBar", new Vector4(0f, 0f, 0f, 0f));
            Words.Initialize(0);
            Fishes = new ConcurrentDictionary<string, FishController>();
            Transit(Status.Disconnected);
        }

        public void SetName()
        {
            Transit(Status.NameSetting);
            //SetProperties
            //SetNameInputField.text
        }

        private void Transit(Status status)
        {
            currentStatus = status;
            switch (status)
            {
                case Status.Disconnected:
                    TransitDisconnectedUI();
                    break;
                case Status.Connected:
                    TransitConnectedUI();
                    break;
                case Status.Joined:
                    TransitJoinedUI();
                    break;
                case Status.Connecting:
                case Status.Disconnecting:
                case Status.Joining:
                case Status.Leaving:
                case Status.NameSetting:
                default:
                    TransitCannotOperationUI();
                    break;
            }
            StatusText.text = Enum.GetName(typeof(Status), status);
        }

        private void TransitDisconnectedUI()
        {
            StatusBar.color = "#E576BC".ToColor(0.3f);
            JoinButton.gameObject.SetActive(true);
            JoinButton.interactable = true;
            LeaveButton.gameObject.SetActive(false);
            LeaveButton.interactable = false;
        }

        private void TransitConnectedUI()
        {
            StatusBar.color = "#00B29A".ToColor(0.3f);
            JoinButton.gameObject.SetActive(true);
            JoinButton.interactable = false;
            LeaveButton.gameObject.SetActive(false);
            LeaveButton.interactable = false;
        }

        private void TransitJoinedUI()
        {
            StatusBar.color = "#00B29A".ToColor(0.3f);
            JoinButton.gameObject.SetActive(false);
            JoinButton.interactable = false;
            LeaveButton.gameObject.SetActive(true);
            LeaveButton.interactable = true;
        }

        private void TransitCannotOperationUI()
        {
            StatusBar.color = "#00B29A".ToColor(0.3f);
            JoinButton.gameObject.SetActive(false);
            JoinButton.interactable = false;
            LeaveButton.gameObject.SetActive(false);
            LeaveButton.interactable = false;
        }

        public override void OnLobbyStatisticsUpdate(List<RoomLimit> roomLimits) { }
        public override void OnOpenRelayPlayerPropertiesChanged(object[] playerAndUpdatedProps) { }
        public override void OnOpenRelayRoomPropertiesChanged(Hashtable changed) 
        {
            Debug.Log("properties changed");
            if (changed != null)
            {
                var keys = changed.Keys;
                foreach (string key in keys)
                {
                    if (!string.IsNullOrEmpty(key) && 0 < key.IndexOf('_'))
                    {
                        var propKeys = key.Split('_');
                        if (propKeys.Length > 1)
                        {
                            int playerId = 0;
                            if(int.TryParse(propKeys[0], out playerId) && propKeys[1].Equals("NickName"))
                            {
                                Fishes[playerId.ToString()].SetName((string)changed[key]);
                            }
                            if (int.TryParse(propKeys[0], out playerId) && propKeys[1].Equals("Color"))
                            {
                                Fishes[playerId.ToString()].SetColor((FishController.ColorType)int.Parse((string)changed[key]));
                            }
                        }
                    } 
                }
            }
        }
        public override void OnRoomListUpdate(List<RoomInfo> roomList) { }

        public void Connect()
        {
            if (!EndpointInputField.interactable) return;
            // TODO ERROR LOGIC

            var dest = EndpointInputField.text.Split(':');
            EndpointInputField.interactable = false;
            NickNameInputField.interactable = false;
            ColorDropdown.interactable = false;
            Transit(Status.Connecting);
            Connect(string.Empty, dest[0], dest[1]);
        }

        private void Connect(string version, string serverAddress, string entryPort)
        {
            OpenRelayClient.Connect(version, serverAddress, entryPort);
            OpenRelayClient.OnSyncCall += OnSync;
        }
        //ConnectComplete

        private void Entry()
        {
            OpenRelayClient.JoinEntry();
        }
        //EntryComplete

        private void Disconnect()
        {
            OpenRelayClient.Disconnect();
        }
        //DisconnectComplete

        private void Join(string name)
        {
            var option = new OpenRelay.RoomOptions();
            OpenRelayClient.CreateOrJoinRoom(name, option, (byte)5);
        }
        //JoinComplete

        public void Leave()
        {
            OpenRelayClient.LeaveRoom();
        }
        //LeaveComplete

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
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("OnJoinedLobby");
            Transit(Status.Connected);
            Transit(Status.Joining);
            var roomName = "perfroom";
            Join(roomName);
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("OnJoinedRoom");
            Transit(Status.Joined);
        }

        public override void OnReadyNewPlayer()
        {
            Debug.Log("OnReadyNewPlayer");

            foreach (var p in OpenRelayClient.PlayerList.OrderBy(p => p.ID))
            {
                // プレハブからインスタンスを生成
                var fishObject = UnityEngine.Object.Instantiate(fishPrefab, transform.position, Quaternion.identity);
                var fish = fishObject.GetComponent<FishController>();
                Fishes.TryAdd(p.ID.ToString(), fish);
                if (p.ID == OpenRelayClient.Player.ID)
                {
                    currentObjectId = OpenRelayClient.AllocateObjectId();
                    fish.Initialize((UInt16)p.ID, p.NickName, mainCamera, distantView);
                    fish.MuteVoice(useVoice);
                    var micorophoneIndex = 0;
                    if (useVoice) fishObject.GetComponent<VoiceRecorder>().StartRecorder((UInt16)p.ID, 2, micorophoneIndex);
                    fishObject.transform.position = AddFish.GetRandPositionSquashHeight();
                    fishObject.transform.rotation = Quaternion.Euler(0f, AddFish.GetRandYRotate(), 0f);
                    fish.SetName(NickNameInputField.text);
                    fish.SetColor((FishController.ColorType)ColorDropdown.value);
                    OpenRelayClient.Room.UpdateDistMap(p.ID + "_NickName", Encoding.ASCII.GetBytes(NickNameInputField.text));
                    OpenRelayClient.Room.UpdateDistMap(p.ID + "_Color", Encoding.ASCII.GetBytes(ColorDropdown.value.ToString()));
                }
                else
                {
                    fish.Initialize((UInt16)p.ID, p.NickName, mainCamera);
                    fish.MuteSound(useVoice);
                    if (useVoice) fishObject.GetComponent<VoicePlayer>().StartPlayer((UInt16)p.ID, 2);
                    var NickName = Encoding.ASCII.GetString(OpenRelayClient.Room.DistMap[p.ID + "_NickName"]);
                    if (NickName != null) fish.SetName(NickName);
                    var Color = Encoding.ASCII.GetString(OpenRelayClient.Room.DistMap[p.ID + "_Color"]);
                    if (Color != null) fish.SetColor((FishController.ColorType)int.Parse(Color));
                }
                var swim = fishObject.GetComponent<Swim>();
                swim.FishId = p.ID;
                swim.Initialize();
                fishObject.SetActive(true);
            }

            OpenRelayClient.IsMessageQueueRunning = true;
            StartCoroutine(CallSyncHeartbeat());
        }

        public override void OnOpenRelayCreateRoomFailed(short returnCode, string failedMessage)
        {
            Debug.Log("OnOpenRelayCreateRoomFailed");
            Transit(Status.Disconnected);
            EndpointInputField.interactable = true;
            NickNameInputField.interactable = true;
            ColorDropdown.interactable = true;
        }

        public override void OnOpenRelayJoinRoomFailed(short returnCode, string failedMessage)
        {
            Debug.Log("OnOpenRelayJoinRoomFailed");
            Transit(Status.Disconnected);
            EndpointInputField.interactable = true;
            NickNameInputField.interactable = true;
            ColorDropdown.interactable = true;
        }

        public override void OnOpenRelayPlayerConnected(UserSession player)
        {
            Debug.Log("OnOpenRelayPlayerConnected " + player.ID.ToString());
            var fishGameObject = UnityEngine.Object.Instantiate(fishPrefab, transform.position, Quaternion.identity);
            var fish = fishGameObject.GetComponent<FishController>();
            Fishes.TryAdd(player.ID.ToString(), fish);
            fish.Initialize((UInt16)player.ID, player.NickName, mainCamera);
            fish.MuteSound(useVoice);
            if (useVoice) fishGameObject.GetComponent<VoicePlayer>().StartPlayer((UInt16)player.ID, 2);
            var NickName = Encoding.ASCII.GetString(OpenRelayClient.Room.DistMap[player.ID + "_NickName"]);
            if (NickName != null) fish.SetName(NickName);
            var Color = Encoding.ASCII.GetString(OpenRelayClient.Room.DistMap[player.ID + "_Color"]);
            if (Color != null) fish.SetColor((FishController.ColorType)int.Parse(Color));
            var swim = fishGameObject.GetComponent<Swim>();
            swim.FishId = player.ID;
            swim.Initialize();
            fishGameObject.SetActive(true);
        }

        public override void OnOpenRelayPlayerDisconnected(UserSession player)
        {
            Debug.Log("OnOpenRelayPlayerDisconnected " + player.ID.ToString());

            FishController fish = null;
            Fishes.TryRemove(player.ID.ToString(), out fish);
            if (fish != null)
            {
                fish.gameObject.SetActive(false);
                GameObject.Destroy(fish.gameObject);
            }
        }

        public override void OnLeftRoom()
        {
            Debug.Log("OnLeftRoom");
            RemoveAllPlayers();
            Transit(Status.Connected);
            Disconnect();
        }

        public override void OnLeftLobby()
        {
            Debug.Log("OnLeftLobby");
        }

        public override void OnDisconnected(string disconnectMessage)
        {
            Debug.Log("OnDisconnected " + disconnectMessage);
            if (currentStatus == Status.Joined)
            {
                RemoveAllPlayers();
            }
            Transit(Status.Disconnected);
            EndpointInputField.interactable = true;
            NickNameInputField.interactable = true;
            ColorDropdown.interactable = true;
        }

        private void RemoveAllPlayers()
        {
            foreach (var fishKey in Fishes.Keys)
            {
                FishController fish = null;
                Fishes.TryRemove(fishKey, out fish);
                if (fish != null)
                {
                    fish.ChangeCameraParent(cameraDefaultRoot); //owner only
                    fish.gameObject.SetActive(false);
                    Destroy(fish.gameObject);
                }
            }
        }

        IEnumerator CallSyncHeartbeat()
        {
            while (true)
            {
                if (OpenRelayClient.inRoom)
                {
                    OpenRelayClient.Sync((byte)EventType.Heatbeat, BitConverter.GetBytes(0), currentObjectId, OpenRelayClient.BroadcastFilter.Default);
                }
                yield return new WaitForSeconds(2f);
            }
        }

        private void OnSync(byte eventcode, byte[] content, UInt16 senderPlayerId, UInt16 senderObjectId)
        {
            switch ((EventType)eventcode)
            {
                case EventType.TransformContent:
                    if (Fishes.ContainsKey(senderPlayerId.ToString()))
                    {
                        Fishes[senderPlayerId.ToString()].GetComponent<FishController>().RecvTransform(ref content);
                    }
                    break;
                case EventType.SetColor:
                case EventType.Heatbeat:
                default:
                    //ignore
                    break;
            }
        }
    }
    public static class StringExtension
    {
        public static Color ToColor(this string self, float alpha = 1f)
        {
            var color = default(Color);
            if (!ColorUtility.TryParseHtmlString(self, out color))
            {
                Debug.LogWarning("Unknown color code... " + self);
            }
            color.a = alpha;
            return color;
        }
    }
}