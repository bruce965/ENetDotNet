namespace ENetDotNet;

public static partial class ENet {
    public static int VersionMajor => 1;
    public static int VersionMinor => 3;
    public static int VersionPatch => 17;

    public static int VersionCreate(int major, int minor, int patch)
        => ((major)<<16) | ((minor)<<8) | (patch);

    public static int VersionGetMajor(int version)
        => ((version)>>16)&0xFF;

    public static int VersionGetMinor(int version)
        => ((version)>>8)&0xFF;

    public static int VersionGetPatch(int version)
        => (version)&0xFF;

    public static int Version { get; } = VersionCreate(VersionMajor, VersionMinor, VersionPatch);
}

public enum ENetSocketType
{
    Stream = 1,
    Datagram = 2,
}

[Flags]
public enum ENetSocketWait
{
    None = 0,
    Send = (1 << 0),
    Receive = (1 << 1),
    Interrupt = (1 << 2),
}

public enum ENetSocketOption
{
    NonBlock = 1,
    Broadcast = 2,
    RcvBuf = 3,
    SndBuf = 4,
    ReuseAddr = 5,
    RcvTimeO = 6,
    SndTimeO = 7,
    Error = 8,
    NoDelay = 9
}

public enum ENetSocketShutdown
{
    Read = 0,
    WRITE = 1,
    READ_WRITE = 2
}

partial class ENet {
    public int HostAny => 0;
    public int HostBroadcast => 0xFFFFFFFFU;
    public int PortAny => 0;
}


/// <summary>
/// Portable internet address structure.
/// <para>
/// The host must be specified in network byte-order, and the port must be in host 
/// byte-order. The constant ENET_HOST_ANY may be used to specify the default 
/// server host. The constant ENET_HOST_BROADCAST may be used to specify the
/// broadcast address (255.255.255.255).  This makes sense for enet_host_connect,
/// but not for enet_host_create.  Once a server responds to a broadcast, the
/// address is updated from ENET_HOST_BROADCAST to the server's actual IP address.
/// </para>
/// </summary>
public struct ENetAddress
{
    uint host;
    ushort port;
}

/**
 * Packet flag bit constants.
 *
 * The host must be specified in network byte-order, and the port must be in
 * host byte-order. The constant ENET_HOST_ANY may be used to specify the
 * default server host.
 
    @sa ENetPacket
*/
[Flags]
public enum ENetPacketFlag : uint
{
    /// <summary>
    /// Packet must be received by the target peer and resend attempts should be
    /// made until the packet is delivered.
    /// </summary>
    Reliable = (1 << 0),
    /// <summary>
    /// Packet will not be sequenced with other packets.
    /// <para>
    /// Not supported for reliable packets.
    /// </para>
    /// </summary>
    Unsequenced = (1 << 1),
    /// <summary>
    /// Packet will not allocate data, and user must supply it instead.
    /// </summary>
    NoAllocate = (1 << 2),
    /// <summary>
    /// Packet will be fragmented using unreliable (instead of reliable) sends
    /// if it exceeds the MTU.
    /// </summary>
    UnreliableFragment = (1 << 3),

    /// <summary>
    /// Whether the packet has been sent from all queues it has been entered into.
    /// </summary>
    Sent = (1<<8)
}

public delegate void ENetPacketFreeCallback(ENetPacket packet);

