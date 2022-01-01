//------------------------------------------------------------------------------
// <copyright file="MonoBehaviour.cs" company="FurtherSystem Co.,Ltd.">
// Copyright (C) 2018 FurtherSystem Co.,Ltd. All rights reserved.
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
// </copyright>
// <author>FurtherSystem Co.,Ltd.</author>
// <email>info@furthersystem.com</email>
// <summary>
// OpenRelay Client Scripts.
// </summary>
//------------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;

namespace Com.FurtherSystems.OpenRelay.Or
{
    public class MonoBehaviour : UnityEngine.MonoBehaviour, IOrCallbacks
    {
        protected virtual void Awake()
        {
            OpenRelayClient.RegistCallbacks(this);
        }

        protected virtual void OnDestroy()
        {
            OpenRelayClient.UnRegistCallbacks(this);
        }

        public virtual void OnConnectedToMaster()
        {
        }

        public virtual void OnConnectedToOpenRelay()
        {
        }

        public virtual void OnConnectionFail(string disconnectMessage)
        {
        }

        public virtual void OnCreatedRoom()
        {
        }

        public virtual void OnDisconnected(string disconnectMessage)
        {
        }

        //public virtual void OnFailedToConnect(string disconnectMessage)
        //{
        //}

        public virtual void OnJoinedLobby()
        {
        }

        public virtual void OnJoinedRoom()
        {
        }

        public virtual void OnReadyNewPlayer()
        {
        }

        public virtual void OnLeftLobby()
        {
        }

        public virtual void OnLeftRoom()
        {
        }

        public virtual void OnOpenRelayCreateRoomFailed(short returnCode, string failedMessage)
        {
        }

        public virtual void OnOpenRelayJoinRoomFailed(short returnCode, string failedMessage)
        {
        }

        public virtual void OnOpenRelayPlayerConnected(UserSession player)
        {
        }

        public virtual void OnOpenRelayPlayerDisconnected(UserSession player)
        {
        }

        public virtual void OnLobbyStatisticsUpdate(List<RoomLimit> roomLimits)
        {
        }

        public virtual void OnRoomListUpdate(List<RoomInfo> roomList)
        {
        }

        public virtual void OnOpenRelayRoomPropertiesChanged(Hashtable propertiesThatChanged)
        {

        }

        public virtual void OnOpenRelayRoomDistMapChanged(sbyte mode, Dictionary<string,byte[]> mapChanged)
        {

        }
        public virtual void OnOpenRelayRoomDistMapGapDetected(uint MergedRevision, uint LatestRevision)
        {

        }
        public virtual void OnOpenRelayRoomDistMapGapClosed(uint MergedRevision, uint LatestRevision)
        {

        }

        public virtual void OnOpenRelayPlayerPropertiesChanged(object[] playerAndUpdatedProps)
        {
        }
    }
}
