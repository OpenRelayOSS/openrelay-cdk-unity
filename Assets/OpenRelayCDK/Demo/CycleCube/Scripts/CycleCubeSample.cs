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
using System.Collections.Generic;
using System.Threading;
using Com.FurtherSystems.OpenRelay;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityOpus;
using Debug = UnityEngine.Debug;
using System.Linq;

namespace Com.FurtherSystems.OpenRelaySample
{

    enum EventType
    {
        Heatbeat = 0,
        TransformContent = 1,
    }

    public class CycleCubeSample : OpenRelay.Or.MonoBehaviour
    {
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
        private Status currentStatus = Status.Disconnected;

        [SerializeField]
        private TMP_Text StatusText;

        [SerializeField]
        private Button ConnectOrDisconnectButton;
        [SerializeField]
        private TMP_Text ConnectOrDisconnectText;
        [SerializeField]
        private TMP_InputField ConnectOrDisconnectInputField;

        [SerializeField]
        private Button JoinOrLeaveButton;
        [SerializeField]
        private TMP_Text JoinOrLeaveText;
        [SerializeField]
        private TMP_InputField JoinOrLeaveInputField;

        [SerializeField]
        private Button SetNameButton;
        [SerializeField]
        private TMP_InputField SetNameInputField;

        [SerializeField]
        private TMP_Text ErrormsgText;

        [SerializeField]
        private Button VoiceButton;
        [SerializeField]
        private TMP_Text VoiceText;

        [SerializeField]
        private Button AutoRollButton;
        [SerializeField]
        private TMP_Text AutoRollText;

        [SerializeField]
        private Transform CubeCradle1;
        [SerializeField]
        private Transform CubeCradle2;
        [SerializeField]
        private Transform CubeCradle3;
        [SerializeField]
        private Transform CubeCradle4;
        [SerializeField]
        private Transform CubeCradle5;

        private Dictionary<UInt16, int> availables = new Dictionary<UInt16, int>();
        private ConcurrentDictionary<string, GameObject> cubeList;
        // Use this for initialization
        private UInt16 currentObjectId;

        private bool autoRolling = false;
        private bool useVoice = false;
        private float rayDistance = 20f;

        void Start()
        {
            Initialize();
        }

        void Update()
        {
            if (Input.touches.Length > 0 && Input.touches[0].phase == TouchPhase.Began) TouchBegan(Input.touches[0].position);

            if (Input.GetMouseButtonDown(0)) TouchBegan(Input.mousePosition);

            if (Input.touches.Length > 0 && Input.touches[0].phase == TouchPhase.Ended) TouchEnd(Input.touches[0].position);

            if (Input.GetMouseButtonUp(0)) TouchEnd(Input.mousePosition);
        }

