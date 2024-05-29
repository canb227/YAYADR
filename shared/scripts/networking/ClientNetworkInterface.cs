using static Steamworks.SteamNetworkingSockets;
using Godot;
using Google.Protobuf;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

public partial class ClientNetworkInterface : Node
{

    //Networking config settings
    public int nMaxMessagesReceivedPerFrame { get; set; } = 100;


    //State vars
    public bool isJoinable = false;
    public bool isConnected = false;

    //Networking Vars
    public HSteamNetConnection connectionToServer = new HSteamNetConnection();

    public List<ulong> peers = new List<ulong>();

    public ulong serverID = 0;


    //Events
    public delegate void ChatMessageEventHandler(ChatPacket chatPacket);
    public event ChatMessageEventHandler ChatMessageEvent;

    public delegate void PlayerJoinEventHandler(ulong steamID);
    public event PlayerJoinEventHandler PlayerJoinEvent;

    public delegate void PlayerLeaveEventHandler(ulong steamID);
    public event PlayerLeaveEventHandler PlayerLeaveEvent;

    public delegate void JoinedServerEventHandler(ulong serverID);
    public event JoinedServerEventHandler JoinedServerEvent;

    public delegate void PeersUpdatedEventHandler(ulong[] peers);
    public event PeersUpdatedEventHandler PeersUpdatedEvent;

    public delegate void LeftServerEventHandler();
    public event LeftServerEventHandler LeftServerEvent;

    public delegate void AdminCommandEventHandler(AdminPacket adminPacket);
    public event AdminCommandEventHandler AdminCommandEvent;

    //Steam Callbacks
    protected Callback<GameRichPresenceJoinRequested_t> GameRichPresenceJoinRequested;
    protected Callback<SteamNetConnectionStatusChangedCallback_t> SteamNetConnectionStatusChanged;


    public ClientNetworkInterface() { }
    


    public void LeaveServer()
    {
        CloseConnection(connectionToServer,0,"guh",true);
        connectionToServer = HSteamNetConnection.Invalid;
        peers.Clear();
        isConnected = false;
        isJoinable = false;
        serverID = 0;
        LeftServerEvent.Invoke();
    }


    public void ConnectToRemoteServer(ulong serverSteamID)
    {
        Global.PrintDebug("Attempting join remote server with ID: " + serverSteamID);
        SteamNetworkingIdentity serverID = new SteamNetworkingIdentity();
        serverID.SetSteamID64(serverSteamID);
        connectionToServer = ConnectP2P(ref serverID, 0, 0, null);
        this.serverID = serverSteamID;
        NetworkUtils.ConfigureConnectionLanes(connectionToServer);
        isConnected = true;
    }



    public override void _Ready()
    {
        Global.PrintDebug("Client Network Interface created and on scene tree.");
        GameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(onGameRichPresenceJoinRequested);
        SteamNetConnectionStatusChanged = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(onSteamNetConnectionStatusChanged);
    }

