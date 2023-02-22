namespace ENetDotNet;

partial class ENetHost
{
    /// <summary>
    /// Creates a host for communicating to peers.
    /// </summary>
    /// <param name="address">The address at which other peers may connect to this host.  If NULL, then no peers may connect to the host.</param>
    /// <param name="peerCount">The maximum number of peers that should be allocated for the host.</param>
    /// <param name="channelLimit">The maximum number of channels allowed; if 0, then this is equivalent to ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT.</param>
    /// <param name="incomingBandwidth">Downstream bandwidth of the host in bytes/second; if 0, ENet will assume unlimited bandwidth.</param>
    /// <param name="outgoingBandwidth">Upstream bandwidth of the host in bytes/second; if 0, ENet will assume unlimited bandwidth.</param>
    /// <returns>The host on success and <see langword="null"/> on failure.</returns>
    /// <remarks>
    /// ENet will strategically drop packets on specific sides of a connection between hosts
    /// to ensure the host's bandwidth is not overwhelmed.  The bandwidth parameters also determine
    /// the window size of a connection which limits the amount of reliable packets that may be in transit
    /// at any given time.
    /// </remarks>
    public ENetHost(ENetAddress address, size_t peerCount, size_t channelLimit, uint incomingBandwidth, uint outgoingBandwidth)
    {
        if (peerCount > ENetProtocol.MaximumPeerId)
            throw new ArgumentException(null, nameof(peerCount));

        Peers = new ENetPeer[peerCount];

        Socket = enet_socket_create(ENET_SOCKET_TYPE_DATAGRAM);
        if (Socket == ENET_SOCKET_NULL || (address != null && enet_socket_bind(Socket, address) < 0))
        {
            if (Socket != ENET_SOCKET_NULL)
                enet_socket_destroy(Socket);

            enet_free(host.Peers);
            enet_free(host);

            return null;
        }

        enet_socket_set_option(Socket, ENET_SOCKOPT_NONBLOCK, 1);
        enet_socket_set_option(Socket, ENET_SOCKOPT_BROADCAST, 1);
        enet_socket_set_option(Socket, ENET_SOCKOPT_RCVBUF, ENET_HOST_RECEIVE_BUFFER_SIZE);
        enet_socket_set_option(Socket, ENET_SOCKOPT_SNDBUF, ENET_HOST_SEND_BUFFER_SIZE);

        if (address != null && enet_socket_get_address(Socket, &Address) < 0)   
            Address = * address;

        if (!channelLimit || channelLimit > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT)
            channelLimit = ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT;
        else if (channelLimit < ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT)
            channelLimit = ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT;

        RandomSeed = (enet_uint32) (size_t) host;
        RandomSeed += enet_host_random_seed();
        RandomSeed = (RandomSeed << 16) | (RandomSeed >> 16);
        ChannelLimit = channelLimit;
        IncomingBandwidth = incomingBandwidth;
        OutgoingBandwidth = outgoingBandwidth;
        BandwidthThrottleEpoch = 0;
        RecalculateBandwidthLimits = 0;
        Mtu = ENET_HOST_DEFAULT_MTU;
        PeerCount = peerCount;
        CommandCount = 0;
        BufferCount = 0;
        Checksum = NULL;
        ReceivedAddress.Host = ENET_HOST_ANY;
        ReceivedAddress.Port = 0;
        ReceivedData = null;
        ReceivedDataLength = 0;
        
        TotalSentData = 0;
        TotalSentPackets = 0;
        TotalReceivedData = 0;
        TotalReceivedPackets = 0;

        ConnectedPeers = 0;
        BandwidthLimitedPeers = 0;
        DuplicatePeers = ENET_PROTOCOL_MAXIMUM_PEER_ID;
        MaximumPacketSize = ENET_HOST_DEFAULT_MAXIMUM_PACKET_SIZE;
        MaximumWaitingData = ENET_HOST_DEFAULT_MAXIMUM_WAITING_DATA;

        Compressor.Context = null;
        Compressor.Compress = null;
        Compressor.Decompress = null;
        Compressor.Destroy = null;

        Intercept = null;

        DispatchQueue.Clear();

        for (int i = 0; i < PeerCount; ++i)
        {
            ENetPeer currentPeer = Peers[i];

            currentPeer.Host = this;
            currentPeer.IncomingPeerId = (ushort)i;
            currentPeer.OutgoingSessionId = 0xFF;
            currentPeer.IncomingSessionId = 0xFF;
            currentPeer.Data = null;

            currentPeer.Acknowledgements.Clear();
            currentPeer.SentReliableCommands.Clear();
            currentPeer.SentUnreliableCommands.Clear();
            currentPeer.OutgoingCommands.Clear();
            currentPeer.DispatchedCommands.Clear();

            currentPeer.Reset();
        }
    }
}

