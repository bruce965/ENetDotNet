using System.Buffers;
using System.Net;
using ENetDotNet.Internal;

namespace ENetDotNet;

public partial class ENetPacket {
    /// <summary>
    /// Creates a packet that may be sent to a peer.
    /// </summary>
    /// <param name="data">Initial contents of the packet's data; the packet's data will remain uninitialized if data is <see langword="null"/>.</param>
    /// <param name="dataLength">Size of the data allocated for this packet.</param>
    /// <param name="flags">Flags for this packet as described for the ENetPacket structure.</param>
    /// <returns>The packet on success, <see langword="null"/> on failure.</returns>
    public static ENetPacket Create(byte[]? data, int dataLength, ENetPacketFlag flags)
    {
        ENetPacket packet = Pool<ENetPacket>.Shared.Rent();
        packet.Initialize(data, dataLength, flags);
        return packet;
    }

    void Initialize(byte[]? data, int dataLength, ENetPacketFlag flags)
    {
        if (flags.HasFlag(ENetPacketFlag.NoAllocate))
            Data = data;
        else if (dataLength <= 0)
            Data = null;
        else
        {
            Data = ArrayPool<byte>.Shared.Rent(dataLength);
            data?.AsSpan(0, dataLength).CopyTo(Data.AsSpan());
        }

        _referenceCount = 0;
        Flags = flags;
        DataLength = dataLength;
        FreeCallback = null;
        UserData = null;
    }

    /// <summary>
    /// Destroys the packet and deallocates its data.
    /// </summary>
    public void Destroy() {
        FreeCallback?.Invoke(this);

        if (!Flags.HasFlag(ENetPacketFlag.NoAllocate) && Data is not null) {
            ArrayPool<byte>.Shared.Return(Data);
            Data = null;
        }

        Pool<ENetPacket>.Shared.Return(this);
    }

    /// <summary>
    /// Attempts to resize the data in the packet to length specified in the
    /// dataLength parameter.
    /// </summary>
    /// <param name="dataLength">New size for the packet data.</param>
    /// <returns><c>0</c> on success, <c>&lt; 0</c> on failure.</returns>
    public int Resize(int dataLength)
    {
        byte[] newData;
    
        if (dataLength <= DataLength || Flags.HasFlag(ENetPacketFlag.NoAllocate))
        {
            DataLength = dataLength;

            return 0;
        }

        newData = ArrayPool<byte>.Shared.Rent(dataLength);

        if (Data is not null) {
            Data.AsSpan(0, DataLength).CopyTo(newData);
            ArrayPool<byte>.Shared.Return(Data);
        }
        
        Data = newData;
        DataLength = dataLength;

        return 0;
    }
}

static class ENetCrc32 {
    static readonly int[] s_crcTable = InitializeCrc32();

    static int ReflectCrc(int val, int bits) {
        int result = 0;

        for (int bit = 0; bit < bits; bit++)
        {
            if ((val & 1) != 0) result |= 1 << (bits - 1 - bit); 
            val >>= 1;
        }

        return result;
    }

    static int[] InitializeCrc32() {
        int[] crcTable = new int[256];

        for (int b = 0; b < 256; ++b)
        {
            int crc = ReflectCrc(b, 8) << 24;
            int offset;

            for(offset = 0; offset < 8; ++ offset)
            {
                if ((crc & 0x80000000) != 0)
                    crc = (crc << 1) ^ 0x04c11db7;
                else
                    crc <<= 1;
            }

            crcTable[b] = ReflectCrc(crc, 32);
        }

        return crcTable;
    }

    public static uint Crc32(byte[][] buffers) {
        uint crc = 0xFFFFFFFF;
    
        for (int i = 0; i < buffers.Length; i++)
        {
            byte[] data = buffers[i];
            for (int j = 0; j < data.Length; j++)
                crc = unchecked((crc >> 8) ^ (uint)s_crcTable[(crc & 0xFF) ^ data[j]]);
        }

        return unchecked((uint)IPAddress.HostToNetworkOrder((int)~crc));
    }
}
