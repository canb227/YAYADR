using static Steamworks.SteamNetworkingSockets;
using static NetworkUtils;
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
    public HSteamNetConnection connectionToServer;

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
        Global.PrintDebug("Client - connection status change. New status: " + param.m_info.m_eState);
        if (param.m_info.m_eState.Equals(ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected))
        {
            isConnected = true;
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
        if (numMessages >0)
        {
            Global.PrintDebug("Hep,I got a message from the server.");
        }

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


    public void SendChatMessage(string message)
    {
        ChatPacket chatPacket = new ChatPacket();
        GetIdentity(out SteamNetworkingIdentity id);
        chatPacket.Sender = id.GetSteamID64();
        chatPacket.Text = message;
        SendSteamMessage(connectionToServer, chatPacket, (ushort)NetworkUtils.NetworkingLanes.LANE_CHAT, false, NetworkUtils.k_nSteamNetworkingSend_ReliableNoNagle);
    }


}