/** Destroys the host and all resources associated with it.
    @param host pointer to the host to destroy
*/
void
enet_host_destroy (ENetHost * host)
{
    ENetPeer * currentPeer;

    if (host == NULL)
      return;

    enet_socket_destroy (host -> socket);

    for (currentPeer = host -> peers;
         currentPeer < & host -> peers [host -> peerCount];
         ++ currentPeer)
    {
       enet_peer_reset (currentPeer);
    }

    if (host -> compressor.context != NULL && host -> compressor.destroy)
      (* host -> compressor.destroy) (host -> compressor.context);

    enet_free (host -> peers);
    enet_free (host);
}

/** Initiates a connection to a foreign host.
    @param host host seeking the connection
    @param address destination for the connection
    @param channelCount number of channels to allocate
    @param data user data supplied to the receiving host 
    @returns a peer representing the foreign host on success, NULL on failure
    @remarks The peer returned will have not completed the connection until enet_host_service()
    notifies of an ENET_EVENT_TYPE_CONNECT event for the peer.
*/
ENetPeer *
enet_host_connect (ENetHost * host, const ENetAddress * address, size_t channelCount, enet_uint32 data)
{
    ENetPeer * currentPeer;
    ENetChannel * channel;
    ENetProtocol command;

    if (channelCount < ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT)
      channelCount = ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT;
    else
    if (channelCount > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT)
      channelCount = ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT;

    for (currentPeer = host -> peers;
         currentPeer < & host -> peers [host -> peerCount];
         ++ currentPeer)
    {
       if (currentPeer -> state == ENET_PEER_STATE_DISCONNECTED)
         break;
    }

    if (currentPeer >= & host -> peers [host -> peerCount])
      return NULL;

    currentPeer -> channels = (ENetChannel *) enet_malloc (channelCount * sizeof (ENetChannel));
    if (currentPeer -> channels == NULL)
      return NULL;
    currentPeer -> channelCount = channelCount;
    currentPeer -> state = ENET_PEER_STATE_CONNECTING;
    currentPeer -> address = * address;
    currentPeer -> connectID = ++ host -> randomSeed;

    if (host -> outgoingBandwidth == 0)
      currentPeer -> windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
    else
      currentPeer -> windowSize = (host -> outgoingBandwidth /
                                    ENET_PEER_WINDOW_SIZE_SCALE) * 
                                      ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;

    if (currentPeer -> windowSize < ENET_PROTOCOL_MINIMUM_WINDOW_SIZE)
      currentPeer -> windowSize = ENET_PROTOCOL_MINIMUM_WINDOW_SIZE;
    else
    if (currentPeer -> windowSize > ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE)
      currentPeer -> windowSize = ENET_PROTOCOL_MAXIMUM_WINDOW_SIZE;
         
    for (channel = currentPeer -> channels;
         channel < & currentPeer -> channels [channelCount];
         ++ channel)
    {
        channel -> outgoingReliableSequenceNumber = 0;
        channel -> outgoingUnreliableSequenceNumber = 0;
        channel -> incomingReliableSequenceNumber = 0;
        channel -> incomingUnreliableSequenceNumber = 0;

        channel.IncomingReliableCommands.Clear();
        channel.IncomingUnreliableCommands.Clear();

        channel -> usedReliableWindows = 0;
        memset (channel -> reliableWindows, 0, sizeof (channel -> reliableWindows));
    }
        
    command.header.command = ENET_PROTOCOL_COMMAND_CONNECT | ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
    command.header.channelID = 0xFF;
    command.connect.outgoingPeerID = IPAddress.HostToNetworkOrder (currentPeer -> incomingPeerID);
    command.connect.incomingSessionID = currentPeer -> incomingSessionID;
    command.connect.outgoingSessionID = currentPeer -> outgoingSessionID;
    command.connect.mtu = IPAddress.HostToNetworkOrder (currentPeer -> mtu);
    command.connect.windowSize = IPAddress.HostToNetworkOrder (currentPeer -> windowSize);
    command.connect.channelCount = IPAddress.HostToNetworkOrder (channelCount);
    command.connect.incomingBandwidth = IPAddress.HostToNetworkOrder (host -> incomingBandwidth);
    command.connect.outgoingBandwidth = IPAddress.HostToNetworkOrder (host -> outgoingBandwidth);
    command.connect.packetThrottleInterval = IPAddress.HostToNetworkOrder (currentPeer -> packetThrottleInterval);
    command.connect.packetThrottleAcceleration = IPAddress.HostToNetworkOrder (currentPeer -> packetThrottleAcceleration);
    command.connect.packetThrottleDeceleration = IPAddress.HostToNetworkOrder (currentPeer -> packetThrottleDeceleration);
    command.connect.connectID = currentPeer -> connectID;
    command.connect.data = IPAddress.HostToNetworkOrder (data);
 
    enet_peer_queue_outgoing_command (currentPeer, & command, NULL, 0, 0);

    return currentPeer;
}

