//------------------------------------------------------------------------------
// <copyright file="ResponseCode.cs" company="FurtherSystem Co.,Ltd.">
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
namespace Com.FurtherSystems.OpenRelay
{
    enum ResponseCode
    {
        OPENRELAY_RESPONSE_CODE_OK = 0,
        OPENRELAY_RESPONSE_CODE_OK_NO_ROOM,
        OPENRELAY_RESPONSE_CODE_OK_ROOM_ASSGIN_AND_CREATED,
        OPENRELAY_RESPONSE_CODE_OK_POLLING_CONTINUE,
        OPENRELAY_RESPONSE_CODE_NG,
        OPENRELAY_RESPONSE_CODE_NG_REQUEST_READ_FAILED,
        OPENRELAY_RESPONSE_CODE_NG_RESPONSE_WRITE_FAILED,
        OPENRELAY_RESPONSE_CODE_NG_ENTRY_LOGIN_FAILED,
        OPENRELAY_RESPONSE_CODE_NG_ENTRY_LOGIN_CLIENT_TIMEOUT,
        OPENRELAY_RESPONSE_CODE_NG_ENTRY_LOGIN_SERVER_TIMEOUT,
        OPENRELAY_RESPONSE_CODE_NG_GET_ROOM_INFO_NOT_FOUND,
        OPENRELAY_RESPONSE_CODE_NG_GET_ROOM_INFO_CLIENT_TIMEOUT,
        OPENRELAY_RESPONSE_CODE_NG_GET_ROOM_INFO_SERVER_TIMEOUT,
        OPENRELAY_RESPONSE_CODE_NG_CREATE_ROOM_CAPACITY_OVER,
        OPENRELAY_RESPONSE_CODE_NG_CREATE_ROOM_ASSIGN_FAILED,
        OPENRELAY_RESPONSE_CODE_NG_CREATE_ROOM_CLIENT_TIMEOUT,
        OPENRELAY_RESPONSE_CODE_NG_CREATE_ROOM_SERVER_TIMEOUT,
        OPENRELAY_RESPONSE_CODE_NG_CREATE_ROOM_ALREADY_EXISTS,
        OPENRELAY_RESPONSE_CODE_NG_JOIN_ROOM_NOT_FOUND,
        OPENRELAY_RESPONSE_CODE_NG_JOIN_ROOM_CAPACITY_OVER,
        OPENRELAY_RESPONSE_CODE_NG_JOIN_ROOM_FAILED,
        OPENRELAY_RESPONSE_CODE_NG_JOIN_ROOM_CLIENT_TIMEOUT,
        OPENRELAY_RESPONSE_CODE_NG_JOIN_ROOM_SERVER_TIMEOUT,
    }
}
