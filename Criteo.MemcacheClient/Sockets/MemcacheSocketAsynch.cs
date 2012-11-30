﻿using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Criteo.MemcacheClient.Requests;

namespace Criteo.MemcacheClient.Sockets
{
    public class MemcacheSocketAsynch : MemcacheSocketBase
    {
        public MemcacheSocketAsynch(IPEndPoint endPoint, BlockingCollection<IMemcacheRequest> itemQueue)
            : base(endPoint, itemQueue)
        {
        }

        private Thread _sendingThread;
        private SocketAsyncEventArgs _receiveArgs;
        private uint _requestId = 0;

        private void StartSendingThread()
        {
            _sendingThread = new Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        var request = WaitingRequests.Take();
                        request.RequestId = ++_requestId;
                        var buffer = request.GetQueryBuffer();

                        PendingRequests.Enqueue(request);
                        Socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        if (_transportError != null)
                            _transportError(e);

                        _sendingThread = null;
                        Reset();
                        return;
                    }
                }
            });
            _sendingThread.Start();
        }

        private void InitReadResponse()
        {
            _receiveArgs = new SocketAsyncEventArgs();
            _receiveArgs.SetBuffer(new byte[24], 0, 24);
            _receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReadResponseComplete);
        }

        private void ReadResponse()
        {
            _receiveArgs.SetBuffer(0, 24);
            Socket.ReceiveAsync(_receiveArgs);
        }

        private void OnReadResponseComplete(object _, SocketAsyncEventArgs args)
        {
            try
            {
                // check if we read a full header, else continue
                if (args.BytesTransferred + args.Offset < 24)
                {
                    int offset = args.BytesTransferred + args.Offset;
                    args.SetBuffer(offset, 24 - offset);
                    Socket.ReceiveAsync(args);
                    return;
                }

                var header = new MemacheResponseHeader();
                header.FromData(args.Buffer);

                byte[] message = null;
                // in case we have a message ! (should not happen for a set)
                if (header.TotalBodyLength > 0)
                {
                    message = new byte[header.TotalBodyLength];
                    Socket.Receive(message);
                }

                // should assert we have the good request
                var request = UnstackToMatch(header);
                if (request != null)
                    request.HandleResponse(header, message);

                if (_memcacheResponse !=  null)
                    _memcacheResponse(header, request);
                if (header.Status != Status.NoError && _memcacheError != null)
                    _memcacheError(header, request);

                // loop the read on the socket
                ReadResponse();
            }
            catch (Exception e)
            {
                if (_transportError != null)
                    _transportError(e);
                Reset();
            }
        }

        protected override void Start()
        {
            StartSendingThread();
            InitReadResponse();
            ReadResponse();
        }

        protected override void ShutDown()
        {
            if(_sendingThread != null)
                _sendingThread.Abort();
            _receiveArgs.Dispose();
        }
    }
}