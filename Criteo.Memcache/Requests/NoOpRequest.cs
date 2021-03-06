﻿/* Licensed to the Apache Software Foundation (ASF) under one
   or more contributor license agreements.  See the NOTICE file
   distributed with this work for additional information
   regarding copyright ownership.  The ASF licenses this file
   to you under the Apache License, Version 2.0 (the
   "License"); you may not use this file except in compliance
   with the License.  You may obtain a copy of the License at

     http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing,
   software distributed under the License is distributed on an
   "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
   KIND, either express or implied.  See the License for the
   specific language governing permissions and limitations
   under the License.
*/
using System;

using Criteo.Memcache.Headers;

namespace Criteo.Memcache.Requests
{
    internal class NoOpRequest : MemcacheRequestBase, IMemcacheRequest
    {
        public override int Replicas { get { return 0; } }

        public Action<MemcacheResponseHeader> Callback { get; set; }

        public byte[] GetQueryBuffer()
        {
            var requestHeader = new MemcacheRequestHeader(Opcode.NoOp)
            {
                VBucket = VBucket,
                Opaque = RequestId,
            };

            var buffer = new byte[MemcacheRequestHeader.Size];
            requestHeader.ToData(buffer);

            return buffer;
        }

        public void HandleResponse(MemcacheResponseHeader header, byte[] key, byte[] extra, byte[] message)
        {
            if (Callback != null)
                Callback(header);
        }

        public void Fail()
        {
            if (Callback != null)
            {
                var response = new MemcacheResponseHeader
                {
                    Opcode = Opcode.NoOp,
                    Status = Status.InternalError,
                    Opaque = RequestId
                };

                Callback(response);
            }
        }
    }
}
