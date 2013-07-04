﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Criteo.Memcache.Headers;
using Criteo.Memcache.Exceptions;

namespace Criteo.Memcache.Requests
{
    internal class GetRequest : RedundantRequest, IMemcacheRequest
    {
        public string Key { get; set; }
        public Action<Status, byte[]> CallBack { get; set; }
        public uint RequestId { get; set; }
        protected virtual Opcode RequestOpcode { get { return Opcode.Get; } }
        public uint Flag { get; private set; }

        public GetRequest() : base(0) { }
        public GetRequest(int replicas) : base(replicas) { }

        public void Sent(int sentRequests)
        {
            if(CallCallbackOnLastSent(sentRequests) && CallBack != null)
            {
                CallBack(Status.InternalError, null);
            }
        }


        public byte[] GetQueryBuffer()
        {
            var keyAsBytes = UTF8Encoding.Default.GetBytes(Key);
            if (keyAsBytes.Length > ushort.MaxValue)
                throw new ArgumentException("The key is too long for the memcache binary protocol : " + Key);

            var requestHeader = new MemcacheRequestHeader(RequestOpcode)
            {
                KeyLength = (ushort)keyAsBytes.Length,
                ExtraLength = 0,
                TotalBodyLength = (uint)(keyAsBytes.Length),
                Opaque = RequestId,
            };

            var buffer = new byte[MemcacheRequestHeader.SIZE + requestHeader.TotalBodyLength];
            requestHeader.ToData(buffer, 0);
            keyAsBytes.CopyTo(buffer, MemcacheRequestHeader.SIZE);

            return buffer;
        }

        public void HandleResponse(MemcacheResponseHeader header, string key, byte[] extra, byte[] message)
        {
            if (header.Status == Status.NoError)
            {
                if (extra == null || extra.Length == 0)
                    throw new MemcacheException("The get command flag is not present !");
                else if (extra.Length != 4)
                    throw new MemcacheException("The get command flag is the wrong size !");
                Flag = extra.CopyToUInt(0);
            }

            Status status = header.Status;
            if (CallCallback(ref status) && CallBack != null)
                CallBack(status, message);
        }

        public void Fail()
        {
            Status status = Status.InternalError;
            if (CallCallback(ref status) && CallBack != null)
                CallBack(status, null);
        }

        public override string ToString()
        {
            return RequestOpcode.ToString() + ";Id=" + RequestId + ";Key=" + Key;
        }
    }
}
