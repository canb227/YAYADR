
using Steamworks;
using System;
using System.Runtime.InteropServices;

/// <summary>
/// 
/// </summary>
public static class NetworkUtils
{

    //Bitflags from the SteamAPI for sending messages - replicated here to make things easier.
    public const int k_nSteamNetworkingSend_NoNagle = 1;
    public const int k_nSteamNetworkingSend_NoDelay = 4;
    public const int k_nSteamNetworkingSend_Unreliable = 0;
    public const int k_nSteamNetworkingSend_Reliable = 8;
    public const int k_nSteamNetworkingSend_UnreliableNoNagle = k_nSteamNetworkingSend_Unreliable | k_nSteamNetworkingSend_NoNagle;
    public const int k_nSteamNetworkingSend_UnreliableNoDelay = k_nSteamNetworkingSend_Unreliable | k_nSteamNetworkingSend_NoDelay | k_nSteamNetworkingSend_NoNagle;
    public const int k_nSteamNetworkingSend_ReliableNoNagle = k_nSteamNetworkingSend_Reliable | k_nSteamNetworkingSend_NoNagle;

    /// <summary>
    /// 
    /// </summary>
    public enum NetworkingLanes : ushort
    {
        LANE_STATE = 0,

        LANE_DIAGNOSTIC = 1,
        LANE_CHAT = 2,
        LANE_ADMIN = 3,
        LANE_HANDSHAKE = 4,
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    public static void ConfigureConnectionLanes(HSteamNetConnection connection)
    {
        int laneCount = Enum.GetNames(typeof(NetworkingLanes)).Length;
        int[] lanePriorities = new int[laneCount];
        ushort[] laneWeights = new ushort[laneCount];
        for (int i = 0; i < laneCount; i++)
        {
            lanePriorities[i] = 0;
            laneWeights[i] = 0;
        }
        SteamNetworkingSockets.ConfigureConnectionLanes(connection, laneCount, null, null);
    }


    /// <summary>
    /// Dereferences a pointer to an array of bytes.
    /// </summary>
    /// <param name="ptr">Pointer to dereference</param>
    /// <param name="cbSize">The number of bytes to read, make sure to get this right</param>
    /// <returns>a raw array of bytes of length cbSize from pointer ptr</returns>
    public static byte[] IntPtrToBytes(IntPtr ptr, int cbSize)
    {
        byte[] retval = new byte[cbSize];
        Marshal.Copy(ptr, retval, 0, cbSize);
        return retval;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="remote"></param>
    /// <returns></returns>
    public static ulong getConnectionRemoteID(HSteamNetConnection remote)
    {
        SteamNetworkingSockets.GetConnectionInfo(remote, out SteamNetConnectionInfo_t info);
        return info.m_identityRemote.GetSteamID64();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="conn"></param>
    /// <returns></returns>
    public static ESteamNetworkingConnectionState GetConnectionState(HSteamNetConnection conn)
    {
        SteamNetworkingSockets.GetConnectionInfo(conn, out SteamNetConnectionInfo_t info);
        return info.m_eState;
    }

    public static ulong GetSelfSteamID()
    {
        SteamNetworkingSockets.GetIdentity(out SteamNetworkingIdentity id);
        return (ulong)id.GetSteamID64();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    internal static Identity GetSelfIdentity()
    {
        Identity identity = new Identity();
        identity.SteamID = Global.steamID;
        identity.Name = Global.steamName;
        return identity;
    }

 
} 

