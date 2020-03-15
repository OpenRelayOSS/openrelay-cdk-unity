//------------------------------------------------------------------------------
// <copyright file="Endianness.cs" company="FurtherSystem Co.,Ltd.">
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
using System.IO;
using System.Text;

namespace Com.FurtherSystems.OpenRelay
{
    internal static class Endianness
    {
        public static char Reverse(char value) => (char)Reverse((ushort)value);
        public static short Reverse(short value) => (short)Reverse((ushort)value);
        public static int Reverse(int value) => (int)Reverse((uint)value);
        public static long Reverse(long value) => (long)Reverse((ulong)value);

        public static ushort Reverse(ushort value)
        {
            return (ushort)((value & 0xFF) << 8 | (value >> 8) & 0xFF);
        }

        public static uint Reverse(uint value)
        {
            return (value & 0xFF) << 24 |
                    ((value >> 8) & 0xFF) << 16 |
                    ((value >> 16) & 0xFF) << 8 |
                    ((value >> 24) & 0xFF);
        }

        public static ulong Reverse(ulong value)
        {
            return (value & 0xFF) << 56 |
                    ((value >> 8) & 0xFF) << 48 |
                    ((value >> 16) & 0xFF) << 40 |
                    ((value >> 24) & 0xFF) << 32 |
                    ((value >> 32) & 0xFF) << 24 |
                    ((value >> 40) & 0xFF) << 16 |
                    ((value >> 48) & 0xFF) << 8 |
                    ((value >> 56) & 0xFF);
        }

        public static float Reverse(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        public static double Reverse(double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        public static char[] Reverse(char[] chars)
        {
            char[] reversed = new char[chars.Length];
            for (int index = 0; index < chars.Length; index++)
            {
                reversed[index] = Reverse(chars[index]);
            }
            return reversed;
        }

        public static char[] Reverse(char[] chars, int idx, int count)
        {
            char[] reversed = new char[count];
            for (int index = 0; index < count; index++)
            {
                reversed[index] = Reverse(chars[idx + index]);
            }
            return reversed;
        }

        public static string Reverse(string str)
        {
            return new string(Reverse(str.ToCharArray()));
        }

    }

    internal class EndiannessBinaryReader : BinaryReader
    {
        public EndiannessBinaryReader(Stream stream) : base(stream)
        {
        }

        public EndiannessBinaryReader(Stream stream, Encoding encoding) : base(stream, encoding)
        {
        }

        public EndiannessBinaryReader(Stream stream, Encoding encoding, bool leaveOpen) : base(stream, encoding, leaveOpen)
        {
        }

        public override char ReadChar()
        {
            return (BitConverter.IsLittleEndian) ? base.ReadChar() : Endianness.Reverse(base.ReadChar());
        }

        public override char[] ReadChars(Int32 count)
        {
            return (BitConverter.IsLittleEndian) ? base.ReadChars(count) : Endianness.Reverse(base.ReadChars(count));
        }

        public override decimal ReadDecimal()
        {
            throw new NotSupportedException();
        }

        public override double ReadDouble()
        {
            return (BitConverter.IsLittleEndian) ? base.ReadDouble() : Endianness.Reverse(base.ReadDouble());
        }

        public override short ReadInt16()
        {
            return (BitConverter.IsLittleEndian) ? base.ReadInt16() : Endianness.Reverse(base.ReadInt16());
        }

        public override int ReadInt32()
        {
            return (BitConverter.IsLittleEndian) ? base.ReadInt32() : Endianness.Reverse(base.ReadInt32());
        }

        public override long ReadInt64()
        {
            return (BitConverter.IsLittleEndian) ? base.ReadInt64() : Endianness.Reverse(base.ReadInt64());
        }

        public override float ReadSingle()
        {
            return (BitConverter.IsLittleEndian) ? base.ReadSingle() : Endianness.Reverse(base.ReadSingle());
        }

        public override string ReadString()
        {
            return (BitConverter.IsLittleEndian) ? base.ReadString() : Endianness.Reverse(base.ReadString());
        }

        public override ushort ReadUInt16()
        {
            return (BitConverter.IsLittleEndian) ? base.ReadUInt16() : Endianness.Reverse(base.ReadUInt16());
        }

        public override uint ReadUInt32()
        {
            return (BitConverter.IsLittleEndian) ? base.ReadUInt32() : Endianness.Reverse(base.ReadUInt32());
        }

        public override ulong ReadUInt64()
        {
            return (BitConverter.IsLittleEndian) ? base.ReadUInt64() : Endianness.Reverse(base.ReadUInt64());
        }
    }

    internal class EndiannessBinaryWriter : BinaryWriter
    {
        public EndiannessBinaryWriter() : base()
        {
        }

        public EndiannessBinaryWriter(Stream stream) : base(stream)
        {
        }

        public EndiannessBinaryWriter(Stream stream, Encoding encoding) : base(stream, encoding)
        {
        }

        public EndiannessBinaryWriter(Stream stream, Encoding encoding, bool leaveOpen) : base(stream, encoding, leaveOpen)
        {
        }

        public override void Write(char ch)
        {
            if (BitConverter.IsLittleEndian) base.Write(ch); else base.Write(Endianness.Reverse(ch));
        }

        public override void Write(char[] chars)
        {
            if (BitConverter.IsLittleEndian) base.Write(chars); else base.Write(Endianness.Reverse(chars));
        }

        public override void Write(char[] chars, int index, int count)
        {
            if (BitConverter.IsLittleEndian) base.Write(chars, index, count); else base.Write(Endianness.Reverse(chars, index, count));
        }

        public override void Write(decimal value)
        {
            throw new NotSupportedException();
        }

        public override void Write(double value)
        {
            if (BitConverter.IsLittleEndian) base.Write(value); else base.Write(Endianness.Reverse(value));
        }

        public override void Write(short value)
        {
            if (BitConverter.IsLittleEndian) base.Write(value); else base.Write(Endianness.Reverse(value));
        }

        public override void Write(int value)
        {
            if (BitConverter.IsLittleEndian) base.Write(value); else base.Write(Endianness.Reverse(value));
        }

        public override void Write(long value)
        {
            if (BitConverter.IsLittleEndian) base.Write(value); else base.Write(Endianness.Reverse(value));
        }

        public override void Write(float value)
        {
            if (BitConverter.IsLittleEndian) base.Write(value); else base.Write(Endianness.Reverse(value));
        }

        public override void Write(string value)
        {
            if (BitConverter.IsLittleEndian) base.Write(value); else base.Write(Endianness.Reverse(value));
        }

        public override void Write(ushort value)
        {
            if (BitConverter.IsLittleEndian) base.Write(value); else base.Write(Endianness.Reverse(value));
        }

        public override void Write(uint value)
        {
            if (BitConverter.IsLittleEndian) base.Write(value); else base.Write(Endianness.Reverse(value));
        }

        public override void Write(ulong value)
        {
            if (BitConverter.IsLittleEndian) base.Write(value); else base.Write(Endianness.Reverse(value));
        }
    }
}
