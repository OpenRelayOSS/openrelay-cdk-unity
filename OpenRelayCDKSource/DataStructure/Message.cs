//------------------------------------------------------------------------------
// <copyright file="Message.cs" company="FurtherSystem Co.,Ltd.">
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
using System.Runtime.InteropServices;

namespace Com.FurtherSystems.OpenRelay
{
    using ObjectId = UInt16;
    using PlayerId = UInt16;

    [StructLayout(LayoutKind.Explicit)]
    internal class Header
    {
        [FieldOffset(0)]
        public byte Ver;
        [FieldOffset(1)]
        public byte RelayCode;
        [FieldOffset(2)]
        public byte ContentCode;
        [FieldOffset(3)]
        public byte DestCode; // 4byte
        [FieldOffset(4)]
        public byte Mask;
        [FieldOffset(5)]
        public PlayerId SrcPid;
        [FieldOffset(7)]
        public byte _alignment0; // 4byte alignment // ISSUE 11 change to private ?
        [FieldOffset(8)]
        public ObjectId SrcOid;
        [FieldOffset(10)]
        public UInt16 DestLen; // 4byte
        [FieldOffset(12)]
        public UInt16 ContentLen;
        [FieldOffset(14)]
        public byte _alignment1; // ISSUE 11 change to private ?
        [FieldOffset(15)]
        public byte _alignment2; // ISSUE 11 change to private ?
        // Offset16 ... total size 16byte/128bit

        public Header()
        {
            Ver = Definitions.FrameVersion;
            RelayCode = (byte)0;
            ContentCode = (byte)0;
            Mask = (byte)0;
            DestCode = (byte)0;
            SrcPid = (PlayerId)0;
            _alignment0 = 0;
            SrcOid = (PlayerId)0;
            DestLen = (UInt16)0;
            ContentLen = (UInt16)0;
            _alignment1 = 0;
            _alignment2 = 0;
        }

	// ISSUE 12 should use constructor ?
        public static byte[] CreateHeader(
            byte ver,
            RelayCode msgCode,
            byte contentCode,
            byte mask,
            byte destCode,
            PlayerId srcPid,
            ObjectId srcOid,
            UInt16 destLen,
            UInt16 contentLen
        )
        {
            var header = new Header();
            header.Ver = Definitions.FrameVersion;
            header.RelayCode = (byte)msgCode;
            header.ContentCode = contentCode;
            header.Mask = mask;
            header.DestCode = destCode;
            header.SrcPid = srcPid;
            header.SrcOid = srcOid;
            header.DestLen = destLen;
            header.ContentLen = contentLen;

            var headerSize = Marshal.SizeOf<Header>(header);
            var headerBytes = new byte[headerSize];
            var gch = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
            Marshal.StructureToPtr(header, gch.AddrOfPinnedObject(), false);
            gch.Free();

            return headerBytes;
        }
    }
}