/// <summary>
/// ENet packet structure.
/// <para>
/// An ENet data packet that may be sent to or received from a peer. The shown
/// fields should only be read and never modified. The data field contains the
/// allocated data for the packet. The dataLength fields specifies the length
/// of the allocated data.  The flags field is either 0 (specifying no flags),
/// or a bitwise-or of any combination of the following flags:
///
///    ENET_PACKET_FLAG_RELIABLE - packet must be received by the target peer
///    and resend attempts should be made until the packet is delivered
///
///    ENET_PACKET_FLAG_UNSEQUENCED - packet will not be sequenced with other packets
///    (not supported for reliable packets)
///
///    ENET_PACKET_FLAG_NO_ALLOCATE - packet will not allocate data, and user must supply it instead
///
///    ENET_PACKET_FLAG_UNRELIABLE_FRAGMENT - packet will be fragmented using unreliable
///    (instead of reliable) sends if it exceeds the MTU
///
///    ENET_PACKET_FLAG_SENT - whether the packet has been sent from all queues it has been entered into
/// </para>
/// </summary>
/// <see cref="ENetPacketFlag"/>
public partial class ENetPacket
{
    /// <summary>
    /// Internal use only.
    /// </summary>
    internal int _referenceCount;
    /// <summary>
    /// Bitwise-or of ENetPacketFlag constants.
    /// </summary>
    public ENetPacketFlag Flags;
    /// <summary>
    /// Allocated data for packet.
    /// </summary>
    public byte[]? Data;
    /// <summary>
    /// Length of data.
    /// </summary>
    public int DataLength;
    /// <summary>
    /// Function to be called when the packet is no longer in use.
    /// </summary>
    public ENetPacketFreeCallback? FreeCallback;
    /// <summary>
    /// Application private data, may be freely modified.
    /// </summary>
    public object? UserData;
}

public struct ENetAcknowledgement
{
    public LinkedListNode<ENetAcknowledgement> AcknowledgementList;
    public uint SentTime;
    public ENetProtocol Command;
}

public class ENetOutgoingCommand
{
    public LinkedListNode<TODO> OutgoingCommandList;
    public ushort ReliableSequenceNumber;
    public ushort UnreliableSequenceNumber;
    public uint SentTime;
    public uint RoundTripTimeout;
    public uint RoundTripTimeoutLimit;
    public uint FragmentOffset;
    public ushort FragmentLength;
    public ushort SendAttempts;
    public ENetProtocol Command;
    public ENetPacket? Packet;
}

public struct ENetIncomingCommand
{
    public LinkedListNode<TODO> IncomingCommandList;
    public ushort ReliableSequenceNumber;
    public ushort UnreliableSequenceNumber;
    public ENetProtocol Command;
    public uint FragmentCount;
    public uint FragmentsRemaining;
    public uint * Fragments;
    public ENetPacket Packet;
}

public enum ENetPeerState
{
    Disconnected = 0,
    Connecting = 1,
    AcknowledgingConnect = 2,
    ConnectionPending = 3,
    ConnectionSucceeded = 4,
    Connected = 5,
    DisconnectLater = 6,
    Disconnecting = 7,
    AcknowledgingDisconnect = 8,
    Zombie = 9,
}

partial class ENet {
    public const int BufferMaximum = 1 + 2 * ENetProtocol.MaximumPacketCommands;

    public const int HostReceiveBufferSize = 256 * 1024;
    public const int HostSendBufferSize = 256 * 1024;
    public const int HostBandwidthThrottleInterval = 1000;
    public const int HostDefaultMtu = 1400;
    public const int HostDefaultMaximumPacketSize = 32 * 1024 * 1024;
    public const int HostDefaultMaximumWaitingData = 32 * 1024 * 1024;

    public const int PeerDefaultRoundTripTime = 500;
    public const int PeerDefaultPacketThrottle = 32;
    public const int PeerPacketThrottleScale = 32;
    public const int PeerPacketThrottleCounter = 7;
    public const int PeerPacketThrottleAcceleration = 2;
    public const int PeerPacketThrottleDeceleration = 2;
    public const int PeerPacketThrottleInterval = 5000;
    public const int PeerPacketLossScale = (1 << 16);
    public const int PeerPacketLossInterval = 10000;
    public const int PeerWindowSizeScale = 64 * 1024;
    public const int PeerTimeoutLimit = 32;
    public const int PeerTimeoutMinimum = 5000;
    public const int PeerTimeoutMaximum = 30000;
    public const int PeerPingInterval = 500;
    public const int PeerUnsequencedWindows = 64;
    public const int PeerUnsequencedWindowSize = 1024;
    public const int PeerFreeUnsequencedWindows = 32;
    public const int PeerReliableWindows = 16;
    public const int PeerReliableWindowSize = 0x1000;
    public const int PeerFreeReliableWindows = 8;
};

