using static Steamworks.SteamNetworkingSockets;
using Godot;
using Google.Protobuf;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public partial class ServerNetworkInterface : Node
{

    //Networking config settings
    public int nMaxMessagesReceivedPerFrame { get; set; } = 100;


    //State vars
    public bool isJoinable = false;
    public bool isConnected = false;

    //Networking Vars

    public List<HSteamNetConnection> connectionsToClients = new List<HSteamNetConnection>();

    public HSteamListenSocket listenSocket;
 
    private bool acceptAllConnections = false;

    //Events
    public delegate void ChatMessageEventHandler(ChatPacket chatPacket);
    public event ChatMessageEventHandler ChatMessageEvent;

    public delegate void PlayerJoinEventHandler(ulong steamID);
    public event PlayerJoinEventHandler PlayerJoinEvent;

    public delegate void PlayerLeaveEventHandler(ulong steamID);
    public event PlayerLeaveEventHandler PlayerLeaveEvent;



    public delegate void PeersUpdatedEventHandler(ulong[] peers);
    public event PeersUpdatedEventHandler PeersUpdatedEvent;


    public delegate void ServerStartedEventHandler();
    public static event ServerStartedEventHandler ServerStartedEvent;

    public delegate void ServerStoppedEventHandler();
    public event ServerStoppedEventHandler ServerStoppedEvent;

    public delegate void AdminCommandEventHandler(AdminPacket adminPacket);
    public event AdminCommandEventHandler AdminCommandEvent;

    //Steam Callbacks
    protected Callback<GameRichPresenceJoinRequested_t> GameRichPresenceJoinRequested;
    protected Callback<SteamNetConnectionStatusChangedCallback_t> SteamNetConnectionStatusChanged;


    public ServerNetworkInterface() { }
    
    public void CloseServer()
    {
        CloseListenSocket(listenSocket);
        listenSocket = HSteamListenSocket.Invalid;
        connectionsToClients.Clear();
        isConnected = false;
        isJoinable = false;
        Global.isServer = false;
        ServerStoppedEvent.Invoke();
    }
    public void StartServer(int port = 0, bool online = true)
    {
        Global.isServer = true;
        if (online)
        {
            listenSocket = CreateListenSocketP2P(port, 0, null);
            SteamFriends.SetRichPresence("status","On the main menu");
            SteamFriends.SetRichPresence("steam_player_group", Global.steamID.ToString());
            SteamFriends.SetRichPresence("steam_player_group_size", "1");
            SteamFriends.SetRichPresence("connect", Global.steamID.ToString());
            isJoinable = true;
            Global.PrintDebug("Online Steam server started, server is joinable.",true);
        }
        Global.instance.EstablishLoopbackConnection();
        isConnected = true;
        ServerStartedEvent.Invoke();
        Global.PrintDebug("Internal server and loopback connection setup complete. Ready to start game.", true);
    }



    public override void _Ready()
    {
        Global.PrintDebug("Server Network Interface created and on scene tree.");
        SteamNetConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(onSteamNetConnectionStatusChanged);
    }

    private void onSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
    {
        HandshakePacket _handshakePacket;
        ulong cID = param.m_info.m_identityRemote.GetSteamID64();

        Global.PrintDebug("Connection status with client: " + cID + "has changed to: " + param.m_info.m_eState, true);

        switch (param.m_info.m_eState)
        {
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                if (acceptAllConnections || ValidateJoinAttempt(param))
                {
                    Global.PrintDebug("Client # " + cID + " attempting connection, accepting request.",true);
                    Global.PrintDebug(SteamNetworkingSockets.AcceptConnection(param.m_hConn).ToString());


                }
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute:
                break;

            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                Global.PrintDebug("Client # " + cID + " sucessfully connected.",true);

                _handshakePacket = new HandshakePacket();
                _handshakePacket.SentTimestamp = Time.GetUnixTimeFromSystem();
                _handshakePacket.Sender = NetworkUtils.GetSelfSteamID();
                //_handshakePacket.Peers.AddRange(connectionsToClients.Keys);
                SendSteamMessage(param.m_hConn, _handshakePacket, (ushort)NetworkUtils.NetworkingLanes.LANE_HANDSHAKE, NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle);

                connectionsToClients.Add(param.m_hConn);

                Global.PrintDebug(param.m_hConn.ToString());
                _handshakePacket = new HandshakePacket();
                _handshakePacket.SentTimestamp = Time.GetUnixTimeFromSystem();
                _handshakePacket.Sender = NetworkUtils.GetSelfSteamID();
                _handshakePacket.PeerJoined = cID;
                BroadcastMessageWithException(param.m_hConn, _handshakePacket, (ushort)NetworkUtils.NetworkingLanes.LANE_HANDSHAKE, NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle);
                break;

            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                Global.PrintDebug("Client # " + cID + " disconnected.", true);
                connectionsToClients.Remove(param.m_hConn);
                _handshakePacket = new HandshakePacket();
                _handshakePacket.SentTimestamp = Time.GetUnixTimeFromSystem();
                _handshakePacket.Sender = NetworkUtils.GetSelfSteamID();
                _handshakePacket.PeerLeft = cID;
                BroadcastMessage(_handshakePacket, (ushort)NetworkUtils.NetworkingLanes.LANE_HANDSHAKE);
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FinWait:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Linger:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Dead:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState__Force32Bit:
                break;
            default:
                break;
            
        }
    }

    private bool ValidateJoinAttempt(SteamNetConnectionStatusChangedCallback_t param)
    {
        if (SteamFriends.GetFriendRelationship((CSteamID)param.m_info.m_identityRemote.GetSteamID64()).Equals(EFriendRelationship.k_EFriendRelationshipFriend))
        {
            return true;
        }
        else { return false; }
    }

    public override void _Process(double delta)
    {
        foreach (HSteamNetConnection connection in connectionsToClients)
        {
            //Create and allocate memory for an array of pointers
            IntPtr[] messages = new IntPtr[nMaxMessagesReceivedPerFrame];

            //Collect up to nMaxMessages that are waiting in the queue on the connection to the server, and load them up into our preallocated message array
            int numMessages = SteamNetworkingSockets.ReceiveMessagesOnConnection(connection, messages, nMaxMessagesReceivedPerFrame);
            if (numMessages == -1)
            {
                Global.PrintCriticalError("INVALID CONNECTION HANDLE TO SERVER");
            }
            //For each message, send it off to further processing
            for (int i = 0; i < numMessages; i++)
            {
                if (messages[i] == IntPtr.Zero) { continue; } //Sanity check. 
                SteamNetworkingMessage_t steamMsg = SteamNetworkingMessage_t.FromIntPtr(messages[i]);
                switch (steamMsg.m_idxLane)
                {
                    case (ushort)NetworkUtils.NetworkingLanes.LANE_ADMIN:
                        AdminPacket adminPacket = AdminPacket.Parser.ParseFrom(NetworkUtils.IntPtrToBytes(steamMsg.m_pData, steamMsg.m_cbSize));
                        Global.PrintDebug("I have received an Admin message.", true);

                        break;
                    case (ushort)NetworkUtils.NetworkingLanes.LANE_DIAGNOSTIC:
                        Global.PrintDebug("I have received a Diagnostic message", true);
                        break;
                    case (ushort)NetworkUtils.NetworkingLanes.LANE_CHAT:
                        ChatPacket chatPacket = ChatPacket.Parser.ParseFrom(NetworkUtils.IntPtrToBytes(steamMsg.m_pData, steamMsg.m_cbSize));
                        Global.PrintDebug("I have received a Chat message, rebroadcasting.", true);
                        BroadcastMessage(chatPacket, (ushort)NetworkUtils.NetworkingLanes.LANE_CHAT, NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle);
                        break;
                    case (ushort)NetworkUtils.NetworkingLanes.LANE_STATE:
                        Global.PrintDebug("I have received a State message.", true);
                        break;
                    case (ushort)NetworkUtils.NetworkingLanes.LANE_HANDSHAKE:
                        Global.PrintDebug("I have received a Handshake message.", true);
                        HandshakePacket handshakePacket = HandshakePacket.Parser.ParseFrom(NetworkUtils.IntPtrToBytes(steamMsg.m_pData, steamMsg.m_cbSize));
                        break;
                    default:
                        Global.PrintError("UNKNOWN LANE??: " + steamMsg.m_idxLane);
                        break;
                }
            }
        }
    }

    public void BroadcastMessage(IMessage message, ushort lane, int sendFlags = NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle)
    {
        foreach (HSteamNetConnection c in connectionsToClients)
        {
            SendSteamMessage(c,message, lane, sendFlags);
        }
    }

    public void BroadcastMessageWithException(HSteamNetConnection exception, IMessage message, ushort lane, int sendFlags = NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle)
    {
        foreach (HSteamNetConnection c in connectionsToClients)
        {
            if (c.Equals(exception)) { continue; }
            SendSteamMessage(c, message, lane, sendFlags);
        }
    }

    public bool SendSteamMessage(HSteamNetConnection sendTo, IMessage message, ushort lane, int sendFlags = NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle)
    {
        Global.PrintDebug("Sending SteamMessage to client: " + sendTo.ToString(),true);
        var msgPtrsToSend = new IntPtr[] { IntPtr.Zero };
        var ptr = IntPtr.Zero;
        try
        {
            byte[] data = message.ToByteArray();
            ptr = SteamNetworkingUtils.AllocateMessage(data.Length);

            var msg = SteamNetworkingMessage_t.FromIntPtr(ptr);

            // Unfortunately, this allocates a managed SteamNetworkingMessage_t,
            // but the native message currently can't be edited via ptr, even with unsafe code
            Marshal.Copy(data, 0, msg.m_pData, data.Length);

            msg.m_nFlags = sendFlags;
            msg.m_idxLane = lane;
            msg.m_conn = sendTo;
            // Copies the bytes of the managed message back into the native structure located at ptr
            Marshal.StructureToPtr(msg, ptr, false);

            msgPtrsToSend[0] = ptr;
        }
        catch (Exception e)
        {
            // Callers only have responsibility to release the message until it's passed to SendMessages
            SteamNetworkingMessage_t.Release(ptr);
            return false;
        }

        var msgSendResult = new long[] { default };
        SteamNetworkingSockets.SendMessages(1, msgPtrsToSend, msgSendResult);
        EResult result = msgSendResult[0] >= 1 ? EResult.k_EResultOK : (EResult)(-msgSendResult[0]);

        return result == EResult.k_EResultOK;
    }


}

