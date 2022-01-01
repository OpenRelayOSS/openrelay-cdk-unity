//------------------------------------------------------------------------------
// <copyright file="IOrCallbacks.cs" company="FurtherSystem Co.,Ltd.">
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

namespace Com.FurtherSystems.OpenRelay
{
    public interface IOrCallbacks
    {
        void OnConnectedToMaster();

        void OnConnectedToOpenRelay();

        //void OnConnectionFail(DisconnectCause cause);

        void OnConnectionFail(string disconnectMessage);

        void OnCreatedRoom();

        //void OnCustomAuthenticationFailed(string debugMessage);

        //void OnCustomAuthenticationResponse(Dictionary<string, object> data);

        void OnDisconnected(string disconnectMessag);

        //void OnFailedToConnectToOpenRelay(DisconnectCause cause);

        //void OnFailedToConnect(string disconnectMessage);

        void OnJoinedLobby();

        void OnJoinedRoom();

        void OnReadyNewPlayer();

        void OnLeftLobby();

        void OnLeftRoom();

        void OnLobbyStatisticsUpdate(List<RoomLimit> roomLimits);

        //void OnMasterClientSwitched(OpenRelayPlayer newMasterClient);

        //void OnOwnershipRequest(object[] viewAndPlayer);

        //void OnOwnershipTransfered(object[] viewAndPlayers);

        //void OnOpenRelayCreateRoomFailed(object[] codeAndMsg);

        void OnOpenRelayCreateRoomFailed(short returnCode, string failedMessage);

        void OnOpenRelayRoomPropertiesChanged(Hashtable changed);

        void OnOpenRelayRoomDistMapChanged(sbyte mode, Dictionary<string, byte[]> changed);

        void OnOpenRelayRoomDistMapGapDetected(uint MergedRevision, uint LatestRevision);
        void OnOpenRelayRoomDistMapGapClosed(uint MergedRevision, uint LatestRevision);

        //void OnOpenRelayInstantiate(OpenRelayMessageInfo info);

        //void OnOpenRelayJoinRoomFailed(object[] codeAndMsg);

        void OnOpenRelayJoinRoomFailed(short returnCode, string failedMessage);

        //void OnOpenRelayMaxCccuReached();

        //void OnOpenRelayPlayerActivityChanged(OpenRelayPlayer otherPlayer);

        void OnOpenRelayPlayerConnected(UserSession newPlayer);

        void OnOpenRelayPlayerDisconnected(UserSession otherPlayer);

        void OnOpenRelayPlayerPropertiesChanged(object[] playerAndUpdatedProps);

        //void OnOpenRelayRandomJoinFailed(object[] codeAndMsg);

        void OnRoomListUpdate(List<RoomInfo> roomList);

        //void OnUpdatedFriendList();

        //void OnWebRpcResponse(OperationResponse response);
    }
}