public struct ENetChannel
{
    public uint OutgoingReliableSequenceNumber;
    public uint OutgoingUnreliableSequenceNumber;
    public uint UsedReliableWindows;
    public uint[ENet.PeerReliableWindows] ReliableWindows;
    public uint IncomingReliableSequenceNumber;
    public uint IncomingUnreliableSequenceNumber;
    public ENetList IncomingReliableCommands;
    public ENetList IncomingUnreliableCommands;
}

[Flags]
public enum ENetPeerFlag
{
    NeedsDispatch = (1 << 0),
}

/// <summary>
/// An ENet peer which data packets may be sent or received from.
/// <para>
/// No fields should be modified unless otherwise specified.
/// </para>
/// </summary>
public partial class ENetPeer
{ 
    public LinkedListNode<TODO> DispatchList;
    public ENetHost Host;
    public ushort OutgoingPeerId;
    public ushort IncomingPeerId;
    public uint ConnectId;
    public byte OutgoingSessionId;
    public byte IncomingSessionId;
    /// <summary>
    /// Internet address of the peer.
    /// </summary>
    public ENetAddress Address;
    /// <summary>
    /// Application private data, may be freely modified.
    /// </summary>
    public object? Data;
    public ENetPeerState State;
    public ENetChannel[] Channels;
    /// <summary>
    /// Number of channels allocated for communication with peer.
    /// </summary>
    public size_t ChannelCount;
    /// <summary>
    /// Downstream bandwidth of the client in bytes/second.
    /// </summary>
    public enet_uint32 IncomingBandwidth;
    /// <summary>
    /// Upstream bandwidth of the client in bytes/second.
    /// </summary>
    public enet_uint32 OutgoingBandwidth;
    public enet_uint32 IncomingBandwidthThrottleEpoch;
    public enet_uint32 OutgoingBandwidthThrottleEpoch;
    public enet_uint32 IncomingDataTotal;
    public enet_uint32 OutgoingDataTotal;
    public enet_uint32 LastSendTime;
    public enet_uint32 LastReceiveTime;
    public enet_uint32 NextTimeout;
    public enet_uint32 EarliestTimeout;
    public enet_uint32 PacketLossEpoch;
    public enet_uint32 PacketsSent;
    public enet_uint32 PacketsLost;
    /// <summary>
    /// Mean packet loss of reliable packets as a ratio with respect to the constant ENET_PEER_PACKET_LOSS_SCALE.
    /// </summary>
    public enet_uint32 PacketLoss;
    public enet_uint32 PacketLossVariance;
    public enet_uint32 PacketThrottle;
    public enet_uint32 PacketThrottleLimit;
    public enet_uint32 PacketThrottleCounter;
    public enet_uint32 PacketThrottleEpoch;
    public enet_uint32 PacketThrottleAcceleration;
    public enet_uint32 PacketThrottleDeceleration;
    public TimeSpan PacketThrottleInterval;
    public enet_uint32 PingInterval;
    public enet_uint32 TimeoutLimit;
    public enet_uint32 TimeoutMinimum;
    public enet_uint32 TimeoutMaximum;
    public enet_uint32 LastRoundTripTime;
    public enet_uint32 LowestRoundTripTime;
    public enet_uint32 LastRoundTripTimeVariance;
    public enet_uint32 HighestRoundTripTimeVariance;
    /// <summary>
    /// Mean round trip time (RTT), in milliseconds, between sending a reliable packet and receiving its acknowledgement.
    /// </summary>
    public enet_uint32 RoundTripTime;
    public enet_uint32 RoundTripTimeVariance;
    public enet_uint32 Mtu;
    public enet_uint32 WindowSize;
    public enet_uint32 ReliableDataInTransit;
    public enet_uint16 OutgoingReliableSequenceNumber;
    public LinkedList<TODO> Acknowledgements;
    public LinkedList<TODO> SentReliableCommands;
    public LinkedList<ENetOutgoingCommand> SentUnreliableCommands;
    public LinkedList<TODO> OutgoingCommands;
    public LinkedList<TODO> DispatchedCommands;
    public enet_uint16 Flags;
    public enet_uint16 Reserved;
    public enet_uint16 IncomingUnsequencedGroup;
    public enet_uint16 OutgoingUnsequencedGroup;
    public enet_uint32 UnsequencedWindow [ENET_PEER_UNSEQUENCED_WINDOW_SIZE / 32]; 
    public enet_uint32 EventData;
    public size_t TotalWaitingData;
}