        private void TouchBegan(Vector3 position)
        {
            var ray = Camera.main.ScreenPointToRay(position);
            Debug.DrawRay(ray.origin, ray.direction, Color.green);
            Debug.Log("origin:"+ray.origin);
            Debug.Log("direction:" + ray.direction);
            var hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, rayDistance))
            {
                hit.collider.gameObject.GetComponent<Cube>()?.TapHold(ref hit);
            }
        }

        private void TouchEnd(Vector3 position)
        {
            var ray = Camera.main.ScreenPointToRay(position);
            var hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, rayDistance))
            {
                hit.collider.gameObject.GetComponent<Cube>()?.TapRelease(ref hit);
            }
        }

        private void Initialize()
        {
            availables.Clear();
            availables.Add(1, -1);
            availables.Add(2, -1);
            availables.Add(3, -1);
            availables.Add(4, -1);
            availables.Add(5, -1);
            cubeList = new ConcurrentDictionary<string, GameObject>();
            Transit(Status.Disconnected);
        }

        public void ConnectOrDisconnect()
        {
            switch (currentStatus)
            {
                case Status.Disconnected:
                    var dest = ConnectOrDisconnectInputField.text.Split(':');
                    //if dest not valid ... error
                    Transit(Status.Connecting);
                    Connect(string.Empty, dest[0], dest[1]);
                    break;
                case Status.Connected:
                    Transit(Status.Disconnecting);
                    Disconnect();
                    break;
                case Status.Joined:
                    Transit(Status.Disconnecting);
                    Leave();
                    Disconnect();
                    break;
                case Status.Connecting:
                case Status.Disconnecting:
                case Status.Joining:
                case Status.Leaving:
                case Status.NameSetting:
                default:
                    // no action
                    break;
            }
        }

        public void JoinOrLeave()
        {
            switch (currentStatus)
            {
                case Status.Connected:
                    var roomName = JoinOrLeaveInputField.text;
                    Transit(Status.Joining);
                    Join(roomName);
                    break;
                case Status.Joined:
                    Transit(Status.Leaving);
                    Leave();
                    break;
                case Status.Disconnected:
                case Status.Connecting:
                case Status.Disconnecting:
                case Status.Joining:
                case Status.Leaving:
                case Status.NameSetting:
                default:
                    // no action
                    break;
            }
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
            StatusText.color = "#E576BC".ToColor();

            //connect
            ConnectOrDisconnectButton.interactable = true;
            ConnectOrDisconnectInputField.interactable = true;
            ConnectOrDisconnectText.text = "Connect";
            ConnectOrDisconnectText.color = "#00B29A".ToColor();

            //join
            JoinOrLeaveButton.interactable = false;
            JoinOrLeaveInputField.interactable = false;
            JoinOrLeaveText.text = "Join";
            JoinOrLeaveText.color = "#A5A4A4".ToColor();

            //name
            SetNameButton.interactable = false;
            SetNameInputField.interactable = false;
        }

        private void TransitConnectedUI()
        {
            StatusText.color = "#00B29A".ToColor();

            //connect
            ConnectOrDisconnectButton.interactable = true;
            ConnectOrDisconnectInputField.interactable = false;
            ConnectOrDisconnectText.text = "Disconnect";
            ConnectOrDisconnectText.color = "#E576BC".ToColor();

            //join
            JoinOrLeaveButton.interactable = true;
            JoinOrLeaveInputField.interactable = true;
            JoinOrLeaveText.text = "Join";
            JoinOrLeaveText.color = "#00B29A".ToColor();

            //name
            SetNameButton.interactable = false;
            SetNameInputField.interactable = false;
        }

        private void TransitJoinedUI()
        {
            StatusText.color = "#00B29A".ToColor();
            //connect
            ConnectOrDisconnectButton.interactable = true;
            ConnectOrDisconnectInputField.interactable = false;
            ConnectOrDisconnectText.text = "Disconnect";
            ConnectOrDisconnectText.color = "#E576BC".ToColor();

            //join
            JoinOrLeaveButton.interactable = true;
            JoinOrLeaveInputField.interactable = false;
            JoinOrLeaveText.text = "Leave";
            JoinOrLeaveText.color = "#E576BC".ToColor();

            //name
            SetNameButton.interactable = true;
            SetNameInputField.interactable = true;
        }

        private void TransitCannotOperationUI()
        {
            StatusText.color = "#00B29A".ToColor();
            //connect
            ConnectOrDisconnectButton.interactable = false;
            ConnectOrDisconnectInputField.interactable = false;
            ConnectOrDisconnectText.color = "#A5A4A4".ToColor();

            //join
            JoinOrLeaveButton.interactable = false;
            JoinOrLeaveText.color = "#A5A4A4".ToColor();

            //name
            SetNameButton.interactable = false;
            SetNameInputField.interactable = false;
        }

        public override void OnLobbyStatisticsUpdate(List<RoomLimit> roomLimits) { }
        public override void OnOpenRelayPlayerPropertiesChanged(object[] playerAndUpdatedProps) { }
        public override void OnOpenRelayRoomPropertiesChanged(Hashtable changed) { }
        public override void OnRoomListUpdate(List<RoomInfo> roomList) { }

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

        private void Leave()
        {
            OpenRelayClient.LeaveRoom();
        }
        //LeaveComplete

        public void SwitchAutoRolling()
        {
            autoRolling = !autoRolling;
            if (autoRolling)
            {
                AutoRollText.text = "AutoRoll -> Off";
                AutoRollText.color = "#E576BC".ToColor();
            }
            else
            {
                AutoRollText.text = "AutoRoll -> On";
                AutoRollText.color = "#00B29A".ToColor();
            }

            if (OpenRelayClient.inRoom)
            {
                GameObject cubeGameObject;
                if (cubeList.TryGetValue(OpenRelayClient.Player.ID.ToString(), out cubeGameObject))
                {
                    cubeGameObject.GetComponent<Cube>().SwitchAutoRolling(autoRolling);
                }
            }

        }

        public void SwitchVoice()
        {
            useVoice = !useVoice;
            if (useVoice)
            {
                VoiceText.text = "Voice -> Off";
                VoiceText.color = "#E576BC".ToColor();
            }
            else
            {
                VoiceText.text = "Voice -> On";
                VoiceText.color = "#00B29A".ToColor();
            }
        }

        private Transform GetCradle(int id)
        {
            int targetId;
            Transform cradle = null;
            int result;
            availables.TryGetValue((UInt16)id, out result);
            if (result > 0)
            {
                targetId = id;
            }
            else
            {
                var available = availables.Where(e => e.Value < 0).FirstOrDefault();
                availables[available.Key] = (UInt16)id;
                targetId = available.Key;
            }

            cradle = AssignCradle(targetId);
            return cradle;
        }

        private Transform AssignCradle(int id)
        {
            Transform cradle = null;
            switch (id)
            {
                case 1:
                    cradle = CubeCradle1;
                    break;
                case 2:
                    cradle = CubeCradle2;
                    break;
                case 3:
                    cradle = CubeCradle3;
                    break;
                case 4:
                    cradle = CubeCradle4;
                    break;
                case 5:
                    cradle = CubeCradle5;
                    break;
                default:
                    break;
            }

            return cradle;
        }

        private void ReleaseCradle(int id)
        {
            var available = availables.Where(e => e.Value == (UInt16)id).FirstOrDefault();
            availables[available.Key] = -1;
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
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("OnJoinedLobby");
            Transit(Status.Connected);
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("OnJoinedRoom");
            Transit(Status.Joined);
        }

        public override void OnReadyNewPlayer()
        {
            Debug.Log("OnReadyNewPlayer");

            var prefab = (GameObject)Resources.Load("Cube");
            foreach (var p in OpenRelayClient.PlayerList.OrderBy(p => p.ID))
            {
                // プレハブからインスタンスを生成
                var transform = GetCradle(p.ID);
                var c = UnityEngine.Object.Instantiate(prefab, transform.position, Quaternion.identity);
                var cube = c.GetComponent<Cube>();
                cubeList.TryAdd(p.ID.ToString(), c);
                if (p.ID == OpenRelayClient.Player.ID)
                {
                    currentObjectId = OpenRelayClient.AllocateObjectId();
                    cube.Initialize(true, (UInt16)p.ID, autoRolling);
                    cube.SwitchVoice(useVoice);
                    var micorophoneIndex = 0;
                    if (useVoice) c.GetComponent<VoiceRecorder>().StartRecorder((UInt16)p.ID, 2, micorophoneIndex);
                }
                else
                {
                    cube.Initialize(false, (UInt16)p.ID, false);
                    cube.SwitchVoice(useVoice);
                    if (useVoice) c.GetComponent<VoicePlayer>().StartPlayer((UInt16)p.ID, 2);
                }
                c.SetActive(true);
            }

            OpenRelayClient.IsMessageQueueRunning = true;
            StartCoroutine(CallSyncHeartbeat());
        }

        public override void OnOpenRelayCreateRoomFailed(short returnCode, string failedMessage)
        {
            Debug.Log("OnOpenRelayCreateRoomFailed");
            Transit(Status.Disconnected);
        }

        public override void OnOpenRelayJoinRoomFailed(short returnCode, string failedMessage)
        {
            Debug.Log("OnOpenRelayJoinRoomFailed");
            Transit(Status.Connected);
        }

        public override void OnOpenRelayPlayerConnected(UserSession player)
        {
            Debug.Log("OnOpenRelayPlayerConnected " + player.ID.ToString());
            var prefab = (GameObject)Resources.Load("Cube");
            // プレハブからインスタンスを生成
            var transform = GetCradle(player.ID);
            var cubeGameObject = UnityEngine.Object.Instantiate(prefab, transform.position, Quaternion.identity);
            cubeList.TryAdd(player.ID.ToString(), cubeGameObject);
            var cube = cubeGameObject.GetComponent<Cube>();
            cube.Initialize(false, (UInt16)player.ID, false);
            cube.SwitchVoice(useVoice);
            if (useVoice) cubeGameObject.GetComponent<VoicePlayer>().StartPlayer((UInt16)player.ID, 2);
            cubeGameObject.SetActive(true);
        }

        public override void OnOpenRelayPlayerDisconnected(UserSession player)
        {
            Debug.Log("OnOpenRelayPlayerDisconnected " + player.ID.ToString());

            GameObject cube = null;
            cubeList.TryRemove(player.ID.ToString(), out cube);
            if (cube != null)
            {
                cube.SetActive(false);
                GameObject.Destroy(cube);
            }
            ReleaseCradle(player.ID);
        }

        public override void OnLeftRoom()
        {
            Debug.Log("OnLeftRoom");
            RemoveAllPlayers();
            Transit(Status.Connected);
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
        }

        private void RemoveAllPlayers()
        {
            foreach (var cubeKey in cubeList.Keys)
            {
                var playerId = int.Parse(cubeKey);
                ReleaseCradle(playerId);
                GameObject cube = null;
                cubeList.TryRemove(cubeKey, out cube);
                if (cube != null)
                {
                    cube.SetActive(false);
                    Destroy(cube);
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
                    if (cubeList.ContainsKey(senderPlayerId.ToString()))
                    {
                        cubeList[senderPlayerId.ToString()].GetComponent<OpenRelaySample.Cube>().RecvTransform(ref content);
                    }
                    break;
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
