
using Godot;

public partial class Vehicle: Node3D, IGameObject
{
    public ulong id { get => id; set => id = value; }
    public float netPriority { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
}

