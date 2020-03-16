//------------------------------------------------------------------------------
// <copyright file="UserSession.cs" company="FurtherSystem Co.,Ltd.">
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
using System;

namespace Com.FurtherSystems.OpenRelay
{
    using ObjectId = UInt16;
    public class UserSession
    {
        private int _pid;
        public int ID
        {
            get { return _pid; }
        }

        private ObjectId _oid;
        public ObjectId ObjectId
        {
            get { return _oid; }
        }
        private bool _isLocal;
        public bool IsLocal
        {
            get { return _isLocal; }
        }
        private bool _isMasterClient;
        public bool IsMasterClient
        {
            get { return _isMasterClient; }
            set { _isMasterClient = value; }
        }
        private string _nickName = string.Empty;
        public string NickName
        {
            get { return _nickName; }
            set { _nickName = value; }
        }

        public UserSession(int pid, ObjectId objectId, bool isLocal, bool isMasterClient)
        {
            _pid = pid;
            _oid = objectId;
            _isLocal = isLocal;
            _isMasterClient = isMasterClient;
        }

        public void Login(int pid, bool isMasterClient)
        {
            _pid = pid;
            _isMasterClient = isMasterClient;
        }
    }
}