/// <summary>
/// An ENet packet compressor for compressing UDP packets before socket sends or receives.
/// </summary>
public struct ENetCompressor
{
    /// <summary>
    /// Context data for the compressor. Must be non-<see langword="null"/>.
    /// </summary>
    void * context;

    /// <summary>
    /// Compresses from inBuffers[0:inBufferCount-1], containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure.
    /// </summary>
    size_t (ENET_CALLBACK * compress) (void * context, const ENetBuffer * inBuffers, size_t inBufferCount, size_t inLimit, enet_uint8 * outData, size_t outLimit);

    /// <summary>
    /// Decompresses from inData, containing inLimit bytes, to outData, outputting at most outLimit bytes. Should return 0 on failure.
    /// </summary>
    size_t (ENET_CALLBACK * decompress) (void * context, const enet_uint8 * inData, size_t inLimit, enet_uint8 * outData, size_t outLimit);

    /// <summary>
    /// Destroys the context when compression is disabled or the host is destroyed. May be <see langword="null"/>.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    void (ENET_CALLBACK * destroy) (void * context);
}

/// <summary>
/// Callback that computes the checksum of the data held in buffers[0:bufferCount-1].
/// </summary>
typedef enet_uint32 (ENET_CALLBACK * ENetChecksumCallback) (const ENetBuffer * buffers, size_t bufferCount);

/** Callback for intercepting received raw UDP packets. Should return 1 to intercept, 0 to ignore, or -1 to propagate an error. */
typedef int (ENET_CALLBACK * ENetInterceptCallback) (struct _ENetHost * host, struct _ENetEvent * event);
 
/** An ENet host for communicating with peers.
  *
  * No fields should be modified unless otherwise stated.

     @sa enet_host_create()
     @sa enet_host_destroy()
     @sa enet_host_connect()
     @sa enet_host_service()
     @sa enet_host_flush()
     @sa enet_host_broadcast()
     @sa enet_host_compress()
     @sa enet_host_compress_with_range_coder()
     @sa enet_host_channel_limit()
     @sa enet_host_bandwidth_limit()
     @sa enet_host_bandwidth_throttle()
  */