/** Queues a packet to be sent to all peers associated with the host.
    @param host host on which to broadcast the packet
    @param channelID channel on which to broadcast
    @param packet packet to broadcast
*/
void
enet_host_broadcast (ENetHost * host, enet_uint8 channelID, ENetPacket * packet)
{
    ENetPeer * currentPeer;

    for (currentPeer = host -> peers;
         currentPeer < & host -> peers [host -> peerCount];
         ++ currentPeer)
    {
       if (currentPeer -> state != ENET_PEER_STATE_CONNECTED)
         continue;

       enet_peer_send (currentPeer, channelID, packet);
    }

    if (packet -> referenceCount == 0)
      enet_packet_destroy (packet);
}

/** Sets the packet compressor the host should use to compress and decompress packets.
    @param host host to enable or disable compression for
    @param compressor callbacks for for the packet compressor; if NULL, then compression is disabled
*/
void
enet_host_compress (ENetHost * host, const ENetCompressor * compressor)
{
    if (host -> compressor.context != NULL && host -> compressor.destroy)
      (* host -> compressor.destroy) (host -> compressor.context);

    if (compressor)
      host -> compressor = * compressor;
    else
      host -> compressor.context = NULL;
}

/** Limits the maximum allowed channels of future incoming connections.
    @param host host to limit
    @param channelLimit the maximum number of channels allowed; if 0, then this is equivalent to ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT
*/
void
enet_host_channel_limit (ENetHost * host, size_t channelLimit)
{
    if (! channelLimit || channelLimit > ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT)
      channelLimit = ENET_PROTOCOL_MAXIMUM_CHANNEL_COUNT;
    else
    if (channelLimit < ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT)
      channelLimit = ENET_PROTOCOL_MINIMUM_CHANNEL_COUNT;

    host -> channelLimit = channelLimit;
}


/** Adjusts the bandwidth limits of a host.
    @param host host to adjust
    @param incomingBandwidth new incoming bandwidth
    @param outgoingBandwidth new outgoing bandwidth
    @remarks the incoming and outgoing bandwidth parameters are identical in function to those
    specified in enet_host_create().
*/
void
enet_host_bandwidth_limit (ENetHost * host, enet_uint32 incomingBandwidth, enet_uint32 outgoingBandwidth)
{
    host -> incomingBandwidth = incomingBandwidth;
    host -> outgoingBandwidth = outgoingBandwidth;
    host -> recalculateBandwidthLimits = 1;
}

