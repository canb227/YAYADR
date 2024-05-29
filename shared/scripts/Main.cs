using Godot;
using Steamworks;
using System;

public partial class Main : Node
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Global.networkInterface.ChatMessageEvent += onChatMessage;
		Global.networkInterface.ServerStartedEvent += onServerStarted;
		Global.networkInterface.JoinedServerEvent += onJoinedServer;
        Global.networkInterface.AdminCommandEvent += onAdminCommandEvent;
		GetNode<Button>("host").Pressed += onStartOnlineServerPressed;
        GetNode<Button>("host2").Pressed += onStartOfflineServerPressed;
        GetNode<Button>("join").Pressed += onJoinServerPressed;
        GetNode<Button>("leave").Pressed += onLeaveServerPressed;
        GetNode<Button>("close").Pressed += onCloseServerPressed;
        GetNode<Button>("startmp").Pressed += onStartMultiplayerPressed;
        GetNode<Button>("startsp").Pressed += onStartSingleplayerPressed;
        GetNode<Button>("invite").Pressed += onInviteFriendPressed;

        GetNode<Button>("startmp").Disabled = true;
        GetNode<Button>("leave").Disabled = true;
        GetNode<Button>("close").Disabled = true;
    }

    private void onCloseServerPressed()
    {
        Global.networkInterface.CloseServer();
    }

    private void onLeaveServerPressed()
    {
        Global.networkInterface.LeaveServer();
    }

    private void onAdminCommandEvent(AdminPacket adminPacket)
    {
		switch (adminPacket.Command)
		{
			case "startgame":
				Global.PrintDebug("Its go time baby");
				break;
			default:
				break;
		}
	}

    private void onStartOfflineServerPressed()
    {
        Global.networkInterface.StartServer(9999,false);
    }

    private void onJoinedServer(ulong serverID)
    {
		GetNode<Button>("join").Disabled = true;
        GetNode<Button>("host").Disabled = true;

        if (Global.networkInterface.isJoinable)
		{
            GetNode<Label>("steamstatus").Text = "Joinable";
        }

		if (Global.networkInterface.isServer)
		{
            GetNode<Label>("status").Text = "Internal Server Online";
            GetNode<Button>("startmp").Disabled = false;
            GetNode<Button>("close").Disabled = false;
            GetNode<Button>("leave").Disabled = true;
        }
		else
		{
            GetNode<Label>("status").Text = "Internal Server Offline (Connected to remote)";
            GetNode<Button>("leave").Disabled = false;
        }

    }

    private void onServerStarted()
    {
        GetNode<Label>("steamstatus").Text = "Joinable";
        GetNode<Label>("status").Text = "Internal Server Online";
    }

    private void onStartSingleplayerPressed()
    {
        throw new NotImplementedException();
    }

    private void onStartMultiplayerPressed()
    {
		AdminPacket adminPacket = new AdminPacket();
		adminPacket.Command = "startgame";
		adminPacket.Sender = NetworkUtils.GetSelfSteamID();
		Global.networkInterface.BroadcastMessage(adminPacket, (ushort)NetworkUtils.NetworkingLanes.LANE_ADMIN);
    }

    private void onChatMessage(ChatPacket chatPacket)
    {
		Label msg = new Label();
		msg.Text = SteamFriends.GetFriendPersonaName((CSteamID)chatPacket.Sender) + " says: " + chatPacket.Text;
		GetNode<VBoxContainer>("chat/chatoutput/chatoutput/chatoutput").AddChild(msg);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}

	public void onSingleplayerPressed()
	{

	}

	public void onJoinServerPressed()
	{
		ulong serverID = ulong.Parse(GetNode<TextEdit>("steamid").Text);
		Global.networkInterface.ConnectToRemoteServer(serverID);
	}

	public void onStartOnlineServerPressed()
	{
		Global.networkInterface.StartServer();
	}

	public void onInviteFriendPressed()
	{
		SteamFriends.ActivateGameOverlayInviteDialogConnectString(Global.networkInterface.serverID.ToString());
	}

	public void onSendPressed()
	{
		string text = GetNode<TextEdit>("chat/text").Text;
		GetNode<TextEdit>("chat/text").Text = "";
		Global.networkInterface.SendChatMessage(text);

    }
}