public class ENetHost
{
    public ENetSocket Socket;
    public ENetAddress Address;                     /**< Internet address of the host */
    public uint IncomingBandwidth;           /**< downstream bandwidth of the host */
    public uint OutgoingBandwidth;           /**< upstream bandwidth of the host */
    public uint BandwidthThrottleEpoch;
    public uint Mtu;
    public uint RandomSeed;
    public bool RecalculateBandwidthLimits;
    public ENetPeer[] Peers;                       /**< array of peers allocated for this host */
    public size_t PeerCount;                   /**< number of peers allocated for this host */
    public size_t ChannelLimit;                /**< maximum number of channels allowed for connected peers */
    public uint ServiceTime;
    public LinkedList<TODO> DispatchQueue;
    public int ContinueSending;
    public size_t PacketSize;
    public ushort HeaderFlags;
    public ENetProtocol Commands [ENET_PROTOCOL_MAXIMUM_PACKET_COMMANDS];
    public size_t CommandCount;
    public ENetBuffer Buffers [ENET_BUFFER_MAXIMUM];
    public size_t BufferCount;
    public ENetChecksumCallback? Checksum;                    /**< callback the user can set to enable packet checksums for this host */
    public ENetCompressor Compressor;
    public byte PacketData [2][ENET_PROTOCOL_MAXIMUM_MTU];
    public ENetAddress ReceivedAddress;
    public byte * ReceivedData;
    public size_t ReceivedDataLength;
    public uint TotalSentData;               /**< total data sent, user should reset to 0 as needed to prevent overflow */
    public uint TotalSentPackets;            /**< total UDP packets sent, user should reset to 0 as needed to prevent overflow */
    public uint TotalReceivedData;           /**< total data received, user should reset to 0 as needed to prevent overflow */
    public uint TotalReceivedPackets;        /**< total UDP packets received, user should reset to 0 as needed to prevent overflow */
    public ENetInterceptCallback Intercept;                  /**< callback the user can set to intercept received raw UDP packets */
    public size_t ConnectedPeers;
    public size_t BandwidthLimitedPeers;
    public size_t DuplicatePeers;              /**< optional number of allowed peers from duplicate IPs, defaults to ENET_PROTOCOL_MAXIMUM_PEER_ID */
    public size_t MaximumPacketSize;           /**< the maximum allowable packet size that may be sent or received on a peer */
    public size_t MaximumWaitingData;          /**< the maximum aggregate amount of buffer space a peer may use waiting for packets to be delivered */
}

/// <summary>
/// An ENet event type, as specified in <see cref="ENetEvent"/>.
/// </summary>
public enum ENetEventType
{
    /// <summary>
    /// No event occurred within the specified time limit.
    /// </summary>
    None = 0,

    /// <summary>
    /// A connection request initiated by enet_host_connect has completed.
    /// The peer field contains the peer which successfully connected.
    /// </summary>
    Connect = 1,

    /// <summary>
    /// A peer has disconnected.  This event is generated on a successful
    /// completion of a disconnect initiated by enet_peer_disconnect, if
    /// a peer has timed out, or if a connection request intialized by
    /// enet_host_connect has timed out.  The peer field contains the peer
    /// which disconnected. The data field contains user supplied data
    /// describing the disconnection, or 0, if none is available.
    /// </summary>
    Disconnect = 2,

    /// <summary>
    /// A packet has been received from a peer.  The peer field specifies the
    /// peer which sent the packet.  The channelID field specifies the channel
    /// number upon which the packet was received.  The packet field contains
    /// the packet that was received; this packet must be destroyed with
    /// enet_packet_destroy after use.
    /// </summary>
    Receive = 3,
}

/// <summary>
/// An ENet event as returned by enet_host_service().
/// </summary>
/// <see cref="enet_host_service"/>
public struct ENetEvent 
{
    /// <summary>
    /// Type of the event.
    /// </summary>
    public ENetEventType Type;

    /// <summary>
    /// Peer that generated a connect, disconnect or receive event.
    /// </summary>
    public ENetPeer Peer;

    /// <summary>
    /// Channel on the peer that generated the event, if appropriate.
    /// </summary>
    public byte ChannelID;

    /// <summary>
    /// Data associated with the event, if appropriate.
    /// </summary>
    public int Data;

    /// <summary>
    /// Packet associated with the event, if appropriate.
    /// </summary>
    public ENetPacket Packet;
}

/** 
  Initializes ENet globally and supplies user-overridden callbacks. Must be called prior to using any functions in ENet. Do not use enet_initialize() if you use this variant. Make sure the ENetCallbacks structure is zeroed out so that any additional callbacks added in future versions will be properly ignored.

  @param version the constant ENET_VERSION should be supplied so ENet knows which version of ENetCallbacks struct to use
  @param inits user-overridden callbacks where any NULL callbacks will use ENet's defaults
  @returns 0 on success, < 0 on failure
*/
ENET_API int enet_initialize_with_callbacks (ENetVersion version, const ENetCallbacks * inits);

