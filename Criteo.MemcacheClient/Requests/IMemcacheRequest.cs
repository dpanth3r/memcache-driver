﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Criteo.MemcacheClient.Requests
{
    public interface IMemcacheRequest
    {
        uint RequestId { get; set; }
        string Key { get; set; }
        
        byte[] GetQueryBuffer();
        void HandleResponse(MemacheResponseHeader header, byte[] message);
    }
}