void
enet_host_bandwidth_throttle (ENetHost * host)
{
    enet_uint32 timeCurrent = enet_time_get (),
           elapsedTime = timeCurrent - host -> bandwidthThrottleEpoch,
           peersRemaining = (enet_uint32) host -> connectedPeers,
           dataTotal = ~0,
           bandwidth = ~0,
           throttle = 0,
           bandwidthLimit = 0;
    int needsAdjustment = host -> bandwidthLimitedPeers > 0 ? 1 : 0;
    ENetPeer * peer;
    ENetProtocol command;

    if (elapsedTime < ENET_HOST_BANDWIDTH_THROTTLE_INTERVAL)
      return;

    host -> bandwidthThrottleEpoch = timeCurrent;

    if (peersRemaining == 0)
      return;

    if (host -> outgoingBandwidth != 0)
    {
        dataTotal = 0;
        bandwidth = (host -> outgoingBandwidth * elapsedTime) / 1000;

        for (peer = host -> peers;
             peer < & host -> peers [host -> peerCount];
            ++ peer)
        {
            if (peer -> state != ENET_PEER_STATE_CONNECTED && peer -> state != ENET_PEER_STATE_DISCONNECT_LATER)
              continue;

            dataTotal += peer -> outgoingDataTotal;
        }
    }

    while (peersRemaining > 0 && needsAdjustment != 0)
    {
        needsAdjustment = 0;
        
        if (dataTotal <= bandwidth)
          throttle = ENET_PEER_PACKET_THROTTLE_SCALE;
        else
          throttle = (bandwidth * ENET_PEER_PACKET_THROTTLE_SCALE) / dataTotal;

        for (peer = host -> peers;
             peer < & host -> peers [host -> peerCount];
             ++ peer)
        {
            enet_uint32 peerBandwidth;
            
            if ((peer -> state != ENET_PEER_STATE_CONNECTED && peer -> state != ENET_PEER_STATE_DISCONNECT_LATER) ||
                peer -> incomingBandwidth == 0 ||
                peer -> outgoingBandwidthThrottleEpoch == timeCurrent)
              continue;

            peerBandwidth = (peer -> incomingBandwidth * elapsedTime) / 1000;
            if ((throttle * peer -> outgoingDataTotal) / ENET_PEER_PACKET_THROTTLE_SCALE <= peerBandwidth)
              continue;

            peer -> packetThrottleLimit = (peerBandwidth * 
                                            ENET_PEER_PACKET_THROTTLE_SCALE) / peer -> outgoingDataTotal;
            
            if (peer -> packetThrottleLimit == 0)
              peer -> packetThrottleLimit = 1;
            
            if (peer -> packetThrottle > peer -> packetThrottleLimit)
              peer -> packetThrottle = peer -> packetThrottleLimit;

            peer -> outgoingBandwidthThrottleEpoch = timeCurrent;

            peer -> incomingDataTotal = 0;
            peer -> outgoingDataTotal = 0;

            needsAdjustment = 1;
            -- peersRemaining;
            bandwidth -= peerBandwidth;
            dataTotal -= peerBandwidth;
        }
    }

    if (peersRemaining > 0)
    {
        if (dataTotal <= bandwidth)
          throttle = ENET_PEER_PACKET_THROTTLE_SCALE;
        else
          throttle = (bandwidth * ENET_PEER_PACKET_THROTTLE_SCALE) / dataTotal;

        for (peer = host -> peers;
             peer < & host -> peers [host -> peerCount];
             ++ peer)
        {
            if ((peer -> state != ENET_PEER_STATE_CONNECTED && peer -> state != ENET_PEER_STATE_DISCONNECT_LATER) ||
                peer -> outgoingBandwidthThrottleEpoch == timeCurrent)
              continue;

            peer -> packetThrottleLimit = throttle;

            if (peer -> packetThrottle > peer -> packetThrottleLimit)
              peer -> packetThrottle = peer -> packetThrottleLimit;

            peer -> incomingDataTotal = 0;
            peer -> outgoingDataTotal = 0;
        }
    }

    if (host -> recalculateBandwidthLimits)
    {
       host -> recalculateBandwidthLimits = 0;

       peersRemaining = (enet_uint32) host -> connectedPeers;
       bandwidth = host -> incomingBandwidth;
       needsAdjustment = 1;

       if (bandwidth == 0)
         bandwidthLimit = 0;
       else
       while (peersRemaining > 0 && needsAdjustment != 0)
       {
           needsAdjustment = 0;
           bandwidthLimit = bandwidth / peersRemaining;

           for (peer = host -> peers;
                peer < & host -> peers [host -> peerCount];
                ++ peer)
           {
               if ((peer -> state != ENET_PEER_STATE_CONNECTED && peer -> state != ENET_PEER_STATE_DISCONNECT_LATER) ||
                   peer -> incomingBandwidthThrottleEpoch == timeCurrent)
                 continue;

               if (peer -> outgoingBandwidth > 0 &&
                   peer -> outgoingBandwidth >= bandwidthLimit)
                 continue;

               peer -> incomingBandwidthThrottleEpoch = timeCurrent;
 
               needsAdjustment = 1;
               -- peersRemaining;
               bandwidth -= peer -> outgoingBandwidth;
           }
       }

       for (peer = host -> peers;
            peer < & host -> peers [host -> peerCount];
            ++ peer)
       {
           if (peer -> state != ENET_PEER_STATE_CONNECTED && peer -> state != ENET_PEER_STATE_DISCONNECT_LATER)
             continue;

           command.header.command = ENET_PROTOCOL_COMMAND_BANDWIDTH_LIMIT | ENET_PROTOCOL_COMMAND_FLAG_ACKNOWLEDGE;
           command.header.channelID = 0xFF;
           command.bandwidthLimit.outgoingBandwidth = IPAddress.HostToNetworkOrder (host -> outgoingBandwidth);

           if (peer -> incomingBandwidthThrottleEpoch == timeCurrent)
             command.bandwidthLimit.incomingBandwidth = IPAddress.HostToNetworkOrder (peer -> outgoingBandwidth);
           else
             command.bandwidthLimit.incomingBandwidth = IPAddress.HostToNetworkOrder (bandwidthLimit);

           enet_peer_queue_outgoing_command (peer, & command, NULL, 0, 0);
       } 
    }
}
    
/** @} */
