
using Godot;

public partial class Player : CharacterBody3D, IGameObject
{
    public ulong id { get => id; set => id = value; }
    public ulong attachedClient { get => attachedClient; set => attachedClient = value; }
    public float netPriority { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
}

