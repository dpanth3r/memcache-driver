﻿using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Criteo.Memcache.Requests;
using Criteo.Memcache.Headers;
using Criteo.Memcache.Node;
using Criteo.Memcache.Authenticators;
using Criteo.Memcache.Exceptions;

namespace Criteo.Memcache.Sockets
{
    internal class MemcacheSocketAsynch : MemcacheSocketBase
    {
        public MemcacheSocketAsynch(IPEndPoint endPoint, IMemcacheNodeQueue itemQueue, IMemcacheAuthenticator authenticator)
            : base(endPoint, itemQueue, authenticator)
        {
        }

        private Thread _sendingThread;
        private SocketAsyncEventArgs _receiveArgs;
        private CancellationTokenSource _token;

        private void StartSendingThread()
        {
            _sendingThread = new Thread(t =>
            {
                var token = (CancellationToken)t;
                while (true)
                {
                    try
                    {
                        var request = GetNextRequest();
                        if (request == null)
                            return;
                        
                        var buffer = request.GetQueryBuffer();

                        PendingRequests.Enqueue(request);
                        int sent = 0;
                        do
                        {
                            sent += Socket.Send(buffer, sent, buffer.Length - sent, SocketFlags.None);
                        } while (sent != buffer.Length);
                    }
                    catch (Exception e)
                    {
                        if (!token.IsCancellationRequested)
                        {
                            if (_transportError != null)
                                _transportError(e);

                            _sendingThread = null;
                            Reset();
                        }
                    }
                }
            });
            _sendingThread.Start(_token.Token);
        }

        private void InitReadResponse()
        {
            _receiveArgs = new SocketAsyncEventArgs();
            _receiveArgs.SetBuffer(new byte[MemcacheResponseHeader.SIZE], 0, MemcacheResponseHeader.SIZE);
            _receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReadResponseComplete);
        }

        private void ReadResponse()
        {
            _receiveArgs.SetBuffer(0, MemcacheResponseHeader.SIZE);
            Socket.ReceiveAsync(_receiveArgs);
        }

        private void OnReadResponseComplete(object _, SocketAsyncEventArgs args)
        {
            try
            {
                // check if we read a full header, else continue
                if (args.BytesTransferred + args.Offset < MemcacheResponseHeader.SIZE)
                {
                    int offset = args.BytesTransferred + args.Offset;
                    args.SetBuffer(offset, MemcacheResponseHeader.SIZE - offset);
                    Socket.ReceiveAsync(args);
                    return;
                }

                var header = new MemcacheResponseHeader(args.Buffer);

                byte[] extra = null;
                byte[] message = null;
                int received;
                // in case we have a message ! (should not happen for a set)
                if (header.ExtraLength > 0)
                {
                    extra = new byte[header.ExtraLength];
                    received = 0;
                    do
                    {
                        received += Socket.Receive(extra, received, header.ExtraLength - received, SocketFlags.None);
                    } while (received < header.ExtraLength);
                }
                int messageLength = (int)(header.TotalBodyLength - header.ExtraLength);
                if (header.TotalBodyLength - header.ExtraLength > 0)
                {
                    message = new byte[header.TotalBodyLength - header.ExtraLength];
                    received = 0;
                    do
                    {
                        received += Socket.Receive(message, received, messageLength - received, SocketFlags.None);
                    } while (received < messageLength);
                }

                // should assert we have the good request
                var request = UnstackToMatch(header);
                if (request != null)
                    request.HandleResponse(header, extra, message);

                if (_memcacheResponse !=  null)
                    _memcacheResponse(header, request);

                // TODO : should I keep that or the request only have to handle it ?
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
            _token = new CancellationTokenSource();
            StartSendingThread();
            InitReadResponse();
            ReadResponse();
        }

        protected override void ShutDown()
        {
            if(_token != null)
                _token.Cancel();
            if(_receiveArgs != null)
                _receiveArgs.Dispose();
            if (Socket != null)
            {
                Socket.Dispose();
                Socket = null;
            }
        }
    }
}
