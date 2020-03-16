//------------------------------------------------------------------------------
// <copyright file="RoomInfo.cs" company="FurtherSystem Co.,Ltd.">
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
using System.Collections;

namespace Com.FurtherSystems.OpenRelay
{
    public class RoomInfo
    {
        public delegate void SetPropertiesCall(Hashtable table);
        public SetPropertiesCall SetProperties { get; private set; }
        public delegate void SetPropertiesListedInLobbyCall(string[] list);
        public SetPropertiesListedInLobbyCall SetPropertiesListedInLobby { get; private set; }
        public int PlayerCount { get; private set; }
        public bool IsOpen { get; set; }// TODO need private set?
        public bool IsVisible { get; set; }// TODO need private set?
        public UInt16 MaxPlayers { get; set; }// TODO need private set?
        public byte[] Id { get; private set; }
        public string Name { get; private set; }
        public byte ListenMode { get; private set; }
        public string ListenAddrV4 { get; private set; }
        public string ListenAddrV6 { get; private set; }
        public string StatefullDealPort { get; private set; }
        public string StatefullSubPort { get; private set; }
        public string StatelessDealPort { get; private set; }
        public string StatelessSubPort { get; private set; }
        public Hashtable Properties { get; private set; }
        public string[] PropertiesListedInLobby { get; private set; }
        private RoomOptions initialRoomOptions { get; set; }

        public RoomInfo(string name,
            byte[] id,
            byte   listenMode,
            string listenAddrV4,
            string listenAddrV6,
            string stfDealPort,
            string stfSubPort,
            string stlDealPort,
            string stlSubPort,
            UInt16 maxPlayers,
            RoomOptions initial)
        {
            Name = name;
            Id = id;
            ListenMode = listenMode;
            ListenAddrV4 = listenAddrV4;
            ListenAddrV6 = listenAddrV6;
            StatefullDealPort = stfDealPort;
            StatefullSubPort = stfSubPort;
            StatelessDealPort = stlDealPort;
            StatelessSubPort = stlSubPort;
            Properties = new Hashtable();
            SetProperties = delegate { };
            SetPropertiesListedInLobby = delegate { };
            MaxPlayers = maxPlayers;
            IsOpen = initial.IsOpen;
            IsVisible = initial.IsVisible;
            initialRoomOptions = initial;
            PlayerCount = 0;
        }
        
        public RoomInfo(SetPropertiesCall propCaller,
            SetPropertiesListedInLobbyCall listCaller,
            RoomInfo original)
        { 
            CopyFromOriginal(original);
            SetProperties = propCaller;
            SetPropertiesListedInLobby = listCaller;
        }

        public RoomInfo(UInt16 playerCount, RoomInfo original)
        {
            CopyFromOriginal(original);
            PlayerCount = playerCount;
        }

        private void CopyFromOriginal(RoomInfo original)
        {
            Name = original.Name;
            IsOpen = original.IsOpen;
            IsVisible = original.IsVisible;
            MaxPlayers = original.MaxPlayers;
            PlayerCount = original.PlayerCount;
            Id = original.Id;
            ListenAddrV4 = original.ListenAddrV4;
            StatefullDealPort = original.StatefullDealPort;
            StatefullSubPort = original.StatefullSubPort;
            StatelessDealPort = original.StatelessDealPort;
            StatelessSubPort = original.StatelessSubPort;
            SetProperties = original?.SetProperties ?? delegate { };
            SetPropertiesListedInLobby = original?.SetPropertiesListedInLobby ?? delegate { };
            Properties = original.Properties;
            PropertiesListedInLobby = original.PropertiesListedInLobby;
            initialRoomOptions = original.initialRoomOptions;
        }

        public void InitializeProperties()
        {
            if (initialRoomOptions?.CustomRoomProperties?.Count > 0)
            {
                SetProperties(initialRoomOptions.CustomRoomProperties);
                SetPropertiesListedInLobby(initialRoomOptions.CustomRoomPropertiesForLobby);
            }
        }
    }

    public class RoomOptions
    {
        public bool IsVisible { get { return this.isVisibleField; } set { this.isVisibleField = value; } }
        private bool isVisibleField = true;

        public bool IsOpen { get { return this.isOpenField; } set { this.isOpenField = value; } }
        private bool isOpenField = true;
        public byte MaxPlayers;
        public int PlayerTtl;
        public int EmptyRoomTtl;

        public Hashtable CustomRoomProperties = new Hashtable();
        public string[] CustomRoomPropertiesForLobby = new string[] { };
        public string[] Plugins = new string[] { };
        public bool SuppressRoomEvents { get { return this.suppressRoomEventsField; } }
        private bool suppressRoomEventsField = false;
        public bool PublishPlayerId { get { return this.publishPlayerIdField; } set { this.publishPlayerIdField = value; } }
        private bool publishPlayerIdField = false;
        public bool DeleteNullProperties { get { return this.deleteNullPropertiesField; } set { this.deleteNullPropertiesField = value; } }
        private bool deleteNullPropertiesField = false;

        #region Obsoleted Naming

        [Obsolete("Use property with uppercase naming instead.")]
        public bool isVisible { get { return this.isVisibleField; } set { this.isVisibleField = value; } }
        [Obsolete("Use property with uppercase naming instead.")]
        public bool isOpen { get { return this.isOpenField; } set { this.isOpenField = value; } }
        [Obsolete("Use property with uppercase naming instead.")]
        public byte maxPlayers { get { return this.MaxPlayers; } set { this.MaxPlayers = value; } }
        [Obsolete("Use property with uppercase naming instead.")]
        public Hashtable customRoomProperties { get { return this.CustomRoomProperties; } set { this.CustomRoomProperties = value; } }
        [Obsolete("Use property with uppercase naming instead.")]
        public string[] customRoomPropertiesForLobby { get { return this.CustomRoomPropertiesForLobby; } set { this.CustomRoomPropertiesForLobby = value; } }
        [Obsolete("Use property with uppercase naming instead.")]
        public string[] plugins { get { return this.Plugins; } set { this.Plugins = value; } }
        [Obsolete("Use property with uppercase naming instead.")]
        public bool suppressRoomEvents { get { return this.suppressRoomEventsField; } }
        [Obsolete("Use property with uppercase naming instead.")]
        public bool publishPlayerId { get { return this.publishPlayerIdField; } set { this.publishPlayerIdField = value; } }

        #endregion
    }

    public class RoomLimit
    {
        public string Name;
        public int PlayerCount;
        public int RoomCount;
    }
}
