using System.Runtime.InteropServices;

namespace ENetDotNet;

partial struct ENetProtocol {
    public const int MinimumMtu = 576;
    public const int MaximumMtu = 4096;
    public const int MaximumPacketCommands = 32;
    public const int MinimumWindowSize = 4096;
    public const int MaximumWindowSize = 65536;
    public const int MinimumChannelCount = 1;
    public const int MaximumChannelCount = 255;
    public const int MaximumPeerId = 0xFFF;
    public const int MaximumFragmentCount = 1024 * 1024;
};

public enum ENetProtocolCommand
{
    None = 0,
    Acknowledge = 1,
    Connect = 2,
    VerifyConnect = 3,
    Disconnect = 4,
    Ping = 5,
    SendReliable = 6,
    SendUnreliable = 7,
    SendFragment = 8,
    SendUnsequenced = 9,
    BandwidthLimit = 10,
    ThrottleConfigure = 11,
    SendUnreliableFragment = 12,
    Count = 13,

    Mask = 0x0F,
}

[Flags]
public enum ENetProtocolFlag
{
    COMMAND_ACKNOWLEDGE = (1 << 7),
    COMMAND_UNSEQUENCED = (1 << 6),

    HEADER_COMPRESSED = (1 << 14),
    HEADER_SENT_TIME  = (1 << 15),
    HEADER_MASK       = HEADER_COMPRESSED | HEADER_SENT_TIME,

    HEADER_SESSION_MASK    = (3 << 12),
    HEADER_SESSION_SHIFT   = 12
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolHeader
{
    public ushort PeerID;
    public ushort SentTime;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolCommandHeader
{
    public ENetProtocolCommand Command;
    public byte ChannelId;
    public ushort ReliableSequenceNumber;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolAcknowledge
{
    public ENetProtocolCommandHeader Header;
    public ushort ReceivedReliableSequenceNumber;
    public ushort ReceivedSentTime;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolConnect
{
    public ENetProtocolCommandHeader Header;
    public ushort OutgoingPeerID;
    public byte IncomingSessionID;
    public byte OutgoingSessionID;
    public uint Mtu;
    public uint WindowSize;
    public uint ChannelCount;
    public uint IncomingBandwidth;
    public uint OutgoingBandwidth;
    public uint PacketThrottleInterval;
    public uint PacketThrottleAcceleration;
    public uint PacketThrottleDeceleration;
    public uint ConnectID;
    public uint Data;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolVerifyConnect
{
    public ENetProtocolCommandHeader Header;
    public ushort OutgoingPeerID;
    public byte IncomingSessionID;
    public byte OutgoingSessionID;
    public uint Mtu;
    public uint WindowSize;
    public uint ChannelCount;
    public uint IncomingBandwidth;
    public uint OutgoingBandwidth;
    public uint PacketThrottleInterval;
    public uint PacketThrottleAcceleration;
    public uint PacketThrottleDeceleration;
    public uint ConnectID;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolBandwidthLimit
{
    public ENetProtocolCommandHeader Header;
    public uint IncomingBandwidth;
    public uint OutgoingBandwidth;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolThrottleConfigure
{
    public ENetProtocolCommandHeader Header;
    public uint PacketThrottleInterval;
    public uint PacketThrottleAcceleration;
    public uint PacketThrottleDeceleration;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolDisconnect
{
    public ENetProtocolCommandHeader Header;
    public uint Data;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolPing
{
    public ENetProtocolCommandHeader Header;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolSendReliable
{
    public ENetProtocolCommandHeader Header;
    public ushort DataLength;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolSendUnreliable
{
    public ENetProtocolCommandHeader Header;
    public ushort UnreliableSequenceNumber;
    public ushort DataLength;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolSendUnsequenced
{
    public ENetProtocolCommandHeader Header;
    public ushort UnsequencedGroup;
    public ushort DataLength;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ENetProtocolSendFragment
{
    public ENetProtocolCommandHeader Header;
    public ushort StartSequenceNumber;
    public ushort DataLength;
    public uint FragmentCount;
    public uint FragmentNumber;
    public uint TotalLength;
    public uint FragmentOffset;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public partial struct ENetProtocol
{
    [FieldOffset(0)]
    public ENetProtocolCommandHeader Header;
    [FieldOffset(0)]
    public ENetProtocolAcknowledge Acknowledge;
    [FieldOffset(0)]
    public ENetProtocolConnect Connect;
    [FieldOffset(0)]
    public ENetProtocolVerifyConnect VerifyConnect;
    [FieldOffset(0)]
    public ENetProtocolDisconnect Disconnect;
    [FieldOffset(0)]
    public ENetProtocolPing Ping;
    [FieldOffset(0)]
    public ENetProtocolSendReliable SendReliable;
    [FieldOffset(0)]
    public ENetProtocolSendUnreliable SendUnreliable;
    [FieldOffset(0)]
    public ENetProtocolSendUnsequenced SendUnsequenced;
    [FieldOffset(0)]
    public ENetProtocolSendFragment SendFragment;
    [FieldOffset(0)]
    public ENetProtocolBandwidthLimit BandwidthLimit;
    [FieldOffset(0)]
    public ENetProtocolThrottleConfigure ThrottleConfigure;
}
