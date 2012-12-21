﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Criteo.Memcache.Headers
{
    public struct MemcacheResponseHeader : IEquatable<MemcacheResponseHeader>
    {
        public static readonly int SIZE = 24;

        private const byte Magic = 0x81;
        public Opcode Opcode;
        public ushort KeyLength;
        public byte ExtraLength;
        public byte DataType;
        public Status Status;
        public uint TotalBodyLength;
        public uint Opaque;
        public ulong Cas;

        public override string ToString()
        {
            var bld = new StringBuilder();
            bld.Append("Opcode:").Append(Opcode.ToString()).Append('|')
                .Append("KeyLength:").Append(KeyLength).Append('|')
                .Append("ExtraLength:").Append(ExtraLength).Append('|')
                .Append("DataType:").Append(DataType).Append('|')
                .Append("Status:").Append(Status.ToString()).Append('|')
                .Append("TotalBodyLength:").Append(TotalBodyLength).Append('|')
                .Append("Opaque:").Append(Opaque).Append('|')
                .Append("Cas:").Append(Cas);
            return bld.ToString();
        }

        public void ToData(byte[] data, int offset = 0)
        {
            data[offset] = Magic;
            data[1 + offset] = (byte)Opcode;
            data.CopyFrom(2 + offset, KeyLength);
            data[4 + offset] = ExtraLength;
            data[5 + offset] = DataType;
            data.CopyFrom(6 + offset, (uint)Status);
            data.CopyFrom(8 + offset, TotalBodyLength);
            data.CopyFrom(12 + offset, Opaque);
            data.CopyFrom(16 + offset, Cas);
        }

        public void FromData(byte[] data, int offset = 0)
        {
            if (data[offset] != Magic)
                throw new ArgumentException("The buffer does not begin with the MagicNumber");
            Opcode = (Opcode)data[1 + offset];
            KeyLength = data.CopyToUShort(2 + offset);
            ExtraLength = data[4 + offset];
            DataType = data[5 + offset];
            Status = (Status)data.CopyToUShort(6 + offset);
            TotalBodyLength = data.CopyToUInt(8 + offset);
            Opaque = data.CopyToUInt(12 + offset);
            Cas = data.CopyToULong(16 + offset);
        }

        public MemcacheResponseHeader(byte[] data, int offset = 0)
            : this()
        {
            FromData(data, offset);
        }

        public override bool Equals(object obj)
        {
            return obj is MemcacheResponseHeader
                && Equals((MemcacheResponseHeader)obj);
        }

        public bool Equals(MemcacheResponseHeader other)
        {
            return other.Opcode == Opcode
                && other.KeyLength == KeyLength
                && other.ExtraLength == ExtraLength
                && other.DataType == DataType
                && other.Opaque == Opaque
                && other.TotalBodyLength == TotalBodyLength
                && other.Opaque == Opaque
                && other.Cas == Cas;
        }

        public override int GetHashCode()
        {
            return Opcode.GetHashCode()
                ^ KeyLength.GetHashCode()
                ^ ExtraLength.GetHashCode()
                ^ DataType.GetHashCode()
                ^ Opaque.GetHashCode()
                ^ TotalBodyLength.GetHashCode()
                ^ Opaque.GetHashCode()
                ^ Cas.GetHashCode();
        }
    }
}