/**
  Gives the linked version of the ENet library.
  @returns the version number 
*/
ENET_API ENetVersion enet_linked_version (void);

/** @} */

/** @defgroup private ENet private implementation functions */

/**
  Returns the wall-time in milliseconds.  Its initial value is unspecified
  unless otherwise set.
  */
ENET_API enet_uint32 enet_time_get (void);
/**
  Sets the current wall-time in milliseconds.
  */
ENET_API void enet_time_set (enet_uint32);

/** @defgroup socket ENet socket functions
     @{
*/
ENET_API ENetSocket enet_socket_create (ENetSocketType);
ENET_API int        enet_socket_bind (ENetSocket, const ENetAddress *);
ENET_API int        enet_socket_get_address (ENetSocket, ENetAddress *);
ENET_API int        enet_socket_listen (ENetSocket, int);
ENET_API ENetSocket enet_socket_accept (ENetSocket, ENetAddress *);
ENET_API int        enet_socket_connect (ENetSocket, const ENetAddress *);
ENET_API int        enet_socket_send (ENetSocket, const ENetAddress *, const ENetBuffer *, size_t);
ENET_API int        enet_socket_receive (ENetSocket, ENetAddress *, ENetBuffer *, size_t);
ENET_API int        enet_socket_wait (ENetSocket, enet_uint32 *, enet_uint32);
ENET_API int        enet_socket_set_option (ENetSocket, ENetSocketOption, int);
ENET_API int        enet_socket_get_option (ENetSocket, ENetSocketOption, int *);
ENET_API int        enet_socket_shutdown (ENetSocket, ENetSocketShutdown);
ENET_API void       enet_socket_destroy (ENetSocket);
ENET_API int        enet_socketset_select (ENetSocket, ENetSocketSet *, ENetSocketSet *, enet_uint32);

/** @} */

/** @defgroup Address ENet address functions
     @{
*/

/** Attempts to parse the printable form of the IP address in the parameter hostName
     and sets the host field in the address parameter if successful.
     @param address destination to store the parsed IP address
     @param hostName IP address to parse
     @retval 0 on success
     @retval < 0 on failure
     @returns the address of the given hostName in address on success
*/
ENET_API int enet_address_set_host_ip (ENetAddress * address, const char * hostName);

/** Attempts to resolve the host named by the parameter hostName and sets
     the host field in the address parameter if successful.
     @param address destination to store resolved address
     @param hostName host name to lookup
     @retval 0 on success
     @retval < 0 on failure
     @returns the address of the given hostName in address on success
*/
ENET_API int enet_address_set_host (ENetAddress * address, const char * hostName);

/** Gives the printable form of the IP address specified in the address parameter.
     @param address    address printed
     @param hostName   destination for name, must not be NULL
     @param nameLength maximum length of hostName.
     @returns the null-terminated name of the host in hostName on success
     @retval 0 on success
     @retval < 0 on failure
*/
ENET_API int enet_address_get_host_ip (const ENetAddress * address, char * hostName, size_t nameLength);

/** Attempts to do a reverse lookup of the host field in the address parameter.
     @param address    address used for reverse lookup
     @param hostName   destination for name, must not be NULL
     @param nameLength maximum length of hostName.
     @returns the null-terminated name of the host in hostName on success
     @retval 0 on success
     @retval < 0 on failure
*/
ENET_API int enet_address_get_host (const ENetAddress * address, char * hostName, size_t nameLength);

/** @} */

