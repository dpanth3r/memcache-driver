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
using System.Collections.Generic;
using Criteo.Memcache.Locator;
using Criteo.Memcache.Node;

namespace Criteo.Memcache.Cluster
{
    public interface IMemcacheCluster : IDisposable
    {
        void Initialize();

        /// <summary>
        /// This property should be used every time someone wants to use the locator,
        /// as the locator may be swapped at any time by dynamic cluster implementations.
        /// </summary>
        INodeLocator Locator { get; }

        IEnumerable<IMemcacheNode> Nodes { get; }

        event Action<IMemcacheNode> NodeAdded;
        event Action<IMemcacheNode> NodeRemoved;
    }
}
