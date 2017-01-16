// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Threading;
using System.Threading.Tasks;
using SiliconStudio.Core.Diagnostics;

namespace SiliconStudio.Xenko.Engine.Network
{
    public class RouterClient
    {
        public static readonly Logger Log = GlobalLogger.GetLogger("RouterClient");

        /// <summary>
        /// The default port to connect to router server.
        /// </summary>
        public static readonly int DefaultPort = 31254;

        /// <summary>
        /// The default port to listen for connection from router.
        /// </summary>
        public static readonly int DefaultListenPort = 31255;

        /// <summary>
        /// Starts a service.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static void RegisterService()
        {
            // It will need the control connection (if not started yet)
            // Control connection will be able to list this service as available, and start an instance of it when requested
            throw new NotImplementedException();
        }

        /// <summary>
        /// Requests a specific server.
        /// </summary>
        /// <returns></returns>
        public static async Task<SimpleSocket> RequestServer(string serverUrl)
        {
            var socketContext = await InitiateConnectionToRouter();

            await socketContext.WriteStream.WriteInt16Async((short)ClientRouterMessage.RequestServer);
            await socketContext.WriteStream.WriteStringAsync(serverUrl);
            await socketContext.WriteStream.FlushAsync();

            var result = (ClientRouterMessage)await socketContext.ReadStream.ReadInt16Async();
            if (result != ClientRouterMessage.ServerStarted)
            {
                throw new SimpleSocketException("Could not connect to server");
            }

            var errorCode = await socketContext.ReadStream.ReadInt32Async();
            if (errorCode != 0)
            {
                var errorMessage = await socketContext.ReadStream.ReadStringAsync();
                throw new SimpleSocketException(errorMessage);
            }

            return socketContext;
        }

        /// <summary>
        /// Initiates a connection to the router.
        /// </summary>
        /// <returns></returns>
        private static Task<SimpleSocket> InitiateConnectionToRouter()
        {
            // Let's make sure this run in a different thread (in case some operation are blocking)
            return Task.Factory.StartNew(() =>
            {
                var socketContextTCS = new TaskCompletionSource<SimpleSocket>();
                var socketContext = new SimpleSocket();
                socketContext.Connected = context =>
                {
                    socketContextTCS.TrySetResult(context);
                };

                try
                {
#if SILICONSTUDIO_PLATFORM_UWP
                    var serverAddress = "127.0.0.1";
#else
                    var serverAddress = Environment.GetEnvironmentVariable("XenkoConnectionRouterRemoteIP") ?? "127.0.0.1";
#endif

                    // If connecting as a client, try once, otherwise try to listen multiple time (in case port is shared)
                    switch (ConnectionMode)
                    {
                        case RouterConnectionMode.Connect:
                            socketContext.StartClient(serverAddress, DefaultPort).Wait();
                            break;
                        case RouterConnectionMode.Listen:
                            socketContext.StartServer(DefaultListenPort, true, 10).Wait();
                            break;
                        case RouterConnectionMode.ConnectThenListen:
                            bool clientException = false;
                            try
                            {
                                socketContext.StartClient(serverAddress, DefaultPort).Wait();
                            }
                            catch (Exception) // Ideally we should filter SocketException, but not available on some platforms (maybe it should be wrapped in a type available on all paltforms?)
                            {
                                clientException = true;
                            }
                            if (clientException)
                            {
                                socketContext.StartServer(DefaultListenPort, true, 10).Wait();
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    // Connection should happen within 10 seconds, otherwise consider there is no connection router trying to connect back to us
                    if (!socketContextTCS.Task.Wait(TimeSpan.FromSeconds(10)))
                        throw new SimpleSocketException("Connection router did not connect back to our listen socket");

                    return socketContextTCS.Task.Result;
                }
                catch (Exception e)
                {
                    Log.Error($"Could not connect to connection router using mode {ConnectionMode}", e);
                    throw;
                }
            },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private static void StartControlConnection()
        {
            // Start control connection
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets a value indicating whether this platform initiates connections by listening on a port and wait for router (true) or connecting to router (false).
        /// </summary>
        private static RouterConnectionMode ConnectionMode
        {
            get
            {
#if SILICONSTUDIO_PLATFORM_UWP
                return RouterConnectionMode.ConnectThenListen;
#elif SILICONSTUDIO_PLATFORM_ANDROID || SILICONSTUDIO_PLATFORM_IOS
                return RouterConnectionMode.Listen;
#else
                return RouterConnectionMode.Connect;
#endif
            }
        }

        private enum RouterConnectionMode
        {
            /// <summary>
            /// Tries to connect to the router.
            /// </summary>
            Connect = 1,

            /// <summary>
            /// Tries to listen from a router connection.
            /// </summary>
            Listen = 2,

            /// <summary>
            /// First, tries to connect, and if not possible, listen for a router connection.
            /// This is useful for platform where we can't be sure (no way to determine if emulator and/or run in desktop or remotely, such as UWP).
            /// </summary>
            ConnectThenListen = 3,
        }
    }
}