ENET_API ENetPacket * enet_packet_create (const void *, size_t, enet_uint32);
ENET_API void         enet_packet_destroy (ENetPacket *);
ENET_API int          enet_packet_resize  (ENetPacket *, size_t);
ENET_API enet_uint32  enet_crc32 (const ENetBuffer *, size_t);
                     
ENET_API ENetHost * enet_host_create (const ENetAddress *, size_t, size_t, enet_uint32, enet_uint32);
ENET_API void       enet_host_destroy (ENetHost *);
ENET_API ENetPeer * enet_host_connect (ENetHost *, const ENetAddress *, size_t, enet_uint32);
ENET_API int        enet_host_check_events (ENetHost *, ENetEvent *);
ENET_API int        enet_host_service (ENetHost *, ENetEvent *, enet_uint32);
ENET_API void       enet_host_flush (ENetHost *);
ENET_API void       enet_host_broadcast (ENetHost *, enet_uint8, ENetPacket *);
ENET_API void       enet_host_compress (ENetHost *, const ENetCompressor *);
ENET_API int        enet_host_compress_with_range_coder (ENetHost * host);
ENET_API void       enet_host_channel_limit (ENetHost *, size_t);
ENET_API void       enet_host_bandwidth_limit (ENetHost *, enet_uint32, enet_uint32);
extern   void       enet_host_bandwidth_throttle (ENetHost *);
extern  enet_uint32 enet_host_random_seed (void);

ENET_API int                 enet_peer_send (ENetPeer *, enet_uint8, ENetPacket *);
ENET_API ENetPacket *        enet_peer_receive (ENetPeer *, enet_uint8 * channelID);
ENET_API void                enet_peer_ping (ENetPeer *);
ENET_API void                enet_peer_ping_interval (ENetPeer *, enet_uint32);
ENET_API void                enet_peer_timeout (ENetPeer *, enet_uint32, enet_uint32, enet_uint32);
ENET_API void                enet_peer_reset (ENetPeer *);
ENET_API void                enet_peer_disconnect (ENetPeer *, enet_uint32);
ENET_API void                enet_peer_disconnect_now (ENetPeer *, enet_uint32);
ENET_API void                enet_peer_disconnect_later (ENetPeer *, enet_uint32);
ENET_API void                enet_peer_throttle_configure (ENetPeer *, enet_uint32, enet_uint32, enet_uint32);
extern int                   enet_peer_throttle (ENetPeer *, enet_uint32);
extern void                  enet_peer_reset_queues (ENetPeer *);
extern void                  enet_peer_setup_outgoing_command (ENetPeer *, ENetOutgoingCommand *);
extern ENetOutgoingCommand * enet_peer_queue_outgoing_command (ENetPeer *, const ENetProtocol *, ENetPacket *, enet_uint32, enet_uint16);
extern ENetIncomingCommand * enet_peer_queue_incoming_command (ENetPeer *, const ENetProtocol *, const void *, size_t, enet_uint32, enet_uint32);
extern ENetAcknowledgement * enet_peer_queue_acknowledgement (ENetPeer *, const ENetProtocol *, enet_uint16);
extern void                  enet_peer_dispatch_incoming_unreliable_commands (ENetPeer *, ENetChannel *, ENetIncomingCommand *);
extern void                  enet_peer_dispatch_incoming_reliable_commands (ENetPeer *, ENetChannel *, ENetIncomingCommand *);
extern void                  enet_peer_on_connect (ENetPeer *);
extern void                  enet_peer_on_disconnect (ENetPeer *);

ENET_API void * enet_range_coder_create (void);
ENET_API void   enet_range_coder_destroy (void *);
ENET_API size_t enet_range_coder_compress (void *, const ENetBuffer *, size_t, size_t, enet_uint8 *, size_t);
ENET_API size_t enet_range_coder_decompress (void *, const enet_uint8 *, size_t, enet_uint8 *, size_t);
    
extern size_t enet_protocol_command_size (enet_uint8);

#ifdef __cplusplus
}
#endif

#endif /* __ENET_ENET_H__ */