    private void onSteamNetConnectionStatusChanged(SteamNetConnectionStatusChangedCallback_t param)
    {
        ulong cID = param.m_info.m_identityRemote.GetSteamID64();
        Global.PrintDebug("Connection status with server has changed to: " + param.m_info.m_eState);
        switch (param.m_info.m_eState)
        {
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_FindingRoute:
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                /*
                SteamFriends.SetRichPresence("connect", cID.ToString());
                isJoinable = true;
                isConnected = true;
                JoinedServerEvent.Invoke(cID);
                Global.PrintDebug("Connected to remote server. Host Steam ID: " + cID);
                */
                break;
            case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
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


    private void onGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t param)
    {
        Global.PrintDebug("RICH PRESENCE JOIN REQUEST: "  + param.m_rgchConnect);
    }

    public override void _Process(double delta)
    {
        if (!isConnected) { return; }
        //Create and allocate memory for an array of pointers
        IntPtr[] messages = new IntPtr[nMaxMessagesReceivedPerFrame];

        //Collect up to nMaxMessages that are waiting in the queue on the connection to the server, and load them up into our preallocated message array
        int numMessages = ReceiveMessagesOnConnection(connectionToServer, messages, nMaxMessagesReceivedPerFrame);
        if (numMessages == -1)
        {
            Global.PrintCriticalError("INVALID CONNECTION HANDLE TO SERVER");
        }
        //Global.PrintDebug("Checking for msgs from server");
        //For each message, send it off to further processing
        for (int i = 0; i < numMessages; i++)
        {
            if (messages[i] == IntPtr.Zero) { continue; } //Sanity check. 
            Global.PrintDebug("Hep,I got a message from the server.");
            SteamNetworkingMessage_t steamMsg = SteamNetworkingMessage_t.FromIntPtr(messages[i]);
            switch (steamMsg.m_idxLane)
            {
                case (ushort)NetworkUtils.NetworkingLanes.LANE_ADMIN:
                    AdminPacket adminPacket = AdminPacket.Parser.ParseFrom(NetworkUtils.IntPtrToBytes(steamMsg.m_pData, steamMsg.m_cbSize));
                    Global.PrintDebug("I have received an Admin message.");
                    AdminCommandEvent.Invoke(adminPacket);
                    break;
                case (ushort)NetworkUtils.NetworkingLanes.LANE_DIAGNOSTIC:
                    Global.PrintDebug("I have received a Diagnostic message");
                    break;
                case (ushort)NetworkUtils.NetworkingLanes.LANE_CHAT:
                    ChatPacket chatPacket = ChatPacket.Parser.ParseFrom(NetworkUtils.IntPtrToBytes(steamMsg.m_pData, steamMsg.m_cbSize));
                    Global.PrintDebug("I have received a Chat message, emitting internal event.");
                    ChatMessageEvent.Invoke(chatPacket);
                    break;
                case (ushort)NetworkUtils.NetworkingLanes.LANE_STATE:
                    Global.PrintDebug("I have received a State message.");
                    break;
                case (ushort)NetworkUtils.NetworkingLanes.LANE_HANDSHAKE:
                    HandshakePacket handshakePacket = HandshakePacket.Parser.ParseFrom(NetworkUtils.IntPtrToBytes(steamMsg.m_pData, steamMsg.m_cbSize));
                    Global.PrintDebug("I have received a Handshake message.", true);
                    if (handshakePacket.HasPeerLeft)
                    {
                        PlayerLeaveEvent.Invoke(handshakePacket.PeerLeft);
                    }
                    if (handshakePacket.HasPeerJoined)
                    {
                        PlayerJoinEvent.Invoke(handshakePacket.PeerJoined);
                    }
                    if (handshakePacket.Peers.Count > 0)
                    {
                        peers = handshakePacket.Peers.ToList<ulong>();
                        PeersUpdatedEvent.Invoke(peers.ToArray());
                    }
                    break;
                default:
                    Global.PrintError("UNKNOWN LANE??: " + steamMsg.m_idxLane, true);
                    break;
            }
        }
        }

    /// <summary>
    /// Sends a message using the SteamNetworkingSockets library. In theory, this should be agnostic to steam vs non-steam networking.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="target"></param>
    /// <param name="data"></param>
    /// <param name="sendFlags"></param>
    /// <returns></returns>
    public bool SendSteamMessageToServer(IMessage message, ushort lane, int sendFlags = NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle)
    {
        Global.PrintDebug("Sending SteamMessage to server.");
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
            msg.m_conn = connectionToServer;
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

    public void SendChatMessage(string message)
    {
        ChatPacket chatPacket = new ChatPacket();
        GetIdentity(out SteamNetworkingIdentity id);
        chatPacket.Sender = id.GetSteamID64();
        chatPacket.Text = message;
        SendSteamMessageToServer(chatPacket, (ushort)NetworkUtils.NetworkingLanes.LANE_CHAT, NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle);
    }


}

