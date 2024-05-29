using Godot;
using System;

public partial class PeersUI : PanelContainer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Global.networkInterface.PlayerJoinEvent += onPlayerJoinEvent;
        Global.networkInterface.PeersUpdatedEvent += onPeersUpdatedEvent;
        Global.networkInterface.JoinedServerEvent += onJoinedServerEvent;
	}

    private void onJoinedServerEvent(ulong serverID)
    {
        GetNode<Label>("players/player1/player1/player1label").Text = Global.steamName;
    }

    private void onPeersUpdatedEvent(ulong[] peers)
    {
        throw new NotImplementedException();
    }

    private void onPlayerJoinEvent(ulong steamID)
    {
        throw new NotImplementedException();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}

    public override void _Draw()
    {
        
    }
}
