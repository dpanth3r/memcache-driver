﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Criteo.Memcache.Node;

namespace Criteo.Memcache.Locator
{
    public interface INodeLocator
    {
        /// <summary>
        /// This method should return the node where belongs the key or null if the they're all dead
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IMemcacheNode Locate(string key, IList<IMemcacheNode> nodes);
    }
}