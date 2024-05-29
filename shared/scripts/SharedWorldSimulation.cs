using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using YAYADR.shared.scripts;

public partial class SharedWorldSimulation: Node3D
{
    public ulong worldTick = 0;
    public int nObjectsToSendPerFrame = 100;
    public Dictionary<ulong,float> gameObjectsPriority = new Dictionary<ulong, float>();
    public Dictionary<ulong, IGameObject> gameObjects = new Dictionary<ulong, IGameObject>();
    List<Player> players = new List<Player>();
    List<NPC> npcs = new List<NPC>();
    List<Vehicle> vehicles = new List<Vehicle>();
    List<LootItem> lootItems = new List<LootItem>();
    List<IInteractable> interactables = new List<IInteractable>();
    List<IPhysicsObject> physicsObjects = new List<IPhysicsObject>();
    List<IProjectile> projectiles = new List<IProjectile>();
    List<Node3D> nodes = new List<Node3D>();

    public override void _Ready()
    {
      
    }

    public override void _Process(double delta)
    {
        
    }

    public override void _PhysicsProcess(double delta)
    {
        worldTick++;

        IncrementObjectNetworkingPriority();

        SendSyncUpdatesForPriorityObjects(nObjectsToSendPerFrame);

    }

    private void SendSyncUpdatesForPriorityObjects(int nObjects)
    {
        var ordered = gameObjectsPriority.OrderBy(x => x.Value).ToArray();
        for (int i = 0; i < nObjects; i++)
        {
            ulong objID = ordered[i].Key;
            IGameObject obj = gameObjects[objID];
            gameObjectsPriority[objID] = 0f;
            //so something with obj
        }
    }

    private void IncrementObjectNetworkingPriority()
    {
        foreach (ulong objID in gameObjectsPriority.Keys)
        {
            gameObjectsPriority[objID] += gameObjects[objID].netPriority;
        }
    }
